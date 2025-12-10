using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Helper component for finding items.
/// Logic is now driven by SeekItemState, not by this component's Update loop.
/// </summary>
[RequireComponent(typeof(EnemyWeaponController))]
public class EnemyItemSeeker : MonoBehaviour
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Item Seeking Settings (Override)")]
    [Tooltip("Leave at 0 to use enemyData values")]
    [SerializeField] private float seekRadiusOverride = 0f;
    [SerializeField] private float pickupRadiusOverride = 0f;

    [Header("Detection")]
    [Tooltip("Layer mask for detecting items - Overrides SO if set")]
    [SerializeField] private LayerMask itemLayerMaskOverride;

    private EnemyWeaponController weaponController;

    // Runtime values
    private float seekRadius;
    private float pickupRadius;
    private bool preferPrimaryItems;
    private LayerMask itemMask;

    private void Awake()
    {
        weaponController = GetComponent<EnemyWeaponController>();
        InitializeSettings();
    }

    private void InitializeSettings()
    {
        if (enemyData != null)
        {
            seekRadius = seekRadiusOverride > 0 ? seekRadiusOverride : enemyData.itemSeekRadius;
            pickupRadius = pickupRadiusOverride > 0 ? pickupRadiusOverride : enemyData.itemPickupRadius;
            preferPrimaryItems = enemyData.preferPrimaryWeapons;
            itemMask = itemLayerMaskOverride.value != 0 ? itemLayerMaskOverride : enemyData.itemLayerMask;
        }
        else
        {
            seekRadius = seekRadiusOverride > 0 ? seekRadiusOverride : 10f;
            pickupRadius = pickupRadiusOverride > 0 ? pickupRadiusOverride : 1.5f;
            preferPrimaryItems = true;
            itemMask = itemLayerMaskOverride;
        }
    }

    /// <summary>
    /// Finds the best item to seek based on preferences and distance
    /// Prioritizes items in the priority area if configured and enemy is unarmed
    /// </summary>
    public PickupableItem FindBestItem()
    {
        // Check if we should prioritize the area
        bool shouldCheckPriorityArea = enemyData != null && 
                                        enemyData.usePrioritySeekArea && 
                                        !weaponController.HasWeapon();

        // Use AllLayers if mask is not set (value 0 usually means Nothing in LayerMask, but we want to find something)
        int mask = itemMask.value != 0 ? itemMask.value : Physics2D.AllLayers;

        // First, check priority area if enabled
        if (shouldCheckPriorityArea)
        {
            Debug.Log($"{gameObject.name}: Checking priority area (radius: {enemyData.prioritySeekAreaRadius})");
            PickupableItem priorityItem = FindItemInPriorityArea(mask);
            if (priorityItem != null)
            {
                Debug.Log($"{gameObject.name}: Found priority item: {priorityItem.name} at {priorityItem.transform.position}");
                return priorityItem;
            }

            Debug.Log($"{gameObject.name}: No item found in priority area. OnlySeekInPriority: {enemyData.onlySeekInPriorityArea}");

            // If onlySeekInPriorityArea is true and no item found, return null
            if (enemyData.onlySeekInPriorityArea)
            {
                return null;
            }
        }

        // Otherwise, find items normally
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, seekRadius, mask);

        Weapon bestItem = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D col in nearbyColliders)
        {
            // Check for Weapon component (could be on parent if collider is child, but usually on same)
            Weapon item = col.GetComponent<Weapon>();
            if (item == null) item = col.GetComponentInParent<Weapon>();

            if (item == null || item.IsBroken || !item.canBePickedByEnemies)
                continue;

            // Skip if already held
            if (item.IsHeld)
                continue;

            float distance = Vector2.Distance(transform.position, item.transform.position);

            // Prefer Ranged weapons if configured (assuming Primary = Ranged)
            bool isRanged = item is RangedWeapon;
            bool bestIsRanged = bestItem != null && bestItem is RangedWeapon;

            // Priority logic: prefer ranged weapons, but always choose the closest within the same category
            if (bestItem == null)
            {
                bestItem = item;
                closestDistance = distance;
            }
            else if (preferPrimaryItems && isRanged && !bestIsRanged)
            {
                // New item is ranged, best is not - switch only if reasonably close
                bestItem = item;
                closestDistance = distance;
            }
            else if (preferPrimaryItems && !isRanged && bestIsRanged)
            {
                // New item is melee, best is ranged - keep best (don't switch)
            }
            else if (distance < closestDistance)
            {
                // Same category or no preference - choose closest
                bestItem = item;
                closestDistance = distance;
            }
        }

        return bestItem;
    }

    /// <summary>
    /// Finds the best item within the priority seek area (centered on enemy)
    /// </summary>
    private PickupableItem FindItemInPriorityArea(int defaultMask)
    {
        if (enemyData == null) return null;

        Vector2 areaCenter = transform.position;
        float areaRadius = enemyData.prioritySeekAreaRadius;

        // Use priority layer mask if set, otherwise use default mask
        int mask = enemyData.prioritySeekLayerMask.value != 0 ? enemyData.prioritySeekLayerMask.value : defaultMask;

        Debug.Log($"{gameObject.name}: Searching priority area at {areaCenter} with radius {areaRadius}, mask: {mask}");

        // Find all items in the priority area
        Collider2D[] areaColliders = Physics2D.OverlapCircleAll(areaCenter, areaRadius, mask);

        Debug.Log($"{gameObject.name}: Found {areaColliders.Length} colliders in priority area");

        Weapon bestItem = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D col in areaColliders)
        {
            Weapon item = col.GetComponent<Weapon>();
            if (item == null) item = col.GetComponentInParent<Weapon>();

            if (item == null || item.IsBroken || !item.canBePickedByEnemies)
                continue;

            if (item.IsHeld)
                continue;

            Debug.Log($"{gameObject.name}: Valid weapon found in priority area: {item.name} at {item.transform.position}");

            float distanceToEnemy = Vector2.Distance(transform.position, item.transform.position);

            // Prefer Ranged weapons if configured
            bool isRanged = item is RangedWeapon;
            bool bestIsRanged = bestItem != null && bestItem is RangedWeapon;

            // Priority logic: prefer ranged weapons, but always choose the closest within the same category
            if (bestItem == null)
            {
                bestItem = item;
                closestDistance = distanceToEnemy;
            }
            else if (preferPrimaryItems && isRanged && !bestIsRanged)
            {
                // New item is ranged, best is not - switch only if reasonably close
                bestItem = item;
                closestDistance = distanceToEnemy;
            }
            else if (preferPrimaryItems && !isRanged && bestIsRanged)
            {
                // New item is melee, best is ranged - keep best (don't switch)
            }
            else if (distanceToEnemy < closestDistance)
            {
                // Same category or no preference - choose closest
                bestItem = item;
                closestDistance = distanceToEnemy;
            }
        }

        return bestItem;
    }

    public float GetPickupRadius() => pickupRadius;

    private void OnDrawGizmosSelected()
    {
        float drawSeekRadius = seekRadius > 0 ? seekRadius : (seekRadiusOverride > 0 ? seekRadiusOverride : 10f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, drawSeekRadius);

        // Draw priority seek area if enabled (centered on enemy)
        if (enemyData != null && enemyData.usePrioritySeekArea)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyData.prioritySeekAreaRadius);
        }
    }
}
