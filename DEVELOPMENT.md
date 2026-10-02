# Development Archive / 开发归档

[English](#english) · [简体中文](#简体中文)

## English

### Why the initial Git commit imports an existing project

This project grew from a personal turn-based tactics prototype. Early development was not recorded as feature-by-feature Git commits; Unity Version Control was used later. The project has now been migrated to GitHub for portfolio presentation and continued maintenance.

The initial commit is a genuine snapshot of the project at migration time. The `legacy-import` tag marks that import, not the earliest prototype. The earlier Unity Version Control history remains separate; this repository does not fabricate commit dates, feature-development order, or historical Git commits.

This document is a technical retrospective, **not an export of the original Unity Version Control / Plastic SCM changeset history or client logs**. Those original records have not been uploaded to GitHub. Local `.plastic` files are workspace metadata, are excluded by `.gitignore`, and do not constitute a complete history export.

### Implementation evolution before migration

The following overview summarizes the existing implementation and actual iterations. These are technical milestones, not historical Git versions that can be checked out individually.

| Stage | Work | Current entry points |
| --- | --- | --- |
| Combat prototype | Grid, action points, movement/attacks, turns, and the bomb-defusal mission | `LevelGrid`, `BaseAction`, `TurnSystem`, `MissonManager.cs` |
| Pathfinding refactor | Replace per-target searches with bounded Dijkstra movement fields; heaps and flat arrays; weighted terrain costs; cache invalidation | `TacticalMovementField`, `PathFinding` |
| Tactical AI | Offense/survival/positioning scores, distance-based influence maps, depth-two candidate planning, configurable AI weights, and decision debugging | `MoveAction`, `TacticalPlanner`, `TacticalAIProfile` |
| Camera and battlefield presentation | Selected-unit focus, local wall cutouts, animated noise edges, and preserved wall shadows | `CameraController`, `CameraOcclusionDissolver`, `TacticalWallDissolve.shader` |
| Exploration presentation | Preserve room-exploration logic; play a noise-based fog dissolve after movement finishes; approximately one second by default | `RoomTrigger`, `Room`, `FogOfWarDissolve.shader` |

### Recording development after migration

- Keep the full Unity project private; publish only reviewed source code and documentation in the public portfolio.
- Describe actual changes in commits, for example `feat: add ...`, `fix: ...`, and `docs: ...`.
- Publish playable builds with real version tags, controls, known issues, and validation notes.
- Do not describe roadmap items as completed features or claim unmeasured FPS, pathfinding speedups, or zero-allocation performance.

### Future work

1. Add deterministic tests for pathfinding and candidate scoring, plus PlayMode regression coverage for key scenarios.
2. Establish reproducible baselines for search counts, candidate counts, and frame timings.
3. Introduce modular destructible walls and synchronize collision, pathfinding, and line-of-sight state.
4. Let AI compare the action costs of detouring and destroying obstacles.

HPA*, voxel destruction, arbitrary runtime mesh cutting, and a complete long-term squad-progression system are not implemented.

---

## 简体中文

### 为什么首次 Git 提交包含完整项目

本项目从个人战棋原型逐步迭代，早期没有按功能使用 Git 提交，后期使用过 Unity Version Control。为了作品集展示与后续维护，现在迁移到 GitHub。

首次提交是迁移时的真实项目快照，`legacy-import` 标签代表这次导入，并不代表最初原型版本。迁移前的 Unity Version Control 历史保留在原系统中，本仓库不伪造提交日期、功能开发顺序或过去的 Git 历史。

本文件是技术归档，**不是 Unity Version Control / Plastic SCM 原始 changeset 历史或客户端日志的导出**。这些原始记录尚未上传到 GitHub。本地 `.plastic` 文件属于工作区元数据，已被 `.gitignore` 排除，也不等于完整历史导出。

### 迁移前的实现演进

下面根据当前代码与实际迭代整理，是技术归档，不是可逐项检出的历史 Git 版本。

| 阶段 | 内容 | 当前代码入口 |
| --- | --- | --- |
| 战斗原型 | 网格、行动点、移动/攻击、回合与拆弹任务 | `LevelGrid`、`BaseAction`、`TurnSystem`、`MissonManager.cs` |
| 路径规划重构 | 从逐目标搜索转向有界 Dijkstra 移动范围场；堆与扁平数组；地形带权代价；缓存失效 | `TacticalMovementField`、`PathFinding` |
| 战术 AI | 进攻/生存/站位评分、距离影响力图、二层候选规划、AI 权重配置与决策调试 | `MoveAction`、`TacticalPlanner`、`TacticalAIProfile` |
| 镜头与战场呈现 | 单位聚焦、遮挡墙局部切口、波动噪波边缘、保持墙体投影 | `CameraController`、`CameraOcclusionDissolver`、`TacticalWallDissolve.shader` |
| 探索呈现 | 保留房间探索逻辑；移动结束后播放噪波迷雾消散；默认约 1 秒 | `RoomTrigger`、`Room`、`FogOfWarDissolve.shader` |

### 迁移后的记录方式

- 私有仓库保存完整工程；公开仓库保存审核后的代码与说明。
- 功能提交描述实际修改，例如 `feat: add ...`、`fix: ...`、`docs: ...`。
- 可玩包用真实版本标签发布，附操作说明、已知问题和验证记录。
- 不将路线图内容描述为已完成功能，不填写未经测试的 FPS、寻路提速或零 GC 指标。

### 后续方向

1. 补充路径规划与候选评分的确定性测试、关键场景的 PlayMode 回归。
2. 建立搜索次数、候选数量与帧耗时的可复现测量基线。
3. 模块化可破坏墙体及碰撞、路径、视线状态同步。
4. 让 AI 比较绕路与破坏障碍的行动成本。

HPA*、体素破坏、运行时任意网格切割及完整长期队伍养成目前均未实现。
