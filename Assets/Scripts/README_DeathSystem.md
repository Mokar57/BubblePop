# Death & Restart System - Hızlı Başlangıç

## ✨ Oluşturulan Dosyalar

1. **GameEvents.cs** - Static event manager (decoupled communication)
2. **LevelManager.cs** - Level yönetimi ve restart işlemleri
3. **DeathUI.cs** - Ölüm ekranı UI kontrolü
4. **DeathSystemTester.cs** - Test için yardımcı script (opsiyonel)
5. **PlayerControls.cs** - Güncellendi (GameEvents entegrasyonu)

## 🚀 5 Dakikada Kurulum

### 1️⃣ LevelManager Ekle (30 saniye)
```
Hierarchy → Create Empty → "LevelManager"
Add Component → LevelManager
```

### 2️⃣ UI Oluştur (2 dakika)
```
Hierarchy → UI → Canvas
Canvas altında:
  → UI → Panel (İsim: "DeathPanel")
     → UI → Text (TextMeshPro): "YOU DIED"
     → UI → Button (TextMeshPro): "RESTART"
```

### 3️⃣ DeathUI Bağla (1 dakika)
```
Canvas objesine → Add Component → DeathUI
Inspector'da:
  - Death Panel → DeathPanel'i sürükle
  - Restart Button → RestartButton'u sürükle
```

### 4️⃣ Fade Animasyonu (30 saniye)
```
DeathPanel objesini seç
Add Component → Canvas Group
```

### 5️⃣ Test Et! (1 dakika)
```
Hierarchy → Create Empty → "Tester"
Add Component → DeathSystemTester
Play → K tuşuna bas (damage)
Play → L tuşuna bas (instant death)
```

## 🎯 Nasıl Çalışır?

```
Player Ölür
    ↓
GameEvents.TriggerPlayerDeath()  [Static Event]
    ↓
    ├─→ LevelManager (dinliyor)
    └─→ DeathUI (dinliyor)
         └─→ Restart butonu → Sahne yenilenir
```

## 🔑 Önemli Notlar

✅ **Decoupled**: Player, LevelManager ve UI birbirine referans vermiyor
✅ **Event-based**: GameEvents üzerinden iletişim
✅ **Genişletilebilir**: Yeni listener'lar kolayca eklenebilir
✅ **Memory-safe**: OnDisable'da unsubscribe yapılır

## 🎮 Test Tuşları

- **K** → Player'a 50 damage
- **L** → Player'ı anında öldür
- **Context Menu** → Inspector'da sağ tıklayarak test fonksiyonları

## 📖 Detaylı Dokümantasyon

Daha fazla bilgi için `SETUP_DeathAndRestart.md` dosyasına bakın.
