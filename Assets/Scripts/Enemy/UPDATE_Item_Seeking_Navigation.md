# Enemy Item Seeker - Navigation Update

## 🎯 Yapılan Değişiklik

Enemy'ler artık item'ları **uzaktan almak** yerine, **item'a doğru yürüyüp yakınında iken alır**.

---

## 🔄 Eski Sistem vs Yeni Sistem

### ❌ Eski Sistem (Önceki)
```
1. Enemy seek radius içindeki item'ları tarar
2. En yakın item'ı bulur
3. ❌ Item'ı HEMEN alır (uzaktan teleport gibi)
```

**Problem:**
- Item 5 metre uzaktayken alınıyordu
- Gerçekçi değildi
- Enemy item'a gitmiyordu

### ✅ Yeni Sistem (Şimdi)
```
1. Enemy seek radius içindeki item'ları tarar (algılama alanı)
2. En yakın item'ı bulur
3. ✅ NavMeshAgent ile item'a doğru yürür
4. ✅ Pickup radius içine girince item'ı alır (yakın mesafe)
5. ✅ Normal AI davranışına geri döner
```

**Avantajlar:**
- ✅ Gerçekçi davranış
- ✅ Enemy'nin item'a gittiğini görebilirsin
- ✅ İstenirse iptal edilebilir
- ✅ 2 farklı radius (algılama + alma)

---

## 🆕 Yeni Özellikler

### 1. Pickup Radius
```csharp
[SerializeField] private float pickupRadius = 1.5f;
```

- **Seek Radius**: Item'ı algılama alanı (default: 10 birim)
- **Pickup Radius**: Item'ı alma mesafesi (default: 1.5 birim)

**Örnek:**
- Enemy 10 metre uzaktaki item'ı algılar
- Item'a doğru yürür
- 1.5 metre yaklaşınca item'ı alır

### 2. Navigation Integration
```csharp
private NavMeshAgent agent;
private bool isSeekingItem = false;
private PickupableItem targetItem;
```

Enemy artık `NavMeshAgent` kullanarak item'a gider.

### 3. Pause AI During Seek (Opsiyonel)
```csharp
[SerializeField] private bool pauseAIDuringSeek = false;
```

