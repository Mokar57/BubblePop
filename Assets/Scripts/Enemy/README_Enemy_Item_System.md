# Enemy Item System - Kullanım Kılavuzu

## Genel Bakış

Enemy'ler artık Player'lar gibi `PickupableItem`'ları alabilir, tutabilir ve kullanabilir. Bu sistem, düşmanların silah toplama ve kullanma yeteneklerini sağlar.

## Bileşenler

### 1. EnemyItemHolder
Enemy'nin item tutma ve kullanma yeteneklerini yönetir.

**Temel Özellikler:**
- **Primary Hold Position**: Önde tutulacak item'lar için transform (tabanca için)
- **Secondary Hold Position**: Yanda tutulacak item'lar için transform (beyzbol sopası için)
- **Current Primary/Secondary Item**: Şu anda tutulan item'lar
- **Throw Force**: Item fırlatma gücü
- **Can Use Items**: Enemy item kullanabilir mi?

**Önemli:** Enemy'nin görüş yönü (vision direction) ile birlikte döner. `LateUpdate()` içinde `UpdateRotationBasedOnVision()` metodu çağrılarak enemy ve tuttuğu item'lar otomatik olarak görüş yönüne döndürülür.

**Metodlar:**
- `PickupItem(GameObject item, ItemHoldType holdType)` - Item alır
- `DropItem(ItemHoldType holdType)` - Item düşürür
- `DropAllItems()` - Tüm item'ları düşürür
- `PerformAttack(Vector3 targetPosition)` - Tuttuğu silahla saldırır
- `ThrowCurrentItem(Vector2 direction)` - Item'ı belirtilen yöne fırlatır
- `HasWeapon()` - Silah var mı kontrol eder
- `GetCurrentWeapon()` - Şu anki silahı döndürür
- `UpdateRotationBasedOnVision()` - (Private) Enemy'yi görüş yönüne göre döndürür

### 2. EnemyItemSeeker
Enemy'nin otomatik olarak yakındaki item'ları bulup almasını sağlar.

**Ayarlar:**
- **Auto Seek Items**: Otomatik item arama açık/kapalı
- **Seek Radius**: Item algılama yarıçapı (default: 10 birim)
- **Pickup Radius**: Item almak için gereken yakınlık (default: 1.5 birim)
- **Seek Interval**: Kaç saniyede bir arama yapılacak (default: 2 saniye)
- **Item Layer Mask**: Hangi layer'daki item'lar aranacak
- **Prefer Primary Items**: Primary item'lara öncelik ver
- **Only Seek When Unarmed**: Sadece silahsızken ara
- **Pause AI During Seek**: Item ararken EnemyAI'ı duraklat (opsiyonel)

**Nasıl Çalışır:**
1. Enemy seek radius içindeki item'ları tarar
2. En uygun item'ı seçer (primary tercih, en yakın mesafe)
3. NavMeshAgent ile item'a doğru yürür
4. Pickup radius içine girince item'ı alır
5. Item alındıktan sonra normal AI davranışına döner

**Metodlar:**
- `TryFindAndSeekItem()` - Yakındaki en uygun item'ı bulur ve ona doğru gitmeye başlar
- `TrySeekSpecificItem(GameObject itemObject)` - Belirli bir item'a doğru gitmeye başlar
- `FindNearestItem()` - En yakın item'ı bulur ve döndürür (gitmez)
- `CancelSeeking()` - Item aramayı iptal eder
- `IsSeekingItem` - (Property) Enemy şu anda item arıyor mu?
- `TargetItem` - (Property) Hedef item nedir?

### 3. PickupableItem (Güncellenmiş)
Yeni eklenen özellikler:
- **Can Be Picked By Enemies**: Enemy'lerin bu item'ı alıp alamayacağını belirler
- `TryPickupByEnemy(EnemyItemHolder enemyHolder)` - Enemy'nin item'ı almasını sağlar

## Kurulum

### Adım 1: Enemy GameObject'ine Component Ekle

1. Enemy GameObject'ini seç
2. `EnemyItemHolder` component'ini ekle
3. (Opsiyonel) `EnemyItemSeeker` component'ini ekle (otomatik item toplama için)

