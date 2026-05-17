using UnityEngine;
using TMPro;
using System.Collections;

public class AttentionInputManager : MonoBehaviour
{
    [Header("Controllers")]
    public Shift shiftController;
    public AttentionBalanceController balanceController;
    public NormalizationController normalizationController;

    [Header("Result UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI errorText;

    [Header("Stars")]
    public GameObject blueStar;
    public GameObject yellowStar;
    public GameObject perfectStar;

    [Header("Timing")]
    public float failureReturnDelay = 1.2f;
    public float perfectStarDuration = 1.0f;

    private float[] values = new float[9];

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
    }

    public void ProcessConfirmedInputs(TMP_InputField[] inputFields)
    {
        Debug.Log("AttentionInputManager: ProcessConfirmedInputs called");

        HideAllStars();

        if (errorText != null)
            errorText.text = "";

        if (!ReadAllInputs(inputFields))
        {
            if (errorText != null)
                errorText.text = "Please fill in all 9 boxes with valid values between 0.0 and 1.0.";

            if (shiftController != null)
                shiftController.RestartFromInputStage();

            return;
        }

        CalculateWeights();

        int errorCount = CountErrors(values);

        Debug.Log("Attention error count = " + errorCount);

        if (resultText != null)
        {
            resultText.text =
                "Who: " + whoWeight +
                "   Am: " + amWeight +
                "   I: " + iWeight +
                "   Errors: " + errorCount;
        }

        if (errorCount == 0)
        {
            StartCoroutine(SuccessFlow());
        }
        else
        {
            ShowFailureStar(errorCount);
            StartCoroutine(ReturnToInputAfterDelay());
        }
    }

    bool ReadAllInputs(TMP_InputField[] inputFields)
    {
        if (inputFields == null || inputFields.Length != 9)
        {
            Debug.LogWarning("AttentionInputManager: inputFields must contain exactly 9 fields.");
            return false;
        }

        for (int i = 0; i < inputFields.Length; i++)
        {
            if (inputFields[i] == null)
            {
                Debug.LogWarning("AttentionInputManager: input field " + i + " is null.");
                return false;
            }

            string text = inputFields[i].text.Trim();

            if (string.IsNullOrEmpty(text))
            {
                Debug.Log("Input " + i + " is empty.");
                return false;
            }

            float parsedValue;

            if (!float.TryParse(text, out parsedValue))
            {
                Debug.Log("Input " + i + " is not a valid number.");
                return false;
            }

            if (parsedValue < 0f || parsedValue > 1f)
            {
                Debug.Log("Input " + i + " is out of range: " + parsedValue);
                return false;
            }

            values[i] = parsedValue;
        }

        return true;
    }

    void CalculateWeights()
    {
        float whoSum = values[0] + values[3] + values[6];
        float amSum = values[1] + values[4] + values[7];
        float iSum = values[2] + values[5] + values[8];

        whoWeight = Mathf.RoundToInt(whoSum * 10f);
        amWeight = Mathf.RoundToInt(amSum * 10f);
        iWeight = Mathf.RoundToInt(iSum * 10f);

        Debug.Log("whoSum = " + whoSum + " whoWeight = " + whoWeight);
        Debug.Log("amSum = " + amSum + " amWeight = " + amWeight);
        Debug.Log("iSum = " + iSum + " iWeight = " + iWeight);
    }

    int CountErrors(float[] v)
    {
        int errors = 0;

        if (!IsHighImportant(v[0])) errors++;
        if (!IsMediumImportant(v[1])) errors++;
        if (!IsHighImportant(v[2])) errors++;

        if (!IsMediumImportant(v[3])) errors++;
        if (!IsLowImportant(v[4])) errors++;
        if (!IsMediumImportant(v[5])) errors++;

        if (!IsHighImportant(v[6])) errors++;
        if (!IsMediumImportant(v[7])) errors++;
        if (!IsHighImportant(v[8])) errors++;

        return errors;
    }

    bool IsHighImportant(float value)
    {
        return value > 0.7f;
    }

    bool IsMediumImportant(float value)
    {
        return value >= 0.3f && value <= 0.7f;
    }

    bool IsLowImportant(float value)
    {
        return value < 0.3f;
    }

    IEnumerator SuccessFlow()
    {
        HideAllStars();

        if (perfectStar != null)
            perfectStar.SetActive(true);

        Debug.Log("PerfectStar shown");

        yield return new WaitForSeconds(perfectStarDuration);

        if (perfectStar != null)
            perfectStar.SetActive(false);

        Debug.Log("PerfectStar hidden, now show balance");

        if (balanceController != null)
        {
            balanceController.ShowBalanceWithWeights(
                whoWeight,
                amWeight,
                iWeight,
                OnBalanceStable
            );
        }
        else
        {
            Debug.LogWarning("AttentionInputManager: balanceController is not assigned.");

            if (normalizationController != null)
            {
                normalizationController.SetRawWeights(whoWeight, amWeight, iWeight);
                normalizationController.ShowNormalizeButton();
            }
        }
    }

    void OnBalanceStable()
    {
        Debug.Log("Balance stable, show normalize button");

        if (normalizationController != null)
        {
            normalizationController.SetRawWeights(whoWeight, amWeight, iWeight);
            normalizationController.ShowNormalizeButton();
        }
        else
        {
            Debug.LogWarning("AttentionInputManager: normalizationController is not assigned.");
        }
    }

    void ShowFailureStar(int errorCount)
    {
        HideAllStars();

        if (errorCount <= 2)
        {
            if (yellowStar != null)
                yellowStar.SetActive(true);

            Debug.Log("YellowStar shown");
        }
        else
        {
            if (blueStar != null)
                blueStar.SetActive(true);

            Debug.Log("BlueStar shown");
        }
    }

    IEnumerator ReturnToInputAfterDelay()
    {
        yield return new WaitForSeconds(failureReturnDelay);

        HideAllStars();

        if (shiftController != null)
            shiftController.RestartFromInputStage();
        else
            Debug.LogWarning("AttentionInputManager: shiftController is not assigned.");
    }

    public void HideAllStars()
    {
        if (blueStar != null)
            blueStar.SetActive(false);

        if (yellowStar != null)
            yellowStar.SetActive(false);

        if (perfectStar != null)
            perfectStar.SetActive(false);
    }
}