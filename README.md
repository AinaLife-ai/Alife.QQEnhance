# Alife.QQEnhance

QQ增强插件：贴表情、点赞、撤回、禁言、音乐卡片、感知通知、输入中状态、实时消息ID捕获

从 KiraAI 的 [kira-ai-plugin-qq-enhance](https://github.com/xxynet/kira-ai-plugin-qq-enhance) 移植到 Alife 框架。

## 功能

- 给QQ消息贴表情
- 给QQ用户资料卡点赞
- 撤回QQ消息
- 禁言QQ群成员
- 发送QQ音乐卡片
- 感知自己被禁言/解除禁言（通知携带群号与群名）
- 感知新成员进群（通知携带群号与群名）
- 获取群聊/私聊最近消息及消息ID（`QGetMessages`，用于定位撤回/贴表情目标）
- **实时消息ID捕获**：独立WS监听OneBot实时事件，从 message/message_sent 事件捕获真实消息ID（含自己刚发的消息），`QGetMessages` 只返回实时捕获的真实ID
- 私聊输入中状态

## 依赖

- `Alife.Function.FunctionCaller`（Xml函数执行器）
- `Alife.Function.QChat`（QQ聊天模块）

## 安装

将 `Alife.QQEnhance` 文件夹放入 Alife 的 `Plugins` 目录，同步环境后启用模块即可。

## 配置

| 配置项 | 说明 | 默认值 |
|---|---|---|
| 贴表情 | 启用给QQ消息贴表情功能 | 开启 |
| 点赞 | 启用给QQ用户资料卡点赞功能 | 关闭 |
| 撤回 | 启用撤回QQ消息功能 | 开启 |
| 禁言 | 启用禁言QQ群成员功能 | 开启 |
| 音乐卡片 | 启用发送音乐卡片功能 | 关闭 |
| 感知群禁言 | 感知自己被禁言/解除禁言并通知AI | 开启 |
| 感知成员进群 | 感知新成员进群并通知AI | 开启 |
| 输入中状态 | 私聊时发送输入中状态 | 开启 |
| 输入中延迟(秒) | 收到消息后延迟多久开始发送输入中状态 | 2.0 |
| 输入中间隔(秒) | 输入中状态刷新间隔 | 2.0 |
| 输入中最大时长(秒) | 输入中状态最大持续时长 | 60.0 |
| 实时消息捕获 | 独立WS监听实时事件捕获真实消息ID（撤回/贴表情可靠性核心） | 开启 |
| 实时消息缓存大小 | 缓存最近N条实时消息的消息ID/内容，用于qgetmessages查询 | 200 |

## 关于消息ID（重要）

- **QQ平台消息ID为负数**。撤回/贴表情/引用回复（CQ:reply）必须使用 `QGetMessages` 返回的真实消息ID，严禁编造或猜测（否则 RetCode 100/1400 失败）。
- `QGetMessages` **只返回实时捕获的真实消息ID**，不使用历史消息接口（如 `get_group_msg_history`）——不同 OneBot 实现下历史接口的 `message_id` 语义存在差异，且拿不到自己刚发送消息的ID，使用它会误导撤回/贴表情操作。
- 实时捕获通过一条独立的只读 WebSocket 连接（复用 QChatService 的 Url/Token）监听 `message` / `message_sent` 事件，从原始报文的 `message_id` 字段捕获真实ID，与 OneBot 实现无关，100% 可靠。
- 若实时捕获连接失败（如 OneBot 不支持多连接），`QGetMessages` 会明确提示不可用，而不是给出可能误导的结果。

## 致谢

- 实时消息ID捕获方案参考了 [YuYang.QQTools](https://github.com/3026838203/YuYang.QQTools)（幼央）的独立 WS 监听思路：通过监听 OneBot 实时事件的 `message_id` 字段获取真实消息ID，并从 `message_sent` 事件捕获自己发送的消息ID。感谢作者的优秀实现。
- 原版插件移植自 KiraAI 生态的 [kira-ai-plugin-qq-enhance](https://github.com/xxynet/kira-ai-plugin-qq-enhance)。

## 注意事项

- 需要先启用 Alife 官方 QQ聊天模块（`Alife.Function.QChat`）
- 本插件通过反射访问 `QChatService` 内部的 `OneBotClient`，不修改官方代码