### Adım 2: Hold Position'ları Ayarla

1. Enemy'nin child'ı olarak 2 adet boş GameObject oluştur:
   - `PrimaryHoldPosition` (ör: position: (0.5, 0, 0))
   - `SecondaryHoldPosition` (ör: position: (0, 0.5, 0))

2. Bu transform'ları `EnemyItemHolder` component'inin ilgili alanlarına sürükle

### Adım 3: Ayarları Yapılandır

**EnemyItemHolder Ayarları:**
- Throw Force'u ayarla (önerilen: 10)
- Can Use Items'ı işaretle (enemy item kullanabilsin)

**EnemyItemSeeker Ayarları (Opsiyonel):**
- Auto Seek Items'ı işaretle
- Seek Radius'u ayarla (ne kadar uzaktan item arayacak)
- Seek Interval'i ayarla (ne sıklıkla arayacak)
- Prefer Primary Items'ı işaretle (tabanca gibi primary item'lara öncelik ver)
- Only Seek When Unarmed'ı işaretle (sadece silahsızken item toplasın)

## Kod Örnekleri

### Örnek 1: Enemy'nin Otomatik Item Toplaması

```csharp
// EnemyItemSeeker component'i ile otomatik çalışır
// Herhangi bir kod yazmaya gerek yok, component'i ekle ve ayarları yap
```

### Örnek 2: Enemy'nin Belirli Bir Item'a Gitmesi

```csharp
EnemyItemSeeker seeker = GetComponent<EnemyItemSeeker>();
GameObject targetItem = ...; // Gitmesini istediğin item

if (seeker.TrySeekSpecificItem(targetItem))
{
    Debug.Log("Enemy item'a doğru gidiyor!");
    
    // İsteğe bağlı: Item alındığında bir şey yap
    StartCoroutine(WaitForItemPickup(seeker, targetItem));
}

IEnumerator WaitForItemPickup(EnemyItemSeeker seeker, GameObject item)
{
    // Enemy item'ı alana kadar bekle
    while (seeker.IsSeekingItem && seeker.TargetItem != null)
    {
        yield return null;
    }
    
    if (seeker.GetComponent<EnemyItemHolder>().HasWeapon())
    {
        Debug.Log("Enemy item'ı aldı!");
    }
}
```

### Örnek 3: Enemy'nin Silahla Saldırması

```csharp
EnemyItemHolder itemHolder = GetComponent<EnemyItemHolder>();
Vector3 targetPosition = player.transform.position;

if (itemHolder.HasWeapon())
{
    itemHolder.PerformAttack(targetPosition);
}
```

### Örnek 4: Enemy AI'dan Item Kullanımı

```csharp
public class EnemyAI : MonoBehaviour
{
    private EnemyItemHolder itemHolder;
    private Transform target;
    
    void Start()
    {
        itemHolder = GetComponent<EnemyItemHolder>();
    }
    
    void Update()
    {
        // Enemy silahı varsa ve hedef menzildeyse ateş et
        if (itemHolder.HasWeapon() && target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance <= 10f) // Menzil kontrolü
            {
                itemHolder.PerformAttack(target.position);
            }
        }
    }
}
```

### Örnek 5: Enemy'nin Item Fırlatması

```csharp
EnemyItemHolder itemHolder = GetComponent<EnemyItemHolder>();
Vector2 throwDirection = (target.position - transform.position).normalized;

if (itemHolder.HasWeapon())
{
    itemHolder.ThrowCurrentItem(throwDirection);
}
```

### Örnek 6: Enemy Öldüğünde Item'ı Düşürme

```csharp
void OnEnemyDeath()
{
    EnemyItemHolder itemHolder = GetComponent<EnemyItemHolder>();
    if (itemHolder != null)
    {
        itemHolder.DropAllItems();
    }
}
```

## Item Ayarları

### PickupableItem'da Enemy Ayarları

Her `PickupableItem`'da şu ayarı yapabilirsin:
- **Can Be Picked By Enemies**: Bu item'ı enemy'ler alabilir mi? (default: true)

