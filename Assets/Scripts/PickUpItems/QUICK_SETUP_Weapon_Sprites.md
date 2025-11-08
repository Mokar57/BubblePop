# Weapon Sprite System - Hızlı Kurulum Kılavuzu

## 5 Dakikada Kurulum! 🚀

### Adım 1: Sprite'larınızı Hazırlayın
Silahınız için iki sprite hazırlayın:
- 🤝 **Held Sprite**: Karakter tutarken gösterilecek (yan görünüm)
- 📦 **Dropped Sprite**: Yerde dururken gösterilecek (üstten görünüm)

### Adım 2: Inspector'da Atamaları Yapın

1. Unity'de silah GameObject'inizi seçin
2. `PickupableItem` component'ini bulun
3. **Visual Settings** bölümünde:
   ```
   ├─ Sprite When Held: [Held sprite'ınızı sürükleyin]
   └─ Sprite When Dropped: [Dropped sprite'ınızı sürükleyin]
   ```

### Adım 3: Test Edin!

1. Play Mode'a girin
2. Silahı alın → Held sprite görünmeli
3. Silahı bırakın → Dropped sprite görünmeli

## Tamamdı! ✅

Sistem otomatik çalışır. Ekstra kod yazmaya gerek yok!

---

## Ekstra İpuçları

### Sprite Yoksa Ne Olur?
Sprite atanmazsa orijinal sprite kullanılır. Sorun yok!

### Sadece Birini Atayabilir miyim?
Evet! İstediğiniz sprite'ı atayın, diğerini boş bırakın.

### Depleted Sprite Nedir?
Silahın kullanımı bittiğinde gösterilecek sprite. İsteğe bağlı.

---

## Hızlı Referans

| Durum | Kullanılan Sprite |
|-------|------------------|
| ✋ Tutuluyorken | `spriteWhenHeld` |
| 📦 Yerdeyken | `spriteWhenDropped` |
| 🚫 Tükendiyse | `depletedSprite` |
| ❓ Sprite yoksa | Orijinal sprite |

---

## Sorun mu var?

### Sprite değişmiyor?
✅ Sprite'ların atandığını kontrol edin  
✅ SpriteRenderer component'i olduğunu kontrol edin  
✅ Console'da hata var mı bakın

### Daha fazla yardım?
📖 Detaylı bilgi için: `README_Weapon_Sprite_System.md`

---

**Başarılar! 🎮**
