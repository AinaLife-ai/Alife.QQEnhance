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

    [DisplayName("戳一戳")]
    [Description("启用群聊戳一戳成员功能")]
    public bool PokeEnabled { get; set; } = true;

    [DisplayName("引用回复")]
    [Description("启用引用回复消息功能")]
    public bool ReplyEnabled { get; set; } = true;

    [DisplayName("合并转发")]
    [Description("启用合并转发消息功能（转发已有/构造新转发）")]
    public bool ForwardEnabled { get; set; } = true;

    [DisplayName("兼容模式")]
    [Description("与YuYang.QQTools（幼央工具箱）的协作模式：Auto=检测到幼央自动让位重叠功能；PreferQQEnhance=优先本插件；PreferYuYang=重叠功能一律让位（幼央未装时自动回退自持）；Off=不检测全功能注册")]
    public string CompatibilityMode { get; set; } = "Auto";

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
    [Description("独立WS监听实时事件捕获真实消息ID（撤回/贴表情可靠性核心）")]
    public bool LiveCaptureEnabled { get; set; } = true;

    [DisplayName("实时消息缓存大小")]
    [Description("缓存最近N条实时消息的消息ID/内容，用于qgetmessages查询")]
    public int LiveCacheSize { get; set; } = 200;
}

[Module("QQ增强",
    "提供QQ贴表情、点赞、撤回、禁言、戳一戳、引用回复、合并转发、音乐卡片、感知通知、输入中状态等增强功能，支持与YuYang.QQTools自动分工",
    defaultCategory: "AinaLife/社交平台")]
