using UnityEngine;

public class EnemyWeaponController : MonoBehaviour, IItemHolder
{
    [Header("Item Hold Position")]
    public Transform meleeHoldPosition;
    public Transform rangedHoldPosition;

    [Header("Current Item")]
    private Weapon currentWeapon;

    [Header("Settings")]
    public bool canUseItems = true;

    private EnemyAI enemyAI;

    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        if (meleeHoldPosition == null) meleeHoldPosition = transform;
        if (rangedHoldPosition == null) rangedHoldPosition = transform;
    }

    #region IItemHolder Implementation

    public Transform GetHoldPosition(ItemHoldType holdType) => holdType == ItemHoldType.Ranged ? rangedHoldPosition : meleeHoldPosition;
    public Team GetTeam() => Team.Enemy;

    public void OnItemPickedUp(GameObject item)
    {
        if (currentWeapon != null && currentWeapon.gameObject != item)
        {
            currentWeapon.OnDropped();
        }

        if (item.TryGetComponent<Weapon>(out var weapon))
        {
            currentWeapon = weapon;
        }

        // GameEvents.TriggerItemPickedUp(item, gameObject); // Assuming this exists or will exist
    }

    public void OnItemDropped(GameObject item)
    {
        if (currentWeapon != null && currentWeapon.gameObject == item)
        {
            currentWeapon = null;
        }
        // GameEvents.TriggerItemDropped(item, gameObject);
    }

    #endregion

    public void PerformAttack(Vector3 targetPosition)
    {
        if (!canUseItems || currentWeapon == null) return;

        // Rotate towards target if needed (usually handled by AI movement/rotation)
        // But we can ensure weapon points there
        Vector2 direction = (targetPosition - transform.position).normalized;

        // For now, just try attack. Weapon handles direction via hold position rotation
        // So AI should rotate the enemy/hold position towards target
        currentWeapon.TryAttack();
    }

    public void ThrowCurrentItem(Vector2 direction)
    {
        if (currentWeapon != null)
        {
            currentWeapon.Throw(direction);
        }
    }

    public bool HasWeapon() => currentWeapon != null;

    public GameObject GetCurrentWeapon() => currentWeapon != null ? currentWeapon.gameObject : null;

    // Legacy support / Helper for AI
    public void PickupItem(GameObject item)
    {
        if (item.TryGetComponent<Weapon>(out var weapon))
        {
            weapon.TryPickup(this);
        }
    }

    public void DropItem()
    {
        if (currentWeapon != null)
        {
            currentWeapon.OnDropped();
        }
    }

    public void DropAllItems()
    {
        DropItem();
    }
}
