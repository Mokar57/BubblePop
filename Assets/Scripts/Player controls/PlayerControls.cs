using UnityEngine;

public class PlayerControls : MonoBehaviour, ISpeedBoostable
{
    
    public float moveSpeed;
    public Rigidbody2D rb;

    // Speed boost variables
    private float originalMoveSpeed;
    private float speedBoostMultiplier = 1f;
    private bool isSpeedBoosted = false;

    private Vector2 moveDirection;
    
    private void Start()
    {
        // Store original speed for speed boost functionality
        originalMoveSpeed = moveSpeed;
    }
    
    private void Update()
    {
        ProcessInput();
        
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector2 direction = new Vector2(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y);
        
        transform.up = direction;
    }
    

    private void FixedUpdate()
    {
        Move();
    }

    void ProcessInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        
        moveDirection = new Vector2(moveX, moveY);
    }

    void Move()
    {
        float currentMoveSpeed = originalMoveSpeed * speedBoostMultiplier;
        rb.linearVelocity = new Vector2(moveDirection.x * currentMoveSpeed, moveDirection.y * currentMoveSpeed);
    }
    
    #region ISpeedBoostable Implementation
    
    public void ApplySpeedBoost(float multiplier)
    {
        if (!isSpeedBoosted)
        {
            speedBoostMultiplier = multiplier;
            isSpeedBoosted = true;
            
            Debug.Log($"Player speed boosted! Original: {originalMoveSpeed}, Multiplier: {multiplier}");
        }
    }
    
    public void RemoveSpeedBoost()
    {
        if (isSpeedBoosted)
        {
            speedBoostMultiplier = 1f;
            isSpeedBoosted = false;
            
            Debug.Log($"Player speed boost removed. Speed restored to: {originalMoveSpeed}");
        }
    }
    
    public bool IsSpeedBoosted => isSpeedBoosted;
    
    #endregion
}
