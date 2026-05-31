using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NormalizationController : MonoBehaviour
{
    [Header("Button")]
    public Button normalizeButton;

    [Header("Number UI")]
    public GameObject numberPanel;
    public TextMeshProUGUI whoNumberText;
    public TextMeshProUGUI amNumberText;
    public TextMeshProUGUI iNumberText;

    [Header("Link Build")]
    public RectTransform linkContainer;
    public GameObject linePrefab;
    public GameObject flowDotPrefab;
    public RectTransform[] sourcePoints;
    public RectTransform[] targetPoints;

    [Header("Final Effects")]
    public ParticleSystem particleEffect;
    public GameObject linkRoot;
    public GameObject nextLevelPopup;

    [Header("Timing")]
    public float numberJumpDuration = 1.5f;
    public float afterParticleDelay = 0.8f;
    public float linkBuildDelay = 0.15f;
    public float flowDuration = 1.0f;

    [Header("Line Style")]
    public float lineThickness = 8f;
    public bool clearOldLinksBeforeBuild = true;
    [Header("Advanced Particle Settings")]
    public int particleTypeCount = 5;
    public Color[] particleColors = new Color[5]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(1f, 0f, 1f)
    };

    public Vector2 globalStartOffset = Vector2.zero;
    public Vector2 globalEndOffset = Vector2.zero;

    public bool useCurvedFlow = true;
    public float curveHeightMin = 20f;
    public float curveHeightMax = 60f;

    public bool useRandomStartSpread = true;
    public Vector2 startSpread = new Vector2(20f, 20f);

    public bool useRandomEndSpread = false;
    public Vector2 endSpread = new Vector2(10f, 10f);

    public bool useSineMotion = false;
    public float sineAmplitude = 5f;
    public float sineFrequency = 2f;

    public float minParticleDuration = 0.8f;
    public float maxParticleDuration = 1.4f;
    // ★ 新增：粒子数量控制
    [Header("Particle Settings")]
    public int particlesPerLink = 15;  // 每条连线的粒子数量，原来默认是1，现在可以增加到15、30等

    private int whoWeight;
    private int amWeight;
    private int iWeight;

    private bool hasNormalized = false;
    private bool isNormalizing = false;

    private readonly List<GameObject> spawnedLinks = new List<GameObject>();
    private readonly List<GameObject> spawnedDots = new List<GameObject>();

    void Start()
    {
        Debug.Log(gameObject.name + " -> Start");

        if (normalizeButton != null)
        {
            normalizeButton.gameObject.SetActive(false);
            normalizeButton.onClick.RemoveAllListeners();
            normalizeButton.onClick.AddListener(OnNormalizeButtonClicked);
        }
        else
        {
            Debug.LogError("NormalizationController: normalizeButton 未赋值！");
        }

        if (numberPanel != null)
            numberPanel.SetActive(false);

        if (linkRoot != null)
            linkRoot.SetActive(false);

        if (nextLevelPopup != null)
            nextLevelPopup.SetActive(false);
    }

    public void SetRawWeights(int who, int am, int i)
    {
        whoWeight = who;
        amWeight = am;
        iWeight = i;
        Debug.Log(gameObject.name + " -> Raw weights: " + whoWeight + ", " + amWeight + ", " + iWeight);
    }

    public void ShowNormalizeButton()
    {
        if (hasNormalized)
        {
            Debug.Log("ShowNormalizeButton: 已经归一化过了，跳过");
            return;
        }

        if (normalizeButton != null)
        {
            normalizeButton.gameObject.SetActive(true);
            normalizeButton.interactable = true;
            Debug.Log("NormalizeButton 已显示");
        }
        else
        {
            Debug.LogError("ShowNormalizeButton: normalizeButton 为空！");
        }
    }

    public void HideNormalizeButton()
    {
        if (normalizeButton != null)
        {
            normalizeButton.gameObject.SetActive(false);
            Debug.Log("NormalizeButton 已隐藏");
        }
    }

    public void OnNormalizeButtonClicked()
    {
        Debug.Log("=== OnNormalizeButtonClicked 被调用 ===");
        Debug.Log($"isNormalizing={isNormalizing}, hasNormalized={hasNormalized}");

        if (isNormalizing)
        {
            Debug.Log("归一化已在进行中，忽略本次点击");
            return;
        }

        isNormalizing = true;
        hasNormalized = true;

        Debug.Log("开始执行归一化流程");

        HideNormalizeButton();

        if (normalizeButton != null)
            normalizeButton.interactable = false;

        StartCoroutine(NormalizeRoutine());
    }

    IEnumerator NormalizeRoutine()
    {
        Debug.Log("NormalizeRoutine 开始执行");

        if (numberPanel != null)
        {
            numberPanel.SetActive(true);
            Debug.Log("数字面板已显示");
        }

        float timer = 0f;
        float jumpInterval = 0.08f;

        while (timer < numberJumpDuration)
        {
            timer += jumpInterval;

            if (whoNumberText != null)
                whoNumberText.text = Random.Range(0f, 1f).ToString("0.00");

            if (amNumberText != null)
                amNumberText.text = Random.Range(0f, 1f).ToString("0.00");

            if (iNumberText != null)
                iNumberText.text = Random.Range(0f, 1f).ToString("0.00");

            yield return new WaitForSeconds(jumpInterval);
        }

        Debug.Log("数字跳动动画完成");

        float total = whoWeight + amWeight + iWeight;
        if (total <= 0f) total = 1f;

        if (whoNumberText != null)
            whoNumberText.text = (whoWeight / total).ToString("0.00");
        if (amNumberText != null)
            amNumberText.text = (amWeight / total).ToString("0.00");
        if (iNumberText != null)
            iNumberText.text = (iWeight / total).ToString("0.00");

        Debug.Log($"归一化结果: who={whoWeight / total:F2}, am={amWeight / total:F2}, i={iWeight / total:F2}");

        BuildLinks();

        if (particleEffect != null)
        {
            particleEffect.Play();
            Debug.Log("粒子效果已播放");
        }

        yield return new WaitForSeconds(afterParticleDelay);

        if (nextLevelPopup != null)
        {
            nextLevelPopup.SetActive(true);
            Debug.Log("下一关弹窗已显示");
        }

        Debug.Log("NormalizeRoutine 执行完毕");
    }

    public void BuildLinks()
    {
        Debug.Log("BuildLinks 开始执行");

        if (linkRoot != null)
        {
            linkRoot.SetActive(true);
            Debug.Log("linkRoot 已激活");
        }

        if (linkContainer == null)
        {
            Debug.LogError("linkContainer 为空！");
            return;
        }

        if (linePrefab == null)
        {
            Debug.LogError("linePrefab 为空！");
            return;
        }

        if (flowDotPrefab == null)
        {
            Debug.LogError("flowDotPrefab 为空！");
            return;
        }

        if (sourcePoints == null || targetPoints == null || sourcePoints.Length < 3 || targetPoints.Length < 3)
        {
            Debug.LogError($"sourcePoints 或 targetPoints 无效: srcLen={sourcePoints?.Length}, tgtLen={targetPoints?.Length}");
            return;
        }

        if (clearOldLinksBeforeBuild)
        {
            ClearLinks();
            Debug.Log("已清除旧连线");
        }

        // ★ 修改：为每条连线创建多个粒子（根据 particlesPerLink 数量）
        for (int i = 0; i < 3; i++)
        {
            if (sourcePoints[i] == null)
            {
                Debug.LogWarning($"sourcePoints[{i}] 为空");
                continue;
            }
            if (targetPoints[i] == null)
            {
                Debug.LogWarning($"targetPoints[{i}] 为空");
                continue;
            }

            // ★ 关键修改：循环创建多个粒子，而不是只创建一个
            for (int p = 0; p < particlesPerLink; p++)
            {
                // 为每个粒子添加一点延迟偏移，形成流动效果
                float delayOffset = p * linkBuildDelay / particlesPerLink;
                CreateSingleLink(sourcePoints[i], targetPoints[i], i, p, delayOffset);
            }
        }

        Debug.Log($"BuildLinks 完成，创建了 {spawnedLinks.Count} 条连线和 {spawnedDots.Count} 个流动点（每个连线 {particlesPerLink} 个粒子）");
    }

    // ★ 修改：增加 particleIndex 和 delayOffset 参数
    void CreateSingleLink(RectTransform start, RectTransform end, int linkIndex, int particleIndex, float delayOffset)
    {
        Vector2 localStart;
        Vector2 localEnd;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            linkContainer,
            RectTransformUtility.WorldToScreenPoint(null, start.position),
            null,
            out localStart
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            linkContainer,
            RectTransformUtility.WorldToScreenPoint(null, end.position),
            null,
            out localEnd
        );

        localStart += globalStartOffset;
        localEnd += globalEndOffset;

        if (particleIndex == 0)
        {
            GameObject lineObj = Instantiate(linePrefab, linkContainer);
            lineObj.name = $"UILinkLine_{linkIndex}";
            spawnedLinks.Add(lineObj);

            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            if (lineRect != null)
            {
                Vector2 dir = localEnd - localStart;
                float distance = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0f, 0.5f);
                lineRect.anchoredPosition = localStart;
                lineRect.sizeDelta = new Vector2(distance, lineThickness);
                lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                lineRect.localScale = Vector3.one;
            }
        }

        GameObject dotObj = Instantiate(flowDotPrefab, linkContainer);
        dotObj.name = $"UILinkDot_{linkIndex}_{particleIndex}";
        spawnedDots.Add(dotObj);

        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        if (dotRect != null)
        {
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.localScale = Vector3.one;

            Vector2 startOffset = Vector2.zero;
            Vector2 endOffset = Vector2.zero;

            if (useRandomStartSpread)
            {
                startOffset = new Vector2(
                    Random.Range(-startSpread.x, startSpread.x),
                    Random.Range(-startSpread.y, startSpread.y)
                );
            }

            if (useRandomEndSpread)
            {
                endOffset = new Vector2(
                    Random.Range(-endSpread.x, endSpread.x),
                    Random.Range(-endSpread.y, endSpread.y)
                );
            }

            float startProgress = particlesPerLink <= 1 ? 0f : (float)particleIndex / (particlesPerLink - 1);
            Vector2 startPosition = Vector2.Lerp(localStart, localEnd, startProgress);
            dotRect.anchoredPosition = startPosition + startOffset;

            UILinkFlow flow = dotObj.GetComponent<UILinkFlow>();
            if (flow == null)
                flow = dotObj.AddComponent<UILinkFlow>();

            flow.playOnEnable = false;
            flow.loop = true;
            flow.delayStart = delayOffset;
            flow.duration = Random.Range(minParticleDuration, maxParticleDuration);
            flow.useCurve = useCurvedFlow;
            flow.curveHeight = Random.Range(curveHeightMin, curveHeightMax);
            flow.startOffset = startOffset;
            flow.endOffset = endOffset;
            flow.useSineOffset = useSineMotion;
            flow.sineAmplitude = sineAmplitude;
            flow.sineFrequency = sineFrequency;
            flow.SetPoints(dotRect, localStart, localEnd);
            flow.Play();
        }

        Graphic g = dotObj.GetComponent<Graphic>();
        if (g != null && particleColors != null && particleColors.Length > 0)
        {
            int colorIndex = particleIndex % Mathf.Min(particleTypeCount, particleColors.Length);
            g.color = particleColors[colorIndex];
        }
    }

    public void ClearLinks()
    {
        Debug.Log($"清除连线: {spawnedLinks.Count} 条线, {spawnedDots.Count} 个点");

        for (int i = 0; i < spawnedLinks.Count; i++)
        {
            if (spawnedLinks[i] != null)
                Destroy(spawnedLinks[i]);
        }

        for (int i = 0; i < spawnedDots.Count; i++)
        {
            if (spawnedDots[i] != null)
                Destroy(spawnedDots[i]);
        }

        spawnedLinks.Clear();
        spawnedDots.Clear();
    }

    public void ResetNormalization()
    {
        StopAllCoroutines();
        isNormalizing = false;
        hasNormalized = false;

        ClearLinks();

        if (numberPanel != null)
            numberPanel.SetActive(false);

        if (linkRoot != null)
            linkRoot.SetActive(false);

        if (nextLevelPopup != null)
            nextLevelPopup.SetActive(false);

        if (normalizeButton != null)
        {
            normalizeButton.gameObject.SetActive(false);
            normalizeButton.interactable = true;
        }

        Debug.Log("NormalizationController 已重置");
    }
}