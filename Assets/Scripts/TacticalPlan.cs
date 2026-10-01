public struct TacticalPlanStep
{
    public BaseAction action;
    public GridPosition gridPosition;
    public int score;
    public int offenseValue;
    public int survivalValue;
    public int positioningValue;

    public string GetDescription()
    {
        return $"{action.GetActionName()} {gridPosition}: score={score} " +
               $"(offense={offenseValue}, survival={survivalValue}, positioning={positioningValue})";
    }
}

public struct TacticalPlan
{
    public TacticalPlanStep firstStep;
    public TacticalPlanStep secondStep;
    public bool hasSecondStep;
    public int totalScore;

    public string GetDescription()
    {
        if (!hasSecondStep)
            return $"{firstStep.GetDescription()} | plan total={totalScore}";

        return $"{firstStep.GetDescription()} -> {secondStep.GetDescription()} | plan total={totalScore}";
    }
}
