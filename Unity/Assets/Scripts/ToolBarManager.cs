// The Manager for the Toolbar UI in the game, handling item addition, selection, and equipping weapons or other Items

using UnityEngine;
using UnityEngine.InputSystem;

public class ToolBarManager : MonoBehaviour
{   
    public static ToolBarManager instance;
    public Item[] startItems;
    public int maxstack = 64;
    public ToolBarSlot[] toolbarSlots;
    public GameObject toolbarItemPrefab;

    int selectedSlot = -1;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        ChangeSelectedSlot(0);
        foreach (var item in startItems)
        {
            AddItem(item);
        }
    }

    private void Update() // Sorry for this messy input handling, will refactor later
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 1; i <= toolbarSlots.Length; i++)
        {
            if (keyboard.digit1Key.wasPressedThisFrame && i == 1) { ChangeSelectedSlot(0); }
            if (keyboard.digit2Key.wasPressedThisFrame && i == 2) { ChangeSelectedSlot(1); }
            if (keyboard.digit3Key.wasPressedThisFrame && i == 3) { ChangeSelectedSlot(2); }
        }
    }

    void ChangeSelectedSlot(int newValue)
    {
        if (selectedSlot >= 0)
        {
            toolbarSlots[selectedSlot].Deselect();
        }

        toolbarSlots[newValue].Select();
        selectedSlot = newValue;

        EquipSelectedWeapon();
    }

    public bool AddItem(Item item)
    {
        for (int i = 0; i < toolbarSlots.Length; i++)
        {
            ToolBarSlot slot = toolbarSlots[i];
            ToolBarItem itemInSlot = slot.GetComponentInChildren<ToolBarItem>();
            if (itemInSlot != null && itemInSlot.item == item && itemInSlot.count < maxstack && itemInSlot.item.stackable == true) 
            {
                itemInSlot.count++;
                itemInSlot.RefreshCount();
                return true;
            }
            else if (itemInSlot != null && itemInSlot.item == item && itemInSlot.item.stackable == false && itemInSlot.count != 0)
            {
                return false;
            }
        }

        for (int i = 0; i < toolbarSlots.Length; i++)
        {
            ToolBarSlot slot = toolbarSlots[i];
            ToolBarItem itemInSlot = slot.GetComponentInChildren<ToolBarItem>();
            if (itemInSlot == null)
            {
                SpawnNewItem(item, slot);
                return true;
            }
        }

        return false;
    }

    void SpawnNewItem(Item item, ToolBarSlot slot)
    {
        GameObject newItemGo = Instantiate(toolbarItemPrefab, slot.transform);
        ToolBarItem toolbarItem = newItemGo.GetComponent<ToolBarItem>();
        toolbarItem.InitialiseItem(item);
    }

    public Item GetSelectedItem(bool use)
    {
        ToolBarSlot slot = toolbarSlots[selectedSlot];
        ToolBarItem itemInSlot = slot.GetComponentInChildren<ToolBarItem>();
        if (itemInSlot != null)
        {
            Item item = itemInSlot.item;
            if (use == true)
            {
                itemInSlot.count--;
                if (itemInSlot.count <= 0)
                {
                    Destroy(itemInSlot.gameObject);
                }
                else
                {
                    itemInSlot.RefreshCount();
                }
            }
            return item;
        }
        return null;
    }

    public Item GetSelectedItemRaw()
    {
        if (selectedSlot < 0 || selectedSlot >= toolbarSlots.Length)
            return null;

        ToolBarSlot slot = toolbarSlots[selectedSlot];
        ToolBarItem itemInSlot = slot.GetComponentInChildren<ToolBarItem>();
        return itemInSlot != null ? itemInSlot.item : null;
    }

    void EquipSelectedWeapon()
    {
        Item item = GetSelectedItemRaw();
        if (item == null) return;

        PlayerWeaponController pwc = Object.FindFirstObjectByType<PlayerWeaponController>();
        if (pwc == null) return;

        pwc.currentItem = item;
        pwc.EquipCurrentItem();
    }
}