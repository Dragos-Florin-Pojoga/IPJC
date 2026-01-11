// Items that are used in the game, taken as ScriptableObjects

using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Item")]
public class Item : ScriptableObject
{
    public ItemType type;
    public ActionType actionType;
    public Sprite image;
    public Vector2Int range = new Vector2Int(5, 4);
    public bool stackable = false;
    public int itemCost = 0;
    public GameObject prefabToEquip; 
}

public enum ItemType
{
    Gun,
    Ammo,
    PainKillers,
    Grenade
}

public enum ActionType
{
    Attack,
    Heal,
    Throw,
    Collect
}