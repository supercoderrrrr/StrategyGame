using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUnitSelector : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer unitRenderer;
    [SerializeField] private Material[] materialArray;

    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private int currentMaterialIndex = 0;
    private const string PLAYER_PREFS_MATERIAL_KEY = "SelectedUnitMaterial";

    private void Start()
    {
        currentMaterialIndex = PlayerPrefs.GetInt(PLAYER_PREFS_MATERIAL_KEY, 0);
        UpdateMaterial();

        prevButton.onClick.AddListener(() => {
            currentMaterialIndex--;
            if (currentMaterialIndex < 0)
            {
                currentMaterialIndex = materialArray.Length - 1;
            }
            UpdateMaterial();
        });

        nextButton.onClick.AddListener(() => {
            currentMaterialIndex++;
            if (currentMaterialIndex >= materialArray.Length)
            {
                currentMaterialIndex = 0;
            }
            UpdateMaterial();
        });
    }

    private void UpdateMaterial()
    {
        if (unitRenderer != null && materialArray.Length > 0)
        {
            unitRenderer.material = materialArray[currentMaterialIndex];
        }

        PlayerPrefs.SetInt(PLAYER_PREFS_MATERIAL_KEY, currentMaterialIndex);
        PlayerPrefs.Save();
    }
}