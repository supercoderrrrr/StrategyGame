using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitColorApplier : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer unitRenderer;

    [SerializeField] private Material[] materialArray;

    private const string PLAYER_PREFS_MATERIAL_KEY = "SelectedUnitMaterial";

    private void Start()
    {
        if (TryGetComponent<Unit>(out Unit unit))
        {
            if (unit.IsEnemy())
            {
                return;
            }
        }

        int savedIndex = PlayerPrefs.GetInt(PLAYER_PREFS_MATERIAL_KEY, 0);

        if (savedIndex >= 0 && savedIndex < materialArray.Length)
        {
            if (unitRenderer != null)
            {
                unitRenderer.material = materialArray[savedIndex];
            }
        }
        else
        {
            Debug.LogWarning($"Saved material index {savedIndex} is out of bounds! Check your Material Array size.");
        }
    }
}