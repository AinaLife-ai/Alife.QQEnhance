using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
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

    [DisplayName("撤回成功回执")]
    [Description("开启后撤回请求被接受会推送确认消息；默认关闭=成功静默，只有 delete_msg 直接报错才提示（NapCat 平台不给真实撤回回执，本配置只是接受确认）")]
    public bool RecallConfirmEnabled { get; set; } = false;

    [DisplayName("撤回核验延迟(秒)-已废弃")]
    [Description("已废弃：历史接口读NapCat本地库无法核验撤回结果，后台核验已移除，此配置不再生效")]
    public double RecallVerifyDelaySeconds { get; set; } = 1.0;

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
    [Description("感知资料卡被点赞并提示AI可回赞（走官方连接事件，无需额外上报），默认关闭")]
    public bool PerceiveProfileLike { get; set; } = false;

    [DisplayName("被贴表情感知")]
    [Description("感知群消息被贴表情并提示AI（走官方连接事件，无需额外上报），默认关闭")]
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

    [DisplayName("实时消息捕获(已废弃)")]
    [Description("已废弃：4.9.11起不再开第二条WS，所有功能建立在官方连接+历史回拉之上。此开关无效，仅为兼容旧配置保留")]
    public bool LiveCaptureEnabled { get; set; } = true;

    [DisplayName("实时消息缓存大小")]
    [Description("缓存最近N条消息的消息ID/内容，用于QGetMessages/ReplyRecent等定位")]
    public int LiveCacheSize { get; set; } = 500;

    [DisplayName("捕获连接地址(已废弃)")]
    [Description("已废弃：4.9.11起不再使用，仅为兼容旧配置保留")]
    public string CaptureUrl { get; set; } = "";

    [DisplayName("捕获连接Token(已废弃)")]
    [Description("已废弃：4.9.11起不再使用，仅为兼容旧配置保留")]
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

    /// <summary>QGetMessages 防抖：每个会话的查询时间戳</summary>
    private readonly ConcurrentDictionary<string, List<DateTime>> _qgetTimes = new();
    private readonly object _qgetLock = new();

    /// <summary>bot 真实昵称（转发/显示用），获取失败后退回"我"</summary>
    private string SelfName => _botNickname ?? "我";

    private DateTime _botNicknameLastTry = DateTime.MinValue;

    /// <summary>拉取 bot 昵称（get_login_info）。启动时可能 WS 未连接导致失败，所以带 60 秒节流的重试：
    /// 每次转发/查询用到 SelfName 前都会再试一次，直到成功</summary>
    private async Task FetchBotNicknameAsync(OneBotClient client)
    {
        if (_botNickname != null) return;
        if (DateTime.Now - _botNicknameLastTry < TimeSpan.FromSeconds(60)) return;
        _botNicknameLastTry = DateTime.Now;
        try
        {
            var info = await client.CallActionAsync<LoginInfoResult>("get_login_info");
            if (!string.IsNullOrWhiteSpace(info?.Nickname)) _botNickname = info!.Nickname;
        }
        catch { /* 忽略，用"我"兜底，60秒后重试 */ }
    }

    private sealed class LoginInfoResult
    {
        [JsonPropertyName("nickname")]
        public string? Nickname { get; init; }
    }

    private long GetBotId()
    {
        OneBotClient? client = GetClient();
        return client?.BotId ?? 0;
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

    // ==================== 消息缓存（历史回拉+发送自存，不依赖任何事件上报） ====================
    private sealed class LiveMessage
    {
        public long MessageId { get; init; }
        public long UserId { get; init; }
        public long GroupId { get; init; }
        /// <summary>私聊会话对端QQ（群聊为0）。私聊筛选必须用这个字段而不是UserId（bot自己发的消息UserId=BotId）</summary>
        public long PeerId { get; init; }
        public string Nickname { get; init; } = "";
        public string Raw { get; init; } = "";
        /// <summary>完整原文（含 [CQ:image,file=url] 等完整CQ码），专供合并转发节点用——NapCat 会解析CQ码重发真实图片/语音/表情。AI展示用 Raw（占位符省token）</summary>
        public string FullRaw { get; init; } = "";
        /// <summary>含文件/嵌套转发/引用/卡片/音乐等无法CQ重建的结构化段——转发时必须用id节点（NapCat按真实ID取原消息，结构原样保留）</summary>
        public bool IdNodeOnly { get; init; }
        /// <summary>已确认被撤回（内容保留在缓存中作存档，列表标注【已撤回】；撤回/贴表情/引用定位跳过，转发降级为内容节点）</summary>
        public bool IsRecalled { get; set; }
        public long Time { get; init; }
        public bool IsSelf { get; init; }
        public long Seq { get; init; }
    }

    private long _liveSeq;
    private readonly ConcurrentQueue<LiveMessage> _liveMessages = new();
    private readonly ConcurrentDictionary<long, LiveMessage> _liveById = new();

    // ==================== 撤回确认跟踪 ====================
    // NapCat 对 delete_msg 可能返回成功（retcode 0）但 QQ 实际拒绝撤回（超2分钟时限等），
    // 本插件不依赖任何事件上报——撤回后经历史记录比对核验，确认成功的在此登记
    /// <summary>已确认撤回的消息ID（mid -> 撤回unix秒），用于缓存存档标记与列表标注</summary>
    private readonly ConcurrentDictionary<long, long> _recalledIds = new();

    /// <summary>登记一条已撤回消息：保留在缓存中作存档但打上标记（列表标注【已撤回】），定时修剪</summary>
    private void MarkRecalled(long mid)
    {
        if (mid == 0) return;
        _recalledIds[mid] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (_liveById.TryGetValue(mid, out LiveMessage? known)) known.IsRecalled = true;
        if (_recalledIds.Count > 2000)
        {
            long cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
            foreach (KeyValuePair<long, long> kv in _recalledIds)
                if (kv.Value < cutoff) _recalledIds.TryRemove(kv.Key, out _);
        }
    }

    /// <summary>是否未被撤回（所有缓存查询的统一过滤条件）</summary>
    private bool NotRecalled(LiveMessage m) => !_recalledIds.ContainsKey(m.MessageId);
    private DateTime _lastLikePromptTime = DateTime.MinValue;
    private DateTime _lastEmojiLikePromptTime = DateTime.MinValue;
    private TimeSpan NoticeCooldown => TimeSpan.FromSeconds(Math.Max(1, Configuration.NoticeCooldownSeconds));

    private void AddLiveMessage(LiveMessage msg)
    {
        if (msg.MessageId == 0) return;
        if (_recalledIds.ContainsKey(msg.MessageId)) msg.IsRecalled = true; // 已确认撤回的消息保留入缓存并标注（存档）
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

    /// <summary>无法用CQ码文本重建、转发必须走id节点的消息段类型</summary>
    private static readonly HashSet<string> _idNodeSegmentTypes = new() { "file", "forward", "reply", "json", "music" };

    /// <summary>提取完整原文：raw_message 本身含完整CQ码直接用；否则遍历消息段数组重建完整CQ码（图片/语音/视频带URL，表情带id），供合并转发节点原样重发富媒体。
    /// idNodeOnly=true 表示含文件/嵌套转发/引用/卡片/音乐段，转发时必须用id节点</summary>
    private static (string text, bool idNodeOnly) ExtractFullText(JsonElement root)
    {
        bool idOnly = false;
        if (root.TryGetProperty("message", out var me) && me.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement seg in me.EnumerateArray())
            {
                if (seg.ValueKind == JsonValueKind.Object && _idNodeSegmentTypes.Contains(ReadPropString(seg, "type")))
                { idOnly = true; break; }
            }
        }
        string raw = ReadPropString(root, "raw_message");
        if (!string.IsNullOrEmpty(raw)) return (raw, idOnly);
        if (me.ValueKind == JsonValueKind.Undefined) return ("", idOnly);
        if (me.ValueKind == JsonValueKind.String) return (me.GetString() ?? "", idOnly);
        return (SegmentArrayToFullText(me), idOnly);
    }

    /// <summary>消息段数组 → 完整CQ码文本（富媒体保留 url/id，NapCat 解析后可原样重发）</summary>
    private static string SegmentArrayToFullText(JsonElement segments)
    {
        var parts = new List<string>();
        foreach (JsonElement seg in segments.EnumerateArray())
        {
            if (seg.ValueKind != JsonValueKind.Object) continue;
            string type = ReadPropString(seg, "type");
            if (!seg.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object) continue;
            // 取媒体地址：url 优先，其次 file（可能是URL或本地路径）
            string MediaUrl() => ReadPropString(data, "url") is { Length: > 0 } u ? u : ReadPropString(data, "file");
            switch (type)
            {
                case "text": parts.Add(ReadPropString(data, "text")); break;
                case "at": parts.Add($"[CQ:at,qq={ReadPropString(data, "qq")}]"); break;
                case "face": parts.Add($"[CQ:face,id={ReadPropString(data, "id")}]"); break;
                case "image": parts.Add($"[CQ:image,file={MediaUrl()}]"); break;
                case "record": parts.Add($"[CQ:record,file={MediaUrl()}]"); break;
                case "video": parts.Add($"[CQ:video,file={MediaUrl()}]"); break;
                case "reply": break; // 转发节点里嵌套引用无意义，跳过
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
            Nickname = SelfName, Raw = raw, FullRaw = raw, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
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
                var (fullRaw, idOnly) = ExtractFullText(m);
                AddLiveMessage(new LiveMessage {
                    MessageId = mid, UserId = uid, GroupId = gid, PeerId = peerId,
                    Nickname = nick, Raw = ExtractRawText(m), FullRaw = fullRaw, IdNodeOnly = idOnly, Time = time,
                    IsSelf = isSelf, Seq = Interlocked.Increment(ref _liveSeq),
                    IsRecalled = _recalledIds.ContainsKey(mid) // 回拉恢复存档时保留已撤回标记
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
                - 已检测到 YuYang.QQTools（幼央工具箱）接管：戳一戳、引用回复、点赞、贴表情、撤回、输入中 请调用幼央的函数；本插件负责：禁言(GroupBan)、音乐卡片(SendMusicCard)、合并转发(ForwardRecent/SendForwardById/SendForwardNew)、消息ID查询(QGetMessages)、戳回决策(PokeBack)、感知通知。
                - QQ消息ID通常是负数，编造必败；要操作某条消息先用 QGetMessages 获取真实ID。
                """
            : """
                使用规则：
                - 消息ID禁止编造，必须来自 QGetMessages 或 DeleteMsgRecent list=true 列表；贴表情/引用/撤回默认 target+index 一步到位，无需先查ID。
                - 发送类函数（引用回复/合并转发/音乐卡片）成功即已完成发送，不要再用 QChat 发重复确认；音乐卡片发送较慢，超时先 QGetMessages 确认再决定是否重发。
                - 被戳后系统会提示，用 PokeBack 回戳或忽略。
                - 贴表情 emojiId 常用：201=点赞 264=捂脸 182=笑哭 271=吃瓜 179=doge 268=问号脸；完整对照表用 SetEmojiRecent emojiId=0 查看，按需取用别总用同一个。
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

        // 始终订阅官方连接事件：引用段提取（被引用消息真实ID）、感知通知都走这里——不开第二条WS
        client.EventReceived += OnEventReceived;

        // 输入中状态：幼央接管时自动让位（避免双插件同时发 set_input_status）
        if (Configuration.TypingIndicatorEnabled && !ShouldDelegate())
        {
            ChatBot.ChatSent += OnChatSent;
            ChatBot.ChatOver += OnChatOver;
        }

        // 互动提示：挂到官方消息过滤同款钩子（ChatBot.ChatSend），收到QQ消息时按概率附加提示
        ChatBot.ChatSend += OnChatSendHint;

        _ = FetchBotNicknameAsync(client);


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
                        "ReplyRecent", "ForwardRecent", "SendForwardById", "SendForwardNew",
                        "SendMusicCard", "QGetMessages", "SetEmojiRecent", "DeleteMsgRecent",
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
    /// <summary>全缓存范围定位目标用户最近一条消息（用于 targetId 缺省时推断会话）</summary>
    private LiveMessage? FindFromUser(long scopeId, string target, bool isGroup, int index, bool includeRecalled = false)
    {
        target = target.Trim();
        bool byId = long.TryParse(target, out long targetUin);
        long botId = GetBotId();
        bool self = target is "我" or "自己" || (byId && botId != 0 && targetUin == botId);

        var candidates = _liveMessages
            .Where(m => isGroup ? m.GroupId == scopeId : (m.GroupId == 0 && m.PeerId == scopeId))
            .Where(m => includeRecalled || NotRecalled(m))
            .Where(m => self ? m.IsSelf
                : byId ? m.UserId == targetUin
                : m.Nickname.Contains(target, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .ToList();

        if (!byId && !self && candidates.Select(m => m.UserId).Distinct().Count() > 1)
            return null;
        return candidates.Skip(Math.Max(0, index - 1)).FirstOrDefault();
    }

    private LiveMessage? FindLatestFromUserAnywhere(string target)
    {
        target = target.Trim();
        bool byId = long.TryParse(target, out long targetUin);
        long botId = GetBotId();
        bool self = target is "我" or "自己" || (byId && botId != 0 && targetUin == botId);

        return _liveMessages
            .Where(NotRecalled)
            .Where(m => self ? m.IsSelf
                : byId ? m.UserId == targetUin
                : m.Nickname.Contains(target, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .FirstOrDefault();
    }

    /// <summary>定位结果解析：targetId 缺省时自动推断会话；找不到时回拉历史重试一次。
    /// includeRecalled=true 时（仅撤回功能用）候选含已撤回消息，保证与撤回候选列表序号完全一致</summary>
    private async Task<(LiveMessage? msg, bool isGroup, long scopeId, string? error)> ResolveTargetMessageAsync(
        string target, long targetId, string messageType, int index = 1, bool includeRecalled = false)
    {
        bool isGroup = messageType != "private";
        long scopeId = targetId;

        if (scopeId == 0)
        {
            LiveMessage? any = FindLatestFromUserAnywhere(target);
            // 注意：此处用最近一条定位"会话"，定位后按 index 在该会话内取倒数第N条
            if (any == null)
            {
                // 缓存没有该用户消息（典型：bot自己通过QChat发的消息不在捕获中）——
                // 先对最近活跃的会话回拉历史再查一次，实现真正的"免ID一步撤回"
                var recentScopes = _liveMessages
                    .OrderByDescending(m => m.Time).ThenByDescending(m => m.Seq)
                    .Select(m => (g: m.GroupId, p: m.PeerId))
                    .Distinct()
                    .Take(3)
                    .ToList();
                foreach (var (g, p) in recentScopes)
                    await BackfillHistoryAsync(g, g != 0 ? 0 : p, 20);
                any = FindLatestFromUserAnywhere(target);
            }
            if (any == null)
                return (null, isGroup, 0, $"未找到 {target} 的任何消息记录（缓存+历史回拉均无）。请显式传 targetId（群号或对方QQ）后重试");
            isGroup = any.GroupId != 0;
            scopeId = isGroup ? any.GroupId : any.PeerId;
        }

        // 关键：定位前无条件刷新历史——不依赖任何事件上报（bot经官方QChat发的消息不一定进缓存），
        // 仅靠缓存会拿到陈旧条目（撤回目标其实是几小时前的消息→超2分钟必失败）。历史回拉的 message_id 真实可撤回
        await BackfillHistoryAsync(isGroup ? scopeId : 0, isGroup ? 0 : scopeId, Math.Max(20, index + 10));
        LiveMessage? live = FindFromUser(scopeId, target, isGroup, index, includeRecalled);

        if (live == null)
        {
            string scope = isGroup ? $"群 {scopeId}" : $"与 {scopeId} 的私聊";
            return (null, isGroup, scopeId,
                $"未在{scope}中找到 {target} 的消息（昵称匹配到多人时也会返回此提示，请改用QQ号）。可用 QGetMessages 查列表确认");
        }
        return (live, isGroup, scopeId, null);
    }

    // ==================== 工具函数 ====================

    /// <summary>QQ官方表情ID完整对照表（SetEmojiRecent emojiId=0 查询用，不占常驻文档）</summary>
    private const string EmojiIdTable =
        "201=点赞 264=捂脸 182=笑哭 271=吃瓜 270=emm 179=doge 269=暗中观察 273=我酸了 272=呵呵哒 222=抱抱 227=拍手 246=加油抱抱 116=示爱 122=爱你 214=啵啵 219=蹭一蹭 111=可怜 106=委屈 173=泪奔 262=脑阔疼 268=问号脸 265=辣眼睛 266=哦哟 267=头秃 277=汪汪 278=汗 281=无眼笑 282=敬礼 284=面无表情 285=摸鱼 287=哦 289=睁眼 104=哈欠 109=左亲亲 118=抱拳 120=拳头 123=NO 124=OK 125=转圈 129=挥手 144=喝彩 147=棒棒糖 171=茶 174=无奈 175=卖萌 176=小纠结 180=惊喜 181=骚扰 183=我最美 203=托脸 212=托腮 232=佛系 240=喷脸 243=甩头";

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给QQ消息贴表情回应（一步到位，无需先查ID）。看到有趣/赞同/暖心/好笑的消息随手贴一个（常用：201=点赞 264=捂脸 182=笑哭 271=吃瓜 270=emm 179=doge 269=暗中观察 273=我酸了 272=呵呵哒 222=抱抱 227=拍手 246=加油抱抱 116=示爱 122=爱你 214=啵啵 219=蹭一蹭 111=可怜 106=委屈 173=泪奔 262=脑阔疼 268=问号脸 265=辣眼睛，更多可传 emojiId=0 查看完整对照表再选），这是真人最轻量的互动方式，不需要说话就可以直接贴。两种用法：1) 默认贴 target 的最近一条（index 可指定倒数第N条）；2) 已知真实消息ID时直接传 messageId（必须来自 QGetMessages 或撤回列表，严禁编造）")]
    public async Task SetEmojiRecent(
        [Description("目标用户QQ号或昵称，\"我\"表示自己（messageId 模式下可省略）")] string target = "",
        [Description("表情ID，默认201=点赞；传 0 = 不贴表情，只显示完整表情ID对照表（看完再选）")] int emojiId = 201,
        [Description("贴倒数第几条，默认1=最近一条")] int index = 1,
        [Description("真实消息ID（可选，传入则直接对该消息贴，忽略 target/index）")] long messageId = 0,
        [Description("目标群号（可省略，省略时自动推断最近会话）")] long targetId = 0,
        [Description("消息类型：group或private，可省略")] string messageType = "")
    {
        if (!Configuration.EmojiReactEnabled) { interactor.Poke("贴表情功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("贴表情", "SendEmojiLike")); return; }
        if (emojiId == 0) { interactor.Poke("QQ表情ID对照（选一个重新调用 SetEmojiRecent 并带上该 emojiId）：" + EmojiIdTable); return; }
        index = Math.Clamp(index, 1, 20);

        long mid;
        if (messageId != 0)
        {
            mid = messageId;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(target)) { interactor.Poke("请传 target（QQ号或昵称）或 messageId（真实消息ID）"); return; }
            (LiveMessage? msg, _, _, string? error) = await ResolveTargetMessageAsync(target, targetId, messageType, index);
            if (msg == null) { interactor.Poke(error!); return; }
            mid = msg.MessageId;
        }

        // 自我防护：不贴自己的消息（除非明确指定"我"）
        if (_liveById.TryGetValue(mid, out LiveMessage? known) && known.IsSelf && target is not ("我" or "自己"))
        {
            interactor.Poke("这条消息是你自己发的，不建议给自己的消息贴表情，已跳过。请选别人发的消息");
            return;
        }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("set_msg_emoji_like", new { message_id = mid, emoji_id = emojiId.ToString(), set = true }, "贴表情", client);
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
    [Description("撤回消息（一步到位，无需先查ID）。说错话、发错会话、内容有误时立刻用。三种用法：1) 默认撤 target 的倒数第 index 条；2) 已知真实ID时传 messageId 直接撤（必须来自 QGetMessages 或 list 列表，严禁编造）；3) list=true 先出候选列表（序号+消息ID+折叠内容）再用 index 或 messageId 撤。私聊只能撤自己的；群聊默认撤自己的，是管理员时可撤他人")]
    public async Task DeleteMsgRecent(
        [Description("目标群号或对方QQ（可省略，省略时自动找目标最近发言所在会话）")] long targetId = 0,
        [Description("撤回谁的消息：默认\"我\"，管理员撤群员时填对方QQ号")] string target = "我",
        [Description("消息类型：group或private，可省略")] string messageType = "",
        [Description("撤回倒数第几条：默认1=最近一条，2=倒数第二条，以此类推")] int index = 1,
        [Description("真实消息ID（可选，传入则直接撤该条，忽略 target/index/list）")] long messageId = 0,
        [Description("true=只列出 target 最近10条候选消息（不撤回），看完用 index 或 messageId 撤")] bool list = false)
    {
        if (!Configuration.DeleteMsgEnabled) { interactor.Poke("撤回功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("撤回", "DeleteMessage")); return; }
        index = Math.Clamp(index, 1, 20);

        // 模式2：直接按真实ID撤
        if (messageId != 0)
        {
            if (_liveById.TryGetValue(messageId, out LiveMessage? byId))
            {
                if (byId.IsRecalled)
                {
                    interactor.Poke($"[消息ID:{messageId}]已经是撤回状态（列表中标注【已撤回】），无需再撤，也不要再对它贴表情/引用");
                    return;
                }
                if (!byId.IsSelf && byId.GroupId == 0)
                {
                    interactor.Poke("私聊无法撤回对方的消息（平台限制），只能撤回自己发的");
                    return;
                }
            }
            await RecallByIdAsync(messageId);
            return;
        }

        // list 与默认模式都需要先定位 target 的消息（includeRecalled: 候选与列表序号完全一致，含已撤回项）
        (LiveMessage? msg, bool isGroup, long scopeId, string? error) =
            await ResolveTargetMessageAsync(target, targetId, messageType, list ? 1 : index, includeRecalled: true);

        if (list)
        {
            // 模式3：出候选列表（即使精确定位失败也尽量列出来）
            if (scopeId == 0 && msg == null) { interactor.Poke(error!); return; }
            bool g = msg != null ? isGroup : messageType != "private";
            long sc = msg != null ? scopeId : targetId;
            bool self = target is "我" or "自己";
            long botId = GetBotId();
            bool byIdUin = long.TryParse(target, out long targetUin);
            var candidates = _liveMessages
                .Where(m => g ? m.GroupId == sc : (m.GroupId == 0 && m.PeerId == sc))
                .Where(m => self ? m.IsSelf
                    : byIdUin ? (m.UserId == targetUin || (botId != 0 && targetUin == botId && m.IsSelf))
                    : m.Nickname.Contains(target, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.Time).ThenByDescending(m => m.Seq)
                .Take(10).ToList();
            if (candidates.Count == 0)
            {
                await BackfillHistoryAsync(g ? sc : 0, g ? 0 : sc, 20);
                candidates = _liveMessages
                    .Where(m => g ? m.GroupId == sc : (m.GroupId == 0 && m.PeerId == sc))
                    .Where(m => self ? m.IsSelf
                        : byIdUin ? (m.UserId == targetUin || (botId != 0 && targetUin == botId && m.IsSelf))
                        : m.Nickname.Contains(target, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.Time).ThenByDescending(m => m.Seq)
                    .Take(10).ToList();
            }
            if (candidates.Count == 0) { interactor.Poke($"未找到 {target} 的消息记录"); return; }
            var sb2 = new StringBuilder();
            sb2.AppendLine($"{target} 的最近 {candidates.Count} 条消息（撤回用：DeleteMsgRecent index=序号 或 messageId=消息ID；标注【已撤回】的不能再撤）：");
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                sb2.AppendLine($"{i + 1}. [消息ID:{c.MessageId}]{(c.IsRecalled ? "【已撤回】" : "")} {(c.IsSelf ? SelfName : c.Nickname)}: {FoldText(c.Raw)}");
            }
            interactor.Poke(sb2.ToString());
            return;
        }

        // 模式1：撤倒数第 index 条
        if (msg == null) { interactor.Poke(error!); return; }
        if (msg.IsRecalled)
        {
            interactor.Poke($"第 {index} 条 [消息ID:{msg.MessageId}]已经是撤回状态（列表中标注【已撤回】），无需再撤。要撤别的请用 list=true 看候选列表");
            return;
        }
        if (!msg.IsSelf && msg.GroupId == 0)
        {
            interactor.Poke("私聊无法撤回对方的消息（平台限制），只能撤回自己发的");
            return;
        }
        await RecallByIdAsync(msg.MessageId);
    }

    /// <summary>按真实ID撤回并回执（含折叠内容摘要）。说明：NapCat 可能对实际失败的 delete_msg 也返回成功回包，
    /// 但历史接口读的是NapCat本地库（撤回后本地记录不删），无法作为核验依据，故不做结果核验——
    /// 真实ID+115秒预拒已保证自己的消息撤回必然成功；delete_msg 报错时如实回报</summary>
    private async Task RecallByIdAsync(long mid)
    {
        OneBotClient? client = GetClient();
        _liveById.TryGetValue(mid, out LiveMessage? known);
        string preview = known != null
            ? $" {(known.IsSelf ? SelfName : known.Nickname)}: {FoldText(known.Raw)}" : "";

        // 自己的消息超约2分钟是平台硬时限，必然失败——直接如实拒绝，避免 NapCat 假成功误导
        if (known is { IsSelf: true } &&
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() - known.Time > 115)
        {
            long ageMin = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - known.Time) / 60;
            interactor.Poke($"[消息ID:{mid}]{preview} 发送于约 {ageMin} 分钟前，已超过约2分钟撤回时限（平台硬限制，任何方式都撤不掉），未执行撤回。不要再尝试撤这条");
            return;
        }

        string? err = await CallActionSafeAsync("delete_msg", new { message_id = mid }, "撤回", client);
        if (err != null)
        {
            interactor.Poke(err + "（RetCode 1200 是 NapCat 内部异常的统称，常见原因：消息超过约2分钟撤回时限、非管理员撤回他人消息、目标是卡片/合并转发类消息、或 NapCat 内存中已丢失该消息记录——超时类消息无法撤回属平台限制）");
            return;
        }

        // 撤回请求被接受：乐观标记已撤回存档（真实新鲜ID下自己的消息撤回几乎必然成功），成功默认静默
        MarkRecalled(mid);
        if (Configuration.RecallConfirmEnabled)
            interactor.Poke($"撤回请求已被 NapCat 接受 [消息ID:{mid}]{preview}。撤回已完成，无需再确认（平台无真实回执，超时/无权限等极少数情况可能实际未撤回）");
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
    [Description("引用回复（一步到位，无需先查ID）。群聊里回应特定某人时优先用它（比@更清楚）；私聊接梗/辩论时引用对方原话再回更自然。两种用法：1) 默认引用 target 的最近一条（index 可指定倒数第N条）；2) 已知真实ID时传 replyToId 直接引用该条（必须来自 QGetMessages 或撤回列表，严禁编造）")]
    public async Task ReplyRecent(
        [Description("回复内容")] string message,
        [Description("目标用户QQ号或昵称，\"我\"表示自己（replyToId 模式下可省略）")] string target = "",
        [Description("引用倒数第几条，默认1=最近一条")] int index = 1,
        [Description("被回复消息的真实ID（可选，传入则直接引用该条，忽略 target/index）")] long replyToId = 0,
        [Description("目标群号或对方QQ（可省略，省略时自动推断该用户最近发言所在会话）")] long targetId = 0,
        [Description("消息类型：group或private，可省略")] string messageType = "")
    {
        if (!Configuration.ReplyEnabled) { interactor.Poke("引用回复功能已禁用"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("引用回复", "SendReplyMessage")); return; }
        index = Math.Clamp(index, 1, 20);

        long mid; bool isGroup; long scopeId;
        if (replyToId != 0)
        {
            mid = replyToId;
            isGroup = messageType != "private";
            scopeId = targetId;
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
        }
        else
        {
            if (string.IsNullOrWhiteSpace(target)) { interactor.Poke("请传 target（QQ号或昵称）或 replyToId（真实消息ID）"); return; }
            (LiveMessage? msg, bool g, long sc, string? error) = await ResolveTargetMessageAsync(target, targetId, messageType, index);
            if (msg == null) { interactor.Poke(error!); return; }
            mid = msg.MessageId; isGroup = g; scopeId = sc;
        }

        string? result = await SendReplyCoreAsync(isGroup, scopeId, mid, message);
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
    [Description("转发某群/私聊最近N条消息为合并转发（免ID，一步到位）。文本/图片/语音/视频/表情原样转发（图片按原始URL重发为真实图片）；文件/嵌套转发/引用/卡片/音乐走真实消息ID节点，结构完整保留；含bot自己发的消息，发送者显示真实QQ昵称；缓存不足时自动回拉历史消息补齐")]
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
            .Where(NotRecalled)
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .Take(count)
            .OrderBy(m => m.Time)
            .ThenBy(m => m.Seq)
            .ToList();

        // 转发前无条件刷新历史，保证转发的是此刻最新消息（不依赖任何事件上报）
        await BackfillHistoryAsync(isGroup ? targetId : 0, isGroup ? 0 : targetId, count);
        var matches = Query();

        if (matches.Count == 0)
        {
            interactor.Poke($"{(isGroup ? $"群 {targetId}" : $"与 {targetId} 的私聊")}暂无可转发的消息记录");
            return;
        }

        // 含自己的消息且昵称未知时，先补拉 bot 昵称（启动时WS未连接会导致首次拉取失败）
        if (_botNickname == null && matches.Any(m => m.IsSelf))
        {
            OneBotClient? c0 = GetClient();
            if (c0 != null) await FetchBotNicknameAsync(c0);
        }

        // 混合节点（全自动，无需LLM决策）：
        // - 文本/图片/语音/视频/表情 → 内容节点：FullRaw 含完整 [CQ:image,file=原始URL] 等CQ码，
        //   NapCat 解析后按原始URL重新下载并发出真实媒体，不依赖服务端消息缓存，bot自己发的、历史补拉的都不会丢；
        // - 文件/嵌套转发/引用/卡片/音乐（IdNodeOnly）→ id节点：NapCat 按真实消息ID服务端取原消息，结构原样保留；
        // - 已撤回消息服务端已取不到，强制降级为内容节点（媒体仍可CQ重发，结构化段退化为占位文字）。
        var nodes = matches.Select(m =>
        {
            string nick = m.IsSelf ? SelfName : (string.IsNullOrEmpty(m.Nickname) ? m.UserId.ToString() : m.Nickname);
            if (m.IdNodeOnly && !m.IsRecalled)
                return (object)new { type = "node", data = new { id = m.MessageId.ToString(), nickname = nick, uin = m.UserId.ToString(), content = m.Raw } };
            return (object)new { type = "node", data = new { name = nick, nickname = nick, uin = m.UserId.ToString(), content = string.IsNullOrEmpty(m.FullRaw) ? m.Raw : m.FullRaw } };
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
                    if (!_liveById.TryGetValue(id, out LiveMessage? idMsg))
                    {
                        missingIds.Add(id);
                        continue;
                    }
                    // schema 要求 nickname/content 必填（缺失直接 RetCode 1400），id 有效时这两个字段被忽略
                    string idNick = idMsg.IsSelf ? SelfName : (string.IsNullOrEmpty(idMsg.Nickname) ? idMsg.UserId.ToString() : idMsg.Nickname);
                    nodes.Add(new { type = "node", data = new { id = id.ToString(), nickname = idNick, uin = idMsg.UserId.ToString(), content = idMsg.Raw } });
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
    /// <summary>折叠长文本：>15字符时显示首5...尾5（省token，AI可识别）</summary>
    private static string FoldText(string s) => s.Length <= 15 ? s : s[..5] + "..." + s[^5..];

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
    [Description("纯查询工具：获取群聊/私聊最近消息及每条的[消息ID:xxx]（真实ID，可为负数）。仅用于查看上下文或取ID，撤回/贴表情/引用回复用 DeleteMsgRecent/SetEmojiRecent/ReplyRecent 直接一步到位，无需先调本函数。群聊传 groupId；私聊传 userId。缓存不足时自动回拉历史消息补齐（历史的ID同样真实可用）。15秒内同一会话查询超过2次会被防抖拒绝")]
    public async Task QGetMessages(
        [Description("群号（私聊时传0）")] long groupId = 0,
        [Description("QQ号（仅私聊时需要）")] long userId = 0,
        [Description("获取条数，1-50，默认10")] int count = 10)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("获取消息失败：QQ客户端不可用"); return; }
        if (groupId == 0 && userId == 0) { interactor.Poke("群聊请传 groupId，私聊请传 userId"); return; }

        // 防抖保护：同一会话15秒内查询超过2次进入冷却，防止AI递归查询
        string scopeKey = groupId != 0 ? $"g{groupId}" : $"u{userId}";
        lock (_qgetLock)
        {
            DateTime now = DateTime.Now;
            var times = _qgetTimes.GetOrAdd(scopeKey, _ => new List<DateTime>());
            times.RemoveAll(t => (now - t).TotalSeconds > 15);
            if (times.Count >= 2)
            {
                interactor.Poke("查询过于频繁：该会话15秒内已查询2次，请稍后再试。列表中的[消息ID:xxx]短期内不会变化，直接用上次结果里的ID操作即可，无需重复查询");
                return;
            }
            times.Add(now);
        }
        try
        {
            count = Math.Clamp(count, 1, 50);

            List<LiveMessage> Query() => _liveMessages
                .Where(m => groupId != 0 ? m.GroupId == groupId : (m.GroupId == 0 && m.PeerId == userId))
                .Where(NotRecalled)
                .OrderByDescending(m => m.Time)
                .ThenByDescending(m => m.Seq)
                .Take(count)
                .OrderBy(m => m.Time)
                .ThenBy(m => m.Seq)
                .ToList();

            // 查询前无条件刷新历史，保证拿到的是此刻最新（不依赖任何事件上报）
            await BackfillHistoryAsync(groupId, userId, count);
            var matches = Query();

            if (matches.Count == 0)
            {
                interactor.Poke(groupId != 0
                    ? $"群 {groupId} 暂无消息记录（已尝试历史回拉也为空；请检查OneBot连接与群号是否正确）"
                    : $"与 {userId} 暂无消息记录");
                return;
            }

            var sb = new StringBuilder();
            string target = groupId != 0 ? $"群 {groupId}" : $"与 {userId} 的私聊";
            sb.AppendLine($"{target} 最近 {matches.Count} 条消息（[消息ID:xxx]即真实ID，可为负数，直接用于操作；标注【已撤回】的已不存在于QQ，仅作内容存档，不能再撤回/贴表情/引用）：");
            foreach (var m in matches)
            {
                string nick = m.IsSelf ? SelfName : (string.IsNullOrEmpty(m.Nickname) ? m.UserId.ToString() : m.Nickname);
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(m.Time).LocalDateTime;
                sb.AppendLine($"[{time:HH:mm:ss}] {m.UserId}({nick}) [消息ID:{m.MessageId}]{(m.IsRecalled ? "【已撤回】" : "")} {m.Raw}");
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
            if (noticeType == "profile_like" && Configuration.PerceiveProfileLike)
            {
                if (DateTime.Now - _lastLikePromptTime < NoticeCooldown) return;
                _lastLikePromptTime = DateTime.Now;
                long uid = noticeEvent.UserId;
                interactor.Poke($"[System 用户{uid} 赞了你的资料卡。可以回赞（SendQQLikes qq={uid}）或戳一戳回应，也可以忽略]");
            }
            else if (noticeType == "group_msg_emoji_like" && Configuration.PerceiveEmojiLike)
            {
                if (DateTime.Now - _lastEmojiLikePromptTime < NoticeCooldown) return;
                _lastEmojiLikePromptTime = DateTime.Now;
                long uid = noticeEvent.UserId;
                interactor.Poke($"[System 用户{uid} 在群 {noticeEvent.GroupId} 给你的消息贴了表情。可以贴回去（SetEmojiRecent target={uid} targetId={noticeEvent.GroupId}）或接话回应，也可以忽略]");
            }
            else if (noticeType == "group_ban" && Configuration.PerceiveGroupBan)
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
        if (!Configuration.InteractionHintEnabled) return message;
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
                (message.Contains($"的回复]@{botId}") || message.Contains($"@{botId}") ||
                 message.Contains($"对\"{botId}：")))
            {
                bool quoted = message.Contains($"的回复]@{botId}") || message.Contains($"对\"{botId}：");
                string reason = quoted ? "引用了你的消息" : "@了你";
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
            _ = RunTypingLoopAsync(userId, cts);
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

    private async Task RunTypingLoopAsync(long userId, CancellationTokenSource cts)
    {
        OneBotClient? client = GetClient();
        if (client == null) return;
        CancellationToken ct = cts.Token;

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
                if (_typingCts.TryGetValue(userId, out CancellationTokenSource? cur) && cur == cts)
                    _typingCts.Remove(userId);
            }
        }
    }
}
