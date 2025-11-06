# Kendi Kendine Hasar Önleme Sistemi

## Güncelleme Tarihi
6 Kasım 2025

## Özet
Player ve Enemy'ler artık kendi ellerindeki silahlardan hasar almıyorlar. Bu özellik tüm silah tipleri için geçerli:
- **Projectile (Mermiler)**: Pistol'den çıkan mermiler sahibine zarar vermez
- **Melee (Yakın Dövüş)**: Baseball bat saldırısı sahibini vurmaz
- **Thrown Items (Fırlatılan İtemler)**: Fırlatılan silahlar fırlatan kişiye çarpmaz

## Değişiklikler

### 1. Projectile.cs
**Eklenen Özellikler:**
- `owner` değişkeni - Mermiyi kim ateşledi
- `SetOwner(GameObject)` metodu - Sahipliği ayarla
- Çarpışma kontrolü - Sahibine çarparsa ignore eder

```csharp
private GameObject owner; // Kim ateş etti

public void SetOwner(GameObject newOwner)
{
    owner = newOwner;
}

// OnTriggerEnter2D içinde:
if (owner != null && other.gameObject == owner)
{
    return; // Sahibine çarptıysa ignore et
}
```

### 2. PistolItem.cs
**Güncellemeler:**
- `Fire()` metodu artık sahipliği tespit eder (parent GameObject)
- `CreateProjectile()` metodu owner parametresi alır
- `CreateBasicProjectile()` metodu owner parametresi alır
- Oluşturulan her projectile'a sahibi set eder

```csharp
// Fire metodunda:
GameObject owner = transform.parent != null ? transform.parent.gameObject : null;
CreateProjectile(playerPosition, direction, owner);

// CreateProjectile içinde:
projectile.SetOwner(owner);
```

### 3. BaseballBatItem.cs
**Güncellemeler:**
- `Attack()` metodu artık sahipliği tespit eder (parent GameObject)
- `PerformAreaAttack()` metodu owner parametresi alır
- Alan hasarı verirken owner'ı ignore eder

```csharp
// Attack metodunda:
GameObject owner = transform.parent != null ? transform.parent.gameObject : null;
PerformAreaAttack(playerPosition, direction, owner);

// PerformAreaAttack içinde:
if (owner != null && hitCollider.gameObject == owner)
    continue; // Sahibine hasar verme
```

### 4. ThrownItem.cs
**Eklenen Özellikler:**
- `thrower` değişkeni - Kim fırlattı
- `SetThrower(GameObject)` metodu - Fırlatanı ayarla
- Çarpışma kontrolü - Fırlatana çarparsa ignore eder

```csharp
private GameObject thrower; // Kim fırlattı

public void SetThrower(GameObject newThrower)
{
    thrower = newThrower;
}

// OnCollisionEnter2D içinde:
if (thrower != null && collision.gameObject == thrower)
{
    return; // Ignore collision with thrower
}
```

### 5. PlayerControls.cs
**Güncellemeler:**
- `ThrowItemAtDirection()` metodunda fırlatılan item'a thrower set edilir

```csharp
thrownComponent.SetThrower(gameObject); // Fırlatan kişiyi set et
```

### 6. EnemyItemHolder.cs
**Güncellemeler:**
- `ThrowItemAtDirection()` metodunda fırlatılan item'a thrower set edilir

```csharp
thrownComponent.SetThrower(gameObject); // Fırlatan enemy'yi set et
```

## Nasıl Çalışır?

### Projectile Sistemi
1. Player veya Enemy ateş ettiğinde, PistolItem parent GameObject'i tespit eder
2. Projectile oluşturulurken owner olarak set edilir
3. Projectile bir şeye çarptığında önce owner kontrolü yapar
4. Eğer çarpılan obje owner ise çarpışmayı ignore eder

### Melee (Yakın Dövüş) Sistemi
1. Player veya Enemy sopa ile saldırdığında, BaseballBatItem parent GameObject'i tespit eder
2. Alan hasarı verilirken owner parametresi geçilir
3. Her collider kontrol edilirken owner ile karşılaştırılır
4. Eğer collider owner ise hasar verilmez

