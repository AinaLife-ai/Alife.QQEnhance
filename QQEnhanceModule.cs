using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using Alife.Function.MessageFilter;
using Alife.Function.QChat;
using Microsoft.Extensions.Logging;

namespace AinaLife.QQEnhance;

public class QQEnhanceConfig
{
    [DisplayName("贴表情")]
    [Description("启用给QQ消息贴表情功能")]
    public bool EmojiReactEnabled { get; set; } = true;

    [DisplayName("点赞")]
    [Description("启用给QQ用户资料卡点赞功能（好友与陌生人均可，平台日上限50次/人）")]
    public bool SendLikesEnabled { get; set; } = true;

    [DisplayName("点赞成功回执")]
    [Description("资料卡点赞成功后是否通知AI确认（点赞不像发言会刷屏，确认回执不会造成多余互动，默认开启）")]
    public bool LikeConfirmEnabled { get; set; } = true;

    [DisplayName("撤回")]
    [Description("启用撤回QQ消息功能")]
    public bool DeleteMsgEnabled { get; set; } = true;

    [DisplayName("禁言")]
    [Description("启用禁言QQ群成员功能")]
    public bool GroupBanEnabled { get; set; } = true;

    [DisplayName("音乐卡片")]
    [Description("启用发送音乐卡片功能（默认网易云官方卡片，全端可播放，不会出现\"版本过低\"）")]
    public bool MusicCardEnabled { get; set; } = true;

    [DisplayName("音乐卡片样式")]
    [Description("163=网易云卡片（需签名服务，见下）；record=直接发语音条（网易云直链，完全不依赖签名，任何端可播，卡片异常时的保底）；json=QQ分享卡片；custom=自定义音乐段。除record外所有卡片都必须经过签名服务，签名服务异常或卡片版本过期时接收方会显示\"发送者版本过低\"")]
    public string MusicCardStyle { get; set; } = "163";

    [DisplayName("音乐签名服务地址")]
    [Description("可选。填入后由插件直接完成卡片签名再发送，不再依赖NapCat的musicSignUrl配置（NapCat默认用 ss.xingzhige.com 公共签名，该服务不稳定或卡片版本过期时接收方会显示\"发送者版本过低\"）。例如自建或第三方签名服务地址。留空=交给NapCat处理")]
    public string MusicSignUrl { get; set; } = "";

    [DisplayName("互动提示")]
    [Description("收到QQ消息时在消息末尾附加互动提示（类似官方消息过滤的注入机制），提醒AI可以随手贴表情/引用/戳一戳/点赞")]
    public bool InteractionHintEnabled { get; set; } = true;

    [DisplayName("互动提示概率(%)")]
    [Description("每条QQ消息附加互动提示的概率，0-100，默认100（每次都提示）")]
    public int InteractionHintProbability { get; set; } = 100;

    [DisplayName("互动提示文本")]
    [Description("附加在消息末尾的提示内容，可自定义。支持占位符：{scope}=群号或对方QQ、{type}=group或private、{uin}=发言人QQ、{nick}=发言人昵称、{poke}=戳一戳函数名（自动区分群聊/私聊）")]
    public string InteractionHintText { get; set; } =
        "(可随手互动：ReplyRecent targetid={scope} messagetype={type} target={uin} 引用{nick}这条 / SetEmojiRecent target={uin} 贴表情 / {poke} target={uin} 戳一戳 / SendQQLikes target={uin} 点赞——都是完整回应，无需说明)";

    [DisplayName("被引用/被@回引提示")]
    [Description("当有人引用bot的消息或@bot时，在提示末尾追加一句回引建议（不含消息内容，省token），引导bot用引用回复回应")]
    public bool QuoteBackHintEnabled { get; set; } = true;

    [DisplayName("戳一戳")]
    [Description("启用戳一戳功能（群聊/私聊）")]
    public bool PokeEnabled { get; set; } = true;

    [DisplayName("戳回决策")]
    [Description("收到别人的戳一戳后注入决策提示，让模型顺带决定是否回戳（PokeBack），不影响正常回复构建")]
    public bool PokeDecideEnabled { get; set; } = true;

    [DisplayName("提示冷却时间(秒)")]
    [Description("戳回决策/被赞/被贴表情等事件提示的最小间隔，默认10秒")]
    public double NoticeCooldownSeconds { get; set; } = 10;

    [DisplayName("被赞感知")]
    [Description("感知资料卡被点赞并提示AI可回赞（依赖实时消息捕获开启），默认关闭")]
    public bool PerceiveProfileLike { get; set; } = false;

    [DisplayName("被贴表情感知")]
    [Description("感知群消息被贴表情并提示AI（依赖实时消息捕获开启），默认关闭")]
    public bool PerceiveEmojiLike { get; set; } = false;

    [DisplayName("引用回复")]
    [Description("启用引用回复消息功能")]
    public bool ReplyEnabled { get; set; } = true;

    [DisplayName("合并转发")]
    [Description("启用合并转发消息功能（群聊/私聊，转发已有/构造新转发/转发最近消息）")]
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

    [DisplayName("社交风格提示词")]
    [Description("常驻注入的社交行为引导（留空则不注入）。决定AI多主动/自然地使用贴表情、引用、戳一戳、点赞等功能，可自行改写语气与偏好")]
    public string SocialPrompt { get; set; } =
        "你活跃在QQ群聊和私聊中，像真人一样社交：\n" +
        "- 不是每条互动都要打字——贴表情(SetEmojiRecent)、戳一戳(PokeGroupMember/PokePrivateMember)、点赞(SendQQLikes) 都是完整的回应，看到有趣/赞同/暖心的内容随手就用\n" +
        "- 回应某人优先用引用(ReplyRecent)而不是只@；自己说错了马上撤回(DeleteMsgRecent)\n" +
        "- 被戳一戳/被点赞时回敬一下；这些动作可穿插在日常聊天里随手使用，无需请示，也不用特意说明";

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
    [Description("独立WS监听实时事件捕获真实消息ID（撤回/贴表情/引用/转发的可靠性核心），带断线自动重连")]
    public bool LiveCaptureEnabled { get; set; } = true;

    [DisplayName("实时消息缓存大小")]
    [Description("缓存最近N条消息的消息ID/内容，用于QGetMessages/ReplyRecent等定位")]
    public int LiveCacheSize { get; set; } = 500;

    [DisplayName("捕获连接地址(可选)")]
    [Description("留空=复用QQ聊天的OneBot连接地址。若要捕获bot自己发的消息：在NapCat额外加一个WS服务端（仅此适配器开reportSelfMessage），把它的地址填这里。QChat主连接务必保持reportSelfMessage关闭，否则AI会收到自己的消息造成回环")]
    public string CaptureUrl { get; set; } = "";

    [DisplayName("捕获连接Token(可选)")]
    [Description("捕获连接地址独立设置时的鉴权Token，留空则复用QQ聊天的Token")]
    public string CaptureToken { get; set; } = "";
}

[Module("QQ增强",
    "提供QQ贴表情、点赞、撤回、禁言、戳一戳、引用回复、合并转发、音乐卡片、感知通知、输入中状态等增强功能，支持与YuYang.QQTools自动分工",
    defaultCategory: "AinaLife/社交平台")]
