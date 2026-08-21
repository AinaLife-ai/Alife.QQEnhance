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
    [DisplayName("璐磋〃鎯?)]
    [Description("鍚敤缁橯Q娑堟伅璐磋〃鎯呭姛鑳?)]
    public bool EmojiReactEnabled { get; set; } = true;

    [DisplayName("鐐硅禐")]
    [Description("鍚敤缁橯Q鐢ㄦ埛璧勬枡鍗＄偣璧炲姛鑳?)]
    public bool SendLikesEnabled { get; set; } = false;

    [DisplayName("鎾ゅ洖")]
    [Description("鍚敤鎾ゅ洖QQ娑堟伅鍔熻兘")]
    public bool DeleteMsgEnabled { get; set; } = true;

    [DisplayName("绂佽█")]
    [Description("鍚敤绂佽█QQ缇ゆ垚鍛樺姛鑳?)]
    public bool GroupBanEnabled { get; set; } = true;

    [DisplayName("闊充箰鍗＄墖")]
    [Description("鍚敤鍙戦€侀煶涔愬崱鐗囧姛鑳?)]
    public bool MusicCardEnabled { get; set; } = false;

    [DisplayName("鎴充竴鎴?)]
    [Description("鍚敤缇よ亰鎴充竴鎴虫垚鍛樺姛鑳?)]
    public bool PokeEnabled { get; set; } = true;

    [DisplayName("鎴冲洖鍐崇瓥")]
    [Description("鏀跺埌鍒汉鐨勬埑涓€鎴冲悗娉ㄥ叆鍐崇瓥鎻愮ず锛岃妯″瀷椤哄甫鍐冲畾鏄惁鍥炴埑锛圥okeBack锛夛紝涓嶅奖鍝嶆甯稿洖澶嶆瀯寤?)]
    public bool PokeDecideEnabled { get; set; } = true;

    [DisplayName("寮曠敤鍥炲")]
    [Description("鍚敤寮曠敤鍥炲娑堟伅鍔熻兘")]
    public bool ReplyEnabled { get; set; } = true;

    [DisplayName("鍚堝苟杞彂")]
    [Description("鍚敤鍚堝苟杞彂娑堟伅鍔熻兘锛堣浆鍙戝凡鏈?鏋勯€犳柊杞彂/杞彂鏈€杩戞秷鎭級")]
    public bool ForwardEnabled { get; set; } = true;

    [DisplayName("鍏煎妯″紡")]
    [Description("涓嶻uYang.QQTools锛堝辜澶伐鍏风锛夌殑鍗忎綔妯″紡锛欰uto=妫€娴嬪埌骞煎ぎ鑷姩璁╀綅閲嶅彔鍔熻兘锛汸referQQEnhance=浼樺厛鏈彃浠讹紱PreferYuYang=閲嶅彔鍔熻兘涓€寰嬭浣嶏紙骞煎ぎ鏈鏃惰嚜鍔ㄥ洖閫€鑷寔锛夛紱Off=涓嶆娴嬪叏鍔熻兘娉ㄥ唽")]
    public string CompatibilityMode { get; set; } = "Auto";

    [DisplayName("鎰熺煡缇ょ瑷€")]
    [Description("鎰熺煡鑷繁琚瑷€/瑙ｉ櫎绂佽█骞堕€氱煡AI")]
    public bool PerceiveGroupBan { get; set; } = true;

    [DisplayName("鎰熺煡鎴愬憳杩涚兢")]
    [Description("鎰熺煡鏂版垚鍛樿繘缇ゅ苟閫氱煡AI")]
    public bool PerceiveGroupIncrease { get; set; } = true;

    [DisplayName("杈撳叆涓姸鎬?)]
    [Description("绉佽亰鏃跺彂閫佽緭鍏ヤ腑鐘舵€?)]
    public bool TypingIndicatorEnabled { get; set; } = true;

    [DisplayName("杈撳叆涓欢杩?绉?")]
    [Description("鏀跺埌娑堟伅鍚庡欢杩熷涔呭紑濮嬪彂閫佽緭鍏ヤ腑鐘舵€?)]
    public double TypingDelaySeconds { get; set; } = 2.0;

    [DisplayName("杈撳叆涓棿闅?绉?")]
    [Description("杈撳叆涓姸鎬佸埛鏂伴棿闅?)]
    public double TypingIntervalSeconds { get; set; } = 2.0;

    [DisplayName("杈撳叆涓渶澶ф椂闀?绉?")]
    [Description("杈撳叆涓姸鎬佹渶澶ф寔缁椂闀?)]
    public double TypingMaxSeconds { get; set; } = 60.0;

    [DisplayName("瀹炴椂娑堟伅鎹曡幏")]
    [Description("鐙珛WS鐩戝惉瀹炴椂浜嬩欢鎹曡幏鐪熷疄娑堟伅ID锛堟挙鍥?璐磋〃鎯呭彲闈犳€ф牳蹇冿級")]
    public bool LiveCaptureEnabled { get; set; } = true;

    [DisplayName("瀹炴椂娑堟伅缂撳瓨澶у皬")]
    [Description("缂撳瓨鏈€杩慛鏉″疄鏃舵秷鎭殑娑堟伅ID/鍐呭锛岀敤浜巕getmessages鏌ヨ")]
    public int LiveCacheSize { get; set; } = 200;
}

[Module("QQ澧炲己",
    "鎻愪緵QQ璐磋〃鎯呫€佺偣璧炪€佹挙鍥炪€佺瑷€銆佹埑涓€鎴炽€佸紩鐢ㄥ洖澶嶃€佸悎骞惰浆鍙戙€侀煶涔愬崱鐗囥€佹劅鐭ラ€氱煡銆佽緭鍏ヤ腑鐘舵€佺瓑澧炲己鍔熻兘锛屾敮鎸佷笌YuYang.QQTools鑷姩鍒嗗伐",
    defaultCategory: "AinaLife/绀句氦骞冲彴")]
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

    /// <summary>骞煎ぎ宸ュ叿绠辨ā鍧楃殑瀹屾暣绫诲瀷鍚?/summary>
    private const string YuYangModuleId = "YuYang.QQTools.QQToolsModule";

    // QChatService 鏈叕寮€ OneBotClient锛岄€氳繃鍙嶅皠鑾峰彇锛堜笉淇敼瀹樻柟浠ｇ爜锛?
    private OneBotClient? GetClient()
    {
        FieldInfo? field = typeof(QChatService).GetField("oneBotClient",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(qChatService) as OneBotClient;
    }

    // ==================== 骞煎ぎ鍏煎妫€娴?====================

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
        return $"{feature}鍔熻兘鐢?YuYang.QQTools锛堝辜澶伐鍏风锛夋帴绠★紝璇疯皟鐢ㄥ辜澶殑 {yuYangFunction} 鍑芥暟";
    }

    // ==================== 缁熶竴 OneBot 璋冪敤锛堣秴鏃?澶辫触鍙嬪ソ鎻愮ず锛?====================

    private async Task<string?> CallActionSafeAsync(
        string action,
        object? @params,
        string feature,
        OneBotClient? client)
    {
        if (client == null)
            return $"{feature}澶辫触锛歈Q瀹㈡埛绔笉鍙敤";
        try
        {
            await client.CallActionAsync<object>(action, @params);
            return null; // 鎴愬姛
        }
        catch (TaskCanceledException)
        {
            return $"{feature}璇锋眰瓒呮椂锛?0绉掓湭鏀跺埌鍝嶅簲锛夈€傛搷浣滃彲鑳藉凡鐢熸晥锛岃绋嶅悗鐢?qgetmessages 妫€鏌ョ‘璁わ紝涓嶈閲嶅鎿嶄綔";
        }
        catch (Exception e)
        {
            return $"{feature}澶辫触锛歿e.Message}";
        }
    }

    // Typing indicator 鐘舵€佺鐞?
    private readonly Dictionary<long, CancellationTokenSource> _typingCts = new();
    private readonly object _typingLock = new();

    // ==================== 瀹炴椂娑堟伅鎹曡幏锛堢湡瀹炴秷鎭疘D鏉ユ簮锛?====================
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
            logger.LogInformation("QQ澧炲己锛氬疄鏃舵秷鎭崟鑾峰凡杩炴帴 {Url}", url);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "QQ澧炲己锛氬疄鏃舵秷鎭崟鑾疯繛鎺ュけ璐ワ紙qgetmessages灏嗕笉鍙敤锛屽叾浠栧姛鑳戒笉鍙楀奖鍝嶏級");
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

                // 鎻℃墜锛氱涓€涓姤鏂囨槸 lifecycle connect
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
            logger.LogDebug(e, "QQ澧炲己锛氬疄鏃舵秷鎭崟鑾峰惊鐜粨鏉?);
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

    // ==================== 鍙戦€佽嚜瀛橈紙bot 鑷繁鍙戠殑娑堟伅涔熻繘缂撳瓨锛?====================

    /// <summary>鍙戦€佺被 API 鐨勮繑鍥烇紙鍙?message_id 鑷瓨锛?/summary>
    private sealed class SendResult
    {
        [JsonPropertyName("message_id")]
        public JsonElement MessageId { get; init; }
    }

    /// <summary>鎶婃湰鎻掍欢/鏈?bot 鍙戦€佺殑娑堟伅瀛樺叆瀹炴椂缂撳瓨锛堝惈娑堟伅ID锛夛紝渚?ForwardRecent/ReplyRecent/QGetMessages 浣跨敤</summary>
    private void RecordSentMessage(long messageId, long groupId, long userId, string raw, long time)
    {
        if (messageId == 0) return;
        if (_liveMessages.Any(m => m.MessageId == messageId)) return;

        _liveMessages.Enqueue(new LiveMessage {
            MessageId = messageId, UserId = userId, GroupId = groupId,
            Nickname = "鎴?, Raw = raw, Time = time, IsSelf = true,
            Seq = Interlocked.Increment(ref _liveSeq)
        });
        int max = Math.Max(1, Configuration.LiveCacheSize);
        while (_liveMessages.Count > max)
            _liveMessages.TryDequeue(out _);
    }

    /// <summary>浠庡彂閫佺粨鏋滀腑鎻愬彇 message_id锛堝吋瀹规暟瀛?鏁板瓧瀛楃涓诧級锛屽彇涓嶅埌杩斿洖0</summary>
    private static long ExtractSentId(SendResult? result)
    {
        if (result == null) return 0;
        return result.MessageId.ValueKind == JsonValueKind.Number ? result.MessageId.GetInt64()
            : (result.MessageId.ValueKind == JsonValueKind.String && long.TryParse(result.MessageId.GetString(), out var p) ? p : 0);
    }

    // ==================== 鎴冲洖鍐崇瓥鐘舵€?====================
    private sealed record PokeRequest(long UserId, long GroupId, bool IsGroup, DateTime Time);
    private PokeRequest? _lastPokeRequest;
    private DateTime _lastPokePromptTime = DateTime.MinValue;
    private static readonly TimeSpan PokePromptCooldown = TimeSpan.FromSeconds(30);

    protected override Task OnAwake()
    {
        bool yuYangActive = IsYuYangActive();
        if (yuYangActive)
            logger.LogInformation("QQ澧炲己锛氭娴嬪埌 YuYang.QQTools 宸插惎鐢紝閲嶅彔鍔熻兘灏嗚浣嶏紙鍏煎妯″紡 {Mode}锛?, Configuration.CompatibilityMode);

        string explanation = yuYangActive
            ? $$"""
                浣跨敤瑙勫垯锛?
                - 宸叉娴嬪埌 YuYang.QQTools锛堝辜澶伐鍏风锛夋帴绠★細鎴充竴鎴炽€佸紩鐢ㄥ洖澶嶃€佺偣璧炪€佽创琛ㄦ儏銆佹挙鍥炪€佽緭鍏ヤ腑 璇疯皟鐢ㄥ辜澶殑鍑芥暟锛圥okeGroupMember/SendReplyMessage/SendLike/SendEmojiLike/DeleteMessage锛夈€?
                - 鏈彃浠惰礋璐ｏ細绂佽█(GroupBan)銆侀煶涔愬崱鐗?SendMusicCard)銆佸悎骞惰浆鍙?SendForwardById/SendForwardNew/ForwardRecent)銆佹秷鎭疘D鏌ヨ(QGetMessages)銆佹埑鍥炲喅绛?PokeBack)銆佹劅鐭ラ€氱煡銆?
                - QQ骞冲彴娑堟伅ID閫氬父鏄礋鏁帮紙濡?-1976879391锛夛紝缂栭€犳垨鐚淚D蹇呯劧澶辫触锛圧etCode 100/1400锛夈€?
                - 瑕佹挙鍥?璐磋〃鎯?寮曠敤鍥炲鏌愭潯娑堟伅锛屽繀椤诲厛璋冪敤 QGetMessages 鑾峰彇鐪熷疄ID鍒楄〃锛屼粠涓€夊彇銆?
                - QGetMessages 杩斿洖瀹炴椂鎹曡幏鐨勭湡瀹炴秷鎭疘D锛堝惈鑷繁鍒氬彂鐨勬秷鎭紝鍙戦€佹椂鑷姩璁板綍锛夈€?
                - QGetMessages 杩斿洖鏍煎紡锛歔娑堟伅ID:xxx] 鍗宠娑堟伅鐨勭湡瀹濱D锛岀洿鎺ュ師鏍蜂娇鐢ㄣ€?
                - 鑻ュ彧鎯冲洖澶嶆煇浜烘渶杩戜竴鏉℃秷鎭垨杞彂鏈€杩戝嚑鏉℃秷鎭紝鍙洿鎺ョ敤 ReplyRecent/ForwardRecent锛屾棤闇€鑷繁澶勭悊娑堟伅ID銆?
                """
            : $$"""
                浣跨敤瑙勫垯锛?
                - QQ骞冲彴娑堟伅ID鏄礋鏁帮紙濡?-1976879391锛夛紝缂栭€犳垨鐚淚D蹇呯劧澶辫触锛圧etCode 100/1400锛夈€?
                - 瑕佹挙鍥?DeleteMsg)/璐磋〃鎯?SetEmoji)/寮曠敤鍥炲(ReplyMsg)/杞彂宸叉湁鍚堝苟杞彂(SendForwardById)鏌愭潯娑堟伅锛屽繀椤诲厛璋冪敤 QGetMessages 鑾峰彇鍑嗙‘ID鍒楄〃锛屼粠涓€夊彇銆?
                - QGetMessages 杩斿洖瀹炴椂鎹曡幏鐨勭湡瀹炴秷鎭疘D锛堝惈鑷繁鍒氬彂鐨勬秷鎭紝鍙戦€佹椂鑷姩璁板綍锛夈€?
                - QGetMessages 杩斿洖鏍煎紡锛歔娑堟伅ID:xxx] 鍗宠娑堟伅鐨勭湡瀹濱D锛岀洿鎺ュ師鏍蜂娇鐢ㄣ€?
                - 寮曠敤鍥炲娑堟伅鏍煎紡锛歋endReplyMessage message="鍐呭" replyToId="鐪熷疄ID" messageType="group" targetId="缇ゅ彿鎴朡Q鍙?銆?
                - 鐪佷簨鏂瑰紡锛氬彧鎯冲洖澶嶆煇浜虹殑鏈€杩戜竴鏉℃秷鎭?鈫?鐢?ReplyRecent message="鍐呭" target="QQ鍙锋垨鏄电О" targetId="缇ゅ彿/QQ鍙?锛屼笉鐢ㄦ煡ID銆?
                - 鐪佷簨鏂瑰紡锛氭兂杞彂鏌愮兢鏈€杩慛鏉℃秷鎭负鍚堝苟杞彂 鈫?鐢?ForwardRecent groupId="缇ゅ彿" count="N"锛屼笉鐢ㄦ瀯閫燡SON銆?
                - 鍚堝苟杞彂锛歋endForwardById id=宸叉湁鍚堝苟杞彂ID锛汼endForwardNew 鏋勯€犳柊杞彂锛堜紶JSON鏁扮粍 [{"name":"鏄电О","uin":QQ鍙?"content":"鍐呭"},{"id":鐪熷疄娑堟伅ID}]锛屽繀椤诲畬鏁撮棴鍚堬級銆?
                - 鎴充竴鎴筹細PokeGroupMember 缇よ亰鎴虫垚鍛橈紱PokePrivateMember 绉佽亰鎴崇敤鎴凤紙QQ鍙凤級锛汸okeBack 鍥炲簲鏈€杩戞埑浣犵殑浜猴紙鏀跺埌鎴充竴鎴虫彁绀哄悗鍙敤锛夈€?
                - 闊充箰鍗＄墖锛歋endMusicCard锛堝彲鑳借緝鎱紝鑻ヨ秴鏃惰绋嶇敤QGetMessages纭鏄惁宸插彂鍑猴紝涓嶈閲嶅鍙戦€侊級銆?
                """;

        XmlHandler xmlHandler = new(this) {
            Description = "鎻愪緵QQ璐磋〃鎯呫€佺偣璧炪€佹挙鍥炪€佺瑷€銆佹埑涓€鎴炽€佸紩鐢ㄥ洖澶嶃€佸悎骞惰浆鍙戙€侀煶涔愬崱鐗囥€佹秷鎭疘D鏌ヨ绛夊寮哄姛鑳姐€傗殸閲嶈锛歈Q娑堟伅ID涓鸿礋鏁帮紝鎾ゅ洖/璐磋〃鎯?寮曠敤鍥炲/杞彂蹇呴』鍏堢敤getmessages鑾峰彇鐪熷疄ID锛屼弗绂佺紪閫犮€傚彟鏈塅orwardRecent/ReplyRecent鍙厤ID杞彂/鍥炲锛孭okeBack鍙洖鎴?,
            Explanation = explanation
        };
        functionCaller.RegisterHandler(xmlHandler, DocumentMode.Implicit, DestroyCancellationToken);

        OneBotClient? client = GetClient();
        if (client == null)
        {
            logger.LogWarning("鏃犳硶鑾峰彇 OneBotClient锛孮Q澧炲己鍔熻兘涓嶅彲鐢紙璇风‘璁ゅ凡鍚敤QQ鑱婂ぉ妯″潡锛?);
            return Task.CompletedTask;
        }

        if (Configuration.PerceiveGroupBan || Configuration.PerceiveGroupIncrease || Configuration.PokeDecideEnabled)
            client.EventReceived += OnEventReceived;

        // 杈撳叆涓姸鎬侊細骞煎ぎ鎺ョ鏃惰嚜鍔ㄨ浣嶏紙閬垮厤鍙屾彃浠跺悓鏃跺彂 set_input_status锛?
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

    // ==================== 宸ュ叿鍑芥暟 ====================

    [XmlFunction(FunctionMode.OneShot)]
    [Description("缁橯Q娑堟伅璐磋〃鎯呫€俥mojiId涓鸿〃鎯匢D銆傗殸锛歮essageId蹇呴』鐢╣etmessages鍙栫湡瀹濱D锛堝彲涓鸿礋鏁帮級锛屼弗绂佺紪閫犳垨鐚滄祴")]
    public async Task SetEmoji(
        [Description("娑堟伅ID锛堝繀椤绘潵鑷猤etmessages锛?)] long messageId,
        [Description("琛ㄦ儏ID锛?01涓虹偣璧?)] int emojiId)
    {
        if (!Configuration.EmojiReactEnabled) { interactor.Poke("璐磋〃鎯呭姛鑳藉凡绂佺敤"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("璐磋〃鎯?, "SendEmojiLike")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("set_msg_emoji_like", new { message_id = messageId, emoji_id = emojiId }, "璐磋〃鎯?, client);
        interactor.Poke(err ?? "璐磋〃鎯呮垚鍔?);
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("缁橯Q鐢ㄦ埛璧勬枡鍗＄偣璧?)]
    public async Task SendQQLikes(
        [Description("QQ鍙?)] long qq,
        [Description("鐐硅禐娆℃暟锛岄粯璁?0娆?)] int times = 50)
    {
        if (!Configuration.SendLikesEnabled) { interactor.Poke("鐐硅禐鍔熻兘宸茬鐢?); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("鐐硅禐", "SendLike")); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("鐐硅禐澶辫触锛歈Q瀹㈡埛绔笉鍙敤"); return; }
        try
        {
            var chunks = new List<int>();
            for (int i = 0; i < times / 10; i++) chunks.Add(10);
            if (times % 10 > 0) chunks.Add(times % 10);

            int count = 0;
            foreach (int chunk in chunks)
            {
                string? err = await CallActionSafeAsync("send_like", new { user_id = qq, times = chunk }, "鐐硅禐", client);
                if (err != null)
                {
                    interactor.Poke($"{err}锛堝凡鎴愬姛 {count} 涓禐锛?);
                    return;
                }
                count += chunk;
            }
            interactor.Poke($"鐐硅禐鎴愬姛锛岀偣浜?{count} 涓禐");
        }
        catch (Exception e)
        {
            interactor.Poke($"鐐硅禐澶辫触锛歿e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鎾ゅ洖QQ娑堟伅銆傝姹傦細messageId蹇呴』鐢╣etmessages鍙栫湡瀹濱D锛屼弗绂佺紪閫?)]
    public async Task DeleteMsg(
        [Description("娑堟伅ID锛堝繀椤绘潵鑷猤etmessages锛?)] long messageId)
    {
        if (!Configuration.DeleteMsgEnabled) { interactor.Poke("鎾ゅ洖鍔熻兘宸茬鐢?); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("鎾ゅ洖", "DeleteMessage")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("delete_msg", new { message_id = messageId }, "鎾ゅ洖", client);
        interactor.Poke(err ?? "鎾ゅ洖鎴愬姛");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("绂佽█QQ缇ゆ垚鍛樸€傜兢鍙峰彲浠庣兢娑堟伅鏍囩[缇よ亰娑堟伅(缇ゅ彿,缇ゅ悕)]涓幏鍙?)]
    public async Task GroupBan(
        [Description("缇ゅ彿")] long groupId,
        [Description("QQ鍙?)] long userId,
        [Description("绂佽█鏃堕暱(绉?锛岄粯璁?00绉掞紝0涓鸿В闄ょ瑷€")] int duration = 600)
    {
        if (!Configuration.GroupBanEnabled) { interactor.Poke("绂佽█鍔熻兘宸茬鐢?); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("set_group_ban", new { group_id = groupId, user_id = userId, duration }, "绂佽█", client);
        interactor.Poke(err ?? "绂佽█鎴愬姛");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鍦ㄧ兢鑱婁腑鎴充竴鎴虫寚瀹氭垚鍛?)]
    public async Task PokeGroupMember(
        [Description("缇ゅ彿")] long groupId,
        [Description("QQ鍙?)] long userId)
    {
        if (!Configuration.PokeEnabled) { interactor.Poke("鎴充竴鎴冲姛鑳藉凡绂佺敤"); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("鎴充竴鎴?, "PokeGroupMember")); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("group_poke", new { group_id = groupId, user_id = userId }, "鎴充竴鎴?, client);
        interactor.Poke(err ?? $"鎴愬姛鎴充簡鎴?{userId}");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鍦ㄧ鑱婁腑鎴充竴鎴虫寚瀹氱敤鎴凤紙friend_poke锛孨apCat/LLOneBot鏀寔锛?)]
    public async Task PokePrivateMember(
        [Description("QQ鍙?)] long userId)
    {
        if (!Configuration.PokeEnabled) { interactor.Poke("鎴充竴鎴冲姛鑳藉凡绂佺敤"); return; }
        OneBotClient? client = GetClient();
        string? err = await CallActionSafeAsync("friend_poke", new { user_id = userId }, "绉佽亰鎴充竴鎴?, client);
        interactor.Poke(err ?? $"鎴愬姛绉佽亰鎴充簡鎴?{userId}");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鍥炲簲鏈€杩戜竴娆℃埑浣犵殑浜猴細鍥炴埑鎴栧拷鐣ャ€傚綋绯荤粺鎻愮ず浣犺鎴充簡鏃惰皟鐢ㄣ€俤ecide=\"yes\"鍥炴埑锛沝ecide=\"no\"蹇界暐銆傚彧鐢ㄤ簬鍥炲簲鎴充竴鎴筹紝涓嶇敤浜庝富鍔ㄦ埑浜?)]
    public async Task PokeBack(
        [Description("yes=鍥炴埑锛宯o=蹇界暐")] string decide = "yes")
    {
        if (!Configuration.PokeDecideEnabled || !Configuration.PokeEnabled) { interactor.Poke("鎴冲洖鍔熻兘宸茬鐢?); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("鎴充竴鎴?, "PokeGroupMember")); return; }

        if (decide != "yes")
        {
            _lastPokeRequest = null;
            interactor.Poke("宸插拷鐣ヨ繖娆℃埑涓€鎴?);
            return;
        }

        if (_lastPokeRequest == null)
        {
            interactor.Poke("娌℃湁寰呭洖搴旂殑鎴充竴鎴筹紙鍙兘宸茶繃鏈熸垨宸插鐞嗭級");
            return;
        }

        var req = _lastPokeRequest;
        if ((DateTime.Now - req.Time) > TimeSpan.FromMinutes(10))
        {
            _lastPokeRequest = null;
            interactor.Poke("鎴充竴鎴宠姹傚凡杩囨湡锛屼笉鍥炴埑");
            return;
        }

        OneBotClient? client = GetClient();
        string? err;
        if (req.IsGroup)
            err = await CallActionSafeAsync("group_poke", new { group_id = req.GroupId, user_id = req.UserId }, "鎴充竴鎴?, client);
        else
            err = await CallActionSafeAsync("friend_poke", new { user_id = req.UserId }, "绉佽亰鎴充竴鎴?, client);

        _lastPokeRequest = null;
        interactor.Poke(err ?? "鎴愬姛鍥炴埑");
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("杞彂鎸囧畾缇よ亰鏈€杩慛鏉℃秷鎭负鍚堝苟杞彂娑堟伅銆傛棤闇€浼犳秷鎭疘D锛屾彃浠惰嚜鍔ㄤ粠瀹炴椂缂撳瓨鍙栫湡瀹炴秷鎭紙鍚玝ot鑷繁鍙戠殑娑堟伅锛屽彂閫佹椂鑷姩璁板綍锛涚函鍥剧墖/璇煶娈佃嚜鍔ㄨ烦杩囷級銆俢ount瓒呰繃缂撳瓨鍙敤鏁版椂鎸夊彲鐢ㄦ暟杞?)]
    public async Task ForwardRecent(
        [Description("缇ゅ彿")] long groupId,
        [Description("杞彂鏉℃暟锛?-50锛岄粯璁?")] int count = 5)
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("鍚堝苟杞彂鍔熻兘宸茬鐢?); return; }
        if (groupId == 0) { interactor.Poke("groupId涓嶈兘涓?锛堢兢鍙风己澶憋級"); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("鍚堝苟杞彂澶辫触锛歈Q瀹㈡埛绔笉鍙敤"); return; }

        count = Math.Clamp(count, 1, 50);
        var matches = _liveMessages
            .Where(m => m.GroupId == groupId && !string.IsNullOrEmpty(m.Raw))
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .Take(count)
            .OrderBy(m => m.Time)
            .ThenBy(m => m.Seq)
            .ToList();

        if (matches.Count == 0)
        {
            interactor.Poke($"缇?{groupId} 鏆傛棤瀹炴椂鎹曡幏鐨勬秷鎭紙鎹曡幏杩炴帴鍚庡紑濮嬭褰曪紝璇风◢鍚庨噸璇曟垨鏀圭敤 SendForwardNew 鎵嬪姩鏋勯€狅級");
            return;
        }

        var nodes = new List<object>();
        foreach (var m in matches)
        {
            string content = OneBotSegment.FilterFace(OneBotSegment.FilterAt(OneBotSegment.FilterImage(OneBotSegment.FilterRecord(m.Raw))));
            if (string.IsNullOrWhiteSpace(content)) continue;
            string name = string.IsNullOrEmpty(m.Nickname) ? (m.IsSelf ? "鎴? : m.UserId.ToString()) : m.Nickname;
            nodes.Add(new { type = "node", data = new { name, uin = m.UserId, content } });
        }

        if (nodes.Count == 0)
        {
            interactor.Poke("缂撳瓨涓殑娑堟伅鏃犲彲杞彂鏂囨湰鍐呭锛堝彲鑳芥槸绾浘鐗?璇煶锛夛紝璇风◢鍚庨噸璇?);
            return;
        }

        try
        {
            var sent = await client.CallActionAsync<SendResult>("send_group_forward_msg", new { group_id = groupId, messages = nodes });
            long sentId = ExtractSentId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, groupId, 0, $"[CQ:forward,id={sentId}]", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            interactor.Poke($"鍚堝苟杞彂鍙戦€佹垚鍔燂紙{nodes.Count} 涓妭鐐癸級");
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("鍚堝苟杞彂璇锋眰瓒呮椂锛?0绉掞級锛屽彲鑳藉凡鍙戦€佹垚鍔燂紝璇风◢鍚庣‘璁わ紝涓嶈閲嶅鍙戦€?);
        }
        catch (Exception e)
        {
            interactor.Poke($"鍚堝苟杞彂澶辫触锛歿e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("寮曠敤鍥炲鎸囧畾鐢ㄦ埛鐨勬渶杩戜竴鏉℃秷鎭€倀arget涓鸿鐢ㄦ埛鐨凲Q鍙锋垨鏄电О锛屼笉闇€瑕佷紶娑堟伅ID锛屾彃浠朵粠瀹炴椂缂撳瓨鑷姩瀹氫綅锛堝惈bot鑷繁鍙戠殑娑堟伅锛屽彂閫佹椂鑷姩璁板綍锛夈€傛壘涓嶅埌鏃惰繑鍥炴彁绀?)]
    public async Task ReplyRecent(
        [Description("鍥炲鍐呭")] string message,
        [Description("鐩爣鐢ㄦ埛QQ鍙锋垨鏄电О")] string target,
        [Description("娑堟伅绫诲瀷锛歡roup鎴杙rivate")] string messageType = "group",
        [Description("鐩爣缇ゅ彿锛坓roup锛夋垨QQ鍙凤紙private锛?)] long targetId = 0)
    {
        if (!Configuration.ReplyEnabled) { interactor.Poke("寮曠敤鍥炲鍔熻兘宸茬鐢?); return; }
        if (ShouldDelegate()) { interactor.Poke(DelegateHint("寮曠敤鍥炲", "SendReplyMessage")); return; }
        if (targetId == 0) { interactor.Poke("targetId涓嶈兘涓?"); return; }

        bool isGroup = messageType == "group";
        var live = FindLatestFromUser(targetId, target, isGroup);
        if (live == null)
        {
            string scope = isGroup ? $"缇?{targetId}" : $"涓?{targetId} 鐨勭鑱?;
            interactor.Poke($"鏈湪{scope}涓壘鍒?{target} 鐨勬渶杩戞秷鎭紙鍙兘涓嶅湪瀹炴椂缂撳瓨涓紝鏀圭敤 QGetMessages 鏌D鍐?SendReplyMessage锛?);
            return;
        }

        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("寮曠敤鍥炲澶辫触锛歈Q瀹㈡埛绔笉鍙敤"); return; }
        try
        {
            object @params;
            if (isGroup)
                @params = new { message_type = "group", group_id = targetId, message, reply = new { message_id = live.MessageId } };
            else
                @params = new { message_type = "private", user_id = targetId, message, reply = new { message_id = live.MessageId } };
            var sent = await client.CallActionAsync<SendResult>("send_msg", @params);
            long sentId = ExtractSentId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, isGroup ? targetId : 0, isGroup ? 0 : targetId, message, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            interactor.Poke($"寮曠敤鍥炲鍙戦€佹垚鍔燂紙鍥炲浜?{target} 鐨勬秷鎭?#{live.MessageId}锛?);
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("寮曠敤鍥炲璇锋眰瓒呮椂锛?0绉掞級锛屽彲鑳藉凡鍙戦€佹垚鍔燂紝璇风‘璁ゅ悗涓嶈鍐嶉噸澶嶅彂閫?);
        }
        catch (Exception e)
        {
            try
            {
                string fallback = $"[CQ:reply,id={live.MessageId}] {message}";
                if (isGroup)
                    await client.SendGroupMessage(targetId, fallback);
                else
                    await client.SendPrivateMessage(targetId, fallback);
                interactor.Poke($"寮曠敤鍥炲鍙戦€佹垚鍔燂紙CQ鐮佹柟寮忥紝缁撴瀯鍖栧弬鏁颁笉鍙敤锛歿e.Message}锛?);
            }
            catch (Exception e2)
            {
                interactor.Poke($"寮曠敤鍥炲澶辫触锛氱粨鏋勫寲鍙傛暟澶辫触锛坽e.Message}锛夛紝CQ鐮佸洖閫€涔熷け璐ワ紙{e2.Message}锛?);
            }
        }
    }

    /// <summary>浠庡疄鏃剁紦瀛樹腑瀹氫綅鐩爣鐢ㄦ埛鏈€杩戜竴鏉℃秷鎭€倀arget涓虹函鏁板瓧鎸塓Q鍙风簿纭尮閰嶏紝鍚﹀垯鎸夋樀绉板寘鍚尮閰?/summary>
    private LiveMessage? FindLatestFromUser(long targetScope, string target, bool isGroup)
    {
        bool byId = long.TryParse(target.Trim(), out long targetUin);
        var candidates = _liveMessages
            .Where(m => isGroup ? m.GroupId == targetScope : (m.GroupId == 0 && m.UserId == targetScope))
            .Where(m => byId ? m.UserId == targetUin : m.Nickname.Contains(target.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Time)
            .ThenByDescending(m => m.Seq)
            .ToList();
        return candidates.FirstOrDefault();
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("杞彂涓€鏉″凡鏈夌殑鍚堝苟杞彂娑堟伅鍒扮兢鑱娿€俧orwardId涓哄悎骞惰浆鍙戞秷鎭疘D锛堝繀椤绘潵鑷猤etmessages锛屽彲涓鸿礋鏁帮級")]
    public async Task SendForwardById(
        [Description("缇ゅ彿")] long groupId,
        [Description("鍚堝苟杞彂娑堟伅ID锛堝繀椤绘潵鑷猤etmessages锛?)] long forwardId)
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("鍚堝苟杞彂鍔熻兘宸茬鐢?); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("鍚堝苟杞彂澶辫触锛歈Q瀹㈡埛绔笉鍙敤"); return; }
        try
        {
            string message = $"[CQ:forward,id={forwardId}]";
            var sent = await client.CallActionAsync<SendResult>("send_group_msg", new { group_id = groupId, message });
            long sentId = ExtractSentId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, groupId, 0, message, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            interactor.Poke("鍚堝苟杞彂鍙戦€佹垚鍔?);
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("鍚堝苟杞彂璇锋眰瓒呮椂锛?0绉掞級锛屽彲鑳藉凡鍙戦€佹垚鍔燂紝璇风◢鍚庣‘璁わ紝涓嶈閲嶅鍙戦€?);
        }
        catch (Exception e)
        {
            interactor.Poke($"鍚堝苟杞彂澶辫触锛歿e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鏋勯€犲苟鍙戦€佹柊鐨勫悎骞惰浆鍙戞秷鎭埌缇よ亰銆俷odesJson涓篔SON鏁扮粍锛屾瘡涓妭鐐逛袱绉嶆牸寮忥細{\"name\":\"鏄电О\",\"uin\":QQ鍙?\"content\":\"鍐呭\"}锛堣嚜瀹氫箟鍐呭锛夋垨 {\"id\":鐪熷疄娑堟伅ID}锛堝紩鐢ㄧ湡瀹炴秷鎭紝id蹇呴』鏉ヨ嚜getmessages锛屾暟瀛楁垨鏁板瓧瀛楃涓插潎鍙級銆傗殸蹇呴』浼犲畬鏁村悎娉曠殑JSON鏁扮粍锛屾渶澶栧眰鐢╗]鍖呰９锛屼笉瑕佹紡鏀跺熬鎷彿")]
    public async Task SendForwardNew(
        [Description("缇ゅ彿")] long groupId,
        [Description("鑺傜偣JSON鏁扮粍锛堝繀椤绘槸瀹屾暣鍚堟硶鐨凧SON锛孾]闂悎锛?)] string nodesJson)
    {
        if (!Configuration.ForwardEnabled) { interactor.Poke("鍚堝苟杞彂鍔熻兘宸茬鐢?); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("鍚堝苟杞彂澶辫触锛歈Q瀹㈡埛绔笉鍙敤"); return; }
        try
        {
            // 瀹归敊锛氬幓鎺夐灏惧浣欑┖鐧斤紱鑻I婕忎簡鏀跺熬鎷彿锛屽皾璇曡ˉ鍏紙鏈€澶氳ˉ涓€灞?]锛?
            string json = nodesJson.Trim();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    throw new Exception("nodesJson蹇呴』涓篔SON鏁扮粍");
            }
            catch (JsonException)
            {
                string? repaired = RepairJsonArray(json);
                if (repaired == null)
                    throw new JsonException("nodesJson涓嶆槸鍚堟硶JSON鏁扮粍锛堟鏌ユ槸鍚︽紡浜嗘敹灏炬嫭鍙锋垨寮曞彿鏈棴鍚堬級銆傛纭ず渚嬶細[{\"name\":\"鏄电О\",\"uin\":123456,\"content\":\"鍐呭\"},{\"id\":-1234567890}]");
                json = repaired;
            }

            using var doc2 = JsonDocument.Parse(json);
            if (doc2.RootElement.ValueKind != JsonValueKind.Array)
                throw new Exception("nodesJson蹇呴』涓篔SON鏁扮粍");

            var nodes = new List<object>();
            foreach (JsonElement node in doc2.RootElement.EnumerateArray())
            {
                if (node.ValueKind != JsonValueKind.Object)
                    throw new Exception("姣忎釜鑺傜偣蹇呴』鏄疛SON瀵硅薄锛岃妫€鏌ユ槸鍚︽紡浜嗚姳鎷彿");

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
                throw new Exception("鑺傜偣鍒楄〃涓虹┖");

            var sent = await client.CallActionAsync<SendResult>("send_group_forward_msg", new { group_id = groupId, messages = nodes });
            long sentId = ExtractSentId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, groupId, 0, $"[CQ:forward,id={sentId}]", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            interactor.Poke($"鍚堝苟杞彂鍙戦€佹垚鍔燂紙{nodes.Count} 涓妭鐐癸級");
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("鍚堝苟杞彂璇锋眰瓒呮椂锛?0绉掞級锛屽彲鑳藉凡鍙戦€佹垚鍔熴€傝绋嶅悗纭锛屼笉瑕侀噸澶嶅彂閫?);
        }
        catch (Exception e)
        {
            interactor.Poke($"鍚堝苟杞彂澶辫触锛歿e.Message}");
        }
    }

    /// <summary>淇AI鐢熸垚鐨勬畫缂篔SON鏁扮粍锛氬彧鍏佽琛ュ叏缂哄け鐨勬敹灏炬嫭鍙凤紝涓嶅厑璁镐慨鏀瑰唴瀹广€傝繑鍥炰慨澶嶅悗鐨凧SON锛屾棤娉曚慨澶嶆椂杩斿洖null</summary>
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

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鍙戦€侀煶涔愬崱鐗囧埌QQ鑱婂ぉ銆傗殸QQ骞冲彴闊充箰鍗＄墖璇锋眰鍙兘杈冩參锛?10绉掞級锛岃嫢鎻愮ず瓒呮椂璇风◢鍚庣敤getmessages纭鏄惁宸插彂鍑猴紝涓嶈閲嶅鍙戦€?)]
    public async Task SendMusicCard(
        [Description("鐩爣QQ鍙?绉佽亰)鎴栫兢鍙?缇よ亰)")] long targetId,
        [Description("娑堟伅绫诲瀷锛歱rivate鎴杇roup")] string type,
        [Description("闊充箰骞冲彴(qq/163/kugou/migu/kuwo)")] string platform,
        [Description("闊充箰ID")] string musicId)
    {
        if (!Configuration.MusicCardEnabled) { interactor.Poke("闊充箰鍗＄墖鍔熻兘宸茬鐢?); return; }
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("闊充箰鍗＄墖鍙戦€佸け璐ワ細QQ瀹㈡埛绔笉鍙敤"); return; }
        try
        {
            string message = $"[CQ:music,type={platform},id={musicId}]";
            SendResult? sent;
            if (type == "group")
                sent = await client.CallActionAsync<SendResult>("send_group_msg", new { group_id = targetId, message });
            else
                sent = await client.CallActionAsync<SendResult>("send_private_msg", new { user_id = targetId, message });
            long sentId = ExtractSentId(sent);
            if (sentId != 0)
                RecordSentMessage(sentId, type == "group" ? targetId : 0, type == "group" ? 0 : targetId, message, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            interactor.Poke("闊充箰鍗＄墖鍙戦€佹垚鍔?);
        }
        catch (TaskCanceledException)
        {
            interactor.Poke("闊充箰鍗＄墖璇锋眰瓒呮椂锛?0绉掓湭鏀跺埌OneBot鍝嶅簲锛夈€俀Q鏈嶅姟鍣ㄥ彲鑳戒粛鍦ㄥ悗鍙板鐞嗭紝鍗＄墖鍙兘绋嶅悗鍑虹幇锛涜鐢℅etMessages纭锛屼笉瑕侀噸澶嶅彂閫併€傝嫢鎸佺画瓒呮椂璇锋鏌usicId鏄惁鏈夋晥");
        }
        catch (Exception e)
        {
            interactor.Poke($"闊充箰鍗＄墖鍙戦€佸け璐ワ細{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鑾峰彇缇よ亰/绉佽亰鏈€杩戞秷鎭強姣忔潯娑堟伅鐨勬秷鎭疘D锛岀敤浜庡畾浣嶈鎾ゅ洖(DeleteMsg)/璐磋〃鎯?SetEmoji)/寮曠敤鍥炲(SendReplyMessage)鎴栬浆鍙?SendForwardById)鐨勬秷鎭€傜兢鑱婁紶groupId锛涚鑱婁紶groupId=0骞朵紶userId銆傝繑鍥炲疄鏃舵崟鑾风殑鐪熷疄ID锛堝惈鑷繁鍒氬彂鐨勬秷鎭紝鍙戦€佹椂鑷姩璁板綍锛夛紝杩斿洖鏍煎紡锛歔娑堟伅ID:xxx]")]
    public async Task QGetMessages(
        [Description("缇ゅ彿锛堢鑱婃椂浼?锛?)] long groupId,
        [Description("QQ鍙凤紙浠呯鑱婃椂闇€瑕侊級")] long userId = 0,
        [Description("鑾峰彇鏉℃暟锛?-50锛岄粯璁?0")] int count = 10)
    {
        OneBotClient? client = GetClient();
        if (client == null) { interactor.Poke("鑾峰彇娑堟伅澶辫触锛歈Q瀹㈡埛绔笉鍙敤"); return; }
        try
        {
            count = Math.Clamp(count, 1, 50);
            var lines = new List<string>();

            var cacheMatches = _liveMessages
                .Where(m => groupId != 0 ? m.GroupId == groupId : (m.GroupId == 0 && (userId == 0 || m.UserId == userId)))
                .OrderByDescending(m => m.Time)
                .ThenByDescending(m => m.Seq)
                .Take(count)
                .ToList();

            if (cacheMatches.Count == 0)
            {
                interactor.Poke(groupId != 0
                    ? $"缇?{groupId} 鏆傛棤瀹炴椂鎹曡幏鐨勬秷鎭褰曪紙瀹炴椂鎹曡幏杩炴帴鍚庡紑濮嬭褰曪紝璇风◢鍚庨噸璇曪紱鑻ユ寔缁负绌鸿妫€鏌ュ疄鏃舵秷鎭崟鑾烽厤缃笌OneBot杩炴帴锛?
                    : $"涓?{userId} 鏆傛棤瀹炴椂鎹曡幏鐨勬秷鎭褰曪紙瀹炴椂鎹曡幏杩炴帴鍚庡紑濮嬭褰曪紝璇风◢鍚庨噸璇曪級");
                return;
            }

            var sb = new StringBuilder();
            string target = groupId != 0 ? $"缇?{groupId}" : $"涓?{userId}";
            sb.AppendLine($"{target} 鏈€杩?{cacheMatches.Count} 鏉℃秷鎭紙[娑堟伅ID:xxx]鍗崇湡瀹濱D锛屽彲涓鸿礋鏁帮紝鐩存帴鐢ㄤ簬鎿嶄綔锛夛細");
            foreach (var m in cacheMatches)
            {
                string nick = string.IsNullOrEmpty(m.Nickname) ? (m.IsSelf ? "鎴? : m.UserId.ToString()) : m.Nickname;
                string raw = OneBotSegment.FilterFace(OneBotSegment.FilterAt(OneBotSegment.FilterImage(OneBotSegment.FilterRecord(m.Raw))));
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(m.Time).LocalDateTime;
                lines.Add($"[{time:HH:mm:ss}] {m.UserId}({nick}) [娑堟伅ID:{m.MessageId}] {raw}");
            }
            foreach (string line in lines)
                sb.AppendLine(line);
            interactor.Poke(sb.ToString());
        }
        catch (Exception e)
        {
            interactor.Poke($"鑾峰彇娑堟伅澶辫触锛歿e.Message}");
        }
    }

    // ==================== notice 鎰熺煡 ====================

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
                        interactor.Poke($"[System 浣犺绂佽█浜嗭紙{groupInfo}锛塢");
                    else if (subType == "lift_ban")
                        interactor.Poke($"[System 浣犺瑙ｉ櫎绂佽█浜嗭紙{groupInfo}锛塢");
                }
            }
            else if (noticeType == "group_increase" && Configuration.PerceiveGroupIncrease)
            {
                long userId = noticeEvent.UserId;
                string userName = await GetQQUserName(userId, noticeEvent.GroupId);
                string userText = string.IsNullOrEmpty(userName)
                    ? $"鐢ㄦ埛{userId}"
                    : $"鐢ㄦ埛{userId}({userName})";
                string groupInfo = await GetGroupInfoText(noticeEvent.GroupId);
                interactor.Poke($"[System {userText}鍔犲叆浜嗙兢鑱婏紙{groupInfo}锛塢");
            }
            else if (noticeType == "notify" && noticeEvent.SubType == "poke" && Configuration.PokeDecideEnabled)
            {
                long targetId = 0;
                if (oneBotEvent is OneBotPokeEvent pokeEvent)
                    targetId = pokeEvent.TargetId;

                // 鍙鐞嗚嚜宸辫鎴?
                if (targetId != 0 && targetId != noticeEvent.SelfId) return;

                bool isGroup = noticeEvent.GroupId != 0;
                _lastPokeRequest = new PokeRequest(noticeEvent.UserId, noticeEvent.GroupId, isGroup, DateTime.Now);

                // 鍐峰嵈鏈熷唴涓嶉噸澶嶆敞鍏ワ紝閬垮厤杩炵画鎴充竴鎴冲埛灞忎笂涓嬫枃
                if (DateTime.Now - _lastPokePromptTime < PokePromptCooldown) return;
                _lastPokePromptTime = DateTime.Now;

                string userName = await GetQQUserName(noticeEvent.UserId, noticeEvent.GroupId);
                string userText = string.IsNullOrEmpty(userName)
                    ? $"鐢ㄦ埛{noticeEvent.UserId}"
                    : $"鐢ㄦ埛{noticeEvent.UserId}({userName})";
                string where = isGroup ? $"鍦ㄧ兢 {noticeEvent.GroupId} 鎴充簡鎴充綘" : "绉佽亰鎴充簡鎴充綘";
                interactor.Poke($"[System {userText} {where}銆備綘鍙互杈撳嚭 <PokeBack decide=\"yes\"/> 鍥炴埑锛屾垨 <PokeBack decide=\"no\"/> 蹇界暐]");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "鎰熺煡notice浜嬩欢澶辫触");
        }
    }

    private async Task<string> GetGroupInfoText(long groupId)
    {
        if (groupId == 0) return "缇ゅ彿:鏈煡";
        string name = await GetGroupNameAsync(groupId);
        return string.IsNullOrEmpty(name) ? $"缇ゅ彿:{groupId}" : $"缇ゅ彿:{groupId} 缇ゅ悕:{name}";
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
            logger.LogWarning(e, "鑾峰彇缇ゅ悕澶辫触: {GroupId}", groupId);
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
            logger.LogWarning(e, "鑾峰彇QQ鐢ㄦ埛鍚嶅け璐? {UserId}", userId);
            return "";
        }
    }

    // ==================== Typing Indicator ====================

    private void OnChatSent(string message)
    {
        var match = Regex.Match(message, @"\[绉佽亰娑堟伅\((\d+)");
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
            logger.LogDebug(e, "Typing indicator 鍙戦€佸け璐?);
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
