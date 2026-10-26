# 通用接口约定 v1.1

本包提供操作说明、输入输出契约、提示词模板、风格预设和真实生成过的慕容垂示例。工具名为 `design_general_card`。本包没有运行中的 HTTP 地址，不内置模型客户端或 API 密钥。

## 接入其他 AI

- 普通聊天 AI：读取 SKILL.md、所需模板和请求 JSON，并取得请求引用的图片。
- 技能宿主：加载整个 skill 目录。
- MCP/函数调用宿主：将 tool-definition.json 的输入输出 schema 映射到宿主工具声明；实际处理器负责参数解析、参考图传递、生图、检查和产物保存。契约文件不等于已部署服务。
- 只整理方案：生成提示词，返回 prepared。

## 输入

契约使用 JSON Schema 2020-12。default 只是约定值，普通校验器不会自动写入，宿主应显式应用。

| 字段 | 用途 |
| --- | --- |
| schema_version | 新请求使用 1.1；兼容旧的 1.0 输入与结果 |
| operation | generate 或 edit |
| asset_type | card 完整卡片；portrait 纯立绘 |
| character | 姓名与人物设定，name 必填 |
| style | 预设、补充风格与配色；custom 需 description |
| reference_images | 来源、用途和限制；相对路径基于请求 JSON 的目录 |
| card | 星数、阵营、兵种、COST、属性与战法的精确文字 |
| extra_text | 额外需要逐字呈现的文字 |
| constraints | 用户强调的要求 |
| edit | 修改要求及保留项，edit 模式必填 |
| output | 画幅、透明背景、格式偏好、目录、数量与修正次数 |

默认：generate、card、historical-strategy、2:3、不透明、PNG、1 张、最多自动修正 2 轮。卡片默认 5 星、底注“概念设计”；缺失的数值、兵种、COST 和战法不补造。card.faction_label 缺失时可以采用 character.faction 原文，均未提供则省略。portrait 不接受 card 字段，也不应用星级或底注默认值。用户的透明背景、无字等明确要求优先于预设。

中文、标点和数值字符串逐字保留。同名武将用 era、faction、title 等信息消歧。星数、文字和布局须视觉验收，结构校验不能保证图片正确。

quantity 是要求交付的张数；revision_limit 是每张自动修正上限，不含初次生成。输出目录相对于请求文件所在目录解析；未提供则使用宿主通常的产物目录。实际格式和尺寸写入结果，不把偏好当作已实现事实。

## 阵营、技能与历史简介

| 用户说法 | 参数 | 补全方式 |
| --- | --- | --- |
| 阵营 | character.faction / card.faction_label | 用户指定优先；授权后可按历史推定，游戏阵营另行确认 |
| 初始技能、自带战法 | card.ability.name | 用户提供，或明确允许原创/沿用已有设定 |
| 技能描述 | card.ability.description | 保留给定效果、数值和文案，不自动制造机制 |
| 历史简介、历史描述 | character.biography | provided 原文；auto 查证后撰写；omit 不添加 |
| 是否显示历史简介 | card.show_biography | 默认在有有效简介的卡片中显示；false 时只存资料 |
| 简介标题 | card.biography_label | 默认“人物小传” |

`biography.mode=provided` 要有非空 `text`。`mode=auto` 可只提供模式，AI 查证后把成文与来源记录回 `text` 和 `sources`（每项含 title、url）。未传 biography 不产生额外文案。auto 模式不能生成虚构史实；资料不足时记录限制，必要时询问。`asset_type=portrait` 只把人物资料用于绘画，不自动印出简介。

`card.ability.label` 可以填写“初始技能”或“自带战法”。历史简介与技能效果是独立内容；用户授权撰写历史，不等于授权原创游戏数值。完整示例见 [银甲红披风请求](../examples/murong-chui-silver.request.json)。


## 输出

| status | 含义 |
| --- | --- |
| prepared | 已完成提示词，尚未生成图片 |
| generated | 图片确实存在，已查看，要求的检查均通过 |
| needs_input | 缺少必需图片、修改目标或决定性信息 |
| tool_unavailable | 当前宿主没有可用的生成/编辑能力 |
| review_required | 已有图片，但有缺陷、未完成检查或未满足交付要求 |
| error | 工具或文件处理失败 |

artifacts 填写真实产物路径或宿主资源地址，不填示意下载地址；相对路径以结果 JSON 所在目录为基准。provider 记录实际使用的工具。review 中 pass/fail/not_checked 表示已检查符合、已检查不符、未检查。参考图未打开不能写 viewed。没有图片的示例不能写 generated。

## 图片与提示词

历史人物与多人系列应按 [历史形貌与人物辨识度](historical-identity.md) 建立设计记录。该记录使用同名 `.design.json` 保存证据、人物外观档案与美术假设，并以输出 `artifacts.kind=manifest` 引用；不向现有输入 schema 塞入未定义字段。`character.appearance` 汇总实际用于生成的外观描述。外貌查证与是否显示历史简介是独立选项。

多人结果的 `review` 应包含史料形貌落实、人物辨识度和参考身份污染检查。明显像同一人换装时，状态为 `review_required`，失败项为 `fail`；尚未横向比较则为 `not_checked`。不能仅因图片存在、文件哈希不同或姓名正确就标为 `generated`。

参考图要用工具支持的图像参数传入，不能只在提示词里放文件名或链接。工具不支持图像参考时说明限制，不假称保持了身份或精确布局。模板标题不是供应商参数，不能凭空添加不存在的 size、seed、path 等工具字段。

强一致性或可编辑项目可先用位图探索，再用真实排版/设计工具实现字体和布局；这种工程交付需要单独实施和检查。

## 示例

examples/murong-chui.request.json 使用相对路径，可随整个包移交。murong-chui-card-v1.png 是真实生成的质量参考，不保证其他模型精确复现。murong-chui.original-prompt.txt 保留实际使用的提示词；参考图出处见 reference-sources.json；实际成品的结构化结果见 [murong-chui.result.json](../examples/murong-chui.result.json)。

minimal-portrait.request.json 演示纯立绘。换人时替换 character、卡面文字与身份参考，重新考虑服饰、兵器、姿态和阵营；可保持同一 style 以形成系列。

