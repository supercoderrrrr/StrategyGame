using System.Collections.Generic;
using UnityEngine;

public class PathFinding : MonoBehaviour
{
    public static PathFinding Instance { get; private set; }

    private const int StraightMoveCost = 10;
    private const int DiagonalMoveCost = 14;
    private const int TacticalInfluenceRange = 7;

    [SerializeField] private Transform gridDebugObjectPrefab;
    [SerializeField] private LayerMask obstaclesLayerMask;
    [SerializeField] private LayerMask waterLayerMask;
    [SerializeField] private LayerMask grassLayerMask;

    private int width;
    private int height;
    private GridSystem<PathNode> gridSystem;
    private PathNode[] nodes;
    private bool[] occupied;
    private int[] friendlyInfluence;
    private int[] enemyInfluence;
    private TacticalMovementField movementField;
    private int topologyVersion;
    private int cachedOccupancyVersion = -1;
    private int influenceOccupancyVersion = -1;
    private int influenceTopologyVersion = -1;

    public int LastExpandedNodeCount => movementField == null ? 0 : movementField.LastExpandedNodeCount;
    public int MovementFieldBuildCount { get; private set; }
    public int MovementFieldCacheHitCount { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one Pathfinding!" + transform + "-" + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Setup(int width, int height, float cellSize)
    {
        this.width = width;
        this.height = height;
        gridSystem = new GridSystem<PathNode>(width, height, cellSize,
            (grid, gridPosition) => new PathNode(gridPosition));
        nodes = new PathNode[width * height];
        occupied = new bool[nodes.Length];
        friendlyInfluence = new int[nodes.Length];
        enemyInfluence = new int[nodes.Length];
        movementField = new TacticalMovementField(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                PathNode node = gridSystem.GetGridObject(gridPosition);
                nodes[ToIndex(gridPosition)] = node;
                Vector3 worldPosition = LevelGrid.Instance.GetWorldPosition(gridPosition);
                const float raycastOffsetDistance = 5f;

                if (Physics.Raycast(worldPosition + Vector3.down * raycastOffsetDistance, Vector3.up,
                    raycastOffsetDistance * 2, obstaclesLayerMask)) node.SetIsWalkable(false);
                if (Physics.Raycast(worldPosition + Vector3.up * raycastOffsetDistance, Vector3.down,
                    raycastOffsetDistance * 2, waterLayerMask)) node.SetMoveCostMultiplier(2);
                if (Physics.Raycast(worldPosition + Vector3.up * raycastOffsetDistance, Vector3.down,
                    raycastOffsetDistance * 2, grassLayerMask)) node.SetIsTallGrass(true);
            }
        }

        topologyVersion++;
    }

    public void GetReachableGridPositions(GridPosition start, int maxCost, List<GridPosition> output)
    {
        EnsureMovementField(start, maxCost);
        movementField.FillReachablePositions(output);
    }

    public bool TryBuildPath(GridPosition start, GridPosition destination, int maxCost,
        List<GridPosition> output, out int pathCost)
    {
        if (!LevelGrid.Instance.IsValidGridPosition(start) || !LevelGrid.Instance.IsValidGridPosition(destination))
        {
            output.Clear();
            pathCost = 0;
            return false;
        }
        EnsureMovementField(start, maxCost);
        return movementField.TryBuildPath(destination, output, out pathCost);
    }

    public List<GridPosition> FindPath(GridPosition start, GridPosition destination, out int pathLength)
    {
        List<GridPosition> path = new List<GridPosition>();
        return TryBuildPath(start, destination, int.MaxValue, path, out pathLength) ? path : null;
    }

    public bool HasPath(GridPosition start, GridPosition destination)
    {
        if (!LevelGrid.Instance.IsValidGridPosition(start) || !LevelGrid.Instance.IsValidGridPosition(destination))
            return false;
        EnsureMovementField(start, int.MaxValue);
        return movementField.IsReachable(destination);
    }

    public int GetPathLength(GridPosition start, GridPosition destination)
    {
        return HasPath(start, destination) ? movementField.GetCost(destination) : 0;
    }

    public int GetReachableCost(GridPosition start, GridPosition destination, int maxCost)
    {
        if (!LevelGrid.Instance.IsValidGridPosition(start) || !LevelGrid.Instance.IsValidGridPosition(destination))
            return 0;
        EnsureMovementField(start, maxCost);
        return movementField.IsReachable(destination) ? movementField.GetCost(destination) : 0;
    }

    public int CalculateDistance(GridPosition a, GridPosition b)
    {
        GridPosition delta = a - b;
        int xDistance = Mathf.Abs(delta.x);
        int zDistance = Mathf.Abs(delta.z);
        return DiagonalMoveCost * Mathf.Min(xDistance, zDistance) +
               StraightMoveCost * Mathf.Abs(xDistance - zDistance);
    }

