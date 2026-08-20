using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Alife.Framework;
using Alife.Function.FunctionCaller;
using Alife.Function.QChat;
using Microsoft.Extensions.Logging;

namespace AinaLife.QQEnhance;

public class QQEnhanceConfig
{
    [DisplayName("贴表情")]
    [Description("启用给QQ消息贴表情功能")]
    public bool EmojiReactEnabled { get; set; } = true;

    [DisplayName("点赞")]
    [Description("启用给QQ用户资料卡点赞功能")]
    public bool SendLikesEnabled { get; set; } = false;

    [DisplayName("撤回")]
    [Description("启用撤回QQ消息功能")]
    public bool DeleteMsgEnabled { get; set; } = true;

    [DisplayName("禁言")]
    [Description("启用禁言QQ群成员功能")]
    public bool GroupBanEnabled { get; set; } = true;

    [DisplayName("音乐卡片")]
    [Description("启用发送音乐卡片功能")]
    public bool MusicCardEnabled { get; set; } = false;

    [DisplayName("感知群禁言")]
    [Description("感知自己被禁言/解除禁言并通知AI")]
    public bool PerceiveGroupBan { get; set; } = true;

    [DisplayName("感知成员进群")]
    [Description("感知新成员进群并通知AI")]
    public bool PerceiveGroupIncrease { get; set; } = true;

    [DisplayName("输入中状态")]
    [Description("私聊时发送输入中状态")]
    public bool TypingIndicatorEnabled { get; set; } = true;

    [DisplayName("输入中延迟(秒)")]
    [Description("收到消息后延迟多久开始发送输入中状态")]
    public double TypingDelaySeconds { get; set; } = 2.0;

    [DisplayName("输入中间隔(秒)")]
    [Description("输入中状态刷新间隔")]
    public double TypingIntervalSeconds { get; set; } = 2.0;

    [DisplayName("输入中最大时长(秒)")]
    [Description("输入中状态最大持续时长")]
    public double TypingMaxSeconds { get; set; } = 60.0;

    [DisplayName("实时消息捕获")]
    [Description("独立WS监听实时事件捕获真实消息ID（撤回/贴表情可靠性核心，比历史接口更稳）")]
    public bool LiveCaptureEnabled { get; set; } = true;

    [DisplayName("实时消息缓存大小")]
    [Description("缓存最近N条实时消息的消息ID/内容，用于qgetmessages优先查询")]
    public int LiveCacheSize { get; set; } = 200;
}

[Module("QQ增强",
    "提供QQ贴表情、点赞、撤回、禁言、音乐卡片、感知通知、输入中状态等增强功能",
    defaultCategory: "AinaLife/社交平台")]
