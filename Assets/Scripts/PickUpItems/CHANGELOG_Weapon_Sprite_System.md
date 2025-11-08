# Weapon Sprite System - Değişiklik Kaydı

## 8 Kasım 2025 - v1.0

### Yeni Özellikler

#### PickupableItem.cs
- **Yeni Değişkenler**:
  - `spriteWhenHeld`: Silah tutuluyorken gösterilecek sprite
  - `spriteWhenDropped`: Silah yerde iken gösterilecek sprite
  - `isHeld`: Silahın tutulup tutulmadığını kontrol eden bool

- **Yeni Metodlar**:
  - `UpdateSprite()`: Silahın sprite'ını durumuna göre günceller (private)
  - `SetHeldState(bool held)`: Manuel olarak tutulma durumunu ayarlar
  - `IsHeld()`: Silahın tutulup tutulmadığını döndürür

- **Güncellenmiş Metodlar**:
  - `Start()`: `spriteWhenDropped` için varsayılan değer ataması ve başlangıç sprite güncellemesi
  - `Update()`: Tutulma durumu değişikliğini kontrol eder ve sprite'ı günceller
  - `ResetPickupState()`: Silah bırakıldığında `isHeld = false` yapar ve sprite'ı günceller

#### PlayerControls.cs
- **Güncellenmiş Metodlar**:
  - `PickupItem()`: Silah alındığında `SetHeldState(true)` çağrılır

#### EnemyItemHolder.cs
- **Güncellenmiş Metodlar**:
  - `PickupItem()`: Enemy silah aldığında `SetHeldState(true)` çağrılır

### Çalışma Mantığı

#### Otomatik Kontrol Sistemi
```
Update() döngüsü:
1. transform.parent != null ile tutulma durumu kontrol edilir
2. Durum değiştiyse (tutulmuş/bırakılmış):
   - isHeld güncellenir
   - UpdateSprite() çağrılır
```

#### Sprite Seçim Önceliği
```
UpdateSprite() mantığı:
1. Eğer isDepleted ise -> depletedSprite kullanılır (değişmez)
2. Eğer isHeld && spriteWhenHeld != null -> spriteWhenHeld kullanılır
3. Eğer !isHeld && spriteWhenDropped != null -> spriteWhenDropped kullanılır
```

### Inspector Ayarları

Yeni alanlar "Visual Settings" başlığı altında:
```
[Header("Visual Settings")]
public GameObject pickupIndicator;
public Sprite spriteWhenHeld;      // YENİ
public Sprite spriteWhenDropped;   // YENİ
```

### Kullanım Örnekleri

#### Unity Inspector
```
GameObject: Pistol
├─ PickupableItem
│  ├─ Visual Settings
│  │  ├─ Sprite When Held: pistol_held
│  │  └─ Sprite When Dropped: pistol_ground
│  └─ ...
```

#### Kod Kullanımı
```csharp
// Durumu kontrol et
if (weapon.GetComponent<PickupableItem>().IsHeld())
{
    Debug.Log("Silah tutuluyorken");
}

// Manuel olarak ayarla (normalde gerekli değil)
weapon.GetComponent<PickupableItem>().SetHeldState(true);
```

### Teknik Detaylar

#### Performance Optimizasyonu
- Sprite sadece durum değiştiğinde güncellenir (her frame değil)
- `isHeld != isCurrentlyHeld` kontrolü ile gereksiz güncellemeler engellenir

#### Geriye Dönük Uyumluluk
- Eski projeler için `spriteWhenDropped` atanmamışsa orijinal sprite kullanılır
- Mevcut sistem çalışmaya devam eder

#### Test Edildi
- ✅ Player silah alma/bırakma
- ✅ Enemy silah alma/bırakma
- ✅ Silah fırlatma
- ✅ Depleted sprite ile uyumluluk
- ✅ Her iki sprite atanmadığında varsayılan davranış

### Değişiklik Özeti

| Dosya | Değişiklik Türü | Açıklama |
|-------|-----------------|----------|
| PickupableItem.cs | Yeni Özellik | Sprite değiştirme sistemi eklendi |
| PlayerControls.cs | Güncelleme | Item alımında sprite güncelleme eklendi |
| EnemyItemHolder.cs | Güncelleme | Item alımında sprite güncelleme eklendi |
| README_Weapon_Sprite_System.md | Dokümantasyon | Kullanım kılavuzu oluşturuldu |

### Bilinen Kısıtlamalar
- Depleted sprite aktif olduğunda diğer sprite'lar çalışmaz (intended behavior)
- Sprite değişimi sadece SpriteRenderer component'i varsa çalışır

### Gelecek Geliştirmeler (Öneriler)
- [ ] Animasyon desteği (sprite'lar yerine animation clip'ler)
- [ ] Daha fazla durum (örn: reloading, aiming)
- [ ] Custom sprite transition efektleri
- [ ] Sprite flip/rotation ayarları

---

**Not**: Bu sistem geriye dönük uyumludur. Mevcut projelerde herhangi bir değişiklik yapmadan çalışır. Yeni sprite sistemini kullanmak için sadece Inspector'dan sprite'ları atamanız yeterlidir.
