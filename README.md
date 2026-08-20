# Alife.QQEnhance

QQ增强插件：贴表情、点赞、撤回、禁言、戳一戳、引用回复、合并转发、音乐卡片、感知通知、输入中状态

从 KiraAI 的 [kira-ai-plugin-qq-enhance](https://github.com/xxynet/kira-ai-plugin-qq-enhance) 移植到 Alife 框架。

## 功能

- 给QQ消息贴表情
- 给QQ用户资料卡点赞
- 撤回QQ消息
- 禁言QQ群成员
- 群聊戳一戳成员
- 私聊戳一戳用户（`friend_poke`）
- 引用回复消息（结构化 reply 参数，失败自动回退 CQ 码）
- 合并转发（转发已有合并转发 / 构造新合并转发）
- 发送QQ音乐卡片
- 感知自己被禁言/解除禁言（通知携带群号与群名）
- 感知新成员进群（通知携带群号与群名）
- 获取群聊/私聊最近消息及消息ID（`GetMessages`，用于定位撤回/贴表情/引用回复/转发目标）
- 私聊输入中状态

## 与 YuYang.QQTools（幼央工具箱）兼容

检测幼央工具箱是否已加载且被当前角色启用，自动分工避免功能重复。

## 依赖

- `Alife.Function.FunctionCaller`（Xml函数执行器）
- `Alife.Function.QChat`（QQ聊天模块）

## 安装

将 `Alife.QQEnhance` 文件夹放入 Alife 的 `Plugins` 目录，同步环境后启用模块即可。

## 注意事项

- 需要先启用 Alife 官方 QQ聊天模块（`Alife.Function.QChat`）
- **QQ平台消息ID为负数**，撤回/贴表情/引用回复必须使用 `GetMessages` 返回的真实消息ID，严禁编造（否则 RetCode 100/1400 失败）
- 实时消息捕获参考了 [YuYang.QQTools](https://github.com/3026838203/YuYang.QQTools) 的 WebSocket 监听思路