Bu sayede bazı özel item'ların sadece player tarafından alınmasını sağlayabilirsin.

## Best Practices

1. **Hold Position'lar**: Hold position'ları enemy'nin sprite'ına göre doğru ayarla. Silahın doğal görünmesi için konumlandır.

2. **Layer Mask**: `EnemyItemSeeker`'da Layer Mask'ı doğru ayarla. Item'ların ayrı bir layer'da olması performans için önemli.

3. **Seek Interval**: Çok fazla enemy varsa seek interval'i artır (ör: 3-4 saniye). Bu performansı iyileştirir.

4. **Auto Seek**: Eğer enemy'nin belirli durumlarda item toplamasını istiyorsan Auto Seek'i kapat ve manuel kontrol et.

5. **Primary vs Secondary**: Enemy'nin hangi tür item'ı kullanacağına göre Prefer Primary Items ayarını yap.

## Sorun Giderme

**Problem**: Enemy item'a gitmiyor
- NavMeshAgent var mı kontrol et
- NavMesh surface baked mi kontrol et
- Seek Radius yeterince büyük mü kontrol et
- Enemy'nin NavMeshAgent'i enabled mi kontrol et
- Item ile enemy arasında NavMesh bağlantısı var mı kontrol et

**Problem**: Enemy item'ı alamıyor
- Item'ın Collider'ı var mı ve Layer doğru mu kontrol et
- `Can Be Picked By Enemies` işaretli mi kontrol et
- Item depleted (tükenmiş) olmadığından emin ol
- Pickup Radius yeterince büyük mü kontrol et (önerilen: 1.5)

**Problem**: Item yerde duruyor, fizik çalışmıyor
- Item'ın Rigidbody2D'si var mı kontrol et
- Rigidbody2D Type'ı Dynamic olmalı (item tutulmuyorken)

**Problem**: Enemy silahla ateş edemiyor
- `Can Use Items` işaretli mi kontrol et
- Item'ın gerekli component'leri var mı (PistolItem, BaseballBatItem vb.)

**Problem**: Performans sorunu
- Seek Interval'i artır
- Seek Radius'u küçült
- Only Seek When Unarmed'ı kullan
- Layer Mask'ı daralt

## İleri Seviye Kullanım

### Özel Item Toplama Logic'i

```csharp
public class CustomEnemyItemBehavior : MonoBehaviour
{
    private EnemyItemHolder itemHolder;
    private EnemyItemSeeker seeker;
    
    void Start()
    {
        itemHolder = GetComponent<EnemyItemHolder>();
        seeker = GetComponent<EnemyItemSeeker>();
        
        // Auto seek'i kapat, manuel kontrol et
        seeker.autoSeekItems = false;
    }
    
    void Update()
    {
        // Sadece düşük can'da item topla
        if (GetComponent<EnemyAI>().currentHealth < 50f)
        {
            seeker.TryFindAndPickupNearbyItem();
        }
    }
}
```

### Stratejik Item Kullanımı

```csharp
public class TacticalEnemyAI : MonoBehaviour
{
    private EnemyItemHolder itemHolder;
    
    void AttackStrategy(float distanceToTarget)
    {
        GameObject weapon = itemHolder.GetCurrentWeapon();
        if (weapon == null) return;
        
        PickupableItem item = weapon.GetComponent<PickupableItem>();
        
        // Uzaktan tabanca, yakından sopa kullan
        if (item.holdType == ItemHoldType.Primary && distanceToTarget > 5f)
        {
            itemHolder.PerformAttack(target.position);
        }
        else if (item.holdType == ItemHoldType.Secondary && distanceToTarget <= 3f)
        {
            itemHolder.PerformAttack(target.position);
        }
    }
}
```

## Notlar

- Enemy'ler Player ile aynı item sistemini kullanır
- Her enemy aynı anda sadece 1 item tutabilir (Primary VEYA Secondary)
- Item'lar depleted olduğunda enemy tarafından alınamaz
- Thrown item'lar ThrownItem component'i ile otomatik yavaşlar ve durur
