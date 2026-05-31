using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GlowButton : MonoBehaviour
{
    [Header("Button Data")]
    public Level4Action actionType = Level4Action.None;

    [Header("References")]
    public Button button;
    public Image layer1;
    public Image layer2;
    public Image layer3;

    [Header("Animation")]
    public float selectedAlpha = 1f;
    public float unselectedAlpha = 0.35f;
    public float pulseScale = 1.12f;

    private Level4Manager manager;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        // 启动时先强制恢复基础态，避免场景里残留选中透明度
        SetSelected(false);
    }

    public void Init(Level4Manager levelManager)
    {
        manager = levelManager;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        SetSelected(false);
    }

    public void OnClicked()
{
    Debug.Log($"{name} clicked. actionType = {actionType}, manager = {manager}");

    if (manager == null)
    {
        Debug.LogWarning($"{name}: GlowButton manager is null. Please check Level4Manager actionButtons array.");
        return;
    }

    if (actionType == Level4Action.None)
    {
        Debug.LogWarning($"{name}: actionType is None. Please set it in Inspector.");
        return;
    }

    manager.SelectAction(actionType);
}


    public void SetSelected(bool selected)
    {
        // Layer_1：基础底图，永远显示，只是选中时更亮
        SetImageAlpha(layer1, selected ? selectedAlpha : unselectedAlpha);

        // Layer_2 / Layer_3：选中态光效，未选中时隐藏
        SetImageAlpha(layer2, selected ? selectedAlpha : 0f);
        SetImageAlpha(layer3, selected ? selectedAlpha : 0f);

        if (selected)
        {
            PlayClickPulse();
        }
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void PlayClickPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        Vector3 normalScale = Vector3.one;
        Vector3 bigScale = Vector3.one * pulseScale;

        float time = 0f;
        float duration = 0.08f;

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
    }

}
