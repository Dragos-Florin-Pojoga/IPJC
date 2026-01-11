// Drop the Items on Q

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DropItem : MonoBehaviour
{
    public List<GameObject> toolbarItems = new List<GameObject>();
    public List<Item> toolbarItemData = new List<Item>();
    public GameObject worldItemWrapperPrefab;

    private void Start()
    {
        RefreshToolbarItems();
    }

    public void RefreshToolbarItems()
    {
        toolbarItems.Clear();
        toolbarItemData.Clear();

        if (ToolBarManager.instance == null) return;

        foreach (var slot in ToolBarManager.instance.toolbarSlots)
        {
            if (slot == null) continue;
            var toolbarItem = slot.GetComponentInChildren<ToolBarItem>();
            if (toolbarItem != null)
            {
                toolbarItems.Add(toolbarItem.gameObject);
                if (toolbarItem.item != null)
                    toolbarItemData.Add(toolbarItem.item);
            }
        }
    }

    private void Update()
    {
        if (ToolBarManager.instance == null) return;

        Item selected = ToolBarManager.instance.GetSelectedItem(false);

        if (Keyboard.current.qKey.wasPressedThisFrame && selected != null)
        {
            Item removed = ToolBarManager.instance.GetSelectedItem(true);
            if (removed == null) return;

            if (removed.prefabToEquip != null)
            {
                GameObject wrapper = Instantiate(worldItemWrapperPrefab, transform.position + transform.forward, Quaternion.identity);

                GameObject model = Instantiate(removed.prefabToEquip);
                model.transform.SetParent(wrapper.transform);
                model.transform.localPosition = Vector3.zero;

                WorldItem wi = wrapper.GetComponent<WorldItem>();
                wi.Initialize(removed, model);
            }

            PlayerWeaponController pwc = Object.FindFirstObjectByType<PlayerWeaponController>();
            if (pwc != null && pwc.currentItem == removed)
            {
                if (pwc.currentWeapon != null)
                    Destroy(pwc.currentWeapon.gameObject);

                pwc.currentWeapon = null;
                pwc.currentItem = null;
            }

            RefreshToolbarItems();
        }
    }
}