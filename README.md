# Alife.QQEnhance

QQ增强插件：贴表情、点赞、撤回、禁言、戳一戳、引用回复、合并转发、音乐卡片、感知通知、输入中状态、戳回决策

从 KiraAI 的 [kira-ai-plugin-qq-enhance](https://github.com/xxynet/kira-ai-plugin-qq-enhance) 移植到 Alife 框架。

## 功能

- **贴表情**：`SetEmojiRecent`（对某人最近一条消息贴表情，免ID一步到位）+ `SetEmoji`（指定消息ID）
- **资料卡点赞**：`SendQQLikes`，好友/陌生人均可，默认 50 次（顶满平台日上限），5×10 分块调用
- **撤回**：`DeleteMsgRecent`（撤回自己刚发的消息，免ID）+ `DeleteMsg`（指定消息ID）
- **禁言**：`GroupBan` 禁言/解除群成员
- **戳一戳**：`PokeGroupMember` 群聊 / `PokePrivateMember` 私聊 / `PokeBack` 被戳后回戳（30秒冷却防刷屏）
- **引用回复**：`ReplyRecent`（回复某人最近一条，免ID，targetId 可省略自动推断会话）+ `SendReplyMessage`（回复任意指定消息ID，配合 QGetMessages 可引用 10 条之前的消息）
- **合并转发**：`ForwardRecent`（转发最近N条，免ID，缓存不足自动回拉历史补齐，失效节点自动降级重建，支持私聊）+ `SendForwardById` + `SendForwardNew`（均支持群聊/私聊）
- **点歌/音乐卡片**：`SendMusicCard platform=search musicId=歌名`，默认发**网易云官方卡片**（结构化 music 消息段，免签名，任何 QQ 版本可正常显示播放，不会"版本过低"）
- **感知通知**：被禁言/解禁、新成员进群、被戳（决策提示）、被赞资料卡（可回赞）、群消息被贴表情
- **消息ID查询**：`QGetMessages`，实时捕获 + 历史回拉双来源，含 bot 自己发的消息
- **私聊输入中状态**

## 主动社交设计

插件通过四层机制让 AI 主动、自然地使用互动功能：

1. **场景触发式函数描述**：每个函数的描述写清"什么时候随手用"（如"看到有趣/赞同的消息随手贴个表情，不用说话直接贴"）
2. **一步函数**：SetEmojiRecent/ReplyRecent/DeleteMsgRecent/PokeBack 全部免ID一步完成，消灭"查ID→抄ID"的决策成本
3. **事件主动喂提示**：被戳/被赞/被贴表情时注入轻量行动建议（30秒冷却，可忽略）
4. **常驻社交风格提示**：OnAwake 注入一段可配置的行为总纲（配置项"社交风格提示词"，可自行改写或清空），函数文档仍走 Implicit 省 token

## 真实消息ID机制

QQ 消息 ID 为负数，撤回/贴表情/引用/转发必须使用真实 ID。本插件有三层来源：

1. **实时捕获**：独立 WS 监听 message/message_sent 事件（带断线自动重连，5s→30s 指数退避）
2. **发送自存**：本插件发出的消息发送成功后自动记录真实 message_id
3. **历史回拉**：缓存不足时自动调 `get_group_msg_history`/`get_friend_msg_history` 补齐（history 返回的 message_id 是真实可用 ID，注意与分页参数 message_seq 区分）

### 捕获 bot 自己发的消息

`reportSelfMessage` 在 QChat 主连接上**必须保持关闭**（否则 AI 收到自己的消息造成自我回环）。如需捕获 bot 日常聊天发出的消息（用于转发/引用/撤回），推荐做法：在 NapCat **额外加一个 WS 服务端**，仅此适配器开 `reportSelfMessage: true`，然后把它的地址填入插件配置"捕获连接地址"。不配也能用——历史回拉会自动补齐 bot 自己的消息。

## 与 YuYang.QQTools（幼央工具箱）兼容

| 兼容模式 | 行为 |
|---|---|
| `Auto`（默认） | 检测到幼央 → 重叠功能（戳一戳/引用回复/点赞/贴表情/撤回/输入中）自动让位 |
| `PreferQQEnhance` | 无视幼央，全部功能由本插件提供 |
| `PreferYuYang` | 重叠功能一律让位（幼央未装时自动回退自持） |
| `Off` | 不检测，全功能注册，手动管理 |

## 依赖

- `Alife.Function.FunctionCaller`（Xml函数执行器）
- `Alife.Function.QChat`（QQ聊天模块）

## 安装

将 `Alife.QQEnhance` 文件夹放入 Alife 的 `Plugins` 目录，同步环境后启用模块即可。

## 注意事项

- 需要先启用 Alife 官方 QQ聊天模块（`Alife.Function.QChat`）
- 本插件通过反射访问 `QChatService` 内部的 `OneBotClient`，不修改官方代码
- **QQ平台消息ID为负数**。操作指定消息必须使用 `QGetMessages` 返回的真实ID，严禁编造
- 引用回复使用结构化消息段 `{type:"reply",data:{id}}`（NapCat 原生支持；`send_msg` 没有顶层 reply 参数）
- **音乐卡片**：默认网易云官方卡片（music 消息段 type=163），免签名全端可渲染；配置"音乐卡片样式=custom"为高级选项，需 NapCat 配置 musicSignUrl，否则接收方显示"发送者版本过低"
- 撤回合并转发卡片类消息会失败（NapCat 平台限制）
- 实时消息捕获参考了 [YuYang.QQTools](https://github.com/3026838203/YuYang.QQTools) 的 WebSocket 监听思路，特此致谢

## v4.9.1 修复说明

- **合并转发/贴表情 RetCode 1400 修复**：NapCat 对 `set_msg_emoji_like` 的 `emoji_id`、转发节点的 `id`/`uin`、reply 段的 `id` 均要求 **string** 类型，此前传数字导致 1400 参数错误，已全部修正
- **音乐卡片新增 record 语音条模式**（配置项 音乐卡片样式=record）：直接发网易云直链语音条，完全不依赖签名，任何端可播。卡片显示"发送者版本过低"说明 NapCat 未配置 `musicSignUrl`（WebUI → OneBot11 配置里填签名服务地址），不想配置就用 record 模式保底
- **新增互动提示**（官方消息过滤同款 ChatSend 钩子）：收到 QQ 消息时在末尾附加提示，提醒 AI 可随手贴表情/引用/戳一戳/点赞。配置项可开关、调概率（默认 100%）、自定义文本
