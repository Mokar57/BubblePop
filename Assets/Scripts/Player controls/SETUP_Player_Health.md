# Player Can Sistemi - Hızlı Kurulum Rehberi

## Adım 1: Player Ayarları

1. Hierarchy'de Player GameObject'ini seç
2. Inspector'da **PlayerControls** component'ini bul
3. **Health Settings** bölümünde:
   - `Max Health`: 100 (varsayılan değer, istediğiniz gibi değiştirebilirsiniz)
4. **Death Effects** bölümünde:
   - `Damage Color`: Kırmızı (hasar aldığında yanacak renk)
   - `Flash Duration`: 0.1 saniye

## Adım 2: Player Tag'ini Kontrol Et

1. Player GameObject'i seçili iken
2. Inspector'ın en üstünde **Tag** dropdown'unu kontrol edin
3. "Player" seçili olmalı
4. Değilse, dropdown'dan "Player" seçin

## Adım 3: Can Barı UI Oluşturma (Opsiyonel)

### Canvas Oluştur
1. Hierarchy'de sağ tık → UI → Canvas
2. Canvas oluşturuldu

### Health Bar (Slider)
1. Canvas'a sağ tık → UI → Slider
2. Slider'ı "HealthBar" olarak yeniden adlandır
3. Inspector'da:
   - Rect Transform'u ayarlayın (ekranın üst köşesinde olabilir)
   - `Min Value`: 0
   - `Max Value`: 100
   - `Whole Numbers`: Kapalı

### Health Text
1. Canvas'a sağ tık → UI → Legacy → Text
2. Text'i "HealthText" olarak yeniden adlandır
3. Inspector'da:
   - Text: "HP: 100/100"
   - Font Size: 24 veya istediğiniz boyut
   - Color: Beyaz veya istediğiniz renk
   - Alignment: Ortala (opsiyonel)

### UI Script Ekle
1. Canvas'ı seç
2. Inspector'da **Add Component**
3. "PlayerHealthUI" yazın ve scripti ekleyin
4. Script component'inde:
   - `Health Slider`: HealthBar'ı sürükle-bırak
   - `Health Text`: HealthText'i sürükle-bırak
   - `Player`: Boş bırakabilirsiniz (otomatik bulunur) veya Player GameObject'ini sürükleyebilirsiniz

## Adım 4: Test Etme

1. Play butonuna basın
2. Düşmanların size ateş etmesini bekleyin veya:
   - Bir silah alın
   - Sağ tıklayarak fırlatın
   - Kendinize çarptırın
3. Can barının azaldığını ve player'ın kırmızı yanıp söndüğünü göreceksiniz
4. Can sıfıra düştüğünde player ölecek

## Şu Anda Çalışan Özellikler

✅ Player'ın canı var (varsayılan 100)
✅ Mermiler player'a hasar verir
✅ Sopa (baseball bat) player'a hasar verir
✅ Fırlatılan itemler player'a hasar verir
✅ Hasar aldığında görsel geri bildirim (kırmızı yanıp sönme)
✅ Can sıfıra düştüğünde ölüm
✅ Can barı UI (opsiyonel)
✅ Event sistemi (OnHealthChanged, OnPlayerDeath)

## Sorun Giderme

### Player hasar almıyor
- Player GameObject'inin "Player" tag'ine sahip olduğundan emin olun
- Player'ın Collider2D component'i olduğundan emin olun

### UI görünmüyor
- Canvas'ın Render Mode'u "Screen Space - Overlay" olmalı
- HealthBar ve HealthText aktif olmalı
- PlayerHealthUI script'inin references'ları doğru atanmalı

### Player hemen ölüyor
- Max Health değerini artırın (örn: 200 veya 500)
- Silahların damage değerlerini azaltın

### Hasar efekti görünmüyor
- Player GameObject'inin bir SpriteRenderer component'i olduğundan emin olun
- Damage Color'ın belirgin bir renk olduğundan emin olun (örn: kırmızı)

## İleri Seviye Özelleştirme

### Ölüm Sonrası Restart
```csharp
// PlayerControls.cs içinde Die() fonksiyonuna ekleyin:
using UnityEngine.SceneManagement;

private void Die()
{
    // ... mevcut kod ...
    
    // 2 saniye sonra sahneyi yeniden yükle
    Invoke("RestartLevel", 2f);
}

private void RestartLevel()
{
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
```

### Can Paketi (Health Pickup)
```csharp
// HealthPickup.cs - yeni bir script
public class HealthPickup : MonoBehaviour
{
    public float healAmount = 50f;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerControls player = other.GetComponent<PlayerControls>();
        if (player != null)
        {
            player.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
```

## Başarılı! 🎉

Player artık can sistemine sahip ve hasar alabilir. Oyununuzun geri kalanını bu sisteme göre geliştirebilirsiniz!
