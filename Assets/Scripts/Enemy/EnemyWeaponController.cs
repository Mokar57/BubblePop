using UnityEngine;

public class EnemyWeaponController : MonoBehaviour, IItemHolder
{
    [Header("Item Hold Position")]
    public Transform meleeHoldPosition;
    public Transform rangedHoldPosition;

    [Header("Current Item")]
    private Weapon currentWeapon;
    public bool IsHoldingWeapon => currentWeapon != null;

    [Header("Settings")]
    public bool canUseItems = true;

    private EnemyAI enemyAI;
    private BounceController bounceController;

    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        bounceController = GetComponent<BounceController>();

        if (bounceController != null)
        {
            bounceController.OnBounceStart += HandleBounceStart;
        }

        if (meleeHoldPosition == null) meleeHoldPosition = transform;
        if (rangedHoldPosition == null) rangedHoldPosition = transform;

        // Check for pre-assigned or child weapon
        if (currentWeapon == null)
        {
            currentWeapon = GetComponentInChildren<Weapon>();
        }

        if (currentWeapon != null)
        {
            // Ensure the weapon knows it's held by us
            // We pass 'this' which is IItemHolder
            // This will also set the parent to the correct hold position if not already
            currentWeapon.OnPickedUp(this);
        }
    }

    private void OnDestroy()
    {
        if (bounceController != null)
        {
            bounceController.OnBounceStart -= HandleBounceStart;
        }
    }

    private void HandleBounceStart()
    {
        if (bounceController != null && bounceController.settings != null && bounceController.settings.dropWeaponOnBounce)
        {
            DropCurrentWeapon();
        }
    }

    private void DropCurrentWeapon()
    {
        if (currentWeapon != null)
        {
            // Just drop it at current position
            currentWeapon.Drop();
        }
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

    public void OnDamageDealt(DamageInfo damageInfo, IDamageable target)
    {
        // Check if we hit the player with a blunt weapon
        if (damageInfo.damageType == DamageType.Blunt && target.GetTeam() == Team.Player)
        {
            // Pause AI to simulate recoil/stun effect on player
            if (enemyAI != null)
            {
                float waitTime = (enemyAI.enemyData != null) ? enemyAI.enemyData.postBluntAttackWaitTime : 1.5f;
                enemyAI.TriggerWait(waitTime);
            }
        }
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