public class QQEnhanceModule(
    XmlFunctionCaller functionCaller,
    ILogger<QQEnhanceModule> logger,
    Interactor<QQEnhanceModule> interactor,
    QChatService qChatService) :
    ChatBehaviour,
    IConfigurable<QQEnhanceConfig>
{
    public QQEnhanceConfig Configuration { get; set; } = null!;

    // QChatService 未公开 OneBotClient，通过反射获取（不修改官方代码）
    private OneBotClient? GetClient()
    {
        FieldInfo? field = typeof(QChatService).GetField("oneBotClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(qChatService) as OneBotClient;
    }

    // Typing indicator 状态管理
    private readonly Dictionary<long, CancellationTokenSource> _typingCts = new();
    private readonly object _typingLock = new();

    // ==================== 实时消息捕获（真实消息ID来源） ====================
    private sealed class LiveMessage
    {
        public long MessageId { get; init; }
        public long UserId { get; init; }
        public long GroupId { get; init; }
        public string Nickname { get; init; } = "";
        public string Raw { get; init; } = "";
        public long Time { get; init; }
        public bool IsSelf { get; init; }
    }

    private ClientWebSocket? _liveWs;
    private CancellationTokenSource? _liveCts;
    private long _liveBotId;
    private readonly ConcurrentQueue<LiveMessage> _liveMessages = new();

    private async Task StartLiveCaptureAsync(OneBotClient client)
    {
        try
        {
            string url = client.Url;
            string token = client.Token;
            if (string.IsNullOrEmpty(url)) return;

            var ws = new ClientWebSocket();
            if (!string.IsNullOrEmpty(token))
                ws.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            await ws.ConnectAsync(new Uri(url), CancellationToken.None);

            var cts = new CancellationTokenSource();
            _liveWs = ws;
            _liveCts = cts;
            _ = LiveReceiveLoopAsync(ws, cts.Token);
            logger.LogInformation("QQ增强：实时消息捕获已连接 {Url}", url);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "QQ增强：实时消息捕获连接失败（不影响其他功能，qgetmessages将回退历史接口）");
        }
    }

    private async Task LiveReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        try
        {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(ms.ToArray()));
                JsonElement root = doc.RootElement;

                // 握手：第一个报文是 lifecycle connect
                if (root.TryGetProperty("post_type", out var pt))
                {
                    string postType = pt.GetString() ?? "";
                    if (postType == "meta_event" &&
                        root.TryGetProperty("meta_event_type", out var met) &&
                        met.GetString() == "lifecycle")
                    {
                        if (root.TryGetProperty("self_id", out var self)) _liveBotId = self.GetInt64();
                        continue;
                    }

                    if (postType != "message" && postType != "message_sent") continue;

                    if (!root.TryGetProperty("message_id", out var midElem)) continue;
                    long msgId = midElem.ValueKind == JsonValueKind.Number ? midElem.GetInt64()
                        : (midElem.ValueKind == JsonValueKind.String && long.TryParse(midElem.GetString(), out var p) ? p : 0);
                    if (msgId == 0) continue;

                    bool isSelf = postType == "message_sent" ||
                        (root.TryGetProperty("user_id", out var uidElem) && uidElem.ValueKind == JsonValueKind.Number && uidElem.GetInt64() == _liveBotId);

                    long userId = root.TryGetProperty("user_id", out var ue) && ue.ValueKind == JsonValueKind.Number ? ue.GetInt64() : 0;
                    long groupId = root.TryGetProperty("group_id", out var ge) && ge.ValueKind == JsonValueKind.Number ? ge.GetInt64() : 0;
                    long time = root.TryGetProperty("time", out var te) && te.ValueKind == JsonValueKind.Number ? te.GetInt64() : 0;
                    string nickname = "";
                    if (root.TryGetProperty("sender", out var se) && se.ValueKind == JsonValueKind.Object &&
                        se.TryGetProperty("nickname", out var ne) && ne.ValueKind == JsonValueKind.String)
                        nickname = ne.GetString() ?? "";
                    string raw = root.TryGetProperty("raw_message", out var re) && re.ValueKind == JsonValueKind.String
                        ? re.GetString() ?? ""
                        : (root.TryGetProperty("message", out var me) && me.ValueKind == JsonValueKind.String ? me.GetString() ?? "" : "");

                    _liveMessages.Enqueue(new LiveMessage { MessageId = msgId, UserId = userId, GroupId = groupId, Nickname = nickname, Raw = raw, Time = time, IsSelf = isSelf });
                    int max = Math.Max(1, Configuration.LiveCacheSize);
                    while (_liveMessages.Count > max)
                        _liveMessages.TryDequeue(out _);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "QQ增强：实时消息捕获循环结束");
        }
        finally
        {
            try { ws.Dispose(); } catch { }
        }
    }

    private void StopLiveCapture()
    {
        try { _liveCts?.Cancel(); } catch { }
        try { _liveCts?.Dispose(); } catch { }
        _liveCts = null;
        try { _liveWs?.Dispose(); } catch { }
        _liveWs = null;
    }

    protected override Task OnAwake()
    {
        XmlHandler xmlHandler = new(this) {
            Description = "提供QQ贴表情、点赞、撤回、禁言、音乐卡片、消息ID查询等增强功能。⚠重要：QQ消息ID为负数，撤回/贴表情/引用回复必须先用qgetmessages获取真实消息ID，严禁编造",
            Explanation = """
                使用规则：
                - QQ平台消息ID通常是负数（如 -1976879391），编造或猜ID必然失败（RetCode 100/1400）。
                - 要撤回(deleteMsg)/贴表情(setemoji)/引用回复(CQ:reply)某条消息，必须先调用 qgetmessages 获取真实ID列表，从中选取。
                - qgetmessages 优先使用实时捕获的真实消息ID（含自己刚发的消息），缓存不足时自动回退历史接口。
                - qgetmessages 返回格式：[消息ID:xxx] 即该消息的真实ID，直接原样使用。
                - 引用回复消息格式：[CQ:reply,id=真实ID]文本，id必须来自qgetmessages。
                """
        };
        functionCaller.RegisterHandler(xmlHandler, DocumentMode.Implicit, DestroyCancellationToken);

        OneBotClient? client = GetClient();
        if (client == null)
        {
            logger.LogWarning("无法获取 OneBotClient，QQ增强功能不可用（请确认已启用QQ聊天模块）");
            return Task.CompletedTask;
        }

        if (Configuration.PerceiveGroupBan || Configuration.PerceiveGroupIncrease)
            client.EventReceived += OnEventReceived;

        if (Configuration.TypingIndicatorEnabled)
        {
            ChatBot.ChatSent += OnChatSent;
            ChatBot.ChatOver += OnChatOver;
        }

        if (Configuration.LiveCaptureEnabled)
            _ = StartLiveCaptureAsync(client);

        return Task.CompletedTask;
    }

    protected override Task OnDestroy()
    {
        OneBotClient? client = GetClient();
        if (client != null)
            client.EventReceived -= OnEventReceived;

        ChatBot.ChatSent -= OnChatSent;
        ChatBot.ChatOver -= OnChatOver;

        StopLiveCapture();

        lock (_typingLock)
        {
            foreach (var cts in _typingCts.Values)
                cts.Cancel();
            _typingCts.Clear();
        }

        return Task.CompletedTask;
    }

    // ==================== 工具函数 ====================

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给QQ消息贴表情。emoji_id为表情ID。⚠：messageId必须用qgetmessages取真实ID（可为负数），严禁编造或猜测")]
    public async Task SetEmoji(
        [Description("消息ID（必须来自qgetmessages）")] long messageId,
        [Description("表情ID，201为点赞")] int emojiId)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("贴表情失败：QQ客户端不可用"); return; }
        try
        {
            await client.CallActionAsync<object>("set_msg_emoji_like", new { message_id = messageId, emoji_id = emojiId });
            interactor.Poke("贴表情成功");
        }
        catch (Exception e)
        {
            interactor.Poke($"贴表情失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给QQ用户资料卡点赞")]
    public async Task SendQQLikes(
        [Description("QQ号")] long qq,
        [Description("点赞次数，默认50次")] int times = 50)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("点赞失败：QQ客户端不可用"); return; }
        try
        {
            var chunks = new List<int>();
            for (int i = 0; i < times / 10; i++) chunks.Add(10);
            if (times % 10 > 0) chunks.Add(times % 10);

            int count = 0;
            foreach (int chunk in chunks)
            {
                await client.CallActionAsync<object>("send_like", new { user_id = qq, times = chunk });
                count += chunk;
                await Task.Delay(100);
            }
            interactor.Poke($"点赞成功，点了 {count} 个赞");
        }
        catch (Exception e)
        {
            interactor.Poke($"点赞失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("撤回QQ消息。⚠：messageId必须用qgetmessages的获取真实ID，严禁编造")]
    public async Task DeleteMsg(
        [Description("消息ID（必须来自qgetmessages）")] long messageId)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("撤回失败：QQ客户端不可用"); return; }
        try
        {
            await client.CallActionAsync<object>("delete_msg", new { message_id = messageId });
            interactor.Poke("撤回成功");
        }
        catch (Exception e)
        {
            interactor.Poke($"撤回失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("禁言QQ群成员。群号可从群消息标签[群聊消息(群号,群名)]中获取")]
    public async Task GroupBan(
        [Description("群号")] long groupId,
        [Description("QQ号")] long userId,
        [Description("禁言时长(秒)，默认600秒，0为解除禁言")] int duration = 600)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("禁言失败：QQ客户端不可用"); return; }
        try
        {
            await client.CallActionAsync<object>("set_group_ban", new { group_id = groupId, user_id = userId, duration });
            interactor.Poke("禁言成功");
        }
        catch (Exception e)
        {
            interactor.Poke($"禁言失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("发送音乐卡片到QQ聊天")]
    public async Task SendMusicCard(
        [Description("目标QQ号(私聊)或群号(群聊)")] long targetId,
        [Description("消息类型：private或group")] string type,
        [Description("音乐平台(qq/163/kugou/migu/kuwo)")] string platform,
        [Description("音乐ID")] string musicId)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("音乐卡片发送失败：QQ客户端不可用"); return; }
        try
        {
            string message = $"[CQ:music,type={platform},id={musicId}]";
            if (type == "group")
                await client.SendGroupMessage(targetId, message);
            else
                await client.SendPrivateMessage(targetId, message);
            interactor.Poke("音乐卡片发送成功");
        }
        catch (Exception e)
        {
            interactor.Poke($"音乐卡片发送失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("获取群聊/私聊最近消息及每条消息的消息ID，用于定位要撤回(DeleteMsg)或贴表情(SetEmoji)的消息。群聊传groupId；私聊传groupId=0并传userId。优先使用实时捕获的真实消息ID缓存，缓存不足时自动回退历史接口")]
    public async Task QGetMessages(
        [Description("群号（私聊时传0）")] long groupId,
        [Description("QQ号（仅私聊时需要）")] long userId = 0,
        [Description("获取条数，1-50，默认10")] int count = 10)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("获取消息失败：QQ客户端不可用"); return; }
        try
        {
            count = Math.Clamp(count, 1, 50);
            var lines = new List<string>();

            // 优先从实时捕获缓存取（消息ID 100%真实，含自己刚发的消息）
            var cacheMatches = _liveMessages
                .Where(m => groupId != 0 ? m.GroupId == groupId : (m.GroupId == 0 && (userId == 0 || m.UserId == userId)))
                .OrderByDescending(m => m.Time)
                .Take(count)
                .ToList();

            string cacheSource = "";
            if (cacheMatches.Count > 0)
            {
                cacheSource = "（实时捕获）";
                foreach (var m in cacheMatches)
                {
                    string nick = string.IsNullOrEmpty(m.Nickname) ? (m.IsSelf ? "我" : m.UserId.ToString()) : m.Nickname;
                    string raw = OneBotSegment.FilterFace(OneBotSegment.FilterAt(OneBotSegment.FilterImage(OneBotSegment.FilterRecord(m.Raw))));
                    DateTime time = DateTimeOffset.FromUnixTimeSeconds(m.Time).LocalDateTime;
                    lines.Add($"[{time:HH:mm:ss}] {m.UserId}({nick}) [消息ID:{m.MessageId}] {raw}");
                }
            }

            // 实时缓存未命中足够条数时，回退/补充历史接口
            if (lines.Count < count && client != null)
            {
                int historyNeed = count - lines.Count;
                string action = groupId != 0 ? "get_group_msg_history" : "get_friend_msg_history";
                object prms = groupId != 0
                    ? new { group_id = groupId, count = historyNeed }
                    : new { user_id = userId, count = historyNeed };

                try
                {
                    JsonElement? data = await client.CallActionAsync<JsonElement>(action, prms);
                    if (data != null && data.Value.ValueKind == JsonValueKind.Array && data.Value.GetArrayLength() > 0)
                    {
                        if (lines.Count > 0) cacheSource = "（实时捕获+历史补充）";
                        else cacheSource = "（历史接口）";
                        foreach (JsonElement msg in data.Value.EnumerateArray())
                        {
                            long mid = 0;
                            if (msg.TryGetProperty("message_id", out var m))
                            {
                                if (m.ValueKind == JsonValueKind.Number) mid = m.GetInt64();
                                else if (m.ValueKind == JsonValueKind.String && long.TryParse(m.GetString(), out long parsed)) mid = parsed;
                            }
                            long uid = msg.TryGetProperty("user_id", out var u) && u.ValueKind == JsonValueKind.Number ? u.GetInt64() : 0;
                            string nick = "";
                            if (msg.TryGetProperty("sender", out var s) && s.ValueKind == JsonValueKind.Object &&
                                s.TryGetProperty("nickname", out var n) && n.ValueKind == JsonValueKind.String)
                                nick = n.GetString() ?? "";
                            string raw = "";
                            if (msg.TryGetProperty("raw_message", out var r) && r.ValueKind == JsonValueKind.String)
                                raw = r.GetString() ?? "";
                            else if (msg.TryGetProperty("message", out var m2) && m2.ValueKind == JsonValueKind.String)
                                raw = m2.GetString() ?? "";
                            long t = msg.TryGetProperty("time", out var tm) && tm.ValueKind == JsonValueKind.Number ? tm.GetInt64() : 0;
                            DateTime time = DateTimeOffset.FromUnixTimeSeconds(t).LocalDateTime;
                            raw = OneBotSegment.FilterFace(OneBotSegment.FilterAt(OneBotSegment.FilterImage(OneBotSegment.FilterRecord(raw))));
                            lines.Add($"[{time:HH:mm:ss}] {uid}({nick}) [消息ID:{mid}] {raw}");
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogDebug(e, "QQ增强：历史接口获取失败（已尝试实时缓存）");
                }
            }

            if (lines.Count == 0)
            {
                interactor.Poke(groupId != 0
                    ? $"群 {groupId} 没有消息记录（实时捕获未启动或历史接口不支持，请确认已启用QQ聊天模块且OneBot支持get_group_msg_history）"
                    : $"与 {userId} 没有消息记录（实时捕获未启动或历史接口不支持）");
                return;
            }

            StringBuilder sb = new();
            string target = groupId != 0 ? $"群 {groupId}" : $"与 {userId}";
            sb.AppendLine($"{target} 最近 {lines.Count} 条消息{cacheSource}（[消息ID:xxx]即真实ID，可能是负数，直接用于撤回/贴表情/引用回复）：");
            foreach (string line in lines)
                sb.AppendLine(line);
            interactor.Poke(sb.ToString());
        }
        catch (Exception e)
        {
            interactor.Poke($"获取消息失败：{e.Message}");
        }
    }

    // ==================== notice感知 ====================

    private sealed class GroupInfoData
    {
        [JsonPropertyName("group_id")]
        public long GroupId { get; init; }

        [JsonPropertyName("group_name")]
        public string? GroupName { get; init; }
    }

    private async void OnEventReceived(OneBotBaseEvent oneBotEvent)
    {
        try
        {
            if (oneBotEvent is not OneBotNoticeEvent noticeEvent)
                return;

            string? noticeType = noticeEvent.NoticeType;
            if (noticeType == "group_ban" && Configuration.PerceiveGroupBan)
            {
                if (noticeEvent.SelfId == noticeEvent.UserId)
                {
                    string subType = noticeEvent.SubType ?? "";
                    string groupInfo = await GetGroupInfoText(noticeEvent.GroupId);
                    if (subType == "ban")
                        interactor.Poke($"[System 你被禁言了（{groupInfo}）]");
                    else if (subType == "lift_ban")
                        interactor.Poke($"[System 你被解除禁言了（{groupInfo}）]");
                }
            }
            else if (noticeType == "group_increase" && Configuration.PerceiveGroupIncrease)
            {
                long userId = noticeEvent.UserId;
                string userName = await GetQQUserName(userId, noticeEvent.GroupId);
                string userText = string.IsNullOrEmpty(userName)
                    ? $"用户{userId}"
                    : $"用户{userId}({userName})";
                string groupInfo = await GetGroupInfoText(noticeEvent.GroupId);
                interactor.Poke($"[System {userText}加入了群聊（{groupInfo}）]");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "感知notice事件失败");
        }
    }

    private async Task<string> GetGroupInfoText(long groupId)
    {
        if (groupId == 0) return "群号:未知";
        string name = await GetGroupNameAsync(groupId);
        return string.IsNullOrEmpty(name) ? $"群号:{groupId}" : $"群号:{groupId} 群名:{name}";
    }

    private async Task<string> GetGroupNameAsync(long groupId)
    {
        // 优先从 QChatService 的群状态缓存拿群名（群里有消息活动时已有）
        if (qChatService.GroupStates.TryGetValue(groupId, out var state) && !string.IsNullOrEmpty(state.Name))
            return state.Name!;

        OneBotClient? client = GetClient();
        if (client == null) return "";
        try
        {
            var info = await client.CallActionAsync<GroupInfoData>("get_group_info", new { group_id = groupId, no_cache = false });
            return info?.GroupName ?? "";
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "获取群名失败: {GroupId}", groupId);
            return "";
        }
    }

    private async Task<string> GetQQUserName(long userId, long groupId = 0)
    {
        OneBotClient? client = GetClient();
        if (client == null) return "";
        try
        {
            if (groupId != 0)
            {
                var sender = await client.CallActionAsync<OneBotSender>(
                    "get_group_member_info",
                    new { group_id = groupId, user_id = userId, no_cache = false });
                if (sender != null)
                {
                    if (!string.IsNullOrEmpty(sender.Card)) return sender.Card;
                    if (!string.IsNullOrEmpty(sender.Nickname)) return sender.Nickname;
                }
            }

            var stranger = await client.CallActionAsync<OneBotSender>(
                "get_stranger_info",
                new { user_id = userId });
            return stranger?.Nickname ?? "";
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "获取QQ用户名失败: {UserId}", userId);
            return "";
        }
    }

    // ==================== Typing Indicator ====================

    private void OnChatSent(string message)
    {
        var match = Regex.Match(message, @"\[私聊消息\((\d+)");
        if (!match.Success) return;

        long userId = long.Parse(match.Groups[1].Value);
        StartTyping(userId);
    }

    private void OnChatOver()
    {
        StopAllTyping();
    }

    private void StartTyping(long userId)
    {
        lock (_typingLock)
        {
            if (_typingCts.TryGetValue(userId, out var existing))
            {
                existing.Cancel();
                _typingCts.Remove(userId);
            }

            var cts = new CancellationTokenSource();
            _typingCts[userId] = cts;
            _ = RunTypingLoopAsync(userId, cts.Token);
        }
    }

    private void StopAllTyping()
    {
        lock (_typingLock)
        {
            foreach (var cts in _typingCts.Values)
                cts.Cancel();
            _typingCts.Clear();
        }
    }

    private async Task RunTypingLoopAsync(long userId, CancellationToken ct)
    {
        OneBotClient? client = GetClient();
        if (client == null) return;

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Configuration.TypingDelaySeconds), ct);

            var startTime = DateTime.Now;
            while (!ct.IsCancellationRequested)
            {
                await client.CallActionAsync<object>("set_input_status", new { user_id = userId, event_type = 1 });

                if ((DateTime.Now - startTime).TotalSeconds >= Configuration.TypingMaxSeconds)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(Configuration.TypingIntervalSeconds), ct);
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception e)
        {
            logger.LogDebug(e, "Typing indicator 发送失败");
        }
        finally
        {
            lock (_typingLock)
            {
                _typingCts.Remove(userId);
            }
        }
    }
}
