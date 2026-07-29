using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteBlinkEffect : MonoBehaviour
{
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;

    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    public float blinkSpeed = 5f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        Color c = spriteRenderer.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha,
            Mathf.PingPong(Time.time * blinkSpeed, 1f));

        spriteRenderer.color = c;
    }

    private void OnDisable()
    {
        Color c = spriteRenderer.color;
        c.a = 1f;
        spriteRenderer.color = c;
    }
}