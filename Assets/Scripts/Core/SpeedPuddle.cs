using UnityEngine;
using System.Collections.Generic;

public class SpeedPuddle : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SpeedPuddleDataSO puddleData;

    [Header("Overrides (Optional)")]
    [SerializeField] private bool overrideData = false;
    [SerializeField] private float zoneRadius = 1f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float zoneDuration = 10f;
    [SerializeField] private LayerMask affectedLayers = ~0;

    private CircleCollider2D zoneCollider;
    private SpriteRenderer zoneRenderer;
    private List<ISpeedBoostable> entitiesInZone = new List<ISpeedBoostable>();
    private float remainingTime;

    // Properties to get actual values
    public float Radius => overrideData ? zoneRadius : (puddleData != null ? puddleData.zoneRadius : zoneRadius);
    public float Multiplier => overrideData ? speedMultiplier : (puddleData != null ? puddleData.speedMultiplier : speedMultiplier);
    public float Duration => overrideData ? zoneDuration : (puddleData != null ? puddleData.zoneDuration : zoneDuration);
    public LayerMask Layers => overrideData ? affectedLayers : (puddleData != null ? puddleData.affectedLayers : affectedLayers);
    public float FadeOutDuration => puddleData != null ? puddleData.fadeOutDuration : 0f;

    private void Start()
    {
        remainingTime = Duration;
        SetupZone();

        if (Duration > 0)
        {
            Invoke(nameof(DestroyZone), Duration);
        }
    }

    private void SetupZone()
    {
        // Collider setup
        zoneCollider = GetComponent<CircleCollider2D>();
        if (zoneCollider == null)
        {
            zoneCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        // zoneCollider.radius = Radius;
        gameObject.transform.localScale = Vector3.one * Radius;
        zoneCollider.isTrigger = true;


        // Effect setup
        if (puddleData != null && puddleData.zoneEffect != null)
        {
            Instantiate(puddleData.zoneEffect, transform.position, transform.rotation, transform);
        }
    }

    private void Update()
    {
        if (Duration > 0)
        {
            remainingTime -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & Layers) == 0)
        {
            return;
        }

        ISpeedBoostable speedBoostable = other.GetComponent<ISpeedBoostable>();
        if (speedBoostable != null)
        {
            if (!entitiesInZone.Contains(speedBoostable))
            {
                entitiesInZone.Add(speedBoostable);
                speedBoostable.ApplySpeedBoost(Multiplier);

                // Play boost sound
                if (puddleData != null && puddleData.boostSound != null && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySound(puddleData.boostSound, puddleData.boostSoundVolume);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ISpeedBoostable speedBoostable = other.GetComponent<ISpeedBoostable>();
        if (speedBoostable != null && entitiesInZone.Contains(speedBoostable))
        {
            entitiesInZone.Remove(speedBoostable);
            // Apply fade out when exiting
            speedBoostable.RemoveSpeedBoost(FadeOutDuration);
        }
    }

    private void DestroyZone()
    {
        foreach (var entity in entitiesInZone)
        {
            if (entity != null)
            {
                // Apply fade out when zone is destroyed
                entity.RemoveSpeedBoost(FadeOutDuration);
            }
        }

        entitiesInZone.Clear();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}
