// Responsible team member: Zhaoning Chu; Description: Checks the legacy Level 3 word-slot puzzle answer state.
using UnityEngine;//25125851  chuzhaoning
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SlotChecker : MonoBehaviour
{
    [Tooltip("Correct words. Use lowercase words only, without the Word_ prefix.")]
    public List<string> correctNames = new List<string>();

    [Header("UI Feedback (Optional)")]
    public Text resultText;
    public Image slotBackground;
    public Color successColor = Color.green;

    private List<DragWord> currentWords = new List<DragWord>();
    private bool isCompleted = false;

    public void OnWordDropped(DragWord word)
    {
        if (isCompleted) return;

        currentWords.Add(word);

        // Automatically check the answer after three words are placed.
        if (currentWords.Count >= correctNames.Count)
        {
            CheckAnswer();
        }
    }

    public void CheckAnswer()
    {
        if (isCompleted || currentWords.Count == 0) return;

        List<string> inputNames = new List<string>();
        foreach (var word in currentWords)
        {
            string objName = word.gameObject.name;

            // Remove any optional suffix.
            if (objName.Contains("(Clone)")) objName = objName.Replace("(Clone)", "");

            // Remove the "Word_" prefix automatically.
            if (objName.StartsWith("Word_"))
            {
                objName = objName.Replace("Word_", "");
            }

            inputNames.Add(objName.Trim().ToLower());
        }

        // Compare without requiring order.
        bool isCorrect = false;
        if (inputNames.Count == correctNames.Count)
        {
            var sortedInput = inputNames.OrderBy(x => x).ToList();
            var sortedCorrect = correctNames.Select(x => x.Trim().ToLower()).OrderBy(x => x).ToList();
            isCorrect = sortedInput.SequenceEqual(sortedCorrect);
        }

        if (isCorrect)
        {
            isCompleted = true;
            Debug.Log($"[{gameObject.name}] Check result: passed.");
            if (resultText != null) resultText.text = "Correct!";
            if (slotBackground != null) slotBackground.color = successColor;
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Check result: failed. Collected words: {string.Join(", ", inputNames)}");
            if (resultText != null) resultText.text = "Wrong!";

            // On failure, show all cards again and return them to their start positions.
            foreach (var word in currentWords)
            {
                word.ReturnToHome();
            }
            currentWords.Clear();
        }
    }
}
