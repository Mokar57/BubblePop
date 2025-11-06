using UnityEngine;

public class BaseballBatItem : MonoBehaviour
{
    [Header("Baseball Bat Settings")]
    public string batName = "Baseball Bat";
    public Color batColor = Color.brown;
    
    [Header("Attack Settings")]
    public float attackRange = 2f; // Saldırı menzili
    public float attackDamage = 50f; // Hasar miktarı
    public float attackCooldown = 1f; // Saldırı bekleme süresi
    public float attackAngle = 90f; // Saldırı açısı (derece)
    public LayerMask enemyLayerMask = -1; // Hangi layer'lardaki düşmanları vuracak
    
    private float lastAttackTime = 0f;
    
    private void Start()
    {
        // Set the bat's color
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = batColor;
        }
        
        // Add the PickupableItem component if it doesn't exist
        PickupableItem pickupable = GetComponent<PickupableItem>();
        if (pickupable == null)
        {
            pickupable = gameObject.AddComponent<PickupableItem>();
        }
        
        // Configure for secondary hold (side position)
        pickupable.itemName = batName;
        pickupable.holdType = ItemHoldType.Secondary;
    }
    
    public void Attack(Vector3 playerPosition, Vector3 direction)
    {
        // Kullanım hakkı kontrolü
        PickupableItem pickupable = GetComponent<PickupableItem>();
        if (pickupable != null && pickupable.IsDepleted())
        {
            return;
        }
        
        // Cooldown kontrolü
        if (Time.time - lastAttackTime < attackCooldown)
            return;
        
        lastAttackTime = Time.time;
        
        // Sahibi bul (parent)
        GameObject owner = transform.parent != null ? transform.parent.gameObject : null;
        
        // Alan hasarı ver
        PerformAreaAttack(playerPosition, direction, owner);
    }
    
    private void PerformAreaAttack(Vector3 centerPosition, Vector3 direction, GameObject owner)
    {
        // Yakındaki tüm collider'ları bul
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(centerPosition, attackRange, enemyLayerMask);
        
        int enemiesHit = 0;
        
        foreach (Collider2D hitCollider in hitColliders)
        {
            // Sahibine hasar verme
            if (owner != null && hitCollider.gameObject == owner)
                continue;
            
            // Enemy mi kontrol et
            EnemyAI enemy = hitCollider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Açı kontrolü yap (isteğe bağlı)
                if (IsInAttackAngle(centerPosition, direction, hitCollider.transform.position))
                {
                    enemy.TakeDamage(attackDamage);
                    enemiesHit++;
                }
            }
            
            // Player mi kontrol et
            PlayerControls player = hitCollider.GetComponent<PlayerControls>();
            if (player != null)
            {
                // Açı kontrolü yap (isteğe bağlı)
                if (IsInAttackAngle(centerPosition, direction, hitCollider.transform.position))
                {
                    player.TakeDamage(attackDamage);
                    enemiesHit++;
                }
            }
        }
        
        if (enemiesHit > 0)
        {
            // Düşman vurduysa kullanım hakkını azalt
            PickupableItem pickupable = GetComponent<PickupableItem>();
            if (pickupable != null)
            {
                pickupable.DecreaseUsage();
            }
        }
    }
    
    private bool IsInAttackAngle(Vector3 attackPosition, Vector3 attackDirection, Vector3 targetPosition)
    {
        // Eğer attackAngle 360 derece ise her yönü vur
        if (attackAngle >= 360f)
            return true;
        
        Vector3 directionToTarget = (targetPosition - attackPosition).normalized;
        float angleToTarget = Vector3.Angle(attackDirection, directionToTarget);
        
        return angleToTarget <= attackAngle / 2f;
    }
    
    // Gizmos ile attack range'i göster
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Attack angle'ı göster (eğer parent varsa - player tutuyorsa)
        if (transform.parent != null)
        {
            Vector3 forward = transform.parent.up;
            Vector3 rightBound = Quaternion.Euler(0, 0, -attackAngle / 2f) * forward;
            Vector3 leftBound = Quaternion.Euler(0, 0, attackAngle / 2f) * forward;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, rightBound * attackRange);
            Gizmos.DrawRay(transform.position, leftBound * attackRange);
        }
    }
}