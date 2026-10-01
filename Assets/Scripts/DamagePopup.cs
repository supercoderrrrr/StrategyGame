using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private Color criticalColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color dodgeColor = new Color(0.2f, 0.8f, 1f);

    [SerializeField] private float stayTime = 0.5f;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float shrinkSpeed = 1f;

    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;

    public void Setup(int damageAmount, bool isCriticalHit, bool isDodge = false)
    {
        if (isCriticalHit)
        {
            textMesh.text = "-" + damageAmount.ToString() + "!!!";
            textMesh.fontSize *= 1.5f;
            textMesh.color = criticalColor;
        }
        else if(isDodge){
            textMesh.text = "DODGE!";
            textMesh.color = dodgeColor;
        }
        else
        {
            textMesh.text = "-" + damageAmount.ToString();
        }

        textColor = textMesh.color;
        disappearTimer = stayTime;
        moveVector = new Vector3(0, 1.5f, 0);
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;

        transform.localScale -= Vector3.one * shrinkSpeed * Time.deltaTime;
        
        if (transform.localScale.x < 0)
        {
            transform.localScale = Vector3.zero;
        }

        disappearTimer -= Time.deltaTime;

        if (disappearTimer < 0)
        {
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