- `true`: Item ararken EnemyAI durdurulur (sadece item'a odaklanır)
- `false`: Item ararken normal AI davranışı devam eder

### 4. Seeking State Management
```csharp
public bool IsSeekingItem => isSeekingItem;
public PickupableItem TargetItem => targetItem;
```

Enemy'nin şu anda item arayıp aramadığını kontrol edebilirsin.

---

## 🔧 Yeni Metodlar

### `TryFindAndSeekItem()`
Yakındaki en uygun item'ı bulur ve ona doğru gitmeye başlar.

```csharp
EnemyItemSeeker seeker = GetComponent<EnemyItemSeeker>();
seeker.TryFindAndSeekItem();
```

### `TrySeekSpecificItem(GameObject item)`
Belirli bir item'a doğru gitmeye başlar.

```csharp
GameObject targetItem = ...;
if (seeker.TrySeekSpecificItem(targetItem))
{
    Debug.Log("Enemy item'a gidiyor!");
}
```

### `CancelSeeking()`
Item aramayı iptal eder.

```csharp
seeker.CancelSeeking();
```

### `IsSeekingItem` (Property)
Enemy şu anda item arıyor mu?

```csharp
if (seeker.IsSeekingItem)
{
    Debug.Log("Enemy bir item arıyor: " + seeker.TargetItem.itemName);
}
```

---

## 🎨 Gizmos (Scene View)

Artık 3 farklı gizmo çiziliyor:

1. **Cyan Daire**: Seek radius (algılama alanı)
2. **Yeşil Daire**: Pickup radius (alma alanı)
3. **Sarı Çizgi**: Enemy → Target item (item ararken)
4. **Kırmızı Daire**: Target item pozisyonu (item ararken)

---

## 📋 Güncellenen Parametreler

### Inspector'da Yeni Alanlar:

```
[Item Seeking Settings]
• Auto Seek Items: ✓
• Seek Radius: 10 (ÖNCEKİ: 5)
• Pickup Radius: 1.5 (YENİ!)
• Seek Interval: 2
• Item Layer Mask: Everything

[Preferences]
• Prefer Primary Items: ✓
• Only Seek When Unarmed: ✓

[Navigation] (YENİ!)
• Pause AI During Seek: ☐
```

---

## 🎮 Çalışma Mantığı

### Adım Adım:

```
1. Update() her frame çalışır
   ├─ isSeekingItem == true mi?
   │  ├─ YES → UpdateItemSeeking() çağır
   │  └─ NO  → Seek interval kontrolü yap
   │
2. UpdateItemSeeking() çağrıldı
   ├─ Target item hala geçerli mi?
   │  ├─ NO  → StopSeekingItem()
   │  └─ YES → Mesafe kontrolü
   │
3. Mesafe Kontrolü
   ├─ Distance <= pickupRadius?
   │  ├─ YES → Item'ı al, StopSeekingItem()
   │  └─ NO  → Item hareket etti mi?
   │           ├─ YES → NavMesh destination güncelle
   │           └─ NO  → Devam et
```

---

## 🔍 Örnek Senaryolar

### Senaryo 1: Basit Item Arama
```csharp
// Enemy otomatik olarak item arayacak
EnemyItemSeeker seeker = GetComponent<EnemyItemSeeker>();
seeker.autoSeekItems = true;
seeker.seekRadius = 10f;
seeker.pickupRadius = 1.5f;
```

**Sonuç:**
1. Enemy her 2 saniyede etrafı tarar
2. 10 metre içindeki item'ı bulur
3. Item'a doğru yürür
4. 1.5 metre yaklaşınca alır

### Senaryo 2: Belirli Item'a Git
```csharp
GameObject pistol = GameObject.Find("Pistol");
EnemyItemSeeker seeker = GetComponent<EnemyItemSeeker>();

if (seeker.TrySeekSpecificItem(pistol))
{
    Debug.Log("Enemy tabancaya gidiyor!");
}
```

### Senaryo 3: Item Arama İptali
```csharp
EnemyItemSeeker seeker = GetComponent<EnemyItemSeeker>();

// Enemy item ararken player'ı gördü
if (seeker.IsSeekingItem && PlayerSpotted())
{
    seeker.CancelSeeking(); // Item aramayı iptal et
    // Şimdi player'ı kovalayabilir
}
```

### Senaryo 4: AI'ı Durdurarak Item Arama
```csharp
EnemyItemSeeker seeker = GetComponent<EnemyItemSeeker>();
seeker.pauseAIDuringSeek = true; // AI'ı durdur

seeker.TryFindAndSeekItem();
// Şimdi enemy SADECE item'a odaklanır
// Normal patrol/chase davranışı durur
```

---

## ⚙️ Teknik Detaylar

### Update Loop
```csharp
private void Update()
{
    if (isSeekingItem)
    {
        UpdateItemSeeking(); // Item takibi aktif
        return; // Diğer logic'i atla
    }
    
    // Normal seek logic
    if (autoSeekItems && Time.time - lastSeekTime >= seekInterval)
    {
        TryFindAndSeekItem();
    }
}
```

### Item Takibi
```csharp
private void UpdateItemSeeking()
{
    // Item hala geçerli mi?
    if (targetItem == null || targetItem.IsDepleted())
    {
        StopSeekingItem();
        return;
    }
    
    // Mesafe kontrolü
    float distance = Vector2.Distance(transform.position, targetItem.transform.position);
    
    if (distance <= pickupRadius)
    {
        // AL!
        targetItem.TryPickupByEnemy(itemHolder);
        StopSeekingItem();
    }
}
```

### NavMesh Navigation
```csharp
private void StartSeekingItem(PickupableItem item)
{
    targetItem = item;
    isSeekingItem = true;
    
    if (agent != null)
    {
        agent.SetDestination(item.transform.position);
    }
}
```

---

## 🐛 Sorun Giderme

### Problem: Enemy item'a gitmiyor
**Çözüm:**
- ✅ NavMeshAgent component'i var mı?
- ✅ NavMesh surface baked mi?
- ✅ Enemy NavMesh üzerinde mi?
- ✅ Item NavMesh üzerinde veya erişilebilir mi?

### Problem: Enemy çok yakına gelip alıyor
**Çözüm:**
- Pickup Radius'u artır (1.5 → 2.0)

### Problem: Enemy item'ı görmüyor
**Çözüm:**
- Seek Radius'u artır (10 → 15)
- Item'ın Layer Mask'ini kontrol et

### Problem: Enemy item ararken donuyor
**Çözüm:**
- `pauseAIDuringSeek = false` yap
- NavMeshAgent speed'ini kontrol et

---

## 📊 Performans

### Eski Sistem:
- Her frame: ❌ Yok
- Seek interval: ✅ Physics2D.OverlapCircleAll (2 saniyede bir)

### Yeni Sistem:
- Her frame: ✅ Distance check (sadece seeking sırasında)
- Seek interval: ✅ Physics2D.OverlapCircleAll (2 saniyede bir)
- NavMesh: ✅ Unity'nin optimizasyonları

**Sonuç:** Minimal performans farkı, çok daha gerçekçi davranış.

---

## ✅ Backward Compatibility

Eski metodlar `[Obsolete]` olarak işaretlendi ama hala çalışıyor:

```csharp
[Obsolete("Use TryFindAndSeekItem instead")]
public void TryFindAndPickupNearbyItem()
{
    TryFindAndSeekItem(); // Yeni metoda yönlendiriyor
}
```

Eski kodlarında `TryFindAndPickupNearbyItem()` kullanıyorsan:
- ✅ Kod çalışmaya devam eder
- ⚠️ Compiler warning verecek
- 💡 `TryFindAndSeekItem()` kullanman önerilir

---

## 🎉 Özet

Artık:
- ✅ Enemy item'a yürüyor (gerçekçi)
- ✅ 2 farklı radius (algılama + alma)
- ✅ NavMesh entegrasyonu
- ✅ İptal edilebilir
- ✅ AI ile entegre (opsiyonel pause)
- ✅ Görsel feedback (Gizmos)

---

Başarılar! 🚀
