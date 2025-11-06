# Enemy Rotation Update - Vision Direction Integration

## 🔄 Yapılan Değişiklik

Enemy'ler artık **görüş yönleri (vision direction)** ile birlikte döner ve tuttuğu item'lar da onunla birlikte döner.

---

## 🎯 Problem

- Enemy'nin `currentVisionDirection` değişkeni vardı ama enemy'nin kendisi (`transform`) dönmüyordu
- Item'lar enemy'nin child'ı olduğu için, enemy dönmediği sürece item'lar da dönmüyordu
- Görüş konisi (vision cone) doğru yöne bakıyordu ama item'lar hareket yönüne göre dönmüyordu

---

## ✅ Çözüm

### `EnemyItemHolder.cs` - Otomatik Dönme Sistemi

```csharp
private void LateUpdate()
{
    // Enemy'nin görüş yönüne göre enemy ve item'ları döndür
    UpdateRotationBasedOnVision();
}

private void UpdateRotationBasedOnVision()
{
    if (enemyAI == null) return;
    
    // EnemyAI'dan görüş yönünü al
    Vector3 visionDirection = enemyAI.GetVisionDirection();
    
    if (visionDirection.magnitude > 0.01f)
    {
        // Enemy'nin kendisini görüş yönüne döndür
        float angle = Mathf.Atan2(visionDirection.y, visionDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}
```

**Ne Yapıyor:**
1. Her frame'de `LateUpdate()` içinde çalışır
2. `EnemyAI`'dan `currentVisionDirection` değerini alır
3. Bu yönü açıya çevirir (Atan2 ile)
4. Enemy'nin transform'unu bu açıya döndürür
5. Item'lar child olduğu için otomatik olarak parent ile birlikte döner

**Neden LateUpdate?**
- `Update()` tüm componentlerin güncellenmesinden SONRA çalışır
- Böylece `EnemyAI` önce `currentVisionDirection`'ı günceller
- Sonra `EnemyItemHolder` bu yönü kullanarak rotasyonu günceller

---

## 🔧 Ek İyileştirmeler

### `EnemyItemIntegration.cs` - Görüş Yönü ile Saldırı

```csharp
// Enemy'nin görüş yönünü kullanarak saldır
Vector3 visionDirection = enemyAI.GetVisionDirection();
Vector3 attackDirection = (target.position - transform.position).normalized;

// Görüş yönü ile hedef arasındaki açıyı kontrol et
float angleToTarget = Vector3.Angle(visionDirection, attackDirection);
```

**Opsiyonel Özellik:** 
İstersen sadece belirli açı içindeyken ateş etmesini sağlayabilirsin:
```csharp
if (angleToTarget <= 45f) // Sadece 45 derece içindeyken ateş et
    itemHolder.PerformAttack(target.position);
```

### `ForceThrowItem()` - Görüş Yönüne Fırlatma

```csharp
else if (enemyAI != null)
{
    // Hedef yoksa görüş yönüne fırlat
    Vector2 throwDirection = enemyAI.GetVisionDirection();
    itemHolder.ThrowCurrentItem(throwDirection);
}
```

Hedef yoksa artık görüş yönüne doğru fırlatır.

---

## 📊 Teknik Detaylar

### Rotation Hesaplaması

```csharp
float angle = Mathf.Atan2(visionDirection.y, visionDirection.x) * Mathf.Rad2Deg;
transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
```

**Neden -90f?**
- Unity'de `transform.up` (yukarı) vektörü varsayılan olarak (0, 1, 0)
- Ama `Atan2` 0 derece'yi sağa (1, 0, 0) olarak kabul eder
- Bu yüzden 90 derece offset eklememiz gerekir
- Negatif çünkü saat yönünün tersine dönüş yapıyoruz

### Vision Direction Kaynağı

`EnemyAI.cs` içinde:
```csharp
public Vector3 GetVisionDirection()
{
    return currentVisionDirection;
}
```

Bu metod zaten mevcut, sadece kullanıyoruz.

---

## 🎮 Sonuç

Artık:
- ✅ Enemy hareket ederken görüş yönüne döner
- ✅ Tuttuğu item'lar da onunla birlikte döner
- ✅ Ateş ederken doğru yöne bakar
- ✅ Item fırlatırken görüş yönüne fırlatır
- ✅ Tüm sistem otomatik çalışır, ekstra kurulum gerektirmez

---

## 🔍 Görsel Doğrulama

Unity'de test ederken:
1. Scene view'da enemy'yi seç
2. Gizmos açık olsun (vision cone görünsün)
3. Play mode'a gir
4. Enemy hareket ederken:
   - Vision cone'un yönü değişiyor mu? ✓
   - Enemy kendisi dönüyor mu? ✓
   - Tuttuğu item dönüyor mu? ✓
   - Item doğru pozisyonda duruyor mu? ✓

---

## 🐛 Olası Sorunlar ve Çözümler

**Problem:** Enemy çok hızlı dönüyor
**Çözüm:** `UpdateRotationBasedOnVision()` içine Slerp ekle:
```csharp
Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90f);
transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
```

**Problem:** Item yanlış pozisyonda
**Çözüm:** Hold Position'ların local position'larını kontrol et

**Problem:** Enemy hareket etmiyor ama dönüyor
**Çözüm:** Normal, `currentVisionDirection` waypoint wait sırasında da değişebilir

---

## 📝 Notlar

- Sistem `LateUpdate()` kullandığı için performans etkisi minimal
- `EnemyAI` component'i olmadan çalışmaz (ama zaten gerekli)
- Hold position'lar local olduğu için parent dönünce onlar da döner
- Sprite'ın forward yönü `transform.up` olmalı (Unity 2D standart)

---

Başarılar! 🚀
