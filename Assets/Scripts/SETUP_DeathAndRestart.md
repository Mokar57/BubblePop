# Death & Restart System - Kullanım Kılavuzu

## 📋 Genel Bakış

Bu sistem, **decoupled (bağımsız)** mimari kullanarak player ölümü ve level restart işlemlerini yönetir. Event-based tasarım sayesinde sınıflar birbirine bağımlı değildir.

## 🏗️ Sistem Bileşenleri

### 1. **GameEvents.cs** (Static Event Manager)
- Oyun genelinde kullanılacak eventleri yönetir
- `OnPlayerDied`: Player öldüğünde tetiklenir
- `OnLevelRestarted`: Level restart edildiğinde tetiklenir

### 2. **LevelManager.cs** (Level Yöneticisi)
- Player death event'ini dinler
- Sahne restart işlemlerini yönetir
- UI veya diğer sistemlerden bağımsızdır

### 3. **DeathUI.cs** (Ölüm Ekranı UI)
- Player death event'ini dinler
- Ölüm ekranını gösterir
- Restart butonunu yönetir

### 4. **PlayerControls.cs** (Güncellendi)
- `Die()` metodunda `GameEvents.TriggerPlayerDeath()` çağrısı eklendi
- Diğer sistemlere direkt referans yok

## 🎮 Unity'de Kurulum

### Adım 1: LevelManager Oluştur
1. Hierarchy'de sağ tık → Create Empty
2. İsim: "LevelManager"
3. Inspector'da "Add Component" → "LevelManager"
4. Ayarlar:
   - **Scene To Load**: Boş bırak (aktif sahneyi yeniden yükler) veya sahne adı yaz
   - **Restart Delay**: 0.5 saniye (isteğe göre ayarla)

### Adım 2: UI Canvas Oluştur
1. Hierarchy'de sağ tık → UI → Canvas
2. Canvas altında Panel oluştur:
   - Sağ tık → UI → Panel
   - İsim: "DeathPanel"
   - Renk: Yarı saydam siyah (örn: R:0, G:0, B:0, A:200)

3. DeathPanel altında UI elementleri ekle:
   ```
   DeathPanel
   ├── TitleText (UI → Text - TextMeshPro)
   │   └── "YOU DIED" yazısı
   ├── RestartButton (UI → Button - TextMeshPro)
   │   └── "RESTART" yazısı
   └── QuitButton (UI → Button - TextMeshPro) [Opsiyonel]
       └── "QUIT" yazısı
   ```

### Adım 3: DeathUI Script Ekle
1. Canvas objesine "Add Component" → "DeathUI"
2. Inspector'da referansları ata:
   - **Death Panel**: DeathPanel objesini sürükle
   - **Restart Button**: RestartButton'u sürükle
   - **Quit Button**: QuitButton'u sürükle (opsiyonel)
3. Ayarlar:
   - **Fade In Duration**: 0.5 saniye
   - **Show Delay**: 1 saniye (player öldükten sonra UI gösterilmeden önce bekleme)

### Adım 4: CanvasGroup Ekle (Fade Animasyonu için)
1. DeathPanel objesini seç
2. "Add Component" → "Canvas Group"
3. Bu sayede UI fade-in animasyonu çalışır

### Adım 5: Test Et
1. Oyunu başlat
2. Player'ın canını 0'a düşür (düşmanla çarpış, test butonu, vb.)
3. Ölüm ekranı görünmeli
4. "RESTART" butonuna bas
5. Sahne yeniden başlamalı

## 🔧 İleri Düzey Özelleştirmeler

### Time Scale Durdurma
Eğer player öldüğünde oyunu durdurmak istersen:

**LevelManager.cs** → `HandlePlayerDeath()` metodunda:
```csharp
private void HandlePlayerDeath()
{
    Debug.Log("LevelManager: Player death detected");
    
    if (!isRestarting)
    {
        isRestarting = true;
        Time.timeScale = 0f; // Bu satırı aktif et
    }
}
```

**Not:** DeathUI zaten `Time.unscaledDeltaTime` kullandığı için fade animasyonu çalışır.

### Farklı Sahneye Yönlendirme
Ana menü veya game over sahnesine gitmek için:

**LevelManager** Inspector'da:
- **Scene To Load**: "GameOverScene" veya "MainMenu"

### Ek Butonlar
Quit butonu için DeathUI script'inde zaten destek var:
```csharp
public void QuitToMenu(string menuSceneName)
```

## 📊 Event Sistemi Nasıl Çalışır?

```
Player Ölür
    ↓
PlayerControls.Die()
    ↓
GameEvents.TriggerPlayerDeath()
    ↓
    ├→ LevelManager.HandlePlayerDeath() (dinliyor)
    │    └→ Restart hazırlığı
    │
    └→ DeathUI.HandlePlayerDeath() (dinliyor)
         └→ UI göster
              └→ Butona bas → LevelManager.RestartLevel()
```

## ✅장점 (Avantajlar)

1. **Decoupled**: Sınıflar birbirine bağımlı değil
2. **Genişletilebilir**: Yeni event listener'lar kolayca eklenebilir
3. **Test Edilebilir**: Her component bağımsız test edilebilir
4. **Bakımı Kolay**: Değişiklikler tek bir yerde yapılır

## 🐛 Sorun Giderme

### Ölüm Ekranı Görünmüyor
- DeathPanel başlangıçta aktif mi kontrol et (olmamalı)
- DeathUI script'inde referanslar atanmış mı?
- Console'da "DeathUI: Player death detected" mesajı görüyor musun?

### Restart Çalışmıyor
- LevelManager sahnede var mı?
- Restart butonuna listener atanmış mı?
- Console'da "LevelManager: Restarting level" mesajı görüyor musun?
- Build Settings'de sahne eklendi mi?

### Event Çalışmıyor
- GameEvents.cs dosyası derlenmiş mi?
- PlayerControls.Die() metodu güncellenmiş mi?
- OnEnable/OnDisable metodları doğru çalışıyor mu?

## 📝 Notlar

- Bu sistem Unity 2022+ için tasarlandı (`FindFirstObjectByType` API kullanır)
- Eski Unity versiyonları için `FindFirstObjectByType` yerine `FindObjectOfType` kullan
- Event sistemi memory-safe'tir (OnDisable'da unsubscribe yapılır)
