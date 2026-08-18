using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Alife.Framework;
using Alife.Function.FunctionCaller;
using Alife.Function.QChat;
using Microsoft.Extensions.Logging;

namespace Alife.Demo.Plugin.QQEnhance;

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
}

[Module("QQ增强",
    "提供QQ贴表情、点赞、撤回、禁言、音乐卡片、感知通知、输入中状态等增强功能",
    defaultCategory: "Alife 官方/社交平台")]
public class QQEnhanceModule(
    XmlFunctionCaller functionCaller,
    ILogger<QQEnhanceModule> logger,
    Interactor<QQEnhanceModule> interactor,
    QChatService qChatService) :
    ChatBehaviour,
    IConfigurable<QQEnhanceConfig>
{
    public QQEnhanceConfig Configuration { get; set; } = null!;

    private OneBotClient Client => qChatService.OneBotClient;

    // Typing indicator 状态管理
    private readonly Dictionary<long, CancellationTokenSource> _typingCts = new();
    private readonly object _typingLock = new();

    protected override Task OnAwake()
    {
        XmlHandler xmlHandler = new(this) {
            Description = "提供QQ贴表情、点赞、撤回、禁言、音乐卡片等增强功能"
        };
        functionCaller.RegisterHandler(xmlHandler, DocumentMode.Implicit, DestroyCancellationToken);

        if (Configuration.PerceiveGroupBan || Configuration.PerceiveGroupIncrease)
            Client.EventReceived += OnEventReceived;

        if (Configuration.TypingIndicatorEnabled)
        {
            ChatBot.ChatSent += OnChatSent;
            ChatBot.ChatOver += OnChatOver;
        }

        return Task.CompletedTask;
    }

    protected override Task OnDestroy()
    {
        Client.EventReceived -= OnEventReceived;
        ChatBot.ChatSent -= OnChatSent;
        ChatBot.ChatOver -= OnChatOver;

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
    [Description("给QQ消息贴表情。emoji_id为表情ID，如 201(点赞)/2(开心)/3(疑惑)等")]
    public async Task SetEmoji(
        [Description("消息ID")] long messageId,
        [Description("表情ID")] int emojiId)
    {
        try
        {
            await Client.CallActionAsync<object>("set_msg_emoji_like", new { message_id = messageId, emoji_id = emojiId });
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
        try
        {
            var chunks = new List<int>();
            for (int i = 0; i < times / 10; i++) chunks.Add(10);
            if (times % 10 > 0) chunks.Add(times % 10);

            int count = 0;
            foreach (int chunk in chunks)
            {
                await Client.CallActionAsync<object>("send_like", new { user_id = qq, times = chunk });
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
    [Description("撤回QQ消息")]
    public async Task DeleteMsg(
        [Description("消息ID")] long messageId)
    {
        try
        {
            await Client.CallActionAsync<object>("delete_msg", new { message_id = messageId });
            interactor.Poke("撤回成功");
        }
        catch (Exception e)
        {
            interactor.Poke($"撤回失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("禁言QQ群成员")]
    public async Task GroupBan(
        [Description("群号")] long groupId,
        [Description("QQ号")] long userId,
        [Description("禁言时长(秒)，默认600秒，0为解除禁言")] int duration = 600)
    {
        try
        {
            await Client.CallActionAsync<object>("set_group_ban", new { group_id = groupId, user_id = userId, duration });
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
        try
        {
            string message = $"[CQ:music,type={platform},id={musicId}]";
            if (type == "group")
                await Client.SendGroupMessage(targetId, message);
            else
                await Client.SendPrivateMessage(targetId, message);
            interactor.Poke("音乐卡片发送成功");
        }
        catch (Exception e)
        {
            interactor.Poke($"音乐卡片发送失败：{e.Message}");
        }
    }

    // ==================== notice感知 ====================

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
                    long operatorId = noticeEvent.UserId;
                    long groupId = noticeEvent.GroupId;
                    string operatorName = await GetQQUserName(operatorId, groupId);
                    string operatorText = string.IsNullOrEmpty(operatorName)
                        ? $"用户{operatorId}"
                        : $"用户{operatorId}({operatorName})";

                    if (subType == "ban")
                        interactor.Poke($"[System {operatorText}禁言了你]");
                    else if (subType == "lift_ban")
                        interactor.Poke($"[System 你之前被禁言了，{operatorText}解除了你的禁言]");
                }
            }
            else if (noticeType == "group_increase" && Configuration.PerceiveGroupIncrease)
            {
                long userId = noticeEvent.UserId;
                interactor.Poke($"[System 用户{userId}加入了群聊]");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "感知notice事件失败");
        }
    }

    private async Task<string> GetQQUserName(long userId, long groupId = 0)
    {
        try
        {
            if (groupId != 0)
            {
                var sender = await Client.CallActionAsync<OneBotSender>(
                    "get_group_member_info",
                    new { group_id = groupId, user_id = userId, no_cache = false });
                if (sender != null)
                {
                    if (!string.IsNullOrEmpty(sender.Card)) return sender.Card;
                    if (!string.IsNullOrEmpty(sender.Nickname)) return sender.Nickname;
                }
            }

            var stranger = await Client.CallActionAsync<OneBotSender>(
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
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Configuration.TypingDelaySeconds), ct);

            var startTime = DateTime.Now;
            while (!ct.IsCancellationRequested)
            {
                await Client.CallActionAsync<object>("set_input_status", new { user_id = userId, event_type = 1 });

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
