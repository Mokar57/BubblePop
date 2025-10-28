using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("AI Settings")]
    [SerializeField] private float followDistance = 10f;
    [SerializeField] private float stopDistance = 1f;
    [SerializeField] private float updateRate = 0.1f; // Update destination every 0.1 seconds for better performance
    
    [Header("Movement Settings")]
    [SerializeField] private float speed = 3.5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 120f;
    
    private NavMeshAgent agent;
    private float lastUpdateTime;
    private Vector3 lastTargetPosition;
    
    private void Start()
    {
        InitializeAgent();
        FindTarget();
    }
    
    private void InitializeAgent()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
            return;
        }
        
        // Configure agent for 2D movement
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        // Set movement properties
        agent.speed = speed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
        agent.stoppingDistance = stopDistance;
    }
    
    private void FindTarget()
    {
        if (autoFindPlayer && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Enemy found player: " + player.name);
            }
            else
            {
                // Try to find by component if tag doesn't exist
                PlayerControls playerScript = FindFirstObjectByType<PlayerControls>();
                if (playerScript != null)
                {
                    target = playerScript.transform;
                    Debug.Log("Enemy found player by script: " + playerScript.name);
                }
                else
                {
                    Debug.LogWarning("Player not found! Make sure the player has 'Player' tag or PlayerControls component.");
                }
            }
        }
    }
    
    private void Update()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }
        
        if (agent == null) return;
        
        // Update destination periodically for better performance
        if (Time.time - lastUpdateTime >= updateRate)
        {
            UpdateDestination();
            lastUpdateTime = Time.time;
        }
    }
    
    private void UpdateDestination()
    {
        if (target == null || agent == null) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Only update if target moved significantly or we're far from target
        if (Vector3.Distance(target.position, lastTargetPosition) > 0.5f || distanceToTarget > followDistance)
        {
            // Check if target is within follow distance
            if (distanceToTarget <= followDistance)
            {
                agent.SetDestination(target.position);
                lastTargetPosition = target.position;
            }
            else
            {
                // Stop moving if target is too far
                agent.ResetPath();
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw follow distance range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        
        // Draw stop distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        
        // Draw line to target
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
    
    // Public methods for external control
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (agent != null)
            agent.speed = speed;
    }
    
    public void StopFollowing()
    {
        if (agent != null)
            agent.ResetPath();
    }
    
    public void ResumeFollowing()
    {
        if (target != null && agent != null)
            agent.SetDestination(target.position);
    }
    
    public bool IsMoving()
    {
        return agent != null && agent.velocity.magnitude > 0.1f;
    }
    
    public float DistanceToTarget()
    {
        return target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
    }
}