    public PathNode GetNode(int x, int z) => gridSystem.GetGridObject(new GridPosition(x, z));

    public void SetIsWalkableGridPosition(GridPosition position, bool isWalkable)
    {
        PathNode node = gridSystem.GetGridObject(position);
        if (node.IsWalkable() == isWalkable) return;
        node.SetIsWalkable(isWalkable);
        topologyVersion++;
    }

    public void SetIsFireAtGridPosition(GridPosition position, bool isFire)
    {
        PathNode node = gridSystem.GetGridObject(position);
        if (node.IsFire() == isFire) return;
        node.SetIsFire(isFire);
        topologyVersion++;
    }

    public void SetIsTallGrassAtGridPosition(GridPosition position, bool isTallGrass)
    {
        PathNode node = gridSystem.GetGridObject(position);
        if (node.IsTallGrass() == isTallGrass) return;
        node.SetIsTallGrass(isTallGrass);
        topologyVersion++;
    }

    public bool IsWalkableGridPosition(GridPosition position) => gridSystem.GetGridObject(position).IsWalkable();
    public int GetMoveCostMultiplierAtGridPosition(GridPosition position) => gridSystem.GetGridObject(position).GetMoveCostMultiplier();
    public bool IsTallGrassAtGridPosition(GridPosition position) => gridSystem.GetGridObject(position).IsTallGrass();
    public bool IsFireAtGridPosition(GridPosition position) => gridSystem.GetGridObject(position).IsFire();

    public int GetOpposingInfluenceAtGridPosition(GridPosition position, bool queryingUnitIsEnemy)
    {
        EnsureInfluenceMap();
        int index = ToIndex(position);
        return queryingUnitIsEnemy ? friendlyInfluence[index] : enemyInfluence[index];
    }

    public int GetFriendlyInfluenceAtGridPosition(GridPosition position, bool queryingUnitIsEnemy)
    {
        EnsureInfluenceMap();
        int index = ToIndex(position);
        return queryingUnitIsEnemy ? enemyInfluence[index] : friendlyInfluence[index];
    }

    private void EnsureMovementField(GridPosition start, int maxCost)
    {
        RefreshOccupancy();
        if (movementField.Build(start, maxCost, nodes, occupied, topologyVersion, cachedOccupancyVersion))
            MovementFieldBuildCount++;
        else
            MovementFieldCacheHitCount++;
    }

    private void RefreshOccupancy()
    {
        int occupancyVersion = LevelGrid.Instance.GetOccupancyVersion();
        if (cachedOccupancyVersion == occupancyVersion) return;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition position = new GridPosition(x, z);
                occupied[ToIndex(position)] = LevelGrid.Instance.HasAnyUnitOnGridPosition(position);
            }
        }
        cachedOccupancyVersion = occupancyVersion;
    }

    private void EnsureInfluenceMap()
    {
        int occupancyVersion = LevelGrid.Instance.GetOccupancyVersion();
        if (influenceOccupancyVersion == occupancyVersion && influenceTopologyVersion == topologyVersion) return;

        System.Array.Clear(friendlyInfluence, 0, friendlyInfluence.Length);
        System.Array.Clear(enemyInfluence, 0, enemyInfluence.Length);
        if (UnitManager.Instance != null)
        {
            AddUnitInfluence(UnitManager.Instance.GetFriendlyUnitList(), friendlyInfluence);
            AddUnitInfluence(UnitManager.Instance.GetEnemyUnitList(), enemyInfluence);
        }
        influenceOccupancyVersion = occupancyVersion;
        influenceTopologyVersion = topologyVersion;
    }

    private void AddUnitInfluence(List<Unit> units, int[] output)
    {
        for (int unitIndex = 0; unitIndex < units.Count; unitIndex++)
        {
            Unit source = units[unitIndex];
            if (source == null) continue;
            GridPosition sourcePosition = source.GetGridPosition();
            const int unitInfluenceWeight = 10;

            for (int x = -TacticalInfluenceRange; x <= TacticalInfluenceRange; x++)
            {
                for (int z = -TacticalInfluenceRange; z <= TacticalInfluenceRange; z++)
                {
                    int distance = Mathf.Abs(x) + Mathf.Abs(z);
                    if (distance > TacticalInfluenceRange) continue;
                    GridPosition position = sourcePosition + new GridPosition(x, z);
                    if (!LevelGrid.Instance.IsValidGridPosition(position)) continue;
                    output[ToIndex(position)] += (TacticalInfluenceRange - distance + 1) * unitInfluenceWeight;
                }
            }
        }
    }

    private int ToIndex(GridPosition position) => position.x + position.z * width;
}
