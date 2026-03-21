using System.Collections;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float duration = 0.08f;
    [SerializeField] private float fadeSpeed = 8f;

    private float _initialAlpha;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        _initialAlpha = lineRenderer.startColor.a;
    }

    public void Show(Vector3 from, Vector3 to)
    {
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
        StopAllCoroutines();
        StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(_initialAlpha, 0f, elapsed / duration);

            SetAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        Color start = lineRenderer.startColor;
        Color end = lineRenderer.endColor;
        start.a = alpha;
        end.a = alpha * 0.2f;
        lineRenderer.startColor = start;
        lineRenderer.endColor   = end;
    }
}