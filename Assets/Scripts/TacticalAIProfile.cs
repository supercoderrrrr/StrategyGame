using UnityEngine;

[CreateAssetMenu(fileName = "TacticalAIProfile", menuName = "Strategy Game/AI/Tactical Profile")]
public sealed class TacticalAIProfile : ScriptableObject
{
    [Header("Utility Weights")]
    [Min(0f)][SerializeField] private float offenseWeight = 1f;
    [Min(0f)][SerializeField] private float survivalWeight = 1f;
    [Min(0f)][SerializeField] private float positioningWeight = 1f;

    [Header("Planning")]
    [Range(1, 12)][SerializeField] private int beamWidth = 8;
    [Range(0f, 1f)][SerializeField] private float followUpDiscount = .8f;
    [Min(0)][SerializeField] private int killBonus = 80;

    public int BeamWidth => beamWidth;
    public float FollowUpDiscount => followUpDiscount;
    public int KillBonus => killBonus;

    public int Evaluate(EnemyAIAction action)
    {
        int unclassifiedValue = action.actionValue - action.offenseValue -
                                action.survivalValue - action.positioningValue;
        return unclassifiedValue + Mathf.RoundToInt(
            action.offenseValue * offenseWeight +
            action.survivalValue * survivalWeight +
            action.positioningValue * positioningWeight);
    }
}
