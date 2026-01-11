//Pick-UP Items by pressing E

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickup : MonoBehaviour
{
    private WorldItem nearbyItem;

    public void SetNearbyItem(WorldItem item)
    {
        nearbyItem = item;
    }

    public void ClearNearbyItem(WorldItem item)
    {
        if (nearbyItem == item)
            nearbyItem = null;
    }

    //OBSERVATION: This code is now handled in WorldItem.cs to simplify pickup logic. IDK if I will need this later so let it be commed

    //private void Update()
    //{
    //    if (nearbyItem != null && Keyboard.current.eKey.wasPressedThisFrame)
    //    {
    //        PickupItem(nearbyItem);
    //    }
    //}

    //void PickupItem(WorldItem worldItem)
    //{
    //    Item item = worldItem.item;

    //    Debug.Log("Player picked up: " + item.name);

    //    // Add item to toolbar
    //    if (ToolBarManager.instance.AddItem(item))
    //    {
    //        // destroy from world
    //        worldItem.gameObject.SetActive(false);

    //        Destroy(worldItem.gameObject);

    //        // Auto-equip in hand !!!BUT HELL IF I KNOW WHY THIS ISN'T WORKING(doesn't instantly equip for use...fuck me...we need to press the current slot button to equip it bruh)!!!
    //        var holder = Object.FindFirstObjectByType<WeaponHolder>();
    //        if (holder != null)
    //        {
    //            holder.Unequip();
    //            holder.Equip(item);
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("Inventory full!");
    //    }
    //}

}
