using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Yuvarlak alan içindeki enemy'leri etkiler
/// Enemy'leri merkeze çeker, 3 saniye orada rastgele yönlere baktırır
/// </summary>
public class AreaEffect : MonoBehaviour
{
    [Header("Area Settings")]
    public float radius = 3f;
    public float duration = 2f;
    public float stunDuration = 8f; // Enemy'lerin merkezde kaç saniye kalacağı
    
    [Header("Effect Settings")]
    public float pullForce = 5f; // Enemy'leri merkeze çekme kuvveti
    public float rotationInterval = 1f; // Kaç saniyede bir rastgele yön değişecek (saniyede 1 kere)
    
    private Vector3 centerPosition;
    private List<EnemyAI> affectedEnemies = new List<EnemyAI>();
    private HashSet<EnemyAI> processedEnemies = new HashSet<EnemyAI>(); // Zaten işlenmiş enemy'ler
    
    private void Start()
    {
        centerPosition = transform.position;
        
        // Alan içindeki tüm enemy'leri bul ve etkile
        DetectAndAffectEnemies();
        
        // Belirtilen süre sonra alanı yok et
        Destroy(gameObject, duration);
    }
    
    private void Update()
    {
        // Sürekli yeni enemy'leri tespit et (alana sonradan girebilirler)
        DetectAndAffectEnemies();
    }
    
    /// <summary>
    /// Alan içindeki enemy'leri tespit eder ve etkiler
    /// </summary>
    private void DetectAndAffectEnemies()
    {
        // Alan içindeki tüm collider'ları bul
        Collider2D[] colliders = Physics2D.OverlapCircleAll(centerPosition, radius);
        
        foreach (Collider2D col in colliders)
        {
            // Enemy tag'i veya isimde "Enemy" geçen objeleri kontrol et
            if (col.CompareTag("Enemy") || col.gameObject.name.Contains("Enemy"))
            {
                EnemyAI enemy = col.GetComponent<EnemyAI>();
                
                // Enemy'yi daha önce işlemediysen ve henüz ölmemişse
                if (enemy != null && !processedEnemies.Contains(enemy) && !enemy.IsDead)
                {
                    // Enemy chase modundaysa alana tepki verme
                    EnemyAI.AIState currentState = enemy.GetCurrentState();
                    if (currentState == EnemyAI.AIState.Chasing)
                    {
                        continue; // Bu enemy'yi atla
                    }
                    
                    processedEnemies.Add(enemy);
                    affectedEnemies.Add(enemy);
                    
                    // Enemy'yi stun et
                    StartCoroutine(StunEnemy(enemy));
                }
            }
        }
    }
    
    /// <summary>
    /// Enemy'yi stun eder - merkeze çeker ve rastgele yönlere baktırır
    /// </summary>
    private IEnumerator StunEnemy(EnemyAI enemy)
    {
        if (enemy == null) yield break;
        
        // Enemy'yi stun state'ine al
        enemy.EnterStunnedState(centerPosition, stunDuration);
        
        // Enemy'nin merkeze ulaşmasını bekle
        float maxWaitTime = 10f; // Maksimum bekleme süresi (enemy çok uzaktaysa)
        float waitTimer = 0f;
        
        while (waitTimer < maxWaitTime && enemy != null && !enemy.IsDead)
        {
            float distanceToCenter = Vector3.Distance(enemy.transform.position, centerPosition);
            
            // Merkeze ulaştı mı kontrol et (0.5 birim içinde)
            if (distanceToCenter <= 0.5f)
            {
                break; // Merkeze ulaştı, döngüden çık
            }
            
            waitTimer += Time.deltaTime;
            yield return null; // Bir frame bekle
        }
        
        // Merkeze ulaştıktan sonra rastgele yön değiştirme başlat
        float elapsedTime = 0f;
        
        while (elapsedTime < stunDuration && enemy != null && !enemy.IsDead)
        {
            // Rastgele bir yön oluştur
            float randomAngle = Random.Range(0f, 360f);
            Vector3 randomDirection = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                Mathf.Sin(randomAngle * Mathf.Deg2Rad),
                0f
            );
            
            // Enemy'yi bu yöne baktır
            enemy.SetVisionDirection(randomDirection);
            
            // Bir sonraki rotasyon için bekle
            yield return new WaitForSeconds(rotationInterval);
            elapsedTime += rotationInterval;
        }
        
        // Stun süresi bitti, enemy'yi serbest bırak
        if (enemy != null && !enemy.IsDead)
        {
            enemy.ExitStunnedState();
        }
    }
    
    private void OnDrawGizmos()
    {
        // Scene view'da alanı görselleştir
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
