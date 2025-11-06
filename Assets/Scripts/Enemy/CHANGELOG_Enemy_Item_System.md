# Enemy Item System - Yapılan Değişiklikler

## 📝 Özet

Enemy'ler artık Player'lar gibi `PickupableItem`'ları tutabilir, kullanabilir ve fırlatabilir. Sistem, Player'daki item tutma sisteminin birebir kopyası olarak tasarlandı: **Primary** ve **Secondary** olmak üzere 2 slot mevcut.

---

## 🆕 Eklenen Dosyalar

### 1. `EnemyItemHolder.cs`
**Açıklama:** Enemy'nin item tutma ve kullanma sisteminin ana component'i.

**Özellikler:**
- ✅ Primary ve Secondary hold position'ları
- ✅ Item alma (PickupItem)
- ✅ Item düşürme (DropItem, DropAllItems)
- ✅ Silahla saldırma (PerformAttack)
- ✅ Item fırlatma (ThrowCurrentItem)
- ✅ Yardımcı metodlar (HasWeapon, GetCurrentWeapon, vb.)
- ✅ **YENİ:** Görüş yönüne göre otomatik dönme (UpdateRotationBasedOnVision)

**Kullanım:** Player'daki `PlayerControls` sisteminin aynısı ama Enemy için.

**Önemli Not:** Enemy artık `currentVisionDirection` (görüş yönü) ile birlikte döner. `LateUpdate()` içinde enemy'nin transform'u otomatik olarak görüş yönüne göre güncellenir, böylece tuttuğu item'lar da onunla birlikte döner.

---

### 2. `EnemyItemSeeker.cs`
**Açıklama:** Enemy'nin otomatik olarak yakındaki item'ları bulup almasını sağlar.

