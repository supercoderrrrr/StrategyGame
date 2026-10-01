using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float transitionSpeed = 15f;

    [SerializeField] private float bounceDuration = 0.4f;
    [SerializeField]
    private AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.2f, 0.9f),
        new Keyframe(0.4f, 1.15f),
        new Keyframe(0.6f, 0.95f),
        new Keyframe(1f, 1f)
    );

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Transform transformToAnimate;

    private bool isBouncing = false;
    private float bounceTimer = 0f;

    private void Awake()
    {
        transformToAnimate = transform;
        originalScale = transformToAnimate.localScale;
        targetScale = originalScale;
    }

    private void OnEnable()
    {
        transformToAnimate.localScale = originalScale;
        targetScale = originalScale;
        isBouncing = false;
    }

    private void OnDisable()
    {
        transformToAnimate.localScale = originalScale;
    }

    private void Update()
    {
        if (isBouncing)
        {
            bounceTimer += Time.unscaledDeltaTime;
            float progress = bounceTimer / bounceDuration;

            if (progress < 1f)
            {
                float curveValue = bounceCurve.Evaluate(progress);
                transformToAnimate.localScale = originalScale * curveValue;
            }
            else
            {
                isBouncing = false;
            }
        }

        else
        {
            transformToAnimate.localScale = Vector3.Lerp(
                transformToAnimate.localScale,
                targetScale,
                Time.unscaledDeltaTime * transitionSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //While mouse stay on it, amplify
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Mouse move away, then return button size to original size
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //Click mouse: bounce animation
        isBouncing = true;
        bounceTimer = 0f;
    }
}