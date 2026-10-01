using System;
using System.Collections.Generic;

/// <summary>A reusable read-only snapshot used while evaluating plans.</summary>
public sealed class TacticalWorldState
{
    public struct UnitState
    {
        public Unit unit;
        public GridPosition position;
        public int health;
        public bool isEnemy;
        public bool isHidden;
    }

    private UnitState[] units = Array.Empty<UnitState>();

    public int UnitCount { get; private set; }

    public void Capture(List<Unit> sourceUnits)
    {
        UnitCount = sourceUnits.Count;
        if (units.Length < UnitCount)
        {
            int capacity = 4;
            while (capacity < UnitCount) capacity *= 2;
            units = new UnitState[capacity];
        }

        for (int i = 0; i < UnitCount; i++)
        {
            Unit unit = sourceUnits[i];
            units[i] = new UnitState
            {
                unit = unit,
                position = unit.GetGridPosition(),
                health = unit.GetHealth(),
                isEnemy = unit.IsEnemy(),
                isHidden = unit.IsHiddenInFog(),
            };
        }
    }

    public UnitState GetUnitState(int index) => units[index];

    public UnitState? GetUnitAt(GridPosition position)
    {
        for (int i = 0; i < UnitCount; i++)
        {
            if (units[i].position == position) return units[i];
        }
        return null;
    }
}
