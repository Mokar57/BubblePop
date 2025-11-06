using UnityEngine;

public class PistolItem : MonoBehaviour
{
    [Header("Pistol Settings")]
    public string pistolName = "Pistol";
    public Color pistolColor = Color.white;
    
    [Header("Attack Settings")]
    public GameObject projectilePrefab; // Projectile prefab'ı (Inspector'da atanacak)
    public Transform firePoint; // Projectile'ın çıkacağı nokta
    public float fireRate = 0.5f; // Ateş etme hızı (saniye)
    public float projectileSpeed = 15f;
    public float projectileDamage = 25f;
    
    private float lastFireTime = 0f;
    
    private void Start()
    {
        // Set the pistol's color
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = pistolColor;
        }
        
        // Add the PickupableItem component if it doesn't exist
        PickupableItem pickupable = GetComponent<PickupableItem>();
        if (pickupable == null)
        {
            pickupable = gameObject.AddComponent<PickupableItem>();
        }
        
        // Configure for primary hold (front position)
        pickupable.itemName = pistolName;
        pickupable.holdType = ItemHoldType.Primary;
        
        // FirePoint yoksa varsayılan konum oluştur
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0f, 1f, 0f); // Silahın önünde
            firePoint = firePointObj.transform;
        }
    }
    
    public void Fire(Vector3 playerPosition, Vector3 direction)
    {
        // Kullanım hakkı kontrolü
        PickupableItem pickupable = GetComponent<PickupableItem>();
        if (pickupable != null && pickupable.IsDepleted())
        {
            return;
        }
        
        // Fire rate kontrolü
        if (Time.time - lastFireTime < fireRate)
            return;
        
        lastFireTime = Time.time;
        
        // Sahibi bul (parent)
        GameObject owner = transform.parent != null ? transform.parent.gameObject : null;
        
        // Projectile oluştur
        CreateProjectile(playerPosition, direction, owner);
        
        // Kullanım hakkını azalt
        if (pickupable != null)
        {
            pickupable.DecreaseUsage();
        }
    }
    
    private void CreateProjectile(Vector3 startPosition, Vector3 direction, GameObject owner)
    {
        Vector3 spawnPosition = firePoint != null ? firePoint.position : startPosition;
        
        if (projectilePrefab != null)
        {
            // Projectile prefab'dan oluştur
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(Vector3.forward, direction));
            
            // Projectile component'ini ayarla
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.speed = projectileSpeed;
                projectile.damage = projectileDamage;
                projectile.SetDirection(direction);
                projectile.SetOwner(owner);
            }
        }
        else
        {
            // Basit bir projectile oluştur (prefab yoksa)
            CreateBasicProjectile(spawnPosition, direction, owner);
        }
    }
    
    private void CreateBasicProjectile(Vector3 startPosition, Vector3 direction, GameObject owner)
    {
        // Basit bir projectile oluştur
        GameObject projectileObj = new GameObject("Bullet");
        projectileObj.transform.position = startPosition;
        projectileObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        
        // Görsel
        SpriteRenderer sr = projectileObj.AddComponent<SpriteRenderer>();
        sr.color = Color.yellow;
        // Basit bir kare sprite oluştur
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        projectileObj.transform.localScale = new Vector3(0.2f, 0.5f, 1f);
        
        // Physics
        Rigidbody2D rb = projectileObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Collider
        BoxCollider2D collider = projectileObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.2f, 0.5f);
        
        // Projectile script
        Projectile projectile = projectileObj.AddComponent<Projectile>();
        projectile.speed = projectileSpeed;
        projectile.damage = projectileDamage;
        projectile.SetDirection(direction);
        projectile.SetOwner(owner);
    }
}