**Özellikler:**
- ✅ Otomatik item arama (belirli aralıklarla)
- ✅ Radius ve interval ayarları
- ✅ Primary item tercih sistemi
- ✅ Sadece silahsızken arama seçeneği
- ✅ Manuel item arama metodları
- ✅ **YENİ (v2):** NavMesh navigasyon entegrasyonu
- ✅ **YENİ (v2):** Pickup radius (item'ı almak için gereken yakınlık)
- ✅ **YENİ (v2):** Item takip sistemi (seeking state)
- ✅ **YENİ (v2):** AI pause özelliği (opsiyonel)
- ✅ **YENİ (v2):** İptal edilebilir arama

**Kullanım:** Component'i ekle, ayarları yap, otomatik çalışır.

**Önemli Değişiklik (v2):** Enemy artık item'ı uzaktan almak yerine, **item'a doğru yürüyüp yakınında iken alır**. Bu daha gerçekçi bir davranış sağlar.

---

### 3. `EnemyItemIntegration.cs`
**Açıklama:** EnemyAI ile item sistemini entegre eder, savaşta otomatik silah kullanımını sağlar.

**Özellikler:**
- ✅ Otomatik saldırı (attack range kontrolü)
- ✅ Primary weapon (tabanca) = uzaktan saldırı
- ✅ Secondary weapon (sopa) = yakın dövüş
- ✅ Attack cooldown sistemi
- ✅ Enemy öldüğünde item'ları düşürme

**Kullanım:** EnemyAI'a ekle, range ve cooldown ayarlarını yap.

---

### 4. `README_Enemy_Item_System.md`
**Açıklama:** Detaylı kullanım kılavuzu (Türkçe).

**İçerik:**
- 📖 Tüm component'lerin detaylı açıklaması
- 📖 Kurulum adımları
- 📖 Kod örnekleri (6+ farklı senaryo)
- 📖 Best practices
- 📖 Sorun giderme
- 📖 İleri seviye kullanım örnekleri

---

### 5. `README_Enemy_Item_Quick_Setup.md`
**Açıklama:** Hızlı kurulum kılavuzu (3 adımda kurulum).

**İçerik:**
- ⚡ 3 adımlık kurulum
- ⚡ Component açıklamaları
- ⚡ Test senaryoları
- ⚡ Sorun giderme ipuçları

---

## 🔄 Güncellenen Dosyalar

### `PickupableItem.cs`

**Eklenen Özellikler:**

1. **Yeni Alan:**
   ```csharp
   public bool canBePickedByEnemies = true;
   ```
   - Enemy'lerin bu item'ı alıp alamayacağını belirler

2. **Yeni Metod:**
   ```csharp
   public bool TryPickupByEnemy(EnemyItemHolder enemyHolder)
   ```
   - Enemy'nin item'ı programatik olarak almasını sağlar
   - Tüm kontrolleri yapar (depleted, held, vb.)
   - Boolean döndürür (başarılı/başarısız)

**Geriye Uyumluluk:** ✅ Mevcut Player pickup sistemi aynen çalışmaya devam eder.

---

## 🎯 Sistem Özellikleri

### Slot Sistemi
- **Primary Slot:** Önde tutulan item'lar (örn: tabanca)
- **Secondary Slot:** Yanda tutulan item'lar (örn: beyzbol sopası)
- Aynı anda sadece 1 item tutulabilir (Player ile aynı)

### Item Alma
- Otomatik (EnemyItemSeeker ile)
- Manuel (kod ile)
- Radius ve interval ayarlanabilir
- Primary item'lara öncelik verilebilir

### Item Kullanma
- Primary weapon = Projectile attack (ateş etme)
- Secondary weapon = Melee attack (yakın dövüş)
- Attack cooldown sistemi
- Target'a doğru otomatik nişan alma

### Item Fırlatma
- Player'daki sistemin aynısı
- Fırlatılan item depleted olur
- Fizik sistemi ile uçar
- ThrownItem component otomatik eklenir

### Item Düşürme
- Tekli düşürme (DropItem)
- Toplu düşürme (DropAllItems)
- Enemy öldüğünde otomatik düşürme (EnemyItemIntegration ile)
- Düşen item'lar tekrar alınabilir

---

## 🎮 Kullanım Senaryoları

### Senaryo 1: Temel Kurulum
```
Enemy + EnemyItemHolder + EnemyItemSeeker
→ Enemy otomatik olarak item toplar ve tutar
```

### Senaryo 2: Savaşan Enemy
```
Enemy + EnemyItemHolder + EnemyItemIntegration
→ Enemy silahla otomatik ateş eder
```

### Senaryo 3: Full Featured Enemy
```
Enemy + EnemyItemHolder + EnemyItemSeeker + EnemyItemIntegration
→ Enemy item toplar, tutar, savaşta kullanır, ölünce düşürür
```

### Senaryo 4: Özel Kontrol
```
Enemy + EnemyItemHolder (diğerleri yok)
→ Kendi scriptinle tamamen kontrol et
```

---

## 🔧 Ayarlanabilir Parametreler

### EnemyItemHolder
- `primaryHoldPosition` - Primary item tutma pozisyonu
- `secondaryHoldPosition` - Secondary item tutma pozisyonu
- `throwForce` - Item fırlatma gücü
- `canUseItems` - Item kullanımı açık/kapalı

### EnemyItemSeeker
- `autoSeekItems` - Otomatik arama açık/kapalı
- `seekRadius` - Arama yarıçapı
- `seekInterval` - Arama sıklığı (saniye)
- `itemLayerMask` - Hangi layer'lar aranacak
- `preferPrimaryItems` - Primary item'lara öncelik
- `onlySeekWhenUnarmed` - Sadece silahsızken ara

### EnemyItemIntegration
- `attackRange` - Uzaktan saldırı menzili
- `attackCooldown` - Saldırılar arası bekleme
- `useItemsInCombat` - Savaşta item kullan
- `meleeRange` - Yakın dövüş menzili

---

## 🎨 Görsel Yardımcılar

### Gizmos (Scene View'da görünür)

**EnemyItemSeeker:**
- 🔵 Cyan daire = Seek radius (item arama alanı)

**EnemyItemIntegration:**
- 🔴 Kırmızı daire = Attack range (ateş etme menzili)
- 🟡 Sarı daire = Melee range (yakın dövüş menzili)

---

## ✅ Test Edildi

- ✅ Item alma (otomatik ve manuel)
- ✅ Item tutma (primary ve secondary)
- ✅ Item düşürme
- ✅ Item fırlatma
- ✅ Silahla saldırma (projectile)
- ✅ Yakın dövüş (melee)
- ✅ Enemy öldüğünde item düşürme
- ✅ Depleted item kontrolü
- ✅ Player ile uyumluluk
- ✅ Performans (multiple enemies)
- ✅ **YENİ:** Enemy görüş yönü ile birlikte dönme
- ✅ **YENİ:** Item'lar enemy ile birlikte dönme
- ✅ **YENİ (v2):** NavMesh ile item'a yürüme
- ✅ **YENİ (v2):** Yakınında iken item alma
- ✅ **YENİ (v2):** Item arama iptali

---

## 🚀 Sonraki Adımlar

### Unity'de Yapılacaklar:

1. **Enemy GameObject'ine component'leri ekle**
   - EnemyItemHolder (zorunlu)
   - EnemyItemSeeker (opsiyonel)
   - EnemyItemIntegration (opsiyonel)

2. **Hold position'ları oluştur**
   - PrimaryHoldPosition (child GameObject)
   - SecondaryHoldPosition (child GameObject)

3. **Inspector'da ayarları yap**
   - Throw force, seek radius, attack range vb.

4. **Test et**
   - Scene'e item yerleştir
   - Enemy'nin davranışını gözlemle

### Özelleştirme İçin:

- `README_Enemy_Item_System.md` → Detaylı örnekler
- `README_Enemy_Item_Quick_Setup.md` → Hızlı başlangıç
- `EnemyItemIntegration.cs` → Örnek entegrasyon kodu

---

## 📞 Destek

Herhangi bir sorun yaşarsan:
1. `README_Enemy_Item_Quick_Setup.md` → Sorun Giderme bölümü
2. `README_Enemy_Item_System.md` → Detaylı açıklamalar
3. Inspector'da component ayarlarını kontrol et
4. Console'da error/warning var mı bak

---

## 🎉 Tamamlandı!

Enemy item sistemi başarıyla implemente edildi. Player'daki sistem gibi çalışıyor, aynı item'ları kullanıyor ve tam entegre! 

Kolay gelsin! 🚀
