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
    [Description("启用发送音乐卡片功能（custom格式，支持关键词搜索）")]
    public bool MusicCardEnabled { get; set; } = false;

    [DisplayName("戳一戳")]
    [Description("启用群聊戳一戳成员功能")]
    public bool PokeEnabled { get; set; } = true;

    [DisplayName("戳回决策")]
    [Description("收到别人的戳一戳后注入决策提示，让模型顺带决定是否回戳（PokeBack），不影响正常回复构建")]
    public bool PokeDecideEnabled { get; set; } = true;

    [DisplayName("引用回复")]
    [Description("启用引用回复消息功能")]
    public bool ReplyEnabled { get; set; } = true;

    [DisplayName("合并转发")]
    [Description("启用合并转发消息功能（转发已有/构造新转发/转发最近消息）")]
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
