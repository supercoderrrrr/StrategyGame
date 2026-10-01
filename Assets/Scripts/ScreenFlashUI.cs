using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlashUI : MonoBehaviour
{
    public static ScreenFlashUI Instance { get; private set; }

    [SerializeField] private Image flashImage;
    [SerializeField] private float flashSpeed = 2f;

    private void Awake()
    {
        Instance = this;
        if (flashImage == null) flashImage = GetComponent<Image>();

        Color color = flashImage.color;
        color.a = 0f;
        flashImage.color = color;
    }

    public void TriggerFlash()
    {
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Color color = flashImage.color;
        color.a = 1f;
        flashImage.color = color;

        while (flashImage.color.a > 0)
        {
            color.a -= Time.deltaTime * flashSpeed;
            flashImage.color = color;
            yield return null;
        }
    }
}