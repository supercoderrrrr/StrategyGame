# Development Archive / 开发历程

## 为什么首次 Git 提交包含完整项目

本项目从个人战棋原型逐步迭代，早期没有按功能使用 Git 提交，后期使用过 Unity Version Control。为了作品集展示与后续维护，现在迁移到 GitHub。

首次提交是迁移时的真实项目快照，`legacy-import` 标签代表这次导入，并不代表最初原型版本。迁移前的 Unity Version Control 历史保留在原系统中，本仓库不伪造提交日期、功能开发顺序或过去的 Git 历史。

## 迁移前的实现演进

下面根据当前代码与实际迭代整理，是技术归档，不是可逐项检出的历史 Git 版本。

| 阶段 | 内容 | 当前代码入口 |
| --- | --- | --- |
| 战斗原型 | 网格、行动点、移动/攻击、回合与拆弹任务 | `LevelGrid`、`BaseAction`、`TurnSystem`、`MissonManager.cs` |
| 路径规划重构 | 从逐目标搜索转向有界 Dijkstra 移动范围场；堆与扁平数组；地形带权代价；缓存失效 | `TacticalMovementField`、`PathFinding` |
| 战术 AI | 进攻/生存/站位评分、距离影响力图、二层候选规划、AI 权重配置与决策调试 | `MoveAction`、`TacticalPlanner`、`TacticalAIProfile` |
| 镜头与战场呈现 | 单位聚焦、遮挡墙局部切口、波动噪波边缘、保持墙体投影 | `CameraController`、`CameraOcclusionDissolver`、`TacticalWallDissolve.shader` |
| 探索呈现 | 保留房间探索逻辑；移动结束后播放噪波迷雾消散；默认约 1 秒 | `RoomTrigger`、`Room`、`FogOfWarDissolve.shader` |

## 迁移后的记录方式

- 私有仓库保存完整工程；公开仓库保存审核后的代码与说明。
- 功能提交描述实际修改，例如 `feat: add ...`、`fix: ...`、`docs: ...`。
- 可玩包用真实版本标签发布，附操作说明、已知问题和验证记录。
- 不将路线图内容描述为已完成功能，不填写未经测试的 FPS、寻路提速或零 GC 指标。

## 后续方向

1. 补充路径规划与候选评分的确定性测试、关键场景的 PlayMode 回归。
2. 建立搜索次数、候选数量与帧耗时的可复现测量基线。
3. 模块化可破坏墙体及碰撞、路径、视线状态同步。
4. 让 AI 比较绕路与破坏障碍的行动成本。

HPA*、体素破坏、运行时任意网格切割及完整长期队伍养成目前均未实现。