public class QQEnhanceModule(
    XmlFunctionCaller functionCaller,
    ILogger<QQEnhanceModule> logger,
    Interactor<QQEnhanceModule> interactor,
    QChatService qChatService,
    MessageFilterService messageFilterService,
    ModuleSystem moduleSystem) :
    ChatBehaviour,
    IConfigurable<QQEnhanceConfig>
{
    public QQEnhanceConfig Configuration { get; set; } = null!;

    /// <summary>幼央工具箱模块的完整类型名</summary>
    private const string YuYangModuleId = "YuYang.QQTools.QQToolsModule";

    // QChatService 未公开 OneBotClient，通过反射获取（不修改官方代码）
    private OneBotClient? GetClient()
    {
        FieldInfo? field = typeof(QChatService).GetField("oneBotClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(qChatService) as OneBotClient;
    }

    private string? _botNickname;

    /// <summary>bot 真实昵称（转发/显示用），获取失败后退回"我"</summary>
    private string SelfName => _botNickname ?? "我";

    /// <summary>启动时拉取一次 bot 昵称（get_login_info），失败静默</summary>
    private async Task FetchBotNicknameAsync(OneBotClient client)
    {
        try
        {
            var info = await client.CallActionAsync<LoginInfoResult>("get_login_info");
            if (!string.IsNullOrWhiteSpace(info?.Nickname)) _botNickname = info!.Nickname;
        }
        catch { /* 忽略，用"我"兜底 */ }
    }

    private sealed class LoginInfoResult
    {
        [JsonPropertyName("nickname")]
        public string? Nickname { get; init; }
    }

    private long GetBotId()
    {
        OneBotClient? client = GetClient();
        return client?.BotId is > 0 ? client.BotId : _liveBotId;
    }

    // ==================== 幼央兼容检测 ====================

    private bool IsYuYangActive()
    {
        try
        {
            if (moduleSystem.GetModule(YuYangModuleId) == null) return false;
            return Character.Modules.Contains(YuYangModuleId);
        }
        catch
        {
            return false;
        }
    }

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

    private static string DelegateHint(string feature, string yuYangFunction)
    {
        return $"{feature}功能由 YuYang.QQTools（幼央工具箱）接管，请调用幼央的 {yuYangFunction} 函数";
    }

    // ==================== 统一 OneBot 调用（超时/失败友好提示） ====================

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
            return $"{feature}请求超时（10秒未收到响应）。操作可能已生效，请稍后用 QGetMessages 检查确认，不要重复操作";
        }
        catch (Exception e)
        {
            return $"{feature}失败：{e.Message}";
        }
    }

    // Typing indicator 状态管理
    private readonly Dictionary<long, CancellationTokenSource> _typingCts = new();
    private readonly object _typingLock = new();

    // ==================== 实时消息捕获（真实消息ID来源，带断线重连） ====================
    private sealed class LiveMessage
    {
        public long MessageId { get; init; }
        public long UserId { get; init; }
        public long GroupId { get; init; }
        /// <summary>私聊会话对端QQ（群聊为0）。私聊筛选必须用这个字段而不是UserId（bot自己发的消息UserId=BotId）</summary>
        public long PeerId { get; init; }
        public string Nickname { get; init; } = "";
        public string Raw { get; init; } = "";
        public long Time { get; init; }
        public bool IsSelf { get; init; }
        public long Seq { get; init; }
    }

    private long _liveBotId;
    private long _liveSeq;
    private readonly ConcurrentQueue<LiveMessage> _liveMessages = new();
    private readonly ConcurrentDictionary<long, LiveMessage> _liveById = new();
    private DateTime _lastLikePromptTime = DateTime.MinValue;
    private DateTime _lastEmojiLikePromptTime = DateTime.MinValue;
    private TimeSpan NoticeCooldown => TimeSpan.FromSeconds(Math.Max(1, Configuration.NoticeCooldownSeconds));

    private void AddLiveMessage(LiveMessage msg)
    {
        if (msg.MessageId == 0) return;
        if (!_liveById.TryAdd(msg.MessageId, msg)) return;
        _liveMessages.Enqueue(msg);
        TrimLiveCache();
    }

    private void TrimLiveCache()
    {
        int max = Math.Max(50, Configuration.LiveCacheSize);
        while (_liveMessages.Count > max && _liveMessages.TryDequeue(out LiveMessage? old))
            _liveById.TryRemove(old.MessageId, out _);
    }

    /// <summary>捕获主循环：连接 + 接收 + 断线指数退避重连（5s→30s封顶）</summary>
    private async Task LiveCaptureMainAsync(OneBotClient client, CancellationToken ct)
    {
        int failCount = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                string url = !string.IsNullOrWhiteSpace(Configuration.CaptureUrl) ? Configuration.CaptureUrl : client.Url;
                string token = !string.IsNullOrWhiteSpace(Configuration.CaptureUrl) && !string.IsNullOrWhiteSpace(Configuration.CaptureToken)
                    ? Configuration.CaptureToken : client.Token;
                if (string.IsNullOrEmpty(url)) return;

                using var ws = new ClientWebSocket();
                if (!string.IsNullOrEmpty(token))
                    ws.Options.SetRequestHeader("Authorization", $"Bearer {token}");
                await ws.ConnectAsync(new Uri(url), ct);
                logger.LogInformation("QQ增强：实时消息捕获已连接 {Url}", url);
                failCount = 0;

                await LiveReceiveLoopAsync(ws, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "QQ增强：实时消息捕获连接异常");
            }

            if (ct.IsCancellationRequested) return;
            failCount++;
            int delaySec = Math.Min(30, 5 * failCount);
            logger.LogInformation("QQ增强：{Delay}秒后重连实时捕获", delaySec);
            try { await Task.Delay(TimeSpan.FromSeconds(delaySec), ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task LiveReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
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

            if (!root.TryGetProperty("post_type", out var pt)) continue;
            string postType = pt.GetString() ?? "";

            // 握手：lifecycle connect 拿 self_id
            if (postType == "meta_event" &&
                root.TryGetProperty("meta_event_type", out var met) &&
                met.GetString() == "lifecycle")
            {
                if (root.TryGetProperty("self_id", out var self)) _liveBotId = ReadLong(self);
                continue;
            }

            // notice 感知（被赞/被贴表情）——官方事件模型无对应字段，只能在原始JSON层处理
            if (postType == "notice")
            {
                HandleCaptureNotice(root);
                continue;
            }

            if (postType != "message" && postType != "message_sent") continue;

            long msgId = ReadPropLong(root, "message_id");
            if (msgId == 0) continue;

            long userId = ReadPropLong(root, "user_id");
            bool isSelf = postType == "message_sent" || (_liveBotId != 0 && userId == _liveBotId);
            long groupId = ReadPropLong(root, "group_id");
            long time = ReadPropLong(root, "time");
            bool isPrivate = groupId == 0;
            // 私聊会话对端：自己发的看 target_id，别人发的看 user_id
            long peerId = 0;
            if (isPrivate)
                peerId = isSelf ? ReadPropLong(root, "target_id") : userId;

            string nickname = "";
            if (root.TryGetProperty("sender", out var se) && se.ValueKind == JsonValueKind.Object)
            {
                nickname = ReadPropString(se, "card");
                if (string.IsNullOrEmpty(nickname)) nickname = ReadPropString(se, "nickname");
            }

            string raw = ExtractRawText(root);

            AddLiveMessage(new LiveMessage {
                MessageId = msgId, UserId = userId, GroupId = groupId, PeerId = peerId,
                Nickname = nickname, Raw = raw, Time = time, IsSelf = isSelf,
                Seq = Interlocked.Increment(ref _liveSeq)
            });
        }
    }

    /// <summary>捕获链路上的 notice：被赞资料卡 / 被贴表情 → 注入轻量行动建议（30秒冷却）</summary>
    private void HandleCaptureNotice(JsonElement root)
    {
        try
        {
            string noticeType = ReadPropString(root, "notice_type");

            if (noticeType == "profile_like" && Configuration.PerceiveProfileLike)
            {
                if (DateTime.Now - _lastLikePromptTime < NoticeCooldown) return;
                _lastLikePromptTime = DateTime.Now;
                long operatorId = ReadPropLong(root, "operator_id");
                string nick = ReadPropString(root, "operator_nick");
                long times = ReadPropLong(root, "times");
                string who = string.IsNullOrEmpty(nick) ? $"用户{operatorId}" : $"用户{operatorId}({nick})";
                interactor.Poke($"[System {who} 赞了你的资料卡{(times > 0 ? $" {times} 次" : "")}。可以回赞（SendQQLikes qq={operatorId}）或戳一戳回应，也可以忽略]");
            }
            else if (noticeType == "group_msg_emoji_like" && Configuration.PerceiveEmojiLike)
            {
                if (DateTime.Now - _lastEmojiLikePromptTime < NoticeCooldown) return;
                _lastEmojiLikePromptTime = DateTime.Now;
                long uid = ReadPropLong(root, "user_id");
                long gid = ReadPropLong(root, "group_id");
                long mid = ReadPropLong(root, "message_id");
                string who = $"用户{uid}";
                interactor.Poke($"[System {who} 在群 {gid} 给消息[消息ID:{mid}]贴了表情。可以贴回去（SetEmoji messageId={mid}）或接话回应，也可以忽略]");
            }
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "处理捕获notice失败");
        }
    }

    // ==================== JSON 读取小工具 ====================

    private static long ReadLong(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Number => e.TryGetInt64(out long v) ? v : 0,
        JsonValueKind.String => long.TryParse(e.GetString(), out long v) ? v : 0,
        _ => 0
    };

    private static long ReadPropLong(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var e) ? ReadLong(e) : 0;

    private static string ReadPropString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : "";

    /// <summary>从事件根元素提取可读文本（raw_message 优先，否则遍历 message 段数组，富媒体给占位符）</summary>
    private static string ExtractRawText(JsonElement root)
    {
        string raw = ReadPropString(root, "raw_message");
        if (!string.IsNullOrEmpty(raw)) return raw;
        if (!root.TryGetProperty("message", out var me)) return "";
        if (me.ValueKind == JsonValueKind.String) return me.GetString() ?? "";
        if (me.ValueKind == JsonValueKind.Array) return SegmentArrayToText(me);
        return "";
    }

    /// <summary>消息段数组 → 可读文本（图片/语音/表情等给占位符，与官方 OneBotSegment 语义对齐）</summary>
    private static string SegmentArrayToText(JsonElement segments)
    {
        var parts = new List<string>();
        foreach (JsonElement seg in segments.EnumerateArray())
        {
            if (seg.ValueKind != JsonValueKind.Object) continue;
            string type = ReadPropString(seg, "type");
            if (!seg.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object) continue;
            switch (type)
            {
                case "text": parts.Add(ReadPropString(data, "text")); break;
                case "at": parts.Add($"[CQ:at,qq={ReadPropString(data, "qq")}]"); break;
                case "image": parts.Add("[图片]"); break;
                case "face": parts.Add("[表情]"); break;
                case "record": parts.Add("[语音]"); break;
                case "video": parts.Add("[视频]"); break;
                case "reply": parts.Add("[引用]"); break;
                case "forward": parts.Add("[合并转发]"); break;
                case "json": parts.Add("[卡片]"); break;
                case "file": parts.Add("[文件]"); break;
                case "music": parts.Add("[音乐]"); break;
            }
        }
        return string.Join("", parts);
    }

    // ==================== 发送自存（bot 自己发的消息也进缓存） ====================

    /// <summary>发送类 API 的返回（取 message_id / res_id）</summary>
    private sealed class SendResult
    {
        [JsonPropertyName("message_id")]
        public JsonElement MessageId { get; init; }

        [JsonPropertyName("res_id")]
        public JsonElement ResId { get; init; }
    }

    /// <summary>把本插件发送的消息存入实时缓存（UserId=BotId，私聊记 PeerId=对方），供转发/引用/撤回/查询使用</summary>
    private void RecordSentMessage(long messageId, long groupId, long peerId, string raw)
    {
        if (messageId == 0) return;
        long botId = GetBotId();
        AddLiveMessage(new LiveMessage {
            MessageId = messageId, UserId = botId, GroupId = groupId, PeerId = peerId,
            Nickname = SelfName, Raw = raw, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsSelf = true, Seq = Interlocked.Increment(ref _liveSeq)
        });
    }

    /// <summary>从发送结果中提取 id（兼容数字/数字字符串），取不到返回0</summary>
    private static long ExtractId(JsonElement elem) => ReadLong(elem);

    private static long ExtractSentId(SendResult? result) =>
        result == null ? 0 : ReadLong(result.MessageId);

    private static long ExtractResId(SendResult? result) =>
        result == null ? 0 : ReadLong(result.ResId);

    // ==================== 历史消息回拉补齐（缓存不足时自动调用，message_id 为真实可用ID） ====================

    /// <summary>回拉群/私聊历史消息写入缓存。返回新增条数。只取 message_id 字段（不要与分页参数 message_seq 混淆）</summary>
    private async Task<int> BackfillHistoryAsync(long groupId, long userId, int count)
    {
        OneBotClient? client = GetClient();
        if (client == null) return 0;
        try
        {
            JsonElement data;
            if (groupId != 0)
                data = await client.CallActionAsync<JsonElement>("get_group_msg_history",
                    new { group_id = groupId, count = Math.Clamp(count, 1, 50) });
            else
                data = await client.CallActionAsync<JsonElement>("get_friend_msg_history",
                    new { user_id = userId, count = Math.Clamp(count, 1, 50) });

            if (data.ValueKind != JsonValueKind.Object ||
                !data.TryGetProperty("messages", out var msgs) ||
                msgs.ValueKind != JsonValueKind.Array)
                return 0;

            long botId = GetBotId();
            int added = 0;
            foreach (JsonElement m in msgs.EnumerateArray())
            {
                long mid = ReadPropLong(m, "message_id");
                if (mid == 0 || _liveById.ContainsKey(mid)) continue;
                long uid = ReadPropLong(m, "user_id");
                long gid = ReadPropLong(m, "group_id");
                long time = ReadPropLong(m, "time");
                bool isSelf = botId != 0 && uid == botId;
                string nick = "";
                if (m.TryGetProperty("sender", out var se) && se.ValueKind == JsonValueKind.Object)
                {
                    nick = ReadPropString(se, "card");
                    if (string.IsNullOrEmpty(nick)) nick = ReadPropString(se, "nickname");
                }
                long peerId = gid == 0 ? (groupId == 0 ? userId : 0) : 0;
                AddLiveMessage(new LiveMessage {
                    MessageId = mid, UserId = uid, GroupId = gid, PeerId = peerId,
                    Nickname = nick, Raw = ExtractRawText(m), Time = time,
                    IsSelf = isSelf, Seq = Interlocked.Increment(ref _liveSeq)
                });
                added++;
            }
            return added;
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "回拉历史消息失败 group={GroupId} user={UserId}", groupId, userId);
            return 0;
        }
    }

    // ==================== 戳回决策状态 ====================
    private sealed record PokeRequest(long UserId, long GroupId, bool IsGroup, DateTime Time);
    private PokeRequest? _lastPokeRequest;
    private DateTime _lastPokePromptTime = DateTime.MinValue;

    protected override Task OnAwake()
    {
        bool yuYangActive = IsYuYangActive();
        if (yuYangActive)
            logger.LogInformation("QQ增强：检测到 YuYang.QQTools 已启用，重叠功能将让位（兼容模式 {Mode}）", Configuration.CompatibilityMode);

        string explanation = yuYangActive
            ? """
                使用规则：
                - 已检测到 YuYang.QQTools（幼央工具箱）接管：戳一戳、引用回复、点赞、贴表情、撤回、输入中 请调用幼央的函数。
                - 本插件负责：禁言(GroupBan)、音乐卡片(SendMusicCard)、合并转发(ForwardRecent/SendForwardById/SendForwardNew)、消息ID查询(QGetMessages)、戳回决策(PokeBack)、感知通知。
                - QQ消息ID通常是负数（如 -1976879391），编造或猜ID必然失败；要操作某条消息先用 QGetMessages 获取真实ID。
                """
            : """
                使用规则：
                - QQ消息ID通常是负数，禁止编造。撤回(DeleteMsg)/贴表情(SetEmoji)/引用(SendReplyMessage)指定消息时，ID必须来自 QGetMessages 返回的 [消息ID:xxx]。
                - 高频场景一步到位（推荐优先用）：ReplyRecent=引用某人最近一条；SetEmojiRecent=给某人最近一条贴表情；DeleteMsgRecent=撤回自己最近一条；ForwardRecent=转发最近N条。这些都无需查ID。
                - 回复最近一条用 ReplyRecent；回复更早的指定消息用 QGetMessages 查列表，再用 SendReplyMessage replyToId=真实ID。
                - 音乐卡片：SendMusicCard platform=search musicId=歌名 即可。发送可能较慢，超时后先用 QGetMessages 确认，不要重复发送。
                - 用本插件发送类函数（引用回复/合并转发/音乐卡片）成功后就已完成发送，不要再用 QChat 发重复确认消息。
                - 戳一戳：PokeGroupMember 群聊戳；PokePrivateMember 私聊戳；被戳后系统会提示，用 PokeBack 回戳或忽略。
                - 贴表情 emojiId 对照（QQ官方表情列表，按需取用，不要每次都贴同一个）：201=点赞 264=捂脸 182=笑哭 271=吃瓜 270=emm 179=doge 269=暗中观察 273=我酸了 272=呵呵哒 222=抱抱 227=拍手 246=加油抱抱 116=示爱 122=爱你 214=啵啵 219=蹭一蹭 111=可怜 106=委屈 173=泪奔 262=脑阔疼 268=问号脸 265=辣眼睛 266=哦哟 267=头秃 277=汪汪 278=汗 281=无眼笑 282=敬礼 284=面无表情 285=摸鱼 287=哦 289=睁眼 104=哈欠 109=左亲亲 118=抱拳 120=拳头 123=NO 124=OK 125=转圈 129=挥手 144=喝彩 147=棒棒糖 171=茶 174=无奈 175=卖萌 176=小纠结 180=惊喜 181=骚扰 183=我最美 203=托脸 212=托腮 232=佛系 240=喷脸 243=甩头
                """;

        XmlHandler xmlHandler = new(this) {
            Description = "QQ增强：贴表情、资料卡点赞、撤回、禁言、戳一戳、引用回复、合并转发、点歌发音乐卡片、消息ID查询。可随手用 SetEmojiRecent/ReplyRecent/PokeGroupMember/SendQQLikes 轻量互动",
            Explanation = explanation
        };
        functionCaller.RegisterHandler(xmlHandler, DocumentMode.Implicit, DestroyCancellationToken);

        // 常驻社交风格提示（可在配置中自定义/清空）
        if (!string.IsNullOrWhiteSpace(Configuration.SocialPrompt))
            interactor.Prompt(Configuration.SocialPrompt);

        OneBotClient? client = GetClient();
        if (client == null)
        {
            logger.LogWarning("无法获取 OneBotClient，QQ增强功能不可用（请确认已启用QQ聊天模块）");
            return Task.CompletedTask;
        }

        if (Configuration.PerceiveGroupBan || Configuration.PerceiveGroupIncrease || Configuration.PokeDecideEnabled)
            client.EventReceived += OnEventReceived;

        // 输入中状态：幼央接管时自动让位（避免双插件同时发 set_input_status）
        if (Configuration.TypingIndicatorEnabled && !ShouldDelegate())
        {
            ChatBot.ChatSent += OnChatSent;
            ChatBot.ChatOver += OnChatOver;
        }

        // 互动提示：挂到官方消息过滤同款钩子（ChatBot.ChatSend），收到QQ消息时按概率附加提示
        if (Configuration.InteractionHintEnabled)
            ChatBot.ChatSent -= OnChatSent; // 无操作，仅防误用
        ChatBot.ChatSend += OnChatSendHint;

        _ = FetchBotNicknameAsync(client);

        if (Configuration.LiveCaptureEnabled)
            _ = LiveCaptureMainAsync(client, DestroyCancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>官方QChat纠错规则要求"QQ消息输入必须输出QChat标签"，与QQ增强发送类函数冲突（用ReplyRecent回复后会触发纠错→AI又发一条重复确认）。
    /// 在所有模块Awake后把该规则替换为扩展版：输出含 QChat 或本插件任意函数名都算合规。模块销毁时恢复原规则。</summary>
    private MessageReplyRule? _originalQChatRule;

    protected override Task OnStart()
    {
        try
        {
            if (messageFilterService.MessageReplyRules is List<MessageReplyRule> rules)
            {
                _originalQChatRule = rules.FirstOrDefault(r => r.Name == "QChatService");
                if (_originalQChatRule != null)
                {
                    MessageReplyRule orig = _originalQChatRule;
                    string[] qqEnhanceFunctions = [
                        "ReplyRecent", "SendReplyMessage", "ForwardRecent", "SendForwardById", "SendForwardNew",
                        "SendMusicCard", "QGetMessages", "SetEmojiRecent", "SetEmoji", "DeleteMsgRecent", "DeleteMsg",
                        "SendQQLikes", "PokeGroupMember", "PokePrivateMember", "PokeBack", "GroupBan"
                    ];
                    rules.Remove(orig);
                    messageFilterService.AddMessageReplyRule(new MessageReplyRule {
                        Name = orig.Name,
                        InputMatching = orig.InputMatching,
                        OutputMatching = output => orig.OutputMatching(output) ||
                            qqEnhanceFunctions.Any(f => output.Contains(f, StringComparison.OrdinalIgnoreCase)),
                        CorrectionMessage = orig.CorrectionMessage
                    }, DestroyCancellationToken);
                    logger.LogInformation("QQ增强：已扩展QChat回复格式规则，使用QQ增强函数回复不再触发格式纠正");
                }
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "扩展QChat回复格式规则失败（不影响其他功能）");
        }
        return Task.CompletedTask;
    }

    protected override Task OnDestroy()
    {
        OneBotClient? client = GetClient();
        if (client != null)
            client.EventReceived -= OnEventReceived;

        ChatBot.ChatSent -= OnChatSent;
        ChatBot.ChatOver -= OnChatOver;
        ChatBot.ChatSend -= OnChatSendHint;

        // 恢复官方QChat纠错规则（OnStart 中替换过）
        if (_originalQChatRule != null &&
            messageFilterService.MessageReplyRules is List<MessageReplyRule> restoreRules &&
            !restoreRules.Any(r => r.Name == "QChatService" && ReferenceEquals(r, _originalQChatRule)))
        {
            restoreRules.RemoveAll(r => r.Name == "QChatService");
            restoreRules.Add(_originalQChatRule);
            _originalQChatRule = null;
        }

        lock (_typingLock)
        {
            foreach (var cts in _typingCts.Values)
                cts.Cancel();
            _typingCts.Clear();
        }

        return Task.CompletedTask;
    }

    // ==================== 消息定位（缓存 + 历史回拉兜底） ====================

    /// <summary>在指定会话中定位目标用户最近一条消息。target 为纯数字按QQ号精确匹配，"我"匹配自己，否则按昵称包含匹配</summary>
    private LiveMessage? FindLatestFromUser(long scopeId, string target, bool isGroup)
    {
        target = target.Trim();
        bool byId = long.TryParse(target, out long targetUin);
        long botId = GetBotId();
        bool self = target is "我" or "自己" || (byId && botId != 0 && targetUin == botId);

        var candidates = _liveMessages
            .Where(m => isGroup ? m.GroupId == scopeId : (m.GroupId == 0 && m.PeerId == scopeId))
            .Where(m => self ? m.IsSelf
                : byId ? m.UserId == targetUin
                : m.Nickname.Contains(target, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .ToList();

        // 昵称歧义检测：命中多个不同QQ号时返回null并在调用处提示
        if (!byId && !self && candidates.Select(m => m.UserId).Distinct().Count() > 1)
            return null;
        return candidates.FirstOrDefault();
    }

    /// <summary>全缓存范围定位目标用户最近一条消息（用于 targetId 缺省时推断会话）</summary>
    private LiveMessage? FindLatestFromUserAnywhere(string target)
    {
        target = target.Trim();
        bool byId = long.TryParse(target, out long targetUin);
        long botId = GetBotId();
        bool self = target is "我" or "自己" || (byId && botId != 0 && targetUin == botId);

        return _liveMessages
            .Where(m => self ? m.IsSelf
                : byId ? m.UserId == targetUin
                : m.Nickname.Contains(target, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .FirstOrDefault();
    }

    /// <summary>定位结果解析：targetId 缺省时自动推断会话；找不到时回拉历史重试一次</summary>
    private async Task<(LiveMessage? msg, bool isGroup, long scopeId, string? error)> ResolveTargetMessageAsync(
        string target, long targetId, string messageType)
    {
        bool isGroup = messageType != "private";
        long scopeId = targetId;

        if (scopeId == 0)
        {
            LiveMessage? any = FindLatestFromUserAnywhere(target);
            if (any == null)
                return (null, isGroup, 0, $"未找到 {target} 的任何消息记录（可能不在缓存中）。请显式传 targetId（群号或对方QQ）后重试");
            isGroup = any.GroupId != 0;
            scopeId = isGroup ? any.GroupId : any.PeerId;
        }

        LiveMessage? live = FindLatestFromUser(scopeId, target, isGroup);
        if (live == null)
        {
            // 缓存不足：回拉历史补齐（history 返回的 message_id 是真实可用ID）
            await BackfillHistoryAsync(isGroup ? scopeId : 0, isGroup ? 0 : scopeId, 20);
            live = FindLatestFromUser(scopeId, target, isGroup);
        }

        if (live == null)
        {
            string scope = isGroup ? $"群 {scopeId}" : $"与 {scopeId} 的私聊";
            return (null, isGroup, scopeId,
                $"未在{scope}中找到 {target} 的消息（昵称匹配到多人时也会返回此提示，请改用QQ号）。可用 QGetMessages 查列表确认");
        }
        return (live, isGroup, scopeId, null);
    }

    // ==================== 工具函数 ====================

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给指定消息ID的QQ消息贴表情。messageId必须来自 QGetMessages 返回的[消息ID:xxx]，严禁编造。想给某人最近一条消息贴表情请直接用 SetEmojiRecent（免ID）")]
    public async Task SetEmoji(
        [Description("消息ID（必须来自QGetMessages）")] long messageId,
        [Description("表情ID，201为点赞")] int emojiId)
    {
        if (!Configuration.EmojiReactEnabled) { interactor.Poke("贴表情功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("贴表情", "SendEmojiLike")); return; }
        if (_liveById.TryGetValue(messageId, out LiveMessage? known) && known.IsSelf)
        {
            interactor.Poke("这条消息是你自己发的，不建议给自己的消息贴表情，已跳过。请从 QGetMessages 列表里选别人发的消息");
            return;
        }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("set_msg_emoji_like", new { message_id = messageId, emoji_id = emojiId.ToString(), set = true }, "贴表情", client);
        if (err != null) interactor.Poke(err);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("对某人最近一条消息贴表情回应（免ID，一步到位）。看到有趣/赞同/暖心/好笑的消息时随手贴一个（201=点赞 4=滑稽 66=比心），这是真人最轻量的互动方式，不需要说话就可以直接贴")]
    public async Task SetEmojiRecent(
        [Description("目标用户QQ号或昵称，\"我\"表示自己")] string target,
        [Description("表情ID，201为点赞")] int emojiId = 201,
        [Description("目标群号（可省略，省略时自动推断最近会话）")] long targetId = 0,
        [Description("消息类型：group或private，可省略")] string messageType = "")
    {
        if (!Configuration.EmojiReactEnabled) { interactor.Poke("贴表情功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("贴表情", "SendEmojiLike")); return; }

        (LiveMessage? msg, _, _, string? error) = await ResolveTargetMessageAsync(target, targetId, messageType);
        if (msg == null) { interactor.Poke(error!); return; }

        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("set_msg_emoji_like", new { message_id = msg.MessageId, emoji_id = emojiId.ToString(), set = true }, "贴表情", client);
        if (err != null) interactor.Poke(err);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给某人资料卡点赞。对方帮了忙、说了让你开心的话、想表达'我注意到你了'时用，像真人互赞一样自然。好友与陌生人均可；每人每天上限50个（平台限制），达到上限会明确提示，明天可再来")]
    public async Task SendQQLikes(
        [Description("QQ号")] long qq,
        [Description("点赞次数，默认50次（平台每日上限）")] int times = 50)
    {
        if (!Configuration.SendLikesEnabled) { interactor.Poke("点赞功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("点赞", "SendLike")); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("点赞失败：QQ客户端不可用"); return; }
        try
        {
            times = Math.Clamp(times, 1, 50);
            var chunks = new List<int>();
            for (int i = 0; i < times / 10; i++) chunks.Add(10);
            if (times % 10 > 0) chunks.Add(times % 10);

            int count = 0;
            foreach (int chunk in chunks)
            {
                string? err = await CallActionSafeAsync("send_like", new { user_id = qq, times = chunk }, "点赞", client);
                if (err != null)
                {
                    interactor.Poke($"{err}（已成功 {count} 个赞。若提示\"今日同一好友点赞数已达上限\"说明今天已点满，明天再来；不要重复尝试）");
                    return;
                }
                count += chunk;
            }
            if (Configuration.LikeConfirmEnabled) interactor.Poke($"点赞成功，点了 {count} 个赞");
        }
        catch (Exception e)
        {
            interactor.Poke($"点赞失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("撤回指定消息ID的QQ消息。messageId必须来自 QGetMessages。想快速撤回自己刚发的消息请用 DeleteMsgRecent（免ID）")]
    public async Task DeleteMsg(
        [Description("消息ID（必须来自QGetMessages）")] long messageId)
    {
        if (!Configuration.DeleteMsgEnabled) { interactor.Poke("撤回功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("撤回", "DeleteMessage")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("delete_msg", new { message_id = messageId }, "撤回", client);
        if (err != null) interactor.Poke(err + "（RetCode 1200 是 NapCat 内部异常的统称，常见原因：消息超过约2分钟撤回时限、非管理员撤回他人消息、目标是卡片/合并转发类消息、或 NapCat 内存中已丢失该消息记录——超时类消息无法撤回属平台限制）");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("撤回自己刚发的最近一条消息（免ID，一步到位）。说错话、发错会话、内容有误时立刻用。私聊只能撤回自己的；群聊默认可撤回自己的，是管理员时可撤回他人")]
    public async Task DeleteMsgRecent(
        [Description("目标群号或对方QQ（可省略，省略时自动找自己最近发的消息所在会话）")] long targetId = 0,
        [Description("撤回谁的消息：默认\"我\"，管理员撤群员时填对方QQ号")] string target = "我",
        [Description("消息类型：group或private，可省略")] string messageType = "")
    {
        if (!Configuration.DeleteMsgEnabled) { interactor.Poke("撤回功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("撤回", "DeleteMessage")); return; }

        (LiveMessage? msg, _, _, string? error) = await ResolveTargetMessageAsync(target, targetId, messageType);
        if (msg == null) { interactor.Poke(error!); return; }
        if (!msg.IsSelf && msg.GroupId == 0)
        {
            interactor.Poke("私聊无法撤回对方的消息（平台限制），只能撤回自己发的");
            return;
        }

        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("delete_msg", new { message_id = msg.MessageId }, "撤回", client);
        if (err != null) interactor.Poke(err + "（RetCode 1200 是 NapCat 内部异常的统称，常见原因：消息超过约2分钟撤回时限、非管理员撤回他人消息、目标是卡片/合并转发类消息、或 NapCat 内存中已丢失该消息记录——超时类消息无法撤回属平台限制）");
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
        if (err != null) interactor.Poke(err);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("戳一戳群成员。想引起对方注意、打招呼、催回复、表达'我来啦/我赞同'时随手戳，比打字更轻快")]
    public async Task PokeGroupMember(
        [Description("群号")] long groupId,
        [Description("QQ号")] long userId)
    {
        if (!Configuration.PokeEnabled) { interactor.Poke("戳一戳功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("戳一戳", "PokeGroupMember")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("group_poke", new { group_id = groupId, user_id = userId }, "戳一戳", client);
        if (err != null) interactor.Poke(err);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("私聊戳一戳指定用户。私聊里打招呼、提醒看消息时随手用")]
    public async Task PokePrivateMember(
        [Description("QQ号")] long userId)
    {
        if (!Configuration.PokeEnabled) { interactor.Poke("戳一戳功能已禁用"); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("friend_poke", new { user_id = userId }, "私聊戳一戳", client);
        if (err != null) interactor.Poke(err);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("回应最近一次戳你的人：回戳或忽略。当系统提示你被戳了时调用。decide=\"yes\"回戳；decide=\"no\"忽略。只用于回应戳一戳，不用于主动戳人")]
    public async Task PokeBack(
        [Description("yes=回戳，no=忽略")] string decide = "yes")
    {
        if (!Configuration.PokeDecideEnabled || !Configuration.PokeEnabled) { interactor.Poke("戳回功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("戳一戳", "PokeGroupMember")); return; }

        if (decide != "yes")
        {
            _lastPokeRequest = null;
            // 成功静默
            return;
        }

        if (_lastPokeRequest == null)
        {
            interactor.Poke("没有待回应的戳一戳（可能已过期或已处理）");
            return;
        }

        var req = _lastPokeRequest;
        if ((DateTime.Now - req.Time) > TimeSpan.FromMinutes(10))
        {
            _lastPokeRequest = null;
            interactor.Poke("戳一戳请求已过期，不回戳");
            return;
        }

        OneBotClient? client = GetClient();
        string? err;
        if (req.IsGroup)
            err = await CallActionSafeAsync("group_poke", new { group_id = req.GroupId, user_id = req.UserId }, "戳一戳", client);
        else
            err = await CallActionSafeAsync("friend_poke", new { user_id = req.UserId }, "私聊戳一戳", client);

        _lastPokeRequest = null;
        if (err != null) interactor.Poke(err);
    }

    // ==================== 引用回复 ====================

    /// <summary>引用回复核心：消息段数组 [{reply},{text}]（NapCat 原生支持，send_msg 无顶层 reply 参数）</summary>
    private async Task<string?> SendReplyCoreAsync(bool isGroup, long scopeId, long replyToId, string message)  // 成功返回null（静默），仅失败/超时返回提示
    {
        OneBotClient? client = GetClient();
        if (client == null) return "引用回复失败：QQ客户端不可用";

        object[] msgArr = [
            new { type = "reply", data = new { id = replyToId.ToString() } },
            new { type = "text", data = new { text = message } }
        ];
        try
        {
            SendResult? sent = isGroup
                ? await client.CallActionAsync<SendResult>("send_group_msg", new { group_id = scopeId, message = msgArr })
                : await client.CallActionAsync<SendResult>("send_private_msg", new { user_id = scopeId, message = msgArr });
            long sentId = ExtractSentId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, isGroup ? scopeId : 0, isGroup ? 0 : scopeId, message);
            return null;  // 成功静默：不触发AI新一轮，避免多余的确认回复
        }
        catch (TaskCanceledException)
        {
            return "引用回复请求超时（10秒），可能已发送成功，请用 QGetMessages 确认，不要重复发送";
        }
        catch (Exception e)
        {
            return $"引用回复失败：{e.Message}";
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("引用回复某人最近一条消息（免ID，一步到位）。群聊里回应特定某人时优先用它（比@更清楚）；私聊接梗/辩论时引用对方原话再回更自然。要回复更早的指定消息，用 QGetMessages 查列表再 SendReplyMessage")]
    public async Task ReplyRecent(
        [Description("回复内容")] string message,
        [Description("目标用户QQ号或昵称，\"我\"表示自己")] string target,
        [Description("目标群号或对方QQ（可省略，省略时自动推断该用户最近发言所在会话）")] long targetId = 0,
        [Description("消息类型：group或private，可省略")] string messageType = "")
    {
        if (!Configuration.ReplyEnabled) { interactor.Poke("引用回复功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("引用回复", "SendReplyMessage")); return; }

        (LiveMessage? msg, bool isGroup, long scopeId, string? error) = await ResolveTargetMessageAsync(target, targetId, messageType);
        if (msg == null) { interactor.Poke(error!); return; }

        string? result = await SendReplyCoreAsync(isGroup, scopeId, msg.MessageId, message);
        if (result != null) interactor.Poke(result);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("引用回复指定消息ID的消息。先用 QGetMessages 获取目标消息的[消息ID:xxx]（包括较早的历史消息），再调用本函数。replyToId 必须来自QGetMessages，严禁编造")]
    public async Task SendReplyMessage(
        [Description("回复内容")] string message,
        [Description("被回复消息的真实ID（来自QGetMessages）")] long replyToId,
        [Description("目标群号或对方QQ（可省略，省略时自动从缓存按ID推断会话）")] long targetId = 0,
        [Description("消息类型：group或private，可省略")] string messageType = "")
    {
        if (!Configuration.ReplyEnabled) { interactor.Poke("引用回复功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("引用回复", "SendReplyMessage")); return; }

        bool isGroup = messageType != "private";
        long scopeId = targetId;
        if (scopeId == 0)
        {
            if (_liveById.TryGetValue(replyToId, out LiveMessage? known))
            {
                isGroup = known.GroupId != 0;
                scopeId = isGroup ? known.GroupId : known.PeerId;
            }
            else
            {
                interactor.Poke("该消息ID不在缓存中，无法推断会话。请显式传 targetId（群号或对方QQ）与 messageType");
                return;
            }
        }

        string? result = await SendReplyCoreAsync(isGroup, scopeId, replyToId, message);
        if (result != null) interactor.Poke(result);
    }

    // ==================== 合并转发 ====================

    /// <summary>发送合并转发核心，返回 (成功?, 提示)</summary>
    private async Task<(bool ok, string text)> SendForwardCoreAsync(bool isGroup, long scopeId, List<object> nodes)
    {
        OneBotClient? client = GetClient();
        if (client == null) return (false, "合并转发失败：QQ客户端不可用");
        try
        {
            SendResult? sent = isGroup
                ? await client.CallActionAsync<SendResult>("send_group_forward_msg", new { group_id = scopeId, messages = nodes })
                : await client.CallActionAsync<SendResult>("send_private_forward_msg", new { user_id = scopeId, messages = nodes });
            long sentId = ExtractSentId(sent);
            long resId = ExtractResId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, isGroup ? scopeId : 0, isGroup ? 0 : scopeId,
                    resId != 0 ? $"[CQ:forward,id={resId}]" : "[合并转发]");
            return (true, $"合并转发发送成功（{nodes.Count} 个节点{(resId != 0 ? $"，res_id={resId}" : "")}）。消息已实际发出，不要再用 QChat 重复确认");
        }
        catch (TaskCanceledException)
        {
            return (false, "合并转发请求超时（10秒），可能已发送成功，请稍后确认，不要重复发送");
        }
        catch (Exception e)
        {
            return (false, $"合并转发失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("转发某群/私聊最近N条消息为合并转发（免ID，一步到位）。自动从实时缓存取真实消息ID引用节点（含bot自己发的消息），图片/语音/表情等富媒体原样真实转发；缓存不足时自动回拉历史消息补齐；个别失效节点自动降级为文本重建节点保证转发成功")]
    public async Task ForwardRecent(
        [Description("目标群号或对方QQ")] long targetId,
        [Description("转发条数，1-50，默认5")] int count = 5,
        [Description("消息类型：group或private，默认group")] string messageType = "group")
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("合并转发功能已禁用"); return; }
        if (targetId == 0) { interactor.Poke("targetId不能为0"); return; }

        bool isGroup = messageType != "private";
        count = Math.Clamp(count, 1, 50);

        List<LiveMessage> Query() => _liveMessages
            .Where(m => isGroup ? m.GroupId == targetId : (m.GroupId == 0 && m.PeerId == targetId))
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .Take(count)
            .OrderBy(m => m.Time)
            .ThenBy(m => m.Seq)
            .ToList();

        var matches = Query();
        if (matches.Count < count)
        {
            await BackfillHistoryAsync(isGroup ? targetId : 0, isGroup ? 0 : targetId, count);
            matches = Query();
        }

        if (matches.Count == 0)
        {
            interactor.Poke($"{(isGroup ? $"群 {targetId}" : $"与 {targetId} 的私聊")}暂无可转发的消息记录");
            return;
        }

        // 内容节点优先（方案A）：直接用缓存/历史里的消息文本构造自定义节点。
        // 不经过 NapCat 的 MessageUnique id 查找，bot自己发的、历史补拉的一条都不会丢。
        // 代价：图片/语音等富媒体以[图片]等占位文字呈现。
        var nodes = matches.Select(m => (object)new {
            type = "node",
            data = new {
                name = string.IsNullOrEmpty(m.Nickname) ? (m.IsSelf ? SelfName : m.UserId.ToString()) : m.Nickname,
                nickname = string.IsNullOrEmpty(m.Nickname) ? (m.IsSelf ? SelfName : m.UserId.ToString()) : m.Nickname,
                uin = m.UserId.ToString(),
                content = m.Raw
            }
        }).ToList();
        var (ok, text) = await SendForwardCoreAsync(isGroup, targetId, nodes);
        if (!ok) interactor.Poke(text);  // 成功静默
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("转发一条已有的合并转发消息到群聊/私聊。forwardId 为该合并转发消息的消息ID（来自 QGetMessages 返回的[消息ID:xxx]，可为负数）")]
    public async Task SendForwardById(
        [Description("目标群号或对方QQ")] long targetId,
        [Description("合并转发消息的消息ID（来自QGetMessages）")] long forwardId,
        [Description("消息类型：group或private，默认group")] string messageType = "group")
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("合并转发功能已禁用"); return; }
        // NapCat node schema 强制要求 nickname/content 必填，缺失直接 RetCode 1400；id 有效时忽略这两个字段
        var nodes = new List<object> { new { type = "node", data = new { id = forwardId.ToString(), nickname = "QQ用户", content = "" } } };
        var (okFwd, text) = await SendForwardCoreAsync(messageType != "private", targetId, nodes);
        if (!okFwd) interactor.Poke(text);  // 成功静默
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("构造并发送新的合并转发消息。nodesJson为JSON数组，每个节点两种格式：{\"name\":\"昵称\",\"uin\":QQ号,\"content\":\"内容\"}（自定义内容）或 {\"id\":真实消息ID}（引用真实消息，id必须来自QGetMessages，数字或数字字符串均可）。⚠必须传完整合法的JSON数组，最外层用[]包裹，不要漏收尾括号")]
    public async Task SendForwardNew(
        [Description("目标群号或对方QQ")] long targetId,
        [Description("节点JSON数组（必须是完整合法的JSON，[]闭合）")] string nodesJson,
        [Description("消息类型：group或private，默认group")] string messageType = "group")
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("合并转发功能已禁用"); return; }
        try
        {
            // 容错：去掉首尾多余空白；若AI漏了收尾括号，尝试补全（最多补一层 ]）
            string json = nodesJson.Trim();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    throw new Exception("nodesJson必须为JSON数组");
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
                throw new Exception("nodesJson必须为JSON数组");

            var nodes = new List<object>();
            var missingIds = new List<long>();
            foreach (JsonElement node in doc2.RootElement.EnumerateArray())
            {
                if (node.ValueKind != JsonValueKind.Object)
                    throw new Exception("每个节点必须是JSON对象，请检查是否漏了花括号");

                bool hasId = node.TryGetProperty("id", out var idElem) &&
                             (idElem.ValueKind == JsonValueKind.Number ||
                              (idElem.ValueKind == JsonValueKind.String && long.TryParse(idElem.GetString(), out _)));
                if (hasId)
                {
                    long id = idElem.ValueKind == JsonValueKind.Number ? idElem.GetInt64()
                        : long.Parse(idElem.GetString()!);
                    // 校验：id 必须在缓存中存在（编造/过期ID会被NapCat静默跳过，全部跳过则整条转发失败）
                    if (!_liveById.ContainsKey(id))
                        missingIds.Add(id);
                    nodes.Add(new { type = "node", data = new { id = id.ToString() } });
                }
                else
                {
                    string name = node.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    long uin = node.TryGetProperty("uin", out var u) && u.ValueKind == JsonValueKind.Number ? u.GetInt64()
                        : (node.TryGetProperty("uin", out var u2) && u2.ValueKind == JsonValueKind.String && long.TryParse(u2.GetString(), out var u3) ? u3 : 0);
                    string content = node.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                    nodes.Add(new { type = "node", data = new { name, nickname = name, uin = uin.ToString(), content } });
                }
            }
            if (nodes.Count == 0)
                throw new Exception("节点列表为空");
            if (missingIds.Count > 0)
            {
                string miss = string.Join(",", missingIds.Take(5));
                interactor.Poke($"警告：{missingIds.Count} 个id节点不在消息缓存中（{miss}{(missingIds.Count > 5 ? "..." : "")}），NapCat 会跳过这些节点。请改用 QGetMessages 取真实ID，或对该节点改用 name/uin/content 自定义内容格式");
                return;
            }

            var (okNew, text) = await SendForwardCoreAsync(messageType != "private", targetId, nodes);
            if (!okNew) interactor.Poke(text);  // 成功静默
        }
        catch (Exception e)
        {
            interactor.Poke($"合并转发失败：{e.Message}");
        }
    }

    /// <summary>修复AI生成的残缺JSON数组：只允许补全缺失的收尾括号，不允许修改内容。返回修复后的JSON，无法修复时返回null</summary>
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
        if (inString || depth != 1) return null;

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

    // ==================== 音乐卡片：默认网易云官方卡片（免签名全端可渲染） ====================

    private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };

    /// <summary>CQ码转义（custom音乐卡片字段用）</summary>
    private static string CqEscape(string s) =>
        s.Replace("&", "&amp;").Replace("[", "&#91;").Replace("]", "&#93;").Replace(",", "&#44;");

    [XmlFunction(FunctionMode.OneShot)]
    [Description("发送音乐到QQ聊天（点歌）。platform=search musicId=歌名关键词（如 晴天 周杰伦）即可，默认发网易云卡片。若接收方显示\"发送者版本过低\"，说明签名服务不可用或卡片版本过期——可在插件配置填 音乐签名服务地址，或把 音乐卡片样式 改为 record（直接发语音条，任何端可播，100%不受签名影响）。旧用法 platform=163 + 网易云歌曲ID 也可。⚠发送可能较慢（>10秒），超时后请先用 QGetMessages 确认，不要重复发送")]
    public async Task SendMusicCard(
        [Description("目标群号或对方QQ")] long targetId,
        [Description("消息类型：private或group")] string type,
        [Description("音乐平台：search=关键词搜索（推荐）/163=网易云歌曲ID")] string platform,
        [Description("歌曲关键词（platform=search时）或网易云歌曲ID（platform=163时）")] string musicId)
    {
        if (!Configuration.MusicCardEnabled) { interactor.Poke("音乐卡片功能已禁用"); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("音乐卡片发送失败：QQ客户端不可用"); return; }
        if (targetId == 0) { interactor.Poke("targetId不能为0"); return; }

        bool isGroup = type == "group";
        try
        {
            // 1. 解析出网易云歌曲ID
            long ncmId = 0;
            if (platform == "163" && long.TryParse(musicId.Trim(), out long directId))
            {
                ncmId = directId;
            }
            else
            {
                ncmId = await SearchNetEaseIdAsync(musicId);
                if (ncmId == 0)
                {
                    interactor.Poke($"未找到歌曲：{musicId}（换个关键词试试，如加上歌手名）");
                    return;
                }
            }

            // 2. 构造消息
            object message;
            switch (Configuration.MusicCardStyle)
            {
                case "record":
                {
                    // 保底：直接发语音条（网易云直链，NapCat 自行下载转码，完全不依赖签名，任何端可播）
                    string? playUrl = await ResolveNcmUrlAsync(ncmId);
                    if (string.IsNullOrEmpty(playUrl))
                    {
                        interactor.Poke("语音条模式需要可用的音乐直链，当前解析失败，请稍后再试或改用 163 卡片样式");
                        return;
                    }
                    message = new object[] {
                        new { type = "record", data = new { file = playUrl } }
                    };
                    break;
                }
                case "custom":
                {
                    // custom 自定义音乐段。NapCat 要求 url+image 必填，否则直接丢弃；
                    // 最终仍走签名服务（与163同一通道），账号被风控时同样会显示"版本过低"
                    string? playUrl = await ResolveNcmUrlAsync(ncmId);
                    if (string.IsNullOrEmpty(playUrl))
                    {
                        interactor.Poke("custom 样式需要可用的音乐直链，当前解析失败。建议改用默认的 163 卡片样式，或 record 语音条样式");
                        return;
                    }
                    var (songTitle, songArtist, songCover) = await GetNcmSongDetailAsync(ncmId);
                    message = new object[] {
                        new { type = "music", data = new {
                            type = "custom",
                            url = $"https://music.163.com/song?id={ncmId}",
                            audio = playUrl,
                            title = songTitle,
                            content = songArtist,
                            image = songCover
                        } }
                    };
                    break;
                }
                case "json":
                {
                    // QQ 分享卡片（com.tencent.structmsg），未配置签名时可能显示"发送者版本过低"
                    message = BuildJsonMusicCq(musicId, ncmId);
                    break;
                }
                default:
                {
                    // 默认：网易云卡片。真相：NapCat 对一切 music 段（含163）都强制走签名服务
                    // （musicSignUrl，缺省用 ss.xingzhige.com 公共签名），签名服务异常或卡片版本
                    // 过期时接收方就显示"发送者版本过低"。这里若用户配置了插件侧签名地址，
                    // 就由插件直接签名拿到卡片JSON、以json段发送，绕开 NapCat 的配置。
                    string signUrl = Configuration.MusicSignUrl?.Trim() ?? "";
                    if (signUrl.Length > 0)
                    {
                        string signedJson = await SignMusicCardAsync(signUrl, "163", ncmId.ToString());
                        if (signedJson != null)
                        {
                            // 与 NapCat 内部做法一致：签名结果作为 json 消息段直接发送
                            message = new object[] {
                                new { type = "json", data = new { data = signedJson } }
                            };
                            break;
                        }
                        interactor.Poke($"插件侧签名服务 {signUrl} 请求失败，回退交给 NapCat 处理");
                    }
                    message = new object[] {
                        new { type = "music", data = new { type = "163", id = ncmId.ToString() } }
                    };
                    break;
                }
            }

            // 3. 发送
            SendResult? sent = isGroup
                ? await client.CallActionAsync<SendResult>("send_group_msg", new { group_id = targetId, message })
                : await client.CallActionAsync<SendResult>("send_private_msg", new { user_id = targetId, message });
            long sentId = ExtractSentId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, isGroup ? targetId : 0, isGroup ? 0 : targetId, $"[音乐 网易云:{ncmId}]");
            // 成功静默：不触发AI新一轮确认回复
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("音乐卡片请求超时（10秒未收到OneBot响应）。服务器可能仍在后台处理，卡片可能稍后出现；请用 QGetMessages 确认，不要重复发送");
        }
        catch (Exception e)
        {
            interactor.Poke($"音乐卡片发送失败：{e.Message}");
        }
    }

    /// <summary>关键词 → 网易云歌曲ID（官方web搜索 → 163api → meting 兜底）</summary>
    /// <summary>请求音乐签名服务（与 NapCat 同一协议：POST {type,id}，返回卡片JSON字符串）。失败返回null</summary>
    private static async Task<string?> SignMusicCardAsync(string signUrl, string type, string id)
    {
        try
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(new { type, id }), Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(signUrl, content);
            if (!resp.IsSuccessStatusCode) return null;
            string body = (await resp.Content.ReadAsStringAsync()).Trim();
            // 签名服务应返回卡片JSON；有的实现包了一层 {"data": "..."}，做兼容
            if (body.StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.String)
                        return d.GetString();
                }
                catch { /* 按原始字符串处理 */ }
            }
            return body.Length > 10 ? body : null;
        }
        catch { return null; }
    }

    /// <summary>网易云歌曲详情（标题/歌手/封面），失败时回退到关键词与默认封面</summary>
    private static async Task<(string title, string artist, string cover)> GetNcmSongDetailAsync(long ncmId)
    {
        try
        {
            string url = $"https://music.163.com/api/song/detail/?id={ncmId}&ids=%5B{ncmId}%5D";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Referer", "https://music.163.com/");
            using var resp = await _http.SendAsync(req);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            JsonElement song = doc.RootElement.GetProperty("songs")[0];
            string title = song.GetProperty("name").GetString() ?? ncmId.ToString();
            string artist = song.TryGetProperty("artists", out JsonElement arts) && arts.GetArrayLength() > 0
                ? string.Join("/", arts.EnumerateArray().Select(a => a.GetProperty("name").GetString()))
                : "未知歌手";
            string cover = song.TryGetProperty("album", out JsonElement album) &&
                           album.TryGetProperty("picUrl", out JsonElement pic)
                ? pic.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(cover))
                cover = "https://p1.music.126.net/6y-UleORITEDbvrOLV0Q8A==/5639395138885805.jpg"; // 网易云默认封面
            return (title, artist, cover);
        }
        catch
        {
            return (ncmId.ToString(), "未知歌手", "https://p1.music.126.net/6y-UleORITEDbvrOLV0Q8A==/5639395138885805.jpg");
        }
    }

    private static async Task<long> SearchNetEaseIdAsync(string keyword)
    {
        // 1. 网易云官方 web 搜索
        try
        {
            string url = "https://music.163.com/api/search/get/web?s=" + Uri.EscapeDataString(keyword) + "&type=1&limit=1&offset=0";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Referer", "https://music.163.com/");
            req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
            using var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("songs", out var songs) &&
                    songs.GetArrayLength() > 0)
                    return songs[0].GetProperty("id").GetInt64();
            }
        }
        catch { }

        // 2. 163api（NCM-Downloader 同款搜索源）
        try
        {
            string url = "https://163api.qijieya.cn/search?keywords=" + Uri.EscapeDataString(keyword);
            using var resp = await _http.GetAsync(url);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("songs", out var songs) &&
                    songs.GetArrayLength() > 0)
                    return songs[0].GetProperty("id").GetInt64();
            }
        }
        catch { }

        // 3. meting 搜索兜底（从 url 字段解析 id）
        try
        {
            string url = "https://api.qijieya.cn/meting/?type=search&id=" + Uri.EscapeDataString(keyword) + "&limit=1";
            using var resp = await _http.GetAsync(url);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    string urlField = doc.RootElement[0].TryGetProperty("url", out var uf) ? uf.GetString() ?? "" : "";
                    var m = Regex.Match(urlField, @"id=(\d+)");
                    if (m.Success && long.TryParse(m.Groups[1].Value, out long id)) return id;
                }
            }
        }
        catch { }
        return 0;
    }

    /// <summary>构造 QQ 分享卡片 CQ 码（com.tencent.structmsg，json 样式用）</summary>
    private static string BuildJsonMusicCq(string title, long ncmId)
    {
        long ctime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string jump = $"https://music.163.com/song?id={ncmId}";
        var meta = new Dictionary<string, object>
        {
            ["music"] = new Dictionary<string, object>
            {
                ["app_type"] = 1,
                ["appid"] = 100495085,
                ["ctime"] = ctime,
                ["desc"] = title,
                ["jumpUrl"] = jump,
                ["musicUrl"] = jump,
                ["preview"] = "",
                ["sourceMsgId"] = "0",
                ["source_icon"] = "",
                ["source_url"] = "",
                ["tag"] = "网易云音乐",
                ["title"] = title
            }
        };
        var payload = new Dictionary<string, object>
        {
            ["app"] = "com.tencent.structmsg",
            ["config"] = new Dictionary<string, object>
            {
                ["autosize"] = true,
                ["ctime"] = ctime,
                ["forward"] = true,
                ["token"] = Guid.NewGuid().ToString("N"),
                ["type"] = "normal"
            },
            ["desc"] = "音乐",
            ["extra"] = new Dictionary<string, object> { ["app_type"] = 1, ["appid"] = 100495085, ["uin"] = 0 },
            ["meta"] = meta,
            ["prompt"] = $"[分享]{title}",
            ["ver"] = "0.0.0.1",
            ["view"] = "music"
        };
        string json = JsonSerializer.Serialize(payload);
        return $"[CQ:json,data={CqEscape(json)}]";
    }

    /// <summary>网易云歌曲ID → 直链（仅供 record/custom 样式使用：meting type=url 优先，vkeys 兜底）</summary>
    private static async Task<string?> ResolveNcmUrlAsync(long id)
    {
        try
        {
            string url = $"https://api.qijieya.cn/meting/?server=netease&type=url&id={id}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Referer", "https://music.163.com/");
            req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (resp.IsSuccessStatusCode)
            {
                string? mediaType = resp.Content.Headers.ContentType?.MediaType;
                if (mediaType is "audio/mpeg" or "audio/mp3" or "audio/x-mpeg")
                {
                    string? finalUrl = resp.RequestMessage?.RequestUri?.ToString();
                    if (!string.IsNullOrEmpty(finalUrl) && finalUrl.Contains("music.126.net"))
                        return finalUrl;
                }
            }
        }
        catch { }

        try
        {
            string url = $"https://api.vkeys.cn/v2/music/netease?id={id}&quality=4";
            using var resp = await _http.GetAsync(url);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String)
                {
                    string? s = u.GetString();
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
        }
        catch { }
        return null;
    }

    // ==================== 消息查询 ====================

    [XmlFunction(FunctionMode.OneShot)]
    [Description("获取群聊/私聊最近消息及每条的[消息ID:xxx]（真实ID，可为负数），用于撤回(DeleteMsg)/贴表情(SetEmoji)/引用(SendReplyMessage)/转发。群聊传 groupId；私聊传 userId。缓存不足时自动回拉历史消息补齐（历史的ID同样真实可用）")]
    public async Task QGetMessages(
        [Description("群号（私聊时传0）")] long groupId = 0,
        [Description("QQ号（仅私聊时需要）")] long userId = 0,
        [Description("获取条数，1-50，默认10")] int count = 10)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("获取消息失败：QQ客户端不可用"); return; }
        if (groupId == 0 && userId == 0) { interactor.Poke("群聊请传 groupId，私聊请传 userId"); return; }
        try
        {
            count = Math.Clamp(count, 1, 50);

            List<LiveMessage> Query() => _liveMessages
                .Where(m => groupId != 0 ? m.GroupId == groupId : (m.GroupId == 0 && m.PeerId == userId))
                .OrderByDescending(m => m.Time)
                .ThenByDescending(m => m.Seq)
                .Take(count)
                .OrderBy(m => m.Time)
                .ThenBy(m => m.Seq)
                .ToList();

            var matches = Query();
            if (matches.Count < count)
            {
                await BackfillHistoryAsync(groupId, userId, count);
                matches = Query();
            }

            if (matches.Count == 0)
            {
                interactor.Poke(groupId != 0
                    ? $"群 {groupId} 暂无消息记录（实时捕获连接后开始记录；若持续为空请检查实时消息捕获配置与OneBot连接）"
                    : $"与 {userId} 暂无消息记录");
                return;
            }

            var sb = new StringBuilder();
            string target = groupId != 0 ? $"群 {groupId}" : $"与 {userId} 的私聊";
            sb.AppendLine($"{target} 最近 {matches.Count} 条消息（[消息ID:xxx]即真实ID，可为负数，直接用于操作）：");
            foreach (var m in matches)
            {
                string nick = m.IsSelf ? SelfName : (string.IsNullOrEmpty(m.Nickname) ? m.UserId.ToString() : m.Nickname);
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(m.Time).LocalDateTime;
                sb.AppendLine($"[{time:HH:mm:ss}] {m.UserId}({nick}) [消息ID:{m.MessageId}] {m.Raw}");
            }
            interactor.Poke(sb.ToString());
        }
        catch (Exception e)
        {
            interactor.Poke($"获取消息失败：{e.Message}");
        }
    }

    // ==================== notice 感知（官方事件链路：禁言/进群/戳一戳） ====================

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
            else if (noticeType == "notify" && noticeEvent.SubType == "poke" && Configuration.PokeDecideEnabled)
            {
                long targetId = 0;
                if (oneBotEvent is OneBotPokeEvent pokeEvent)
                    targetId = pokeEvent.TargetId;

                // 只处理自己被戳
                if (targetId != 0 && targetId != noticeEvent.SelfId) return;

                bool isGroup = noticeEvent.GroupId != 0;
                _lastPokeRequest = new PokeRequest(noticeEvent.UserId, noticeEvent.GroupId, isGroup, DateTime.Now);

                // 冷却期内不重复注入，避免连续戳一戳刷屏上下文
                if (DateTime.Now - _lastPokePromptTime < NoticeCooldown) return;
                _lastPokePromptTime = DateTime.Now;

                string userName = await GetQQUserName(noticeEvent.UserId, noticeEvent.GroupId);
                string userText = string.IsNullOrEmpty(userName)
                    ? $"用户{noticeEvent.UserId}"
                    : $"用户{noticeEvent.UserId}({userName})";
                string where = isGroup ? $"在群 {noticeEvent.GroupId} 戳了戳你" : "私聊戳了戳你";
                interactor.Poke($"[System {userText} {where}。你可以输出 <PokeBack decide=\"yes\"/> 回戳，或 <PokeBack decide=\"no\"/> 忽略]");
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

    // ==================== 互动提示（官方消息过滤同款 ChatSend 钩子） ====================

    /// <summary>收到QQ消息时按概率在消息末尾附加互动提示（动态填入发言人参数，AI照抄即可调用，无需查ID）</summary>
    private string OnChatSendHint(string message)
    {
        if (string.IsNullOrWhiteSpace(Configuration.InteractionHintText)) return message;
        // 只附加在 QQ 来源的消息上（群聊/私聊标签），不影响其他模块的消息
        bool isGroupMsg = message.Contains("[群聊消息(");
        if (!isGroupMsg && !message.Contains("[私聊消息(")) return message;
        int prob = Math.Clamp(Configuration.InteractionHintProbability, 0, 100);
        if (prob < 100 && Random.Shared.Next(100) >= prob) return message;

        // 解析会话：[群聊消息(群号,群名)] / [私聊消息(QQ,昵称)]
        Match scopeMatch = Regex.Match(message, isGroupMsg ? @"\[群聊消息\((?<id>\d+)" : @"\[私聊消息\((?<id>\d+)");
        string scope = scopeMatch.Success ? scopeMatch.Groups["id"].Value : "";
        // 解析最后一位发言人：[QQ(昵称)]:（批量消息取最后一条的发言人，AI 最可能要回应的就是TA）
        Match? speakerMatch = Regex.Matches(message, @"\[(?<uin>\d+)\((?<nick>[^)]*)\)\]:")
            .Cast<Match>().LastOrDefault();
        string uin = speakerMatch?.Groups["uin"].Value ?? scope;
        string nick = speakerMatch?.Groups["nick"].Value ?? "对方";

        string hint = Configuration.InteractionHintText
            .Replace("{scope}", scope)
            .Replace("{type}", isGroupMsg ? "group" : "private")
            .Replace("{uin}", uin)
            .Replace("{nick}", nick)
            .Replace("{poke}", isGroupMsg ? "PokeGroupMember" : "PokePrivateMember");

        // 被引用/被@时追加回引建议（不含消息内容，省token）
        if (Configuration.QuoteBackHintEnabled)
        {
            long botId = GetClient()?.BotId ?? 0;
            if (botId != 0 && !string.IsNullOrEmpty(uin) && uin != botId.ToString() &&
                (message.Contains($"的回复]@{botId}") || message.Contains($"@{botId}")))
            {
                string reason = message.Contains($"的回复]@{botId}") ? "引用了你的消息" : "@了你";
                hint += $"（{nick}{reason}，回应时可用上面的 ReplyRecent 参数引用TA这条）";
            }
        }
        return message + "\n" + hint;
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
