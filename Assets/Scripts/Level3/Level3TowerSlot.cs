using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Level3TowerSlot : MonoBehaviour
{
    [SerializeField] private string expectedWord;
    [SerializeField] private Vector2 triggerSize = new Vector2(1.75f, 0.65f);
    [SerializeField] private bool overrideTriggerSize = true;

    private readonly Dictionary<Level3WordBlock, int> overlapCounts = new Dictionary<Level3WordBlock, int>();
    private readonly List<Level3WordBlock> removeBuffer = new List<Level3WordBlock>();

    public string ExpectedWord => expectedWord;
    public bool IsFilled => ContainsExpectedWord();

    private void Awake()
    {
        ApplyColliderSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyColliderSettings();
    }
#endif

    private void ApplyColliderSettings()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        box.isTrigger = true;

        if (overrideTriggerSize)
        {
            box.size = triggerSize;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Level3WordBlock wordBlock = other.GetComponentInParent<Level3WordBlock>();
        if (wordBlock == null) return;

        if (overlapCounts.TryGetValue(wordBlock, out int count))
        {
            overlapCounts[wordBlock] = count + 1;
        }
        else
        {
            overlapCounts.Add(wordBlock, 1);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Level3WordBlock wordBlock = other.GetComponentInParent<Level3WordBlock>();
        if (wordBlock == null) return;

        if (!overlapCounts.TryGetValue(wordBlock, out int count)) return;

        count--;

        if (count <= 0)
        {
            overlapCounts.Remove(wordBlock);
        }
        else
        {
            overlapCounts[wordBlock] = count;
        }
    }

    public bool ContainsExpectedWord()
    {
        CleanupDestroyedWords();

        foreach (Level3WordBlock wordBlock in overlapCounts.Keys)
        {
            if (wordBlock == null) continue;

            if (string.Equals(wordBlock.Word, expectedWord, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void CleanupDestroyedWords()
    {
        removeBuffer.Clear();

        foreach (Level3WordBlock wordBlock in overlapCounts.Keys)
        {
            if (wordBlock == null)
            {
                removeBuffer.Add(wordBlock);
            }
        }

        foreach (Level3WordBlock wordBlock in removeBuffer)
        {
            overlapCounts.Remove(wordBlock);
        }
    }
}
