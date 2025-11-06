# SpeedBoostZone Enemy Algılama Kurulumu

## Problem
SpeedBoostZone, Player'ı algılıyor ama Enemy'leri algılamıyor.

## Çözüm

### 1. Enemy GameObject Ayarları
Enemy GameObject'inizin üzerinde şunların olduğundan emin olun:
- **Collider2D** (CircleCollider2D veya BoxCollider2D)
- **Rigidbody2D** (önemli!)
  - Body Type: **Kinematic** (otomatik ayarlanır)
  - Simulated: **Checked (✓)**
  - Constraints: **Freeze Rotation Z** (otomatik ayarlanır)
  - Gravity Scale: **0** (otomatik ayarlanır)
  - Interpolate: **Interpolate** (otomatik ayarlanır)
  - Collision Detection: **Continuous** (otomatik ayarlanır)
  
**ÖNEMLİ:** Unity'de 2D trigger collision'ların çalışması için en az bir GameObject'te Rigidbody2D olması gerekir!

**NOT:** Rigidbody2D ayarları artık kod tarafından otomatik yapılıyor. Sadece Rigidbody2D component'ini ekleyin, ayarlar `EnemyAI.cs` tarafından Start()'ta otomatik yapılandırılacak.

### 2. Layer Collision Matrix Ayarları
Unity Editor'de:
1. **Edit > Project Settings > Physics 2D** menüsüne gidin
2. En alttaki **Layer Collision Matrix**'i bulun
3. **Default** layer ile **Default** layer'ın çarpışmasının **aktif (✓)** olduğundan emin olun
4. Eğer Enemy farklı bir layer'daysa (örn: "Enemy" layer'ı), o layer ile SpeedBoostZone'un layer'ının çarpışmasını aktif edin

### 3. SpeedBoostZone Prefab Ayarları
SpeedBoostZone prefab'ınızda:
- **CircleCollider2D** component'i var mı kontrol edin
- **Is Trigger:** **Checked (✓)** olmalı
- **Affected Layers:** Inspector'da yeni eklenen bu alandan hangi layer'ları etkileyeceğini seçin
  - Default: Tüm layer'lar seçili
  - Enemy'yi etkilemek için Enemy'nin bulunduğu layer'ı seçili tutun

### 4. Test ve Debug
Oyunu Play mode'da çalıştırın ve Console'u açın. Şu logları göreceksiniz:
- `"Object entered zone: [ObjectName] on layer [LayerName]"` - Bir obje zone'a girdi
- `"Found ISpeedBoostable component on [ObjectName]"` - ISpeedBoostable bulundu
- `"[ObjectName] speed boosted! Speed: X -> Y, Patrol Speed: A -> B"` - Speed boost uygulandı

Eğer Enemy zone'a girdiğinde hiçbir log görünmüyorsa:
- Enemy'de **Rigidbody2D** var mı kontrol edin
- **Layer Collision Matrix** ayarlarını tekrar kontrol edin

## Yapılan Kod Değişiklikleri

### EnemyAI.cs
✅ Speed boost artık hem `speed` hem de `patrolSpeed` değerlerini etkiliyor
✅ Original patrol speed değeri saklanıyor ve geri yükleniyor
✅ **ApplySpeedBoost:** Enemy'nin mevcut state'ine göre (patrol/chase) doğru hızı uygular
✅ **RemoveSpeedBoost:** Enemy'nin mevcut state'ine göre (patrol/chase) doğru orijinal hızı geri yükler
✅ State-aware hız yönetimi sayesinde alan dışına çıkınca hız düzgün şekilde azalır
✅ **ConfigureRigidbody2D:** Rigidbody2D otomatik olarak doğru şekilde yapılandırılır (Kinematic mode, rotation freeze, vb.)
✅ NavMeshAgent ile Rigidbody2D çakışması önlenir
✅ Debug log'ları iyileştirildi ve state bilgisi eklendi

### SpeedBoostZone.cs
✅ `affectedLayers` parametresi eklendi (hangi layer'ları etkileyeceğini seçebilirsiniz)
✅ Layer kontrolü eklendi
✅ Debug log'ları daha detaylı hale getirildi

## Test Senaryosu
1. Enemy bir SpeedBoostZone'a girsin
2. Console'da şu logları görmelisiniz:
   - "Object entered zone: Enemy on layer Default"
   - "Found ISpeedBoostable component on Enemy"
   - "Enemy speed boosted! Speed: 3.5 -> 5.25, Patrol Speed: 2 -> 3"
3. Enemy'nin hareket hızının arttığını gözlemleyin
4. Enemy zone'dan çıkınca hızı normale dönmeli

## Sorun Devam Ederse
1. Enemy GameObject'e **Rigidbody2D** ekleyin (kod otomatik yapılandıracak)
2. Enemy'nin Collider2D'sinin **Is Trigger** ayarı **KAPALI** olmalı (normal collider)
3. SpeedBoostZone'un Collider2D'sinin **Is Trigger** ayarı **AÇIK** olmalı
4. Physics 2D Settings'deki Layer Collision Matrix'te ilgili layer'ların çarpışmasının aktif olduğundan emin olun
5. **Eğer Enemy hala itiliyorsa:** Inspector'da Rigidbody2D'nin Body Type'ının **Kinematic** olduğunu kontrol edin

## Rigidbody2D ile NavMeshAgent Uyumluluğu
EnemyAI scripti artık Rigidbody2D'yi otomatik olarak yapılandırır:
- **Body Type:** Kinematic (NavMeshAgent hareket kontrolünü sağlar)
- **Constraints:** Freeze Rotation (Enemy dönmeyi önler, bug'ları önler)
- **Gravity Scale:** 0 (2D'de gereksiz)
- **Interpolation:** Interpolate (daha yumuşak hareket)
- **Collision Detection:** Continuous (daha iyi çarpışma algılama)

Bu ayarlar sayesinde:
- ✅ SpeedBoostZone trigger detection çalışır
- ✅ Enemy NavMeshAgent ile normal hareket eder
- ✅ Enemy itilmez veya bug'a girmez
- ✅ Rotation problemi olmaz
