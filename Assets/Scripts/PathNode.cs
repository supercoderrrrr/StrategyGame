using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    private GridPosition gridPosition;
    private int gCost;
    private int hCost;
    private int fCost;
    private PathNode cameFromPathNode;
    private bool isWalkable = true;

    //针对特殊地形如水地块，沼泽地块，移动点的消耗增加
    private int moveCostMultiplier = 1;
    private bool isTallGrass = false;
    private bool isFire = false;

    public PathNode(GridPosition grodPosition)
    {
        this.gridPosition = grodPosition;
    }

    public override string ToString()
    {
        return gridPosition.ToString();
    }

    public int GetMoveCostMultiplier()
    {
        return moveCostMultiplier;
    }

    public void SetMoveCostMultiplier(int multiplier)
    {
        this.moveCostMultiplier = multiplier;
    }

    public bool IsTallGrass()
    {
        return isTallGrass;
    }

    public void SetIsTallGrass(bool isTallGrass)
    {
        this.isTallGrass = isTallGrass;
    }

    public bool IsFire()
    {
        return isFire;
    }

    public void SetIsFire(bool isFire)
    {
        this.isFire = isFire;
    }

    public int GetGCost()
    {
        return gCost;
    }
    public int GetHCost()
    {
        return hCost;
    }
    public int GetFCost()
    {
        return fCost;
    }

    public void SetGCost(int gCost)
    {
        this.gCost = gCost;
    }

    public void SetHCost(int hCost)
    {
        this.hCost = hCost;
    }

    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    public void ResetCameFromPathNode()
    {
        cameFromPathNode = null;
    }

    public void SetCameFromPathNode(PathNode pathNode)
    {
        cameFromPathNode = pathNode;
    }

    public PathNode GetCameFromPathNode()
    {
        return cameFromPathNode;
    }

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public bool IsWalkable()
    {
        return isWalkable;
    }

    public void SetIsWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
    }
}
