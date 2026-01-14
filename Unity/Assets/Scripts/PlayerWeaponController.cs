/////////////////////////////////
// WIP / VERY EXPERIMENTAL !!! //
/////////////////////////////////

using UnityEngine;
[RequireComponent(typeof(StatController))]
public class PlayerWeaponController : MonoBehaviour
{
    // [Tooltip("The camera used for aiming.")]
    private Camera mainCamera;
    
    [Tooltip("The currently equipped weapon.")]
    public Weapon currentWeapon;

    [Tooltip("The currently equipped Item.")]
    public Item currentItem;

    [Tooltip("Max distance for the aiming raycast.")]
    public float aimRaycastDistance = 100f;

    private PlayerControls m_controls; 
    public StatController m_statController;

    private LayerMask m_layerToIgnore;

    private Transform weaponHolder;

    public void EquipCurrentItem()
    {
        if (currentItem == null)
        {
            Debug.LogError("No current item assigned!");
            return;
        }

        if (currentItem.prefabToEquip == null)
        {
            Debug.LogError("Current item has no prefabToEquip!");
            return;
        }

        if (currentWeapon != null)
        {
            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }

        GameObject weaponGO = Instantiate(
            currentItem.prefabToEquip,
            weaponHolder
        );

        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;

        currentWeapon = weaponGO.GetComponent<Weapon>();

        if (currentWeapon == null)
        {
            Debug.LogError("PrefabToEquip does NOT have a Weapon component!");
        }
        else
        {
            Debug.Log("Weapon equipped: " + currentWeapon.name);
        }
    }


    private void Awake()
    {
        m_statController = GetComponent<StatController>();

        if (m_statController == null)
            Debug.LogError("No StatController found on Player!");

        m_controls = new PlayerControls();
        m_controls.Player.Attack.performed += OnFire;

        m_layerToIgnore = LayerMask.GetMask("Invisible_To_FPV");

        weaponHolder = GameObject.Find("WeaponHolder").transform;
        mainCamera = Camera.main;
    }


    void OnEnable()
    {
        m_controls.Player.Enable();
    }

    void OnDisable()
    {
        m_controls.Player.Disable();
    }

    private void OnFire(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (currentWeapon == null || currentItem == null || m_statController == null)
        {
            Debug.Log("No weapon to fire!");
            return;
        }

        currentWeapon.Fire(GetAimDirection(), m_statController);
    }

    private Vector3 GetAimDirection()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        Vector3 targetPoint;
        
        if (Physics.Raycast(ray, out RaycastHit hit, aimRaycastDistance, ~m_layerToIgnore)) {
            targetPoint = hit.point;
        } else {
            // no hit. target is far away in that direction
            targetPoint = ray.GetPoint(aimRaycastDistance);
        }

        // final direction from the *gun's spawn point* to the *target point*.
        return (targetPoint - currentWeapon.spawnPoint.position).normalized;
    }
}