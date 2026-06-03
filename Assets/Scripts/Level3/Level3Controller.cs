using System.Collections;
using UnityEngine;

public class Level3Controller : MonoBehaviour
{
    private const float UiScale = 3f;

    [SerializeField] private float layerHeight = 12f;
    [SerializeField] private float slideDuration = 0.8f;
    [SerializeField] private Camera levelCamera;

    private int currentLayer;
    private bool isSliding;
    private bool hasReportedLevelComplete;

    private readonly string[] layerNames =
    {
        "1 Word Relations",
        "2 Syntax Towers",
        "3 Semantic Scene",
        ""
    };

    private readonly string[] instructions =
    {
        "Connect word relations before climbing upward.",
        "Drag physical word blocks into the syntax towers; nouns are heavier, modifiers are lighter.",
        "",
        ""
    };

    private void Awake()
    {
        if (levelCamera == null)
            levelCamera = Camera.main;

        if (levelCamera != null)
        {
            levelCamera.orthographic = true;
            levelCamera.orthographicSize = 4.75f;
            levelCamera.transform.position = new Vector3(0f, 0f, -10f);
            levelCamera.backgroundColor = new Color(0.78f, 0.84f, 0.86f);
        }
    }

    private void OnGUI()
    {
        GUI.depth = -20;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(28 * UiScale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black }
        };

        GUIStyle instructionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(18 * UiScale),
            normal = { textColor = Color.black }
        };

        if (!string.IsNullOrEmpty(layerNames[currentLayer]))
            GUI.Label(new Rect(Screen.width * 0.5f - 900f, 8f, 1800f, 120f), layerNames[currentLayer], titleStyle);
        if (!string.IsNullOrEmpty(instructions[currentLayer]))
            GUI.Label(new Rect(Screen.width * 0.5f - 1120f, 150f, 2240f, 120f), instructions[currentLayer], instructionStyle);

        GUI.enabled = !isSliding;
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(14 * UiScale)
        };

        if (GUI.Button(new Rect(Screen.width - 585f, 18f, 555f, 132f), currentLayer == 3 ? "Finish Level" : "Complete Layer", buttonStyle))
            CompleteCurrentLayer();

        GUI.enabled = true;
    }

    public void CompleteCurrentLayer()
    {
        if (isSliding)
            return;

        if (currentLayer == 0 && Level3Layer1Manager.Instance != null && !Level3Layer1Manager.Instance.IsComplete)
        {
            Level3Layer1Manager.Instance.ShowBlockedMessage();
            return;
        }

        if (currentLayer == 1 && Level3Layer2Manager.Instance != null && !Level3Layer2Manager.Instance.IsComplete)
        {
            Level3Layer2Manager.Instance.ShowBlockedMessage();
            return;
        }

        if (currentLayer == 2 && Level3Layer3Manager.Instance != null && !Level3Layer3Manager.Instance.IsComplete)
        {
            Level3Layer3Manager.Instance.ShowBlockedMessage();
            return;
        }

        if (currentLayer == 3 && Level3Layer4Manager.Instance != null && !Level3Layer4Manager.Instance.IsComplete)
        {
            Level3Layer4Manager.Instance.ShowBlockedMessage();
            return;
        }

        if (currentLayer >= layerNames.Length - 1)
        {
            if (!hasReportedLevelComplete)
            {
                hasReportedLevelComplete = true;
                GameManager.MarkLevelCompleted(3);
                GameManager.ReturnToLevelSelect();
            }
            return;
        }

        StartCoroutine(SlideToLayer(currentLayer + 1));
    }

    private IEnumerator SlideToLayer(int targetLayer)
    {
        if (levelCamera == null)
            yield break;

        isSliding = true;
        Vector3 from = levelCamera.transform.position;
        Vector3 to = new Vector3(from.x, targetLayer * layerHeight, from.z);
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            levelCamera.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        levelCamera.transform.position = to;
        currentLayer = targetLayer;
        isSliding = false;
    }
}
