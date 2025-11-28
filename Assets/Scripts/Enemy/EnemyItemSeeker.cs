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
    /// </summary>
    public PickupableItem FindBestItem()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, seekRadius, itemMask);

        Weapon bestItem = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D col in nearbyColliders)
        {
            Weapon item = col.GetComponent<Weapon>();

            if (item == null || item.IsBroken || !item.canBePickedByEnemies)
                continue;

            // Skip if already held
            if (item.transform.parent != null)
                continue;

            float distance = Vector2.Distance(transform.position, item.transform.position);

            // Prefer Ranged weapons if configured (assuming Primary = Ranged)
            bool isRanged = item is RangedWeapon;
            bool bestIsRanged = bestItem != null && bestItem is RangedWeapon;

            if (preferPrimaryItems && isRanged)
            {
                if (bestItem == null || !bestIsRanged || distance < closestDistance)
                {
                    bestItem = item;
                    closestDistance = distance;
                }
            }
            else if (bestItem == null || (!bestIsRanged && distance < closestDistance))
            {
                bestItem = item;
                closestDistance = distance;
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
    }
}