### Thrown Item Sistemi
1. Player veya Enemy item fırlattığında, ThrownItem'a thrower set edilir
2. ThrownItem bir şeye çarptığında önce thrower kontrolü yapar
3. Eğer çarpılan obje thrower ise çarpışmayı ignore eder
4. Diğer hedeflere normal şekilde hasar verir

## Önemli Notlar

### Parent-Child İlişkisi
Sistem, silahların parent GameObject'ini kullanarak sahipliği tespit eder:
- Player item aldığında → Item'ın parent'ı Player olur
- Enemy item aldığında → Item'ın parent'ı Enemy olur
- Item fırlatıldığında → Parent null olur ama thrower/owner bilgisi saklanır

### Güvenlik
- **Null Kontrolü**: Owner/thrower null olabilir, bu yüzden her zaman null kontrolü yapılır
- **Erken Return**: Sahibe çarpma durumunda erken return ile performans optimizasyonu
- **GameObject Karşılaştırma**: `==` operatörü ile doğrudan GameObject karşılaştırması

### Performans
- Minimum performans etkisi
- Sadece çarpışma anında kontrol yapılır
- Ek hesaplama yok, sadece referans karşılaştırması

## Test Senaryoları

### ✅ Test 1: Player Kendi Ateşine Hasar Almaz
1. Player bir pistol alsın
2. Sol tık ile ateş etsin
3. Mermi duvardan geri seksin ve player'a çarpsın
4. **Sonuç**: Player hasar almaz (mermi yok olmaz ve geçer veya hasar vermez)

### ✅ Test 2: Enemy Kendi Ateşine Hasar Almaz
1. Enemy bir pistol tutsun
2. Enemy ateş etsin
3. Mermi bir şekilde enemy'ye çarpsın (örn: duvardan sekerek)
4. **Sonuç**: Enemy hasar almaz

### ✅ Test 3: Player Kendi Sopasından Hasar Almaz
1. Player bir sopa alsın
2. Sol tık ile saldırsın
3. **Sonuç**: Player kendine hasar vermez

### ✅ Test 4: Fırlatılan Item Fırlatana Çarpmaz
1. Player bir silah alsın
2. Sağ tık ile fırlatsın
3. Silah geri gelip player'a çarpsın (fizik motoru)
4. **Sonuç**: Player hasar almaz, item geçer

### ✅ Test 5: Fırlatılan Item Diğer Hedeflere Hasar Verir
1. Player bir silah alsın
2. Enemy'ye doğru fırlatsın
3. **Sonuç**: Enemy hasar alır

### ✅ Test 6: Düşman Fırlatılan Item Kendine Çarpmaz
1. Enemy bir silah tutsun
2. Enemy item fırlatsın (ForceThrowItem())
3. Item geri gelip enemy'ye çarpsın
4. **Sonuç**: Enemy hasar almaz

## Gelecek Geliştirmeler

### Potansiyel İyileştirmeler
- **Takım Sistemi**: Friendly fire kontrolü (aynı takımdan olanlar birbirine hasar vermesin)
- **Zamanla Değişen Sahiplik**: Belirli bir süre sonra projectile'ın sahibi "null" olabilir (herkes hasar alır)
- **Ricochet Counter**: Belirli sayıda sekme sonrası sahibine de hasar verebilir
- **Damage Scaling**: Sahibine çarparsa daha az hasar (tam immunity yerine)

### Mevcut Sınırlamalar
- Projectile ömrü boyunca aynı owner'a sahip (değişmez)
- Aynı karakterin birden fazla instance'ı varsa sorun olabilir (çok nadir)
- Thrown item yere düştükten sonra tekrar alınıp fırlatılırsa yeni thrower'ı olur (bu istenen davranış)

## Özet

Bu güncelleme ile oyun deneyimi çok daha adil ve mantıklı hale geldi:
- ✅ Player kendi silahlarından hasar almaz
- ✅ Enemy kendi silahlarından hasar almaz
- ✅ Fırlatılan itemler fırlatana hasar vermez
- ✅ Tüm silah tipleri için geçerli
- ✅ Minimum performans etkisi
- ✅ Temiz ve güvenli implementasyon

Artık oyuncular ve düşmanlar sadece karşı tarafın saldırılarından hasar alıyor! 🎮
