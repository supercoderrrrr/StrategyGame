using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private LayerMask tooltipLayerMask;
    [SerializeField] private float hoverDelay = 0.5f;

    private float hoverTimer = 0f;
    private GameObject currentHoveredObject = null;

    private void Start()
    {
        HideTooltip();
    }

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tooltipLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject == currentHoveredObject)
            {
                hoverTimer += Time.deltaTime;

                if (hoverTimer >= hoverDelay && !tooltipPanel.activeSelf)
                {
                    TryShowTooltip(hit.collider);
                }
            }
            else
            {
                currentHoveredObject = hitObject;
                hoverTimer = 0f;
                HideTooltip();
            }
        }
        else
        {
            if (currentHoveredObject != null)
            {
                currentHoveredObject = null;
                hoverTimer = 0f;
                HideTooltip();
            }
        }

        if (tooltipPanel.activeSelf)
        {
            FollowMouse();
        }
    }

    private void TryShowTooltip(Collider hitCollider)
    {
        Unit unit = hitCollider.GetComponentInParent<Unit>();
        if (unit != null)
        {
            ShowUnitTooltip(unit);
            return;
        }

        TileInfo tileInfo = hitCollider.GetComponentInParent<TileInfo>();
        if (tileInfo != null)
        {
            ShowTileTooltip(tileInfo);
            return;
        }
    }

    private void ShowUnitTooltip(Unit unit)
    {
        tooltipPanel.SetActive(true);
        titleText.text = unit.GetUnitName();

        healthText.gameObject.SetActive(true);
        healthText.text = $"{unit.GetHealth()} / {unit.GetHealthMax()}";

        descriptionText.text = unit.GetUnitDescription();
    }

    private void ShowTileTooltip(TileInfo tile)
    {
        tooltipPanel.SetActive(true);
        titleText.text = tile.tileName;

        healthText.gameObject.SetActive(false);

        descriptionText.text = tile.tileDescription;
    }

    private void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void FollowMouse()
    {
        Vector2 mousePosition = Input.mousePosition;
        transform.position = mousePosition + new Vector2(10f, 10f);
    }
}