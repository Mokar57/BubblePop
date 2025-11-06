# Enemy Item System - Hızlı Kurulum

## 🎯 Özet

Enemy'ler artık Player'lar gibi item tutabilir, kullanabilir ve fırlatabilir. 2 slot sistemi: **Primary** (önde - tabanca) ve **Secondary** (yanda - sopa).

## ⚡ Hızlı Kurulum (3 Adım)

### 1️⃣ Component'leri Ekle

Enemy GameObject'ine şu component'leri ekle:

```
✅ EnemyItemHolder (Zorunlu - Item tutma sistemi)
✅ EnemyItemSeeker (Opsiyonel - Otomatik item toplama)
✅ EnemyItemIntegration (Opsiyonel - Savaşta otomatik item kullanma)
```

### 2️⃣ Hold Position'ları Oluştur

Enemy'nin child'ı olarak 2 boş GameObject oluştur:

```
📍 PrimaryHoldPosition
   └─ Local Position: (0.5, 0, 0) veya istediğin pozisyon
   
📍 SecondaryHoldPosition  
   └─ Local Position: (0, 0.5, 0) veya istediğin pozisyon
```

Bu transform'ları `EnemyItemHolder` component'inin ilgili alanlarına sürükle.

### 3️⃣ Ayarları Yap

**EnemyItemHolder:**
- Throw Force: `10`
- Can Use Items: `✓`

**EnemyItemSeeker (Opsiyonel):**
- Auto Seek Items: `✓`
- Seek Radius: `10` (algılama alanı)
- Pickup Radius: `1.5` (alma alanı)
- Seek Interval: `2`
- Prefer Primary Items: `✓`
- Only Seek When Unarmed: `✓`
- Pause AI During Seek: `☐` (opsiyonel)

**EnemyItemIntegration (Opsiyonel):**
- Attack Range: `10` (tabanca menzili)
- Attack Cooldown: `1` (saniye)
- Use Items In Combat: `✓`
- Melee Range: `2` (sopa menzili)

## 🎮 Nasıl Çalışır?

### Otomatik Mod (Önerilen)

```
EnemyItemSeeker ile:
• Enemy her 2 saniyede bir yakındaki item'ları arar
• En uygun item'ı bulur
• Item'a doğru yürür (NavMeshAgent ile)
• Yakınına gelince (pickup radius içinde) item'ı alır
• Primary item'lara öncelik verir

EnemyItemIntegration ile:
• Enemy silahı varsa otomatik ateş eder
• Tabanca = uzaktan (10m)
• Sopa = yakından (2m)
• Enemy öldüğünde item'ları düşer

EnemyItemHolder ile:
• Enemy'nin görüş yönü (vision cone) ile birlikte döner
• Tuttuğu item'lar da enemy ile birlikte döner
• Transform.up yerine currentVisionDirection kullanır
```

### Manuel Mod (İleri Seviye)

```csharp
// Item al
EnemyItemHolder holder = GetComponent<EnemyItemHolder>();
holder.PickupItem(itemObject, ItemHoldType.Primary);

// Saldır
holder.PerformAttack(targetPosition);

// Item fırlat
holder.ThrowCurrentItem(direction);

// Item düşür
holder.DropAllItems();
```

## 📋 Component Açıklamaları

| Component | Ne İşe Yarar? | Zorunlu mu? |
|-----------|---------------|-------------|
| **EnemyItemHolder** | Item tutma, kullanma ve fırlatma | ✅ Evet |
| **EnemyItemSeeker** | Otomatik item arama ve alma | ⚙️ Opsiyonel |
| **EnemyItemIntegration** | Savaşta otomatik silah kullanma | ⚙️ Opsiyonel |

## 🔧 Özelleştirme

### Item'ın Enemy Tarafından Alınmasını Engelle

PickupableItem component'inde:
- `Can Be Picked By Enemies` = `☐` (işareti kaldır)

### Sadece Belirli Durumlarda Item Topla

```csharp
EnemyItemSeeker seeker = GetComponent<EnemyItemSeeker>();
seeker.autoSeekItems = false; // Otomatiği kapat

// Sadece canı düşükken topla
if (health < 50f)
{
    seeker.TryFindAndPickupNearbyItem();
}
```

### Stratejik Item Kullanımı

```csharp
EnemyItemIntegration integration = GetComponent<EnemyItemIntegration>();

// Belli durumlarda item kullanımını kapat
integration.SetItemUsage(false);

// Item fırlat
integration.ForceThrowItem();
```

## ✅ Test Et

1. **Scene'e Enemy yerleştir** (yukarıdaki component'ler ekli olmalı)
2. **Scene'e PickupableItem yerleştir** (tabanca veya sopa)
3. **Play'e bas**
4. **Gözlemle:**
   - Enemy item'a yaklaşıyor mu? ✓
   - Enemy item'ı alıyor mu? ✓
   - Enemy item'ı tutuyor mu? ✓
   - Enemy silahla ateş ediyor mu? ✓

## 🐛 Sorun Giderme

**Enemy item alamıyor:**
- ✅ Item'ın Collider'ı var mı?
- ✅ Item'ın Layer'ı doğru mu?
- ✅ `Can Be Picked By Enemies` işaretli mi?
- ✅ Seek Radius yeterince büyük mü?

**Enemy ateş etmiyor:**
- ✅ `Can Use Items` işaretli mi?
- ✅ Item tuttuğundan emin misin? (Inspector'da kontrol et)
- ✅ Attack Range içinde mi?
- ✅ Item'ın PistolItem/BaseballBatItem component'i var mı?

**Performans sorunu:**
- ⚙️ Seek Interval'i artır (2 → 3-4 saniye)
- ⚙️ Seek Radius'u küçült (5 → 3)
- ⚙️ Layer Mask'ı daralt

## 📚 Daha Fazlası

Detaylı açıklama ve ileri seviye örnekler için:
👉 `README_Enemy_Item_System.md` dosyasına bak

## 🎉 Hazır!

Enemy'ler artık item kullanabilir. Başarılar! 🚀
