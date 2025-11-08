# Weapon Sprite System - Kullanım Kılavuzu

## Genel Bakış
Bu sistem, silahların tutulup tutulmadığına göre farklı sprite'lar göstermesini sağlar. Bir karakter (Player veya Enemy) silahı tuttuğunda bir sprite, yerde iken başka bir sprite kullanılır.

## Nasıl Çalışır?

### Otomatik Kontrol
Sistem, `PickupableItem` component'inde bulunan `isHeld` bool değişkenini kullanarak silahın tutulup tutulmadığını otomatik olarak kontrol eder:
- **`isHeld = true`**: Silah bir karakter tarafından tutuluyorsa (parent != null)
- **`isHeld = false`**: Silah yerde ise (parent == null)

### Sprite Değişimi
Her frame'de `Update()` metodunda tutulma durumu kontrol edilir ve değişiklik varsa `UpdateSprite()` metodu çağrılarak sprite güncellenir.

## Unity Inspector'da Kurulum

### PickupableItem Component Ayarları

1. **Sprite When Held** (Tutuluyorken Sprite):
   - Silah bir karakter tarafından tutulurken gösterilecek sprite
   - Örnek: Silahın yan görünümü veya tutulmuş hali

2. **Sprite When Dropped** (Yerde İken Sprite):
   - Silah yerde dururken gösterilecek sprite
   - Örnek: Silahın üstten görünümü veya yerde durmuş hali
   - **Not**: Eğer atanmazsa, orijinal sprite kullanılır

3. **Depleted Sprite** (Tükendiğinde Sprite):
   - Silahın kullanımı bittiğinde gösterilecek sprite
   - Bu sprite, diğer iki sprite'tan bağımsız çalışır

## Örnek Kullanım

### Inspector'da Ayarlama
```
PickupableItem Component:
├─ Visual Settings
│  ├─ Sprite When Held: gun_held_sprite
│  ├─ Sprite When Dropped: gun_ground_sprite
│  └─ Depleted Sprite: gun_empty_sprite
└─ ...
```

### Kod İle Kontrol

Silahın tutulma durumunu manuel olarak kontrol etmek isterseniz:

```csharp
PickupableItem pickupable = weapon.GetComponent<PickupableItem>();

// Tutulma durumunu kontrol et
if (pickupable.IsHeld())
{
    Debug.Log("Silah tutuluyorken");
}

// Manuel olarak tutulma durumunu ayarla
pickupable.SetHeldState(true);  // Tutuluyorken sprite'ı göster
pickupable.SetHeldState(false); // Yerdeyken sprite'ı göster
```

## Özellikler

### Otomatik Güncelleme
- Silah alındığında otomatik olarak "held" sprite'ına geçer
- Silah bırakıldığında otomatik olarak "dropped" sprite'ına geri döner
- Her değişiklik anında görünür

### Karakter Desteği
- **Player**: `PlayerControls.PickupItem()` metodunda otomatik güncellenir
- **Enemy**: `EnemyItemHolder.PickupItem()` metodunda otomatik güncellenir

### Tükenen Silahlar
- Silah tükendiğinde (`isDepleted = true`) depleted sprite kullanılır
- Tükenen silahlar için sprite değişimi devre dışı kalır

## Teknik Detaylar

### Çalışma Mantığı
```
Update() her frame:
  ├─ parent != null mı kontrol et
  ├─ Durum değiştiyse:
  │  ├─ isHeld değerini güncelle
  │  └─ UpdateSprite() çağır
  └─ ...

UpdateSprite():
  ├─ Eğer isDepleted ise çık (depleted sprite kalır)
  ├─ Eğer isHeld && spriteWhenHeld != null:
  │  └─ spriteWhenHeld'i kullan
  └─ Eğer !isHeld && spriteWhenDropped != null:
     └─ spriteWhenDropped'i kullan
```

### Önemli Metodlar

#### `UpdateSprite()`
- Silahın mevcut durumuna göre sprite'ı günceller
- Private metod, otomatik olarak çağrılır

#### `SetHeldState(bool held)`
- Manuel olarak tutulma durumunu ayarlamak için
- Durum değişirse otomatik olarak sprite'ı günceller

#### `IsHeld()`
- Silahın tutulup tutulmadığını döndürür
- Returns: `bool` - true ise tutuluyorken, false ise yerde

#### `ResetPickupState()`
- Silah bırakıldığında çağrılır
- `isHeld = false` yapar ve sprite'ı günceller

## İpuçları

1. **Sprite Atama**: Her iki sprite'ı da atamak zorunda değilsiniz. Atanmayan sprite için varsayılan gösterilir.

2. **Test Etme**: Unity'de Play Mode'da silahı alıp bırakarak sprite değişimini test edin.

3. **Debugging**: `IsHeld()` metodunu kullanarak gerçek zamanlı durumu kontrol edebilirsiniz.

4. **Performance**: Sistem sadece durum değiştiğinde sprite'ı günceller, sürekli kontrol etmez.

## Sık Sorulan Sorular

**S: Sprite değişmiyor, ne yapmalıyım?**
- Inspector'da sprite'ların atandığından emin olun
- SpriteRenderer component'inin olduğunu kontrol edin
- Console'da hata mesajı var mı kontrol edin

**S: Depleted sprite çalışmıyor?**
- Depleted sprite diğerlerinden bağımsızdır
- Silah tükendiğinde otomatik olarak değişir
- `MarkAsDepleted()` metodunun çağrıldığından emin olun

**S: Enemy sprite değiştirmiyor?**
- `EnemyItemHolder.PickupItem()` metodunun düzgün çalıştığından emin olun
- Enemy'nin item'ı aldığını console'dan kontrol edin

## Güncellemeler

**v1.0 (8 Kasım 2025)**
- İlk implementasyon
- Player ve Enemy desteği
- Otomatik sprite değiştirme sistemi
- Bool-based tutulma kontrolü
