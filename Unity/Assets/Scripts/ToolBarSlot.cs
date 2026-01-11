// ToolBarSlots for the ToolBar/Inventory system

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ToolBarSlot : MonoBehaviour, IDropHandler
{
    public Image image;
    public Color selectedColor;
    public Color deselectedColor;

    private void Awake()
    {
        Deselect();
    }

    public void Select()
    {
        image.color = selectedColor;
    }

    public void Deselect()
    {
        image.color = deselectedColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            ToolBarItem toolbarItem = eventData.pointerDrag.GetComponent<ToolBarItem>();
            toolbarItem.parentAfterDrag = transform;
        }
    }
}
