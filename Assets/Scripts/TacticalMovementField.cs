using System;
using System.Collections.Generic;

/// <summary>Bounded Dijkstra movement field backed by reusable flat arrays and a fixed binary heap.</summary>
public sealed class TacticalMovementField
{
    public const int UnreachableCost = int.MaxValue;
    private const int StraightCost = 10;
    private const int DiagonalCost = 14;

    private readonly int width;
    private readonly int height;
    private readonly int nodeCount;
    private readonly int[] costs;
    private readonly int[] parents;
    private readonly bool[] closed;
    private readonly int[] heapNodes;
    private readonly int[] heapCosts;
    private readonly int[] heapPositions;
    private int heapCount;
    private int startIndex;
    private int cachedMaxCost;
    private int cachedTopologyVersion = -1;
    private int cachedOccupancyVersion = -1;
    private bool hasResult;

    public int LastExpandedNodeCount { get; private set; }

    public TacticalMovementField(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        this.width = width;
        this.height = height;
        nodeCount = width * height;
        costs = new int[nodeCount];
        parents = new int[nodeCount];
        closed = new bool[nodeCount];
        heapNodes = new int[nodeCount];
        heapCosts = new int[nodeCount];
        heapPositions = new int[nodeCount];
    }

    /// <returns>True when a new field was calculated; false when the cached field was reused.</returns>
    public bool Build(GridPosition start, int maxCost, PathNode[] nodes, bool[] occupied,
        int topologyVersion, int occupancyVersion)
    {
        ValidateInputs(nodes, occupied);
        int requestedStartIndex = ToIndex(start.x, start.z);
        if (hasResult && startIndex == requestedStartIndex && cachedMaxCost == maxCost &&
            cachedTopologyVersion == topologyVersion && cachedOccupancyVersion == occupancyVersion)
        {
            return false;
        }

        startIndex = requestedStartIndex;
        cachedMaxCost = maxCost;
        cachedTopologyVersion = topologyVersion;
        cachedOccupancyVersion = occupancyVersion;
        hasResult = true;
        LastExpandedNodeCount = 0;
        heapCount = 0;

        for (int i = 0; i < nodeCount; i++)
        {
            costs[i] = UnreachableCost;
            parents[i] = -1;
            closed[i] = false;
            heapPositions[i] = -1;
        }

        costs[startIndex] = 0;
        PushOrDecrease(startIndex, 0);

        while (heapCount > 0)
        {
            int currentIndex = PopMin();
            if (closed[currentIndex]) continue;
            int currentCost = costs[currentIndex];
            if (currentCost > maxCost) break;

            closed[currentIndex] = true;
            LastExpandedNodeCount++;
            int currentX = currentIndex % width;
            int currentZ = currentIndex / width;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int x = currentX + dx;
                    int z = currentZ + dz;
                    if (x < 0 || z < 0 || x >= width || z >= height) continue;

                    int neighbour = ToIndex(x, z);
                    if (closed[neighbour] || !nodes[neighbour].IsWalkable()) continue;
                    // The moving unit occupies its origin. All other occupied cells block traversal.
                    if (neighbour != startIndex && occupied[neighbour]) continue;

                    int stepCost = (dx == 0 || dz == 0 ? StraightCost : DiagonalCost) *
                                   nodes[neighbour].GetMoveCostMultiplier();
                    if (currentCost > maxCost - stepCost) continue;
                    int tentativeCost = currentCost + stepCost;
                    if (tentativeCost >= costs[neighbour]) continue;

                    costs[neighbour] = tentativeCost;
                    parents[neighbour] = currentIndex;
                    PushOrDecrease(neighbour, tentativeCost);
                }
            }
        }
        return true;
    }

    public void FillReachablePositions(List<GridPosition> output)
    {
        if (output == null) throw new ArgumentNullException(nameof(output));
        output.Clear();
        if (!hasResult) return;
        for (int index = 0; index < nodeCount; index++)
        {
            if (index == startIndex || costs[index] == UnreachableCost || costs[index] > cachedMaxCost) continue;
            output.Add(ToGridPosition(index));
        }
    }

    public bool TryBuildPath(GridPosition destination, List<GridPosition> output, out int pathCost)
    {
        if (output == null) throw new ArgumentNullException(nameof(output));
        output.Clear();
        if (!hasResult)
        {
            pathCost = 0;
            return false;
        }

        int current = ToIndex(destination.x, destination.z);
        pathCost = costs[current];
        if (pathCost == UnreachableCost || pathCost > cachedMaxCost)
        {
            pathCost = 0;
            return false;
        }

        while (current >= 0)
        {
            output.Add(ToGridPosition(current));
            if (current == startIndex)
            {
                output.Reverse();
                return true;
            }
            current = parents[current];
        }

        output.Clear();
        pathCost = 0;
        return false;
    }

    public bool IsReachable(GridPosition position)
    {
        int index = ToIndex(position.x, position.z);
        return hasResult && costs[index] != UnreachableCost && costs[index] <= cachedMaxCost;
    }

    public int GetCost(GridPosition position) => hasResult ? costs[ToIndex(position.x, position.z)] : UnreachableCost;

    private void ValidateInputs(PathNode[] nodes, bool[] occupied)
    {
        if (nodes == null || nodes.Length != nodeCount)
            throw new ArgumentException("Path node count does not match the movement field.", nameof(nodes));
        if (occupied == null || occupied.Length != nodeCount)
            throw new ArgumentException("Occupancy count does not match the movement field.", nameof(occupied));
    }

    private int ToIndex(int x, int z)
    {
        if (x < 0 || z < 0 || x >= width || z >= height)
            throw new ArgumentOutOfRangeException($"Grid position ({x}, {z}) is outside {width}x{height}.");
        return x + z * width;
    }

    private GridPosition ToGridPosition(int index) => new GridPosition(index % width, index / width);

    private void PushOrDecrease(int node, int cost)
    {
        int position = heapPositions[node];
        if (position >= 0)
        {
            heapCosts[position] = cost;
            SiftUp(position);
            return;
        }
        position = heapCount++;
        heapNodes[position] = node;
        heapCosts[position] = cost;
        heapPositions[node] = position;
        SiftUp(position);
    }

    private int PopMin()
    {
        int node = heapNodes[0];
        heapPositions[node] = -1;
        heapCount--;
        if (heapCount > 0)
        {
            heapNodes[0] = heapNodes[heapCount];
            heapCosts[0] = heapCosts[heapCount];
            heapPositions[heapNodes[0]] = 0;
            SiftDown(0);
        }
        return node;
    }

    private void SiftUp(int position)
    {
        while (position > 0)
        {
            int parent = (position - 1) / 2;
            if (heapCosts[parent] <= heapCosts[position]) return;
            Swap(position, parent);
            position = parent;
        }
    }

    private void SiftDown(int position)
    {
        while (true)
        {
            int left = position * 2 + 1;
            if (left >= heapCount) return;
            int right = left + 1;
            int smallest = right < heapCount && heapCosts[right] < heapCosts[left] ? right : left;
            if (heapCosts[position] <= heapCosts[smallest]) return;
            Swap(position, smallest);
            position = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        int node = heapNodes[a];
        int cost = heapCosts[a];
        heapNodes[a] = heapNodes[b];
        heapCosts[a] = heapCosts[b];
        heapNodes[b] = node;
        heapCosts[b] = cost;
        heapPositions[heapNodes[a]] = a;
        heapPositions[heapNodes[b]] = b;
    }
}
