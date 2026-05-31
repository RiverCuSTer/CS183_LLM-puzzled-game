using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class FeatureBlock : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Basic Data")]
    public string featureId = "feature";
    public Level4Action correctAction = Level4Action.None;
    public int outputOrder = 0;

    [Header("UI References")]
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public Image stateLightImage;
    public Image vanishImage;
    public TextMeshProUGUI labelText;

    [Header("Display")]
    public string displayName = "特征";

    private Level4Manager manager;

    private Vector2 startAnchoredPosition;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;
    private int originalSiblingIndex;

    private Level4Action appliedAction = Level4Action.None;
    private bool activated = false;
    private bool vanished = false;
    private bool placed = false;
    private bool dragging = false;

    private Coroutine scaleRoutine;

    public void Init(Level4Manager levelManager)
    {
        manager = levelManager;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (labelText != null)
        {
            labelText.text = displayName;
        }

        if (stateLightImage != null)
        {
            stateLightImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (vanishImage != null)
        {
            vanishImage.gameObject.SetActive(false);
        }

        if (rectTransform != null)
        {
            startAnchoredPosition = rectTransform.anchoredPosition;
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        appliedAction = Level4Action.None;
        activated = false;
        vanished = false;
        placed = false;
        dragging = false;

        transform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dragging)
        {
            return;
        }

        if (manager != null)
        {
            manager.OnFeatureClicked(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
        {
            dragging = false;
            return;
        }

        dragging = true;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        if (rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
            startAnchoredPosition = rectTransform.anchoredPosition;
        }

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        transform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
        }

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }

        if (!CanDrag())
        {
            return;
        }

        if (rectTransform == null)
        {
            return;
        }

        Vector2 delta = eventData.delta;
        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null)
        {
            delta = delta / parentCanvas.scaleFactor;
        }

        rectTransform.anchoredPosition += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
        {
            return;
        }

        dragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (!CanDrag())
        {
            ReturnToStart();
            return;
        }

        if (manager == null)
        {
            ReturnToStart();
            return;
        }

        OutputSlot targetSlot;

        if (manager.IsPointerOverAnySlot(eventData.position, out targetSlot))
        {
            bool success = manager.TryPlaceFeatureInSlot(this, targetSlot);

            if (success)
            {
                placed = true;
                transform.localScale = Vector3.one;
                return;
            }

            ReturnToStart();
            PlayShakeAnimation();
            return;
        }

        ReturnToStart();
    }

    public void ApplyAction(Level4Action action, Sprite lightSprite)
    {
        appliedAction = action;
        activated = true;

        if (stateLightImage != null)
        {
            stateLightImage.sprite = lightSprite;
            stateLightImage.color = Color.white;
        }

        PlayPopAnimation();
    }

    public IEnumerator PlayVanishAnimation(Sprite[] frames, float frameTime)
    {
        if (vanished)
        {
            yield break;
        }

        vanished = true;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        if (vanishImage != null && frames != null && frames.Length > 0)
        {
            vanishImage.gameObject.SetActive(true);

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    vanishImage.sprite = frames[i];
                }

                yield return new WaitForSeconds(frameTime);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        float time = 0f;
        float duration = 0.15f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void PlayPopAnimation()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }

        scaleRoutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        Vector3 normalScale = Vector3.one;
        Vector3 bigScale = Vector3.one * 1.12f;

        float time = 0f;
        float duration = 0.1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.localScale = Vector3.Lerp(normalScale, bigScale, t);
            yield return null;
        }

        time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.localScale = Vector3.Lerp(bigScale, normalScale, t);
            yield return null;
        }

        transform.localScale = normalScale;
        scaleRoutine = null;
    }

    public void PlayShakeAnimation()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }

        scaleRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        if (rectTransform == null)
        {
            yield break;
        }

        Vector2 originalPos = rectTransform.anchoredPosition;

        for (int i = 0; i < 6; i++)
        {
            float offset = 8f;

            if (i % 2 == 0)
            {
                rectTransform.anchoredPosition = originalPos + new Vector2(offset, 0f);
            }
            else
            {
                rectTransform.anchoredPosition = originalPos + new Vector2(-offset, 0f);
            }

            yield return new WaitForSeconds(0.035f);
        }

        rectTransform.anchoredPosition = originalPos;
        scaleRoutine = null;
    }

    public void ReturnToStart()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        transform.localScale = Vector3.one;

        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void MoveToWorldPosition(Vector3 worldPosition)
    {
        if (rectTransform != null)
        {
            rectTransform.position = worldPosition;
        }
    }

    private bool CanDrag()
    {
        if (!activated)
        {
            return false;
        }

        if (vanished)
        {
            return false;
        }

        if (placed)
        {
            return false;
        }

        if (appliedAction == Level4Action.Refrain)
        {
            return false;
        }

        return true;
    }

    public bool IsActivated()
    {
        return activated;
    }

    public bool IsVanished()
    {
        return vanished;
    }

    public bool IsPlaced()
    {
        return placed;
    }

    public void SetPlaced(bool value)
    {
        placed = value;
    }

    public Level4Action GetAppliedAction()
    {
        return appliedAction;
    }
}
