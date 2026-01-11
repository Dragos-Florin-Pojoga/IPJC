// Script to automatically fit a toolbar item to its parent toolbar slot in Unity.

using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class AutoFitToolBarItem : MonoBehaviour
{
    [Range(0.1f, 1f)]
    public float sizeFactor = 0.9f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        FitToParentSlot();
    }

    private void OnTransformParentChanged()
    {
        FitToParentSlot();
    }

    private void FitToParentSlot()
    {
        if (transform.parent == null) return;

        ToolBarSlot slot = transform.parent.GetComponent<ToolBarSlot>();
        if (slot == null) return;

        RectTransform slotRect = slot.GetComponent<RectTransform>();
        if (slotRect == null) return;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = slotRect.sizeDelta * sizeFactor;
    }
}
