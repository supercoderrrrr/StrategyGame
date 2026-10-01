# Architecture / 系统与算法说明

## 游戏状态与动作

`TurnSystem` 管理当前回合，`Unit` 保存单位状态与行动点，`LevelGrid` 保存格子占用关系。`UnitActionSystem` 处理玩家选择、忙碌状态和动作执行，`BaseAction` 派生类完成移动、射击、近战、投掷及交互。

敌方控制器 `EnemyAI` 为已交战且可见的单位调用 `TacticalPlanner`。规划结果执行首个动作，完成回调后进入下一次决策。数据、动作执行和视觉监听分别由已有组件承担，当前工程仍使用 Singleton 连接若干核心系统。

## 路径规划：有界 Dijkstra 移动范围场

入口：[TacticalMovementField.cs](../Assets/Scripts/TacticalMovementField.cs)、[PathFinding.cs](../Assets/Scripts/PathFinding.cs)、[MoveAction.cs](../Assets/Scripts/Actions/MoveAction.cs)。

当前实现不包含 Weighted A* 的启发式权重，也不是流场导航。搜索使用八邻域 Dijkstra，直线基础成本 10、对角成本 14，乘以目标格子的地形倍率；水格倍率为 2。墙、关门、箱体和其他单位占用格阻止通行，移动单位自身起点占用被允许。

一次查询输出预算范围内的最小成本与父节点树，移动 UI 读取可达集合，点击目标时沿父链恢复路径，AI 读取成本评分。这样可以复用一次搜索结果，避免对每个候选格执行独立搜索。

- 节点通过 `x + z * width` 映射到一维数组。
- 成本、父节点、关闭标记和堆位置均在初始化时分配并复用。
- 二叉最小堆支持 decrease-key，节点更优时更新堆位置。
- 缓存仅保存**最近一次**范围场，键为起点、移动预算、拓扑版本、占用版本；候选起点变化仍会重新搜索。
- `FindPath` / `HasPath` 的无预算查询使用 `int.MaxValue`，可能遍历整个连通区域。

对于访问到的节点与边，搜索主体复杂度约为 `O((V + E) log V)`，另有每次构建时全网格数组清理的 `O(N)` 成本。HPA* 暂未引入，也没有发布实测提速百分比。

门、箱体、草地和火焰状态改变时更新节点并增加拓扑版本；单位格子变化增加占用版本。下一次查询发现版本不一致时重建范围场。此机制是**缓存失效后按需全场重算**，并非 D* Lite 或增量修复算法。

## 战术 AI：效用评分与二层候选规划

入口：[TacticalPlanner.cs](../Assets/Scripts/TacticalPlanner.cs)、[TacticalAIProfile.cs](../Assets/Scripts/TacticalAIProfile.cs)、[TacticalWorldState.cs](../Assets/Scripts/TacticalWorldState.cs)。

规划器先评估行动点允许的动作及目标，保留前 K 个首步候选；K 默认 8、上限 12。然后根据剩余行动点评估第二步，以 `首步分数 + 后续折扣 × 第二步分数` 比较方案，最后只执行胜出方案的首步。

支持的候选类型为移动、射击、近战、手雷和交互；法师火球没有纳入当前二层规划器。`TacticalAIProfile` 配置进攻、生存、站位权重、候选宽度、后续折扣和击杀奖励。

移动评分结合可攻击目标数、对手影响力、友方支持、草地、火焰危险、移动成本、接近目标及守卫拆弹点的距离。影响力按曼哈顿距离扩散，是廉价空间启发，不是考虑墙体与弹道遮挡的真实威胁概率图。

`TacticalWorldState` 保存单位快照用于目标与击杀预估；第二步候选会考虑假定的移动位置，避免明显重复交互与部分重复攻击，但攻击评分仍部分依赖实时状态。当前为近似短视野规划，尚不是完整的伤害、开门和环境状态模拟器。实际动作后重规划可以应对已发生的状态变化，但不能保证全局最优。

## 镜头遮挡与迷雾

`CameraController` 聚焦选中单位，并调用 `CameraOcclusionDissolver` 探测摄像机与角色间的遮挡。遮挡材质通过 `MaterialPropertyBlock` 控制局部切口，Shader 噪波随时间运动；ShadowCaster 保持原墙体投影。切换单位会重新播放过渡。

检测依赖 Collider、LayerMask 与 Renderer 的配置。无匹配 Collider/层级的墙门、重叠墙和摄像机进入厚墙的场景仍需回归验证，不能仅靠 Shader 保证检测完整。

房间探索状态由 `Room` 管理，`RoomTrigger` 接收玩家进入事件；若单位正在移动，则订阅 `OnStopMoving` 后再揭示房间。`FogOfWarDissolve.shader` 使用多层噪波阈值消散，默认持续约 1 秒。它是房间触发型迷雾，不是逐格实时视野算法。

## 性能与验证边界

范围场核心复用数组，其他层仍会创建候选列表或评分对象；目前不能宣称整套 AI 或整帧零 GC。搜索计数器和候选计数器用于诊断，实际时间与分配量需要 Unity Profiler 测量。

目前未建立正式 EditMode/PlayMode 测试套件。后续优先覆盖地形带权路径、版本失效、对角拐角、动态门/箱体、重复候选规划、迷雾触发和墙体遮挡。
