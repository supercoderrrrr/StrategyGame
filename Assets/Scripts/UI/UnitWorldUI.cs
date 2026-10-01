using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private Unit unit;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private HealthSystem healthSystem;

    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Transform damagePopupPrefab;

    private CanvasGroup canvasGroup;
    private ShootAction shootAction;
    private float targetAlpha = 1f;
    private float fadeSpeed = 5f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        Unit.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;
        healthSystem.OnDamaged += HealthSystem_OnDamaged;

        shootAction = unit.GetAction<ShootAction>();
        if (shootAction != null)
        {
            shootAction.OnShootActionStarted += ShootAction_OnShootActionStarted;
            shootAction.OnShootActionCompleted += ShootAction_OnShootActionCompleted;
        }

        UpdateActionPointsText();
        UpdateHealthBar();
    }

    private void Update()
    {
        if (canvasGroup.alpha != targetAlpha)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    private void ShootAction_OnShootActionStarted(object sender, EventArgs e)
    {
        targetAlpha = 0f;
    }

    private void ShootAction_OnShootActionCompleted(object sender, EventArgs e)
    {
        targetAlpha = 1f;
    }

    private void UpdateActionPointsText()
    {
        actionPointsText.text = unit.GetActionPoints().ToString();
    }

    private void Unit_OnAnyActionPointsChanged(object sender, EventArgs eventArgs)
    {
        UpdateActionPointsText();
    }

    private void UpdateHealthBar()
    {
        healthBarImage.fillAmount = healthSystem.GetHealthNormalized();

        if (healthText != null)
        {
            healthText.text = healthSystem.GetHealth().ToString();
        }
    }

    private void HealthSystem_OnDamaged(object sender, HealthSystem.OnDamagedEventArgs e)
    {
        UpdateHealthBar();

        if (damagePopupPrefab != null)
        {
            Transform popupParent = healthBarImage.canvas.transform;

            Transform damagePopupTransform = Instantiate(damagePopupPrefab, popupParent);

            Vector3 verticalOffset = new Vector3(0, 0.5f, 0);
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0, 0);
            damagePopupTransform.position = transform.position + verticalOffset + randomOffset;

            damagePopupTransform.localScale = Vector3.one;
            damagePopupTransform.localRotation = Quaternion.identity;

            DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();
            if (damagePopup != null)
            {
                damagePopup.Setup(e.damageAmount,e.isCriticalHit, e.isDodge);
            }
            else
            {
                Debug.LogError("UnitWorldUI: No Script Found In DamagePopup Prefab!");
            }
        }
    }

    private void OnDestroy()
    {
        Unit.OnAnyActionPointsChanged -= Unit_OnAnyActionPointsChanged;

        if (healthSystem != null)
        {
            healthSystem.OnDamaged -= HealthSystem_OnDamaged;
        }

        if (shootAction != null)
        {
            shootAction.OnShootActionStarted -= ShootAction_OnShootActionStarted;
            shootAction.OnShootActionCompleted -= ShootAction_OnShootActionCompleted;
        }
    }
}