public class QQEnhanceModule(
    XmlFunctionCaller functionCaller,
    ILogger<QQEnhanceModule> logger,
    Interactor<QQEnhanceModule> interactor,
    QChatService qChatService,
    ModuleSystem moduleSystem) :
    ChatBehaviour,
    IConfigurable<QQEnhanceConfig>
{
    public QQEnhanceConfig Configuration { get; set; } = null!;

    /// <summary>幼央工具箱模块的完整类型名（ModuleSystem.GetModuleID = FullName）</summary>
    private const string YuYangModuleId = "YuYang.QQTools.QQToolsModule";

    // QChatService 未公开 OneBotClient，通过反射获取（不修改官方代码）
    private OneBotClient? GetClient()
    {
        FieldInfo? field = typeof(QChatService).GetField("oneBotClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(qChatService) as OneBotClient;
    }

    // ==================== 幼央兼容检测 ====================

    /// <summary>幼央工具箱是否已加载且被当前角色启用（同时兼容旧类名 YuYang.QQTools.QQToolsModule）</summary>
    private bool IsYuYangActive()
    {
        try
        {
            if (moduleSystem.GetModule(YuYangModuleId) == null &&
                moduleSystem.GetModule("YuYang.QQTools.QQToolsModule") == null) return false;
            return Character.Modules.Contains(YuYangModuleId) ||
                   Character.Modules.Contains("YuYang.QQTools.QQToolsModule");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>重叠功能是否应让位给幼央（运行时实时判断，幼央热装卸载后自动恢复）</summary>
    private bool ShouldDelegate()
    {
        return Configuration.CompatibilityMode switch
        {
            "PreferQQEnhance" => false,
            "PreferYuYang" => IsYuYangActive(),
            "Off" => false,
            _ => IsYuYangActive() // Auto
        };
    }

    private string DelegateHint(string feature, string yuYangFunction)
    {
        return $"{feature}功能由 YuYang.QQTools（幼央工具箱）接管，请调用幼央的 {yuYangFunction} 函数";
    }

    // ==================== 统一 OneBot 调用（超时/失败友好提示） ====================

    /// <summary>
    /// 统一执行 OneBot Action，捕获超时（OneBotClient 内部 10s 超时）与异常。
    /// 返回 null 表示成功；返回字符串为失败原因描述（含超时提示，避免 AI 重复操作）。
    /// </summary>
    private async Task<string?> CallActionSafeAsync(
        string action,
        object? @params,
        string feature,
        OneBotClient? client)
    {
        if (client == null)
            return $"{feature}失败：QQ客户端不可用";
        try
        {
            await client.CallActionAsync<object>(action, @params);
            return null; // 成功
        }
        catch (TaskCanceledException)
        {
            // OneBotClient 内部 10s 超时：操作可能已在服务器生效，提示不要重复执行
            return $"{feature}请求超时（10秒未收到响应）。操作可能已生效，请稍后用 qgetmessages 检查确认，不要重复操作";
        }
        catch (Exception e)
        {
            return $"{feature}失败：{e.Message}";
        }
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
        public long Seq { get; init; }
    }

    private ClientWebSocket? _liveWs;
    private CancellationTokenSource? _liveCts;
    private long _liveBotId;
    private long _liveSeq;
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
            logger.LogWarning(e, "QQ增强：实时消息捕获连接失败（qgetmessages将不可用，其他功能不受影响）");
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

                    // raw_message 优先；缺失时 message 为字符串直接取，为数组时拼接 text/at 段
                    string raw = "";
                    if (root.TryGetProperty("raw_message", out var re) && re.ValueKind == JsonValueKind.String)
                    {
                        raw = re.GetString() ?? "";
                    }
                    else if (root.TryGetProperty("message", out var me))
                    {
                        if (me.ValueKind == JsonValueKind.String)
                            raw = me.GetString() ?? "";
                        else if (me.ValueKind == JsonValueKind.Array)
                        {
                            var parts = new List<string>();
                            foreach (JsonElement seg in me.EnumerateArray())
                            {
                                if (seg.ValueKind != JsonValueKind.Object) continue;
                                if (!seg.TryGetProperty("type", out var segType)) continue;
                                string type = segType.GetString() ?? "";
                                if (!seg.TryGetProperty("data", out var segData) || segData.ValueKind != JsonValueKind.Object)
                                    continue;
                                if (type == "text" && segData.TryGetProperty("text", out var txt) && txt.ValueKind == JsonValueKind.String)
                                    parts.Add(txt.GetString() ?? "");
                                else if (type == "at" && segData.TryGetProperty("qq", out var qq) && qq.ValueKind == JsonValueKind.String)
                                    parts.Add($"[CQ:at,qq={qq.GetString()}]");
                            }
                            raw = string.Join("", parts);
                        }
                    }

                    _liveMessages.Enqueue(new LiveMessage {
                        MessageId = msgId, UserId = userId, GroupId = groupId, Nickname = nickname,
                        Raw = raw, Time = time, IsSelf = isSelf, Seq = Interlocked.Increment(ref _liveSeq)
                    });
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
        bool yuYangActive = IsYuYangActive();
        if (yuYangActive)
            logger.LogInformation("QQ增强：检测到 YuYang.QQTools 已启用，重叠功能将让位（兼容模式 {Mode}）", Configuration.CompatibilityMode);

        string explanation = yuYangActive
            ? $$"""
                使用规则：
                - 已检测到 YuYang.QQTools（幼央工具箱）接管：戳一戳、引用回复、点赞、贴表情、撤回、输入中 请调用幼央的函数（PokeGroupMember/SendReplyMessage/SendLike/SendEmojiLike/DeleteMessage）。
                - 本插件负责：禁言(GroupBan)、音乐卡片(SendMusicCard)、合并转发(SendForwardById/SendForwardNew)、消息ID查询(GetMessages)、感知通知。
                - QQ平台消息ID通常是负数（如 -1976879391），编造或猜ID必然失败（RetCode 100/1400）。
                - 要撤回/贴表情/引用回复某条消息，必须先调用 getmessages 获取真实ID列表，从中选取。
                - getmessages 仅返回实时捕获的真实消息ID（含自己刚发的消息），不使用历史接口。
                - getmessages 返回格式：[消息ID:xxx] 即该消息的真实ID，直接原样使用。
                """
            : """
                使用规则：
                - QQ平台消息ID通常是负数（如 -1976879391），编造或猜ID必然失败（RetCode 100/1400）。
                - 要撤回(deleteMsg)/贴表情(setemoji)/引用回复(sendreplymessage)/转发合并转发(sendforwardbyid)某条消息，必须先调用 qgetmessages 获取真实ID列表，从中选取。
                - qgetmessages 仅返回实时捕获的真实消息ID（含自己刚发的消息），不使用历史接口（历史接口ID语义不可靠，会误导操作）。
                - qgetmessages 返回格式：[消息ID:xxx] 即该消息的真实ID，直接原样使用。
                - 引用回复消息格式：sendreplymessage message="内容" replytoid="真实ID" messagetype="group" targetid="群号或QQ号"。
                - 合并转发：sendforwardbyid 转发已有合并转发（id来自qgetmessages）；sendforwardnew 构造新合并转发（传JSON数组，节点格式 [{"name":"昵称","uin":QQ号,"content":"内容"},{"id":真实消息ID}]，必须完整闭合括号）。
                - 戳一戳：pokegroupmember 群聊戳成员；pokeprivatemember 私聊戳用户（QQ号）。
                - 音乐卡片：sendmusiccard（QQ平台音乐卡片请求可能较慢，若提示超时请稍后用qgetmessages确认是否发出，不要重复发送）。
                """;

        XmlHandler xmlHandler = new(this) {
            Description = "提供QQ贴表情、点赞、撤回、禁言、戳一戳、引用回复、合并转发、音乐卡片、消息ID查询等增强功能。⚠重要：QQ消息ID为负数，撤回/贴表情/引用回复/转发必须先用getmessages获取真实消息ID，严禁编造",
            Explanation = explanation
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

        // 输入中状态：幼央接管时自动让位（避免双插件同时发 set_input_status）
        if (Configuration.TypingIndicatorEnabled && !ShouldDelegate())
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
    [Description("给QQ消息贴表情。emoji_id为表情ID。⚠：messageId必须用getmessages取真实ID（可为负数），严禁编造或猜测")]
    public async Task SetEmoji(
        [Description("消息ID（必须来自getmessages）")] long messageId,
        [Description("表情ID，201为点赞")] int emojiId)
    {
        if (!Configuration.EmojiReactEnabled) { interactor.Poke("贴表情功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("贴表情", "SendEmojiLike")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("set_msg_emoji_like", new { message_id = messageId, emoji_id = emojiId }, "贴表情", client);
        interactor.Poke(err ?? "贴表情成功");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给QQ用户资料卡点赞")]
    public async Task SendQQLikes(
        [Description("QQ号")] long qq,
        [Description("点赞次数，默认50次")] int times = 50)
    {
        if (!Configuration.SendLikesEnabled) { interactor.Poke("点赞功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("点赞", "SendLike")); return; }
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
                string? err = await CallActionSafeAsync("send_like", new { user_id = qq, times = chunk }, "点赞", client);
                if (err != null)
                {
                    interactor.Poke($"{err}（已成功 {count} 个赞）");
                    return;
                }
                count += chunk;
            }
            interactor.Poke($"点赞成功，点了 {count} 个赞");
        }
        catch (Exception e)
        {
            interactor.Poke($"点赞失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("撤回QQ消息。⚠：messageId必须用getmessages取真实ID，严禁编造")]
    public async Task DeleteMsg(
        [Description("消息ID（必须来自getmessages）")] long messageId)
    {
        if (!Configuration.DeleteMsgEnabled) { interactor.Poke("撤回功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("撤回", "DeleteMessage")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("delete_msg", new { message_id = messageId }, "撤回", client);
        interactor.Poke(err ?? "撤回成功");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("禁言QQ群成员。群号可从群消息标签[群聊消息(群号,群名)]中获取")]
    public async Task GroupBan(
        [Description("群号")] long groupId,
        [Description("QQ号")] long userId,
        [Description("禁言时长(秒)，默认600秒，0为解除禁言")] int duration = 600)
    {
        if (!Configuration.GroupBanEnabled) { interactor.Poke("禁言功能已禁用"); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("set_group_ban", new { group_id = groupId, user_id = userId, duration }, "禁言", client);
        interactor.Poke(err ?? "禁言成功");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("在群聊中戳一戳指定成员")]
    public async Task PokeGroupMember(
        [Description("群号")] long groupId,
        [Description("QQ号")] long userId)
    {
        if (!Configuration.PokeEnabled) { interactor.Poke("戳一戳功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("戳一戳", "PokeGroupMember")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("group_poke", new { group_id = groupId, user_id = userId }, "戳一戳", client);
        interactor.Poke(err ?? $"成功戳了戳 {userId}");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("在私聊中戳一戳指定用户（friend_poke，NapCat/LLOneBot 支持）")]
    public async Task PokePrivateMember(
        [Description("QQ号")] long userId)
    {
        if (!Configuration.PokeEnabled) { interactor.Poke("戳一戳功能已禁用"); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("friend_poke", new { user_id = userId }, "私聊戳一戳", client);
        interactor.Poke(err ?? $"成功私聊戳了戳 {userId}");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("引用回复消息。replyToId必须来自getmessages的真实ID（可为负数），严禁编造。messagetype为group时targetId传群号，private时传QQ号。先尝试结构化reply参数，失败自动回退CQ码")]
    public async Task SendReplyMessage(
        [Description("回复内容")] string message,
        [Description("被回复消息ID（必须来自getmessages）")] long replyToId,
        [Description("消息类型：group或private")] string messageType = "group",
        [Description("目标群号（group）或QQ号（private）")] long targetId = 0)
    {
        if (!Configuration.ReplyEnabled) { interactor.Poke("引用回复功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("引用回复", "SendReplyMessage")); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("引用回复失败：QQ客户端不可用"); return; }
        try
        {
            object @params;
            if (messageType == "private" && targetId != 0)
            {
                @params = new { message_type = "private", user_id = targetId, message, reply = new { message_id = replyToId } };
            }
            else
            {
                @params = new { message_type = "group", group_id = targetId, message, reply = new { message_id = replyToId } };
            }
            await client.CallActionAsync<object>("send_msg", @params);
            interactor.Poke("引用回复发送成功");
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("引用回复请求超时（10秒），可能已发送成功，请确认后不要再重复发送");
        }
        catch (Exception e)
        {
            // 结构化 reply 参数不被当前实现支持时，回退 CQ 码方式
            try
            {
                string fallback = $"[CQ:reply,id={replyToId}] {message}";
                if (messageType == "private" && targetId != 0)
                    await client.SendPrivateMessage(targetId, fallback);
                else
                    await client.SendGroupMessage(targetId, fallback);
                interactor.Poke($"引用回复发送成功（CQ码方式，结构化参数不可用：{e.Message}）");
            }
            catch (Exception e2)
            {
                interactor.Poke($"引用回复失败：结构化参数失败（{e.Message}），CQ码回退也失败（{e2.Message}）");
            }
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("转发一条已有的合并转发消息到群聊。forwardId为合并转发消息ID（必须来自getmessages，可为负数）")]
    public async Task SendForwardById(
        [Description("群号")] long groupId,
        [Description("合并转发消息ID（必须来自getmessages）")] long forwardId)
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("合并转发功能已禁用"); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("合并转发失败：QQ客户端不可用"); return; }
        try
        {
            string message = $"[CQ:forward,id={forwardId}]";
            await client.SendGroupMessage(groupId, message);
            interactor.Poke("合并转发发送成功");
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("合并转发请求超时（10秒），可能已发送成功，请稍后确认，不要重复发送");
        }
        catch (Exception e)
        {
            interactor.Poke($"合并转发失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("构造并发送新的合并转发消息到群聊。nodesJson为JSON数组，每个节点两种格式：{\"name\":\"昵称\",\"uin\":QQ号,\"content\":\"内容\"}（自定义内容）或 {\"id\":真实消息ID}（引用真实消息，id必须来自getmessages，数字或数字字符串均可）。⚠必须传完整合法的JSON数组，最外层用方括号[]包裹，不要漏收尾括号")]
    public async Task SendForwardNew(
        [Description("群号")] long groupId,
        [Description("节点JSON数组（必须是完整合法的JSON，[]闭合）")] string nodesJson)
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("合并转发功能已禁用"); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("合并转发失败：QQ客户端不可用"); return; }
        try
        {
            // 容错：去掉首尾多余空白；若因AI漏了收尾括号，尝试补全（最多补一层 ] 和 }）
            string json = nodesJson.Trim();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    throw new Exception("nodesJson必须是JSON数组");
            }
            catch (JsonException)
            {
                string? repaired = RepairJsonArray(json);
                if (repaired == null)
                    throw new JsonException("nodesJson不是合法JSON数组（检查是否漏了收尾括号或引号未闭合）。正确示例：[{\"name\":\"昵称\",\"uin\":123456,\"content\":\"内容\"},{\"id\":-1234567890}]");
                json = repaired;
            }

            using var doc2 = JsonDocument.Parse(json);
            if (doc2.RootElement.ValueKind != JsonValueKind.Array)
                throw new Exception("nodesJson必须是JSON数组");

            var nodes = new List<object>();
            foreach (JsonElement node in doc2.RootElement.EnumerateArray())
            {
                if (node.ValueKind != JsonValueKind.Object)
                    throw new Exception("每个节点必须是JSON对象，请检查是否漏了花括号");

                // id 引用：支持数字或数字字符串
                bool hasId = node.TryGetProperty("id", out var idElem) &&
                             (idElem.ValueKind == JsonValueKind.Number ||
                              (idElem.ValueKind == JsonValueKind.String && long.TryParse(idElem.GetString(), out _)));
                if (hasId)
                {
                    long id = idElem.ValueKind == JsonValueKind.Number ? idElem.GetInt64()
                        : long.Parse(idElem.GetString()!);
                    nodes.Add(new { type = "node", data = new { id } });
                }
                else
                {
                    string name = node.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    long uin = node.TryGetProperty("uin", out var u) && u.ValueKind == JsonValueKind.Number ? u.GetInt64()
                        : (node.TryGetProperty("uin", out var u2) && u2.ValueKind == JsonValueKind.String && long.TryParse(u2.GetString(), out var u3) ? u3 : 0);
                    string content = node.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                    nodes.Add(new { type = "node", data = new { name, uin, content } });
                }
            }
            if (nodes.Count == 0)
                throw new Exception("节点列表为空");

            await client.CallActionAsync<object>("send_group_forward_msg", new { group_id = groupId, messages = nodes });
            interactor.Poke($"合并转发发送成功（{nodes.Count} 个节点）");
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("合并转发请求超时（10秒），可能已发送成功。请稍后确认，不要重复发送");
        }
        catch (Exception e)
        {
            interactor.Poke($"合并转发失败：{e.Message}");
        }
    }

    /// <summary>
    /// 尝试修复 AI 生成的残缺 JSON 数组：只允许补全缺失的收尾括号，不允许修改内容。
    /// 返回修复后的 JSON；无法修复时返回 null。
    /// </summary>
    private static string? RepairJsonArray(string json)
    {
        string s = json.Trim();
        if (string.IsNullOrEmpty(s) || s[0] != '[') return null;

        int depth = 0;
        bool inString = false;
        bool escaped = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (inString)
            {
                if (escaped) { escaped = false; continue; }
                if (c == '\\') { escaped = true; continue; }
                if (c == '"') inString = false;
                continue;
            }
            switch (c)
            {
                case '"': inString = true; break;
                case '[': case '{': depth++; break;
                case ']': case '}': depth--; break;
            }
        }
        if (inString || depth != 1) return null; // 引号未闭合或深度不对，无法安全修复

        // 只差一个收尾括号：补 ]
        string candidate = s + "]";
        try
        {
            using var doc = JsonDocument.Parse(candidate);
            return doc.RootElement.ValueKind == JsonValueKind.Array ? candidate : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("发送音乐卡片到QQ聊天。⚠QQ平台音乐卡片请求可能较慢（>10秒），若提示超时请稍后用getmessages确认是否已发出，不要重复发送")]
    public async Task SendMusicCard(
        [Description("目标QQ号(私聊)或群号(群聊)")] long targetId,
        [Description("消息类型：private或group")] string type,
        [Description("音乐平台(qq/163/kugou/migu/kuwo)")] string platform,
        [Description("音乐ID")] string musicId)
    {
        if (!Configuration.MusicCardEnabled) { interactor.Poke("音乐卡片功能已禁用"); return; }
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
        catch (TaskCanceledException)
        {
            interactor.Poke("音乐卡片请求超时（10秒未收到OneBot响应）。QQ服务器可能仍在后台处理，卡片可能稍后出现；请用getmessages确认，不要重复发送。若持续超时请检查musicId是否有效");
        }
        catch (Exception e)
        {
            interactor.Poke($"音乐卡片发送失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("获取群聊/私聊最近消息及每条消息的消息ID，用于定位要撤回(DeleteMsg)、贴表情(SetEmoji)、引用回复(SendReplyMessage)或转发(SendForwardById)的消息。群聊传groupId；私聊传groupId=0并传userId。仅返回实时捕获的真实消息ID（含自己刚发的消息），捕获未启用或没有数据时返回提示")]
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

            // 仅从实时捕获缓存取（消息ID 100%真实，含自己刚发的消息）
            var cacheMatches = _liveMessages
                .Where(m => groupId != 0 ? m.GroupId == groupId : (m.GroupId == 0 && (userId == 0 || m.UserId == userId)))
                .OrderByDescending(m => m.Time)
                .ThenByDescending(m => m.Seq)
                .Take(count)
                .ToList();

            if (cacheMatches.Count == 0)
            {
                interactor.Poke(groupId != 0
                    ? $"群 {groupId} 暂无实时捕获的消息记录（实时捕获连接后开始记录，请稍后重试；若持续为空请检查实时消息捕获配置与OneBot连接）"
                    : $"与 {userId} 暂无实时捕获的消息记录（实时捕获连接后开始记录，请稍后重试）");
                return;
            }

            StringBuilder sb = new();
            string target = groupId != 0 ? $"群 {groupId}" : $"与 {userId}";
            sb.AppendLine($"{target} 最近 {cacheMatches.Count} 条消息（实时捕获，[消息ID:xxx]即真实ID，可能是负数，直接用于撤回/贴表情/引用回复/转发）：");
            foreach (var m in cacheMatches)
            {
                string nick = string.IsNullOrEmpty(m.Nickname) ? (m.IsSelf ? "我" : m.UserId.ToString()) : m.Nickname;
                string raw = OneBotSegment.FilterFace(OneBotSegment.FilterAt(OneBotSegment.FilterImage(OneBotSegment.FilterRecord(m.Raw))));
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(m.Time).LocalDateTime;
                lines.Add($"[{time:HH:mm:ss}] {m.UserId}({nick}) [消息ID:{m.MessageId}] {raw}");
            }
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
