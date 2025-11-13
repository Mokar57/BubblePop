# 🎵 Item Ses Sistemi - Hızlı Kurulum

## 5 Adımda Kurulum

### 1️⃣ Ses Dosyalarını Hazırlayın
- Kullanım sesi (örn: `pistol_shot.wav`, `bat_swing.wav`)
- Fırlatma sesi (örn: `throw_whoosh.wav`)
- Dosyaları `Assets/Sounds/` klasörüne koyun

### 2️⃣ Item Prefab'ını Seçin
- Pistol veya BaseballBat prefab'ınızı seçin
- Inspector'da `PickupableItem` component'ini bulun

### 3️⃣ Audio Settings'i Doldurun
```
Audio Settings:
├─ Use Sound:        [Ses dosyanızı buraya sürükleyin]
├─ Throw Sound:      [Fırlatma sesini buraya sürükleyin]
├─ Use Sound Volume:  0.8  (Slider ile ayarlayın)
└─ Throw Sound Volume: 0.8  (Slider ile ayarlayın)
```

### 4️⃣ Prefab'ı Kaydedin
- Ctrl+S veya File → Save

### 5️⃣ Test Edin
- Play Mode'a girin
- Item'ı alın
- **Sol Tık** → Kullanım sesi ✅
- **Sağ Tık** → Fırlatma sesi ✅

---

## ⚡ Önemli Bilgiler

### ✅ Otomatik Özellikler
- AudioSource otomatik eklenir
- Player ve Enemy için çalışır
- Ses dosyası yoksa hata vermez

### 🎚️ Hacim Ayarları
- **0.0** = Sessiz
- **0.5** = Orta
- **1.0** = Maksimum

### 🎯 Ne Zaman Ses Çalar?
- **Sol Tık (Mouse0)**: 
  - Pistol ateş eder → `Use Sound` çalar
  - Beyzbol sopası sallanır → `Use Sound` çalar
  
- **Sağ Tık (Mouse1)**:
  - Item fırlatılır → `Throw Sound` çalar

---

## 🔍 Sorun Giderme

**Ses çalmıyor mu?**
1. ✓ Ses dosyası atandı mı?
2. ✓ Volume 0'dan büyük mü?
3. ✓ Unity'de Audio açık mı? (Edit → Project Settings → Audio)

**Ses çok yüksek/alçak?**
- Volume slider'larını ayarlayın (0-1 arası)

---

Detaylı bilgi için: `SETUP_Item_Sounds.md`
