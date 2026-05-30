using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Level3TowerSlot : MonoBehaviour
{
    private const float UiScale = 1.2f;

    [SerializeField] private string expectedWord;
    [SerializeField] private string hint = "zone";
    [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 0.28f);
    [SerializeField] private Vector2 guiSize = new Vector2(140f, 48f);

    private readonly List<Level3WordBlock> containedWords = new List<Level3WordBlock>();
    private Camera mainCamera;

    public string ExpectedWord => expectedWord;
    public bool IsFilled => ContainsExpectedWord();

    private void Awake()
    {
        mainCamera = Camera.main;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.75f, 0.65f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Level3WordBlock wordBlock = other.GetComponent<Level3WordBlock>();
        if (wordBlock != null && !containedWords.Contains(wordBlock))
            containedWords.Add(wordBlock);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Level3WordBlock wordBlock = other.GetComponent<Level3WordBlock>();
        if (wordBlock != null)
            containedWords.Remove(wordBlock);
    }

    private void OnGUI()
    {
        return;
    }

    public bool ContainsExpectedWord()
    {
        foreach (Level3WordBlock wordBlock in containedWords)
        {
            if (wordBlock != null && wordBlock.Word == expectedWord)
                return true;
        }

        return false;
    }
}
