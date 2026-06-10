// Responsible team member: Hanyun Zhu, Zhiyu Huang; Description: Validates attention weights and drives success or failure feedback for the attention puzzle.
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AttentionInputManager : MonoBehaviour
{
    [Header("Controllers")]
    public Shift shiftController;
    public AttentionBalanceController balanceController;
    public NormalizationController normalizationController;

    [Header("Result UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI errorText;

    [Header("Explanation Popup")]
    public GameObject explanationPanel;
    public TextMeshProUGUI explanationText;
    public float explanationDuration = 3f;

    [Header("Stars")]
    public GameObject blueStar;
    public GameObject yellowStar;
    public GameObject perfectStar;

    [Header("Timing")]
    public float perfectStarDuration = 1.0f;
    public float redLineDuration = 5f;

    // Node order must match AttentionGraph:
    // 0 = Who
    // 1 = Am
    // 2 = I
    //
    // 0 = thin (0.5), 1 = medium (1), 2 = thick (2)
    private int[,] correctWeights = new int[3, 3]
    {
        { 2, 1, 0 }, // Who -> Who, Who -> Am, Who -> I
        { 2, 0, 2 }, // Am  -> Who, Am  -> Am, Am  -> I
        { 0, 1, 2 }  // I   -> Who, I   -> Am, I   -> I
    };

    private int whoWeight;
    private int amWeight;
    private int iWeight;

    void Start()
    {
        HideAllStars();

        if (errorText != null)
            errorText.text = "";

        if (resultText != null)
            resultText.text = "";

        if (explanationPanel != null)
            explanationPanel.SetActive(false);
    }

    public void ValidateAttentionWeights(float[,] weights, Shift shift)
    {
        Debug.Log("ValidateAttentionWeights called");

        HideAllStars();

        if (errorText != null)
            errorText.text = "";

        int errorCount = CountErrorsFromWeights(weights);
        Debug.Log($"Error count = {errorCount}");

        if (errorCount == 0)
        {
            CalculateWeightCounts(weights);
            StartCoroutine(SuccessFlow(shift));
        }
        else
        {
            ShowFailureStar(errorCount);
            StartCoroutine(ShowRedLinesAndReturn(shift));
        }
    }

    int CountErrorsFromWeights(float[,] w)
    {
        int err = 0;

        for (int from = 0; from < 3; from++)
        {
            for (int to = 0; to < 3; to++)
            {
                int playerLevel = FloatToLevel(w[from, to]);

                if (playerLevel != correctWeights[from, to])
                {
                    err++;
                }
            }
        }

        return err;
    }

    int FloatToLevel(float value)
    {
        if (value >= 1.5f) return 2;   // Thick line = 2
        if (value >= 0.75f) return 1;  // Medium line = 1
        return 0;                      // Thin line = 0.5
    }

    void CalculateWeightCounts(float[,] weights)
    {
        // Forced values for this puzzle step.
        whoWeight = 7;
        amWeight = 9;
        iWeight = 7;
        Debug.Log($"Forced weight counts: Who={whoWeight}, Am={amWeight}, I={iWeight}");
    }
    IEnumerator SuccessFlow(Shift shift)
    {
        HideAllStars();

        if (perfectStar != null)
        {
            perfectStar.SetActive(true);
            yield return new WaitForSeconds(perfectStarDuration);
            perfectStar.SetActive(false);
        }

        yield return StartCoroutine(ShowExplanationPopup());

        if (balanceController != null)
        {
            balanceController.ShowBalanceWithWeights(whoWeight, amWeight, iWeight, () =>
            {
                if (normalizationController != null)
                {
                    normalizationController.SetRawWeights(whoWeight, amWeight, iWeight);
                    normalizationController.ShowNormalizeButton();
                }
            });
        }
        else if (normalizationController != null)
        {
            normalizationController.SetRawWeights(whoWeight, amWeight, iWeight);
            normalizationController.ShowNormalizeButton();
        }

        if (shift != null)
            shift.EndDialogue();
    }

    IEnumerator ShowExplanationPopup()
    {
        if (explanationPanel == null)
            yield break;

        explanationPanel.SetActive(true);

        if (explanationText != null)
        {
            explanationText.text =
                "Correct!\n" +
                "Now we convert each token's three attention values into weights.\n" +
                "Weight count = sum of the three values × 2.(Thick=2, Medium=1, Thin=0.5.)";
        }

        yield return new WaitForSeconds(explanationDuration);

        explanationPanel.SetActive(false);
    }

    List<AttentionGraph.GraphEdge> GetWrongEdges(float[,] weights)
    {
        List<AttentionGraph.GraphEdge> wrongEdges = new List<AttentionGraph.GraphEdge>();

        if (shiftController == null || shiftController.attentionGraph == null)
            return wrongEdges;

        for (int from = 0; from < 3; from++)
        {
            for (int to = 0; to < 3; to++)
            {
                int playerLevel = FloatToLevel(weights[from, to]);

                if (playerLevel != correctWeights[from, to])
                {
                    AttentionGraph.GraphEdge edge = shiftController.attentionGraph.GetEdge(from, to);

                    if (edge != null)
                        wrongEdges.Add(edge);
                }
            }
        }

        return wrongEdges;
    }

    IEnumerator ShowRedLinesAndReturn(Shift shift)
    {
        if (shift != null && shift.attentionGraph != null)
        {
            float[,] currentWeights = shift.attentionGraph.GetAttentionWeights();
            List<AttentionGraph.GraphEdge> wrongEdges = GetWrongEdges(currentWeights);

            shift.attentionGraph.HighlightEdges(wrongEdges, Color.red);

            yield return new WaitForSeconds(redLineDuration);

            shift.attentionGraph.ResetEdgesToDefault();
        }
        else
        {
            yield return new WaitForSeconds(redLineDuration);
        }

        if (shift != null)
            shift.RestartFromInputStage();
    }

    void ShowFailureStar(int errorCount)
    {
        HideAllStars();

        if (errorCount <= 3)
        {
            if (yellowStar != null)
                yellowStar.SetActive(true);
        }
        else
        {
            if (blueStar != null)
                blueStar.SetActive(true);
        }

        if (errorText != null)
            errorText.text = $"There are {errorCount} incorrect attention connections.";
    }

    public void HideAllStars()
    {
        if (blueStar != null) blueStar.SetActive(false);
        if (yellowStar != null) yellowStar.SetActive(false);
        if (perfectStar != null) perfectStar.SetActive(false);
    }
}
