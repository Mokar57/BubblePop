using UnityEngine;

/// <summary>
/// Detects the player via proximity or direct contact.
/// Raises an event when the player is detected.
/// </summary>
public class EnemyProximityDetector : MonoBehaviour
{
    [Header("Proximity Detection")]
    [Tooltip("Enable proximity detection (detect player when close even without vision)")]
    public bool enableProximityDetection = true;

    [Tooltip("Time player must stay in proximity before triggering chase")]
    public float proximityTriggerTime = 1f;

    [Header("Contact Detection")]
    [Tooltip("Enable contact detection (trigger chase on collision with player)")]
    public bool enableContactDetection = true;

    // Public event
    public event System.Action<Transform> OnPlayerDetected;

    // References
    private Transform target;
    private EnemyDataSO enemyData;

    // State
    private float proximityTimer = 0f;
    private bool playerInProximity = false;
    private bool hasDetected = false;

    public void Initialize(EnemyDataSO data, Transform targetTransform)
    {
        this.enemyData = data;
        this.target = targetTransform;
    }
    
    public void SetTarget(Transform newTarget)
    {
        this.target = newTarget;
        ResetDetection();
    }

    private void Update()
    {
        if (enableProximityDetection && !hasDetected)
        {
            CheckProximityDetection();
        }
    }

    private void CheckProximityDetection()
    {
        if (target == null || enemyData == null)
        {
            proximityTimer = 0f;
            playerInProximity = false;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= enemyData.proximityRadius)
        {
            if (!playerInProximity)
            {
                playerInProximity = true;
                proximityTimer = 0f;
            }

            proximityTimer += Time.deltaTime;

            if (proximityTimer >= proximityTriggerTime)
            {
                hasDetected = true;
                OnPlayerDetected?.Invoke(target);
            }
        }
        else
        {
            playerInProximity = false;
            proximityTimer = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enableContactDetection || hasDetected) return;
        if (IsPlayerCollider(other))
        {
            hasDetected = true;
            OnPlayerDetected?.Invoke(other.transform);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableContactDetection || hasDetected) return;
        if (IsPlayerCollider(collision.collider))
        {
            hasDetected = true;
            OnPlayerDetected?.Invoke(collision.transform);
        }
    }

    private bool IsPlayerCollider(Collider2D col)
    {
        if (target != null && col.transform == target)
        {
            return true;
        }
        // Comparing tags is generally faster than GetComponent
        if (col.CompareTag("Player"))
        {
            return true;
        }
        // Fallback if tag is not set
        return col.GetComponent<PlayerControls>() != null;
    }

    public void ResetDetection()
    {
        hasDetected = false;
        proximityTimer = 0f;
        playerInProximity = false;
    }
}
