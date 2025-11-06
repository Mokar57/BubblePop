# Player Can Sistemi (Player Health System)

## Genel Bakış
Player artık bir can sistemine sahip. Silahlar ve fırlatılan itemler player'a dokunduğunda hasar alır ve canı sıfıra düştüğünde ölür.

## Özellikler

### Sağlık Sistemi
- **Maksimum Can**: Inspector'dan ayarlanabilir (varsayılan: 100)
- **Mevcut Can**: Oyun başında maksimum can ile başlar
- **Hasar Alma**: Silahlar ve fırlatılan itemler hasar verir
- **Görsel Geri Bildirim**: Hasar alındığında kırmızı yanıp söner
- **Ölüm**: Can sıfıra düştüğünde player ölür

### Hasar Kaynakları
1. **Projectile (Mermiler)**: Pistol ateşlendiğinde oluşan mermiler player'a çarptığında hasar verir
2. **Baseball Bat (Sopa)**: Yakın mesafe saldırısı player'a isabet ederse hasar verir
3. **Thrown Items (Fırlatılan İtemler)**: Fırlatılan silahlar player'a çarptığında hasar verir

### Event Sistemi
- `OnHealthChanged`: Can değiştiğinde tetiklenir (UI güncellemeleri için)
- `OnPlayerDeath`: Player öldüğünde tetiklenir (game over ekranı için)

## Kullanım

### Unity Inspector'da Ayarlar

#### PlayerControls Component
```
Health Settings:
- Max Health: 100 (oyuncunun maksimum canı)

Death Effects:
- Damage Color: Red (hasar aldığında yanıp söneceği renk)
- Flash Duration: 0.1 (yanıp sönme süresi)
```

### Code'dan Kullanım

```csharp
// Player'a hasar ver
playerControls.TakeDamage(25f);

// Player'ı iyileştir
playerControls.Heal(50f);

// Mevcut canı kontrol et
float currentHealth = playerControls.CurrentHealth;

// Maksimum canı kontrol et
float maxHealth = playerControls.MaxHealth;

// Player öldü mü?
bool isDead = playerControls.IsDead;
```

### Event'lere Subscribe Olma

```csharp
void Start()
{
    PlayerControls player = FindObjectOfType<PlayerControls>();
    
    // Can değiştiğinde
    player.OnHealthChanged += (health) => {
        Debug.Log($"Player health: {health}");
    };
    
    // Player öldüğünde
    player.OnPlayerDeath += () => {
        Debug.Log("Game Over!");
        // Restart level veya game over ekranı göster
    };
}
```

## UI Entegrasyonu

`PlayerHealthUI` scripti player'ın canını göstermek için kullanılabilir:

### Kurulum
1. Canvas oluştur (yoksa)
2. Canvas'a bir Slider ekle (Health bar için)
3. Canvas'a bir Text ekle (Can sayısı için)
4. Canvas'a `PlayerHealthUI` component'i ekle
5. References'ları ayarla:
   - Health Slider
   - Health Text
   - Player (otomatik bulunur ama manuel atanabilir)

### PlayerHealthUI Özellikleri
- Can değişikliklerini otomatik takip eder
- Health bar'ı günceller
- Can miktarını text olarak gösterir
- Player öldüğünde "DEAD" yazısını gösterir

## Önemli Notlar

### Player Tag
Player GameObject'inin "Player" tag'ine sahip olduğundan emin olun. Bu, silahların player'ı tanıması için gereklidir.

### Collider Ayarları
Player'ın bir Collider2D component'ine sahip olması gerekir (zaten var).

### Ölüm Sonrası
Player öldüğünde:
- PlayerControls component'i devre dışı kalır
- Hareket durur
- Sprite'ı gri renk ve yarı şeffaf olur
- Debug.Log ile "Player Died!" mesajı gösterilir

Kendi game over logic'inizi `OnPlayerDeath` event'ine subscribe olarak ekleyebilirsiniz:
```csharp
player.OnPlayerDeath += () => {
    // Restart level
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    
    // Veya game over ekranı göster
    // gameOverPanel.SetActive(true);
};
```

## Test Etme

1. Scene'e bir Player ekleyin (PlayerControls component'li)
2. Scene'e düşmanlar ekleyin (silahlarla)
3. Play mode'a geçin
4. Düşmanların ateş etmesini bekleyin veya kendinize silah atın
5. Player'ın hasar aldığını ve kırmızı yanıp söndüğünü göreceksiniz
6. Can sıfıra düştüğünde player ölecek

## Gelecek Geliştirmeler

- Yeniden canlanma sistemi
- Can paketleri (health pickups)
- Shield/armor sistemi
- İnvulnerability frames (hasar almadan sonra kısa süre bağışıklık)
- Farklı ölüm animasyonları
- Game over ekranı entegrasyonu
