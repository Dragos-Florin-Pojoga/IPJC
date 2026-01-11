// ToolBarItems for inventory management in-game

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ToolBarItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Elements")]
    public Image image;
    public Text countText;
    
    public Item item;
    [HideInInspector] public int count = 0;
    [HideInInspector] public Transform parentAfterDrag;

    private void Start()
    {
        InitialiseItem(item);
    }

    public void InitialiseItem(Item newItem)
    {
        item = newItem;
        image.sprite = newItem.image;
        image.enabled = true;

        if (count <= 0)
            count = 1;

        RefreshCount();
    }

    public void RefreshCount() // Ignore this lil guy...he's shy and want to scare us with errors. Will use it later :)
    {
        countText.text = count.ToString();
        bool textActive = count > 1;
        countText.gameObject.SetActive(textActive);
    }

    public void AddToSlot(Item newItem)
    {
        if (item == null)
        {
            InitialiseItem(newItem);
        }
        else if (item == newItem)
        {
            count++;
            RefreshCount();
        }
        else
        {
            Debug.LogWarning("Slot already occupied by a different item!");
        }
    }

    public void ClearSlot()
    {
        item = null;
        image.sprite = null;
        image.enabled = false;
        count = 0;
        countText.gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return; 
        image.raycastTarget = false;
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (item == null) return; 
        transform.position = Input.mousePosition; 
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (item == null) return; 
        image.raycastTarget = true;
        transform.SetParent(parentAfterDrag);
    }
}
