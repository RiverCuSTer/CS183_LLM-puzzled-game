using UnityEngine;
using System.Collections;

public class UILinkFlow : MonoBehaviour
{
    [Header("Play")]
    public bool playOnEnable = true;
    public bool loop = true;
    public float duration = 1f;
    public float delayStart = 0f;

    [Header("Path")]
    public bool useCurve = true;
    public float curveHeight = 30f;
    public Vector2 startOffset = Vector2.zero;
    public Vector2 endOffset = Vector2.zero;

    [Header("Motion")]
    public AnimationCurve motionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public bool useSineOffset = false;
    public float sineAmplitude = 6f;
    public float sineFrequency = 2f;

    private RectTransform rectTransform;
    private Vector2 startPoint;
    private Vector2 endPoint;
    private Coroutine playRoutine;

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    public void SetPoints(RectTransform rect, Vector2 start, Vector2 end)
    {
        rectTransform = rect;
        startPoint = start;
        endPoint = end;
    }

    public void Play()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(FlowAnimation());
    }

    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    IEnumerator FlowAnimation()
    {
        if (delayStart > 0f)
            yield return new WaitForSeconds(delayStart);

        do
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float evalT = motionCurve != null ? motionCurve.Evaluate(t) : t;

                Vector2 s = startPoint + startOffset;
                Vector2 e = endPoint + endOffset;
                Vector2 pos;

                if (useCurve)
                {
                    Vector2 mid = (s + e) * 0.5f;
                    Vector2 dir = e - s;
                    Vector2 normal = dir.sqrMagnitude > 0.001f
                        ? new Vector2(-dir.y, dir.x).normalized
                        : Vector2.up;

                    Vector2 control = mid + normal * curveHeight;

                    Vector2 a = Vector2.Lerp(s, control, evalT);
                    Vector2 b = Vector2.Lerp(control, e, evalT);
                    pos = Vector2.Lerp(a, b, evalT);
                }
                else
                {
                    pos = Vector2.Lerp(s, e, evalT);
                }

                if (useSineOffset)
                {
                    Vector2 dir = e - s;
                    Vector2 normal = dir.sqrMagnitude > 0.001f
                        ? new Vector2(-dir.y, dir.x).normalized
                        : Vector2.up;

                    float wave = Mathf.Sin(evalT * Mathf.PI * 2f * sineFrequency) * sineAmplitude;
                    pos += normal * wave;
                }

                if (rectTransform != null)
                    rectTransform.anchoredPosition = pos;

                yield return null;
            }

            if (rectTransform != null)
                rectTransform.anchoredPosition = endPoint + endOffset;

            if (loop && rectTransform != null)
                rectTransform.anchoredPosition = startPoint + startOffset;

        } while (loop);

        playRoutine = null;
    }
}