using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIAction
{
    public GridPosition gridPosition;
    public int actionValue;
    public int offenseValue;
    public int survivalValue;
    public int positioningValue;

    public string GetScoreBreakdown()
    {
        return $"total={actionValue}, offense={offenseValue}, survival={survivalValue}, positioning={positioningValue}";
    }
}
