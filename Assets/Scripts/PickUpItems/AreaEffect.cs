using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// using BubblePop.Enemy.States;

/// <summary>
/// Yuvarlak alan içindeki enemy'leri etkiler
/// Enemy'leri merkeze çeker, 3 saniye orada rastgele yönlere baktırır
/// REFACTORED: Now uses new enemy component system
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
    private List<EnemyStunController> affectedEnemies = new List<EnemyStunController>();
    private HashSet<EnemyStunController> processedEnemies = new HashSet<EnemyStunController>(); // Zaten işlenmiş enemy'ler

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
                // Get component references
                EnemyAI enemyAI = col.GetComponent<EnemyAI>();
                EnemyHealth health = col.GetComponent<EnemyHealth>();
                EnemyStunController stunController = col.GetComponent<EnemyStunController>();

                // Enemy'yi daha önce işlemediysen ve henüz ölmemişse
                if (stunController != null && !processedEnemies.Contains(stunController) &&
                    health != null && !health.IsDead)
                {
                    // Enemy chase modundaysa alana tepki verme
                    if (enemyAI != null)
                    {
                        AIStateBase currentState = enemyAI.CurrentState;
                        if (currentState == enemyAI.ChaseStateInstance)
                        {
                            continue; // Bu enemy'yi atla
                        }
                    }

                    processedEnemies.Add(stunController);
                    affectedEnemies.Add(stunController);

                    // Enemy'yi stun et
                    StartCoroutine(StunEnemy(stunController, health, enemyAI));
                }
            }
        }
    }

    /// <summary>
    /// Enemy'yi stun eder - merkeze çeker ve rastgele yönlere baktırır
    /// </summary>
    private IEnumerator StunEnemy(EnemyStunController stunController, EnemyHealth health, EnemyAI enemyAI)
    {
        if (stunController == null || health == null) yield break;

        // Enemy'yi stun state'ine al
        stunController.EnterStun(centerPosition, stunDuration);

        // Enemy'nin merkeze ulaşmasını bekle
        float maxWaitTime = 10f; // Maksimum bekleme süresi (enemy çok uzaktaysa)
        float waitTimer = 0f;

        while (waitTimer < maxWaitTime && stunController != null && health != null && !health.IsDead)
        {
            float distanceToCenter = Vector3.Distance(stunController.transform.position, centerPosition);

            // Merkeze ulaştı mı kontrol et (0.5 birim içinde)
            if (distanceToCenter <= 0.5f || stunController.HasReachedStunCenter())
            {
                break; // Merkeze ulaştı, döngüden çık
            }

            waitTimer += Time.deltaTime;
            yield return null; // Bir frame bekle
        }

        // Merkeze ulaştıktan sonra rastgele yön değiştirme başlat
        float elapsedTime = 0f;

        while (elapsedTime < stunDuration && stunController != null && health != null && !health.IsDead)
        {
            // Rastgele bir yön oluştur
            float randomAngle = Random.Range(0f, 360f);
            Vector3 randomDirection = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                Mathf.Sin(randomAngle * Mathf.Deg2Rad),
                0f
            );

            // Enemy'yi bu yöne baktır (through AI if available)
            if (enemyAI != null)
            {
                enemyAI.SetVisionDirection(randomDirection);
            }

            // Bir sonraki rotasyon için bekle
            yield return new WaitForSeconds(rotationInterval);
            elapsedTime += rotationInterval;
        }

        // Stun süresi bitti, enemy'yi serbest bırak
        if (stunController != null && health != null && !health.IsDead)
        {
            stunController.ExitStun();
        }
    }

    private void OnDrawGizmos()
    {
        // Scene view'da alanı görselleştir
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
