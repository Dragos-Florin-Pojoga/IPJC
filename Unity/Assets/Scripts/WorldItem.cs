// Items that are dumped in the WORLD

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WorldItem : MonoBehaviour
{
    public List<Item> possibleItems;   // All possible ScriptableObjects
    public Item item;                  // Current Item
    private bool playerInRange = false;

    [HideInInspector] public GameObject model; // Prefabs FBX-type cause issues in the inspector

    public void Initialize(Item newItem, GameObject fbxModel)
    {
        item = newItem;
        model = fbxModel;
        model.transform.SetParent(transform);
        model.transform.localPosition = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ignoring intern colliders and take the root
        Transform root = other.transform.root;

        if (root.CompareTag("Player"))
        {
            Debug.Log("Player entered pickup range");
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform root = other.transform.root;
        if (root.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("Picked up: " + item.name);

            Destroy(gameObject);

            ToolBarManager.instance.AddItem(item);
        }
    }
}
