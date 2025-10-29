using UnityEngine;

[System.Serializable]
public class Waypoint : MonoBehaviour
{
    [Header("Waypoint Settings")]
    [SerializeField] private Vector3 waitDirection = Vector3.up;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private bool showDirection = true;
    [SerializeField] private float directionArrowLength = 2f;
    
    public Vector3 Position => transform.position;
    public Vector3 WaitDirection => waitDirection.normalized;
    public float WaitTime => waitTime;
    
    private void OnDrawGizmos()
    {
        // Draw waypoint position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw direction arrow if enabled
        if (showDirection)
        {
            Gizmos.color = Color.cyan;
            Vector3 arrowEnd = transform.position + waitDirection.normalized * directionArrowLength;
            Gizmos.DrawRay(transform.position, waitDirection.normalized * directionArrowLength);
            
            // Draw arrow head
            Vector3 arrowLeft = Quaternion.AngleAxis(-135, Vector3.forward) * waitDirection.normalized * 0.3f;
            Vector3 arrowRight = Quaternion.AngleAxis(135, Vector3.forward) * waitDirection.normalized * 0.3f;
            Gizmos.DrawRay(arrowEnd, arrowLeft);
            Gizmos.DrawRay(arrowEnd, arrowRight);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw selected waypoint with different color
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.7f);
        
        // Draw direction with stronger color when selected
        if (showDirection)
        {
            Gizmos.color = Color.white;
            Vector3 arrowEnd = transform.position + waitDirection.normalized * directionArrowLength;
            Gizmos.DrawRay(transform.position, waitDirection.normalized * directionArrowLength);
            
            // Draw arrow head
            Vector3 arrowLeft = Quaternion.AngleAxis(-135, Vector3.forward) * waitDirection.normalized * 0.3f;
            Vector3 arrowRight = Quaternion.AngleAxis(135, Vector3.forward) * waitDirection.normalized * 0.3f;
            Gizmos.DrawRay(arrowEnd, arrowLeft);
            Gizmos.DrawRay(arrowEnd, arrowRight);
        }
    }
    
    public void SetWaitDirection(Vector3 direction)
    {
        waitDirection = direction.normalized;
    }
    
    public void SetWaitTime(float time)
    {
        waitTime = Mathf.Max(0f, time);
    }
}