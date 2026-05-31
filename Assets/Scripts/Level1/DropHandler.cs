using UnityEngine;
using UnityEngine.EventSystems;

public class DropHandler : MonoBehaviour, IDropHandler
{
    // 当有拖拽对象放到这个字母上时触发
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag; // 拖拽的对象
        if (dropped != null)
        {
            // 将拖拽对象设置为当前字母的子对象
            dropped.transform.SetParent(transform);

            // 对齐到字母中心
            dropped.transform.localPosition = Vector3.zero;
        }
    }
}