using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Alife.Framework;
using Alife.Function.FunctionCaller;
using Alife.Function.QChat;
using Microsoft.Extensions.Logging;

namespace Alife.Demo.Plugin.QQEnhance;

public class QQEnhanceConfig
{
    [DisplayName("主人QQ号")]
    [Description("允许执行敏感操作的主人QQ号，多个用逗号分隔")]
    public string MasterIds { get; set; } = "";

    [DisplayName("启用主人检查")]
    [Description("敏感操作是否仅限主人")]
    public bool MasterCheckEnabled { get; set; } = true;

    [DisplayName("请求超时(秒)")]
    [Description("HTTP请求超时时间")]
    public int Timeout { get; set; } = 30;
}

[Module("QQ增强",
    "提供QQ贴表情、点赞、撤回、禁言、音乐卡片等增强功能",
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

    private readonly HashSet<string> _masterIds = new();

    private bool IsMaster(string? userId)
    {
        if (!Configuration.MasterCheckEnabled) return true;
        if (string.IsNullOrEmpty(userId)) return true;
        return _masterIds.Contains(userId);
    }

    private OneBotClient Client => qChatService.OneBotClient;

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给指定QQ群消息贴表情。emoji_id为表情ID，如 201(点赞)/2(开心)/3(疑惑)等")]
    public async Task SetEmoji(
        [Description("群号")] long groupId,
        [Description("消息ID")] long messageId,
        [Description("表情ID")] int emojiId)
    {
        try
        {
            var ok = await Client.SetGroupEmojiAsync(groupId, messageId, emojiId);
            interactor.Poke(ok ? "贴表情成功" : "贴表情失败");
        }
        catch (Exception e)
        {
            interactor.Poke($"贴表情失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("给指定QQ群消息点赞")]
    public async Task SendQQLikes(
        [Description("群号")] long groupId,
        [Description("消息ID")] long messageId,
        [Description("点赞次数(1-10)")] int times = 1)
    {
        try
        {
            var ok = await Client.SendGroupLikesAsync(groupId, messageId, times);
            interactor.Poke(ok ? "点赞成功" : "点赞失败");
        }
        catch (Exception e)
        {
            interactor.Poke($"点赞失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("撤回指定QQ群消息")]
    public async Task DeleteMsg(
        [Description("群号")] long groupId,
        [Description("消息ID")] long messageId)
    {
        try
        {
            var ok = await Client.DeleteGroupMessageAsync(groupId, messageId);
            interactor.Poke(ok ? "撤回成功" : "撤回失败");
        }
        catch (Exception e)
        {
            interactor.Poke($"撤回失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("禁言指定QQ群成员")]
    public async Task GroupBan(
        [Description("群号")] long groupId,
        [Description("QQ号")] long userId,
        [Description("禁言时长(秒)")] int durationSeconds)
    {
        try
        {
            var ok = await Client.SetGroupBanAsync(groupId, userId, durationSeconds);
            interactor.Poke(ok ? "禁言成功" : "禁言失败");
        }
        catch (Exception e)
        {
            interactor.Poke($"禁言失败：{e.Message}");
        }
    }

    [XmlFunction(FunctionMode.OneShot)]
    [Description("发送QQ音乐卡片到群聊")]
    public async Task SendMusicCard(
        [Description("群号")] long groupId,
        [Description("音乐平台(qq/163/migu/kugou/kuwo)")] string platform,
        [Description("音乐ID")] string musicId)
    {
        try
        {
            var ok = await Client.SendGroupMusicCardAsync(groupId, platform, musicId);
            interactor.Poke(ok ? "音乐卡片发送成功" : "音乐卡片发送失败");
        }
        catch (Exception e)
        {
            interactor.Poke($"音乐卡片发送失败：{e.Message}");
        }
    }
}