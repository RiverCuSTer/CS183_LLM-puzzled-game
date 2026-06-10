// Responsible team member: Zhiyan Lin; Description: Provides a simple UI drop target that parents dropped objects to the target transform.
using UnityEngine;
using UnityEngine.EventSystems;

public class DropHandler : MonoBehaviour, IDropHandler
{
    // Triggered when a dragged object is dropped onto this letter.
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag; // The dragged object.
        if (dropped != null)
        {
            // Parent the dragged object to the current letter.
            dropped.transform.SetParent(transform);

            // Align it to the center of the letter.
            dropped.transform.localPosition = Vector3.zero;
        }
    }
}
