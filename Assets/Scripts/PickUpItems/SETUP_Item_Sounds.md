# Item Ses Sistemi Kurulum Rehberi

## 🎵 Genel Bakış
Bu rehber, `PickupableItem` scriptine eklenen ses sisteminin nasıl kullanılacağını açıklar. Artık her item için sol tık (kullanım) ve sağ tık (fırlatma) sesleri tanımlayabilirsiniz.

## ✨ Özellikler
- **Sol Tık Sesi (Use Sound)**: Item kullanıldığında (ateş edildiğinde, sallandığında) çalan ses
- **Sağ Tık Sesi (Throw Sound)**: Item fırlatıldığında çalan ses
- **Hacim Kontrolü**: Her ses için ayrı ayrı hacim ayarı (0-1 arası)
- **Otomatik AudioSource**: Script otomatik olarak AudioSource component'i ekler
- **Player ve Enemy Desteği**: Hem Player hem de Enemy'ler item kullandığında sesler çalar

## 📦 Kurulum Adımları

### 1. Ses Dosyalarını Unity'ye Ekleyin
1. **Sounds** klasörünüzde (Assets/Sounds) ses dosyalarınızı bulundurun
2. Örnek ses tipleri:
   - `pistol_shot.wav` - Tabanca atış sesi
   - `bat_swing.wav` - Beyzbol sopası sallanma sesi
   - `throw_whoosh.wav` - Fırlatma sesi
   - `weapon_throw.wav` - Silah fırlatma sesi

### 2. Item Prefab'ını Yapılandırın

#### Adım 1: Prefab'ı Açın
1. **Assets/Prefab/** klasöründen item prefab'ınızı seçin (örn: `Pistol`, `BaseballBat`)
2. Unity Editor'de Prefab'ı açın

#### Adım 2: Audio Settings Bölümünü Doldurun
`PickupableItem` component'inde **Audio Settings** bölümünü göreceksiniz:

```
┌─ Audio Settings ─────────────────────────┐
│                                           │
│ Use Sound:         [AudioClip Slot]      │
│ Throw Sound:       [AudioClip Slot]      │
│ Use Sound Volume:  [Slider: 0-1]         │
│ Throw Sound Volume:[Slider: 0-1]         │
│                                           │
└───────────────────────────────────────────┘
```

#### Adım 3: Ses Dosyalarını Atayın
1. **Use Sound**: Project penceresinden uygun ses dosyasını sürükleyip buraya bırakın
   - Pistol için: Atış sesi
   - Beyzbol sopası için: Sallanma sesi
   
2. **Throw Sound**: Fırlatma sesi dosyasını sürükleyip buraya bırakın
   - Genellikle "whoosh" veya "throw" sesi

3. **Use Sound Volume**: Kullanım sesinin yüksekliğini ayarlayın (0 = sessiz, 1 = maksimum)
   
4. **Throw Sound Volume**: Fırlatma sesinin yüksekliğini ayarlayın

### 3. Örnek Kurulum - Pistol

```
Item Name: "Pistol"
Hold Type: Primary

Audio Settings:
├─ Use Sound: "pistol_shot"
├─ Throw Sound: "weapon_throw"
├─ Use Sound Volume: 0.7
└─ Throw Sound Volume: 0.8
```

### 4. Örnek Kurulum - Baseball Bat

```
Item Name: "Baseball Bat"
Hold Type: Secondary

Audio Settings:
├─ Use Sound: "bat_swing"
├─ Throw Sound: "weapon_throw"
├─ Use Sound Volume: 0.6
└─ Throw Sound Volume: 0.8
```

## 🎮 Nasıl Çalışır?

### Player Kullanımı
- **Sol Tık**: Player silahı kullandığında (ateş etme/sallama)
  - `PlayerControls.cs` → `PerformAttack()` → `pickupableItem.PlayUseSound()`
  
- **Sağ Tık**: Player silahı fırlattığında
  - `PlayerControls.cs` → `ThrowCurrentItem()` → `pickupableItem.PlayThrowSound()`

### Enemy Kullanımı
- **Saldırı**: Enemy silah kullandığında
  - `EnemyItemHolder.cs` → `PerformAttack()` → `pickupableItem.PlayUseSound()`
  
- **Fırlatma**: Enemy silahı fırlattığında
  - `EnemyItemHolder.cs` → `ThrowCurrentItem()` → `pickupableItem.PlayThrowSound()`

## 🔧 Teknik Detaylar

### AudioSource Ayarları
Script otomatik olarak şu ayarlarla AudioSource ekler:
- **Play On Awake**: `false` (Manuel kontrol için)
- **Spatial Blend**: `0` (2D ses - mesafeye bağlı değişmez)
- **Volume**: Her ses için ayrı ayrı ayarlanabilir

### API Referansı

```csharp
// Kullanım sesini çal
pickupableItem.PlayUseSound();

// Fırlatma sesini çal
pickupableItem.PlayThrowSound();
```

## ⚠️ Önemli Notlar

1. **Ses Dosyası Formatı**: 
   - Önerilen: `.wav` veya `.ogg` formatı
   - Unity'de ses dosyalarını import ettikten sonra Inspector'da ayarları kontrol edin

2. **AudioSource Otomatik Eklenir**:
   - Prefab'da manuel AudioSource eklemenize gerek yok
   - Script otomatik olarak ekler ve yapılandırır

3. **Ses Çalmama Durumu**:
   - Eğer ses çalmıyorsa:
     - Audio Settings'de ses dosyalarının atandığından emin olun
     - Volume değerlerinin 0'dan büyük olduğunu kontrol edin
     - Unity'nin Audio Settings'inde Master Volume'un açık olduğunu kontrol edin

4. **İsteğe Bağlı**:
   - Sesler opsiyoneldir
   - Eğer `Use Sound` veya `Throw Sound` boş bırakılırsa, hiçbir ses çalmaz
   - Hata vermez, sessizce devam eder

## 🎯 Hızlı Test

1. Item prefab'ını yapılandırın
2. Play Mode'a girin
3. Player ile item'ı alın
4. **Sol tıklayın** → Kullanım sesi duyulmalı
5. **Sağ tıklayın** → Fırlatma sesi duyulmalı

## 📝 Güncelleme Geçmişi

**Tarih**: 13 Kasım 2025
**Versiyon**: 1.0

**Değişiklikler**:
- ✅ `PickupableItem.cs` - Audio Settings eklendi
- ✅ `PickupableItem.cs` - `PlayUseSound()` ve `PlayThrowSound()` metodları eklendi
- ✅ `PlayerControls.cs` - Sol tık ve sağ tık ses entegrasyonu
- ✅ `EnemyItemHolder.cs` - Enemy attack ve throw ses entegrasyonu
- ✅ Otomatik AudioSource ekleme ve yapılandırma

---

**Hazırlayan**: GitHub Copilot
**Proje**: BubblePop
**Script**: PickupableItem.cs
