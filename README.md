# StrategyGame — 3D Turn-Based Tactics

**战棋策略游戏与敌人战术 AI** · Unity / C# / URP

基于个人战棋原型持续迭代的小队制回合战术游戏，围绕动态路径规划、战术决策与环境交互展开。玩家控制士兵和法师，使用有限行动点移动、攻击与施放技能，在回合倒计时内完成拆弹任务。

A Unity turn-based tactics prototype featuring utility AI, bounded Dijkstra movement fields, and reactive battlefield interactions.

> 本仓库公开核心代码与技术文档；原始第三方美术、场景和预制体保存在私有工程中。它是代码展示仓库，无法单独还原完整可玩游戏。Windows 试玩包与演示视频将在验证后发布，当前尚无下载版本。

## 技术亮点

- **战术决策**：Utility AI 将效用分为进攻、生存、站位；二层 Beam Search 比较短期行动组合，执行首步后重新规划；ScriptableObject 调整 AI 风格。
- **动态路径规划**：有界 Dijkstra 一次构建可移动范围、带权成本与路径父树，复用扁平数组和二叉最小堆；拓扑与占用版本控制缓存失效。
- **战场交互**：水面移动代价、草地隐蔽、火焰危险、门与可破坏箱体参与移动和决策流程。
- **战场呈现**：选中单位镜头聚焦、保留投影的局部墙体噪波溶解、移动结束后播放的房间迷雾消散。

当前实现的路径算法是有界 Dijkstra 范围场，不使用 Weighted A* 的启发式权重。HPA*、体素破坏和 AI 炸墙均未实现，详见 [架构说明](docs/ARCHITECTURE.md)。

## 建议阅读顺序

| 主题 | 代码入口 | 看点 |
| --- | --- | --- |
| 搜索核心 | [TacticalMovementField](Assets/Scripts/TacticalMovementField.cs) | 预算搜索、decrease-key 二叉堆、父树与单条缓存 |
| 动态状态 | [PathFinding](Assets/Scripts/PathFinding.cs)、[LevelGrid](Assets/Scripts/Grid/LevelGrid.cs) | 地形/障碍、占用与拓扑版本、影响力图 |
| AI 规划 | [TacticalPlanner](Assets/Scripts/TacticalPlanner.cs)、[TacticalAIProfile](Assets/Scripts/TacticalAIProfile.cs) | 二层候选搜索、权重、每步重规划 |
| 行动评分 | [MoveAction](Assets/Scripts/Actions/MoveAction.cs)、[EnemyAIAction](Assets/Scripts/EnemyAIAction.cs) | 进攻、生存和站位分项 |
| 墙体遮挡 | [CameraOcclusionDissolver](Assets/Scripts/CameraOcclusionDissolver.cs)、[Shader](Assets/Resources/Shaders/TacticalWallDissolve.shader) | 物理遮挡检测、局部切口与独立投影 |
| 房间迷雾 | [RoomTrigger](Assets/Scripts/RoomTrigger.cs)、[Room](Assets/Scripts/Room.cs)、[Shader](Assets/Resources/Shaders/FogOfWarDissolve.shader) | 移动完成事件与噪波消散 |

## 环境与目录

Unity **2022.3.62f2** · URP **14.0.12** · Cinemachine **2.10.5** · Input System **1.14.2**。包版本保存在 `Packages` 中，Editor 版本见 `ProjectSettings/ProjectVersion.txt`。

```text
Assets/
  Scripts/                 游戏逻辑、网格、规划器与 UI
  Resources/Shaders/       墙体遮挡与战争迷雾 Shader
  PlayerInputActions.*    输入配置及 Unity 自动生成代码
Packages/                  包清单与版本锁
docs/                      架构、验证边界与发布说明
DEVELOPMENT.md             真实迁移说明与开发归档
THIRD_PARTY_NOTICES.md     素材来源与公开范围
```

完整工程启动场景为 `MainMenuScene`，关卡为 `Level1`；这些场景不包含在本仓库中。阅读者可将代码导入相应 Unity 环境，但需自行提供场景、组件引用、合法素材及渲染配置。

## 操作与验证

鼠标左键选择单位/动作/目标；WASD 移动镜头，Q/E 或中键旋转，滚轮缩放；Tab 切换动作，左 Alt 切换单位，Space 结束回合，Esc 暂停。

范围场和规划器提供搜索/缓存/候选计数器，以及 AI 评分日志与 Scene Gizmos。当前没有发布实测性能百分比，也尚未建立完整自动化测试套件。二层 AI 是近似规划；对角拐角、厚墙和墙门遮挡等仍需进一步回归验证。

Windows Build 的操作与测试标准见 [发布说明](docs/RELEASE.md)。

## 开发历史与资源

首次 GitHub 提交为既有项目的真实导入快照，迁移前曾使用 Unity Version Control。`legacy-import` 标签记录迁移节点，不伪造过去的 Git 历史；详见 [DEVELOPMENT.md](DEVELOPMENT.md)。

第三方模型、动画和特效不作为原创美术声明，也不以源文件形式分发。当前未附加统一开源许可，资源和生成代码说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

后续重点是自动化回归、可复现性能评估、模块化墙体破坏与 AI 的破坏/绕路成本决策。
