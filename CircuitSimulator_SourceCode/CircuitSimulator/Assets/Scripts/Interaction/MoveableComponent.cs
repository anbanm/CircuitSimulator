using UnityEngine;

public class MoveableComponent : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public LayerMask groundLayer = 1; // Default layer
    
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    private Camera _mainCamera;
    private SelectableComponent _selectableComponent;
    
    void Start()
    {
        _mainCamera = Camera.main;
        _selectableComponent = GetComponent<SelectableComponent>();
    }
    
    void OnMouseDown()
    {
        // Only allow moving if component is selected
        if (_selectableComponent != null && _selectableComponent.IsSelected())
        {
            StartDragging();
        }
    }
    
    void StartDragging()
    {
        _isDragging = true;
        
        // Calculate offset between mouse position and object center
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        _dragOffset = transform.position - mouseWorldPos;
        
        Debug.Log($"Started dragging: {gameObject.name}");
    }
    
    void Update()
    {
        if (_isDragging)
        {
            HandleDragging();
            
            // Stop dragging on mouse release
            if (Input.GetMouseButtonUp(0))
            {
                StopDragging();
            }
        }
        
        // ESC to cancel dragging
        if (Input.GetKeyDown(KeyCode.Escape) && _isDragging)
        {
            StopDragging();
        }
    }
    
    void HandleDragging()
    {
        Vector3 targetPosition = GetMouseWorldPosition() + _dragOffset;
        
        // Keep Y position fixed (stay on plane)
        targetPosition.y = transform.position.y;
        
        // Smooth movement
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
    
    void StopDragging()
    {
        _isDragging = false;
        
        // Snap to grid (optional)
        SnapToGrid();
        
        Debug.Log($"Stopped dragging: {gameObject.name} at {transform.position}");
    }
    
    void SnapToGrid()
    {
        // Snap to nearest grid position (AR-optimized spacing)
        float gridSize = 1f;
        Vector3 snappedPos = transform.position;
        
        snappedPos.x = Mathf.Round(snappedPos.x / gridSize) * gridSize;
        snappedPos.z = Mathf.Round(snappedPos.z / gridSize) * gridSize;
        
        // Check if this position is occupied by another component
        if (IsPositionOccupied(snappedPos))
        {
            // Find nearest free position
            snappedPos = FindNearestFreePosition(snappedPos, gridSize);
        }
        
        transform.position = snappedPos;
        
        // Trigger circuit re-solve when component moves (for real-time feedback)
        var manager = Circuit3DManager.Instance;
        if (manager != null)
        {
            manager.SolveCircuit();
        }
    }
    
    bool IsPositionOccupied(Vector3 position)
    {
        // Check for other components at this position (with small tolerance)
        Collider[] overlapping = Physics.OverlapSphere(position, 0.4f);
        
        foreach (Collider col in overlapping)
        {
            // Ignore self
            if (col.gameObject == gameObject) continue;
            
            // Check if it's another moveable component
            if (col.GetComponent<MoveableComponent>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    Vector3 FindNearestFreePosition(Vector3 targetPosition, float gridSize)
    {
        // Try positions in expanding spiral around target
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3 testPos = targetPosition + new Vector3(x * gridSize, 0, z * gridSize);
                    testPos.y = transform.position.y; // Keep Y consistent
                    
                    if (!IsPositionOccupied(testPos))
                    {
                        Debug.Log($"Found free position at {testPos}");
                        return testPos;
                    }
                }
            }
        }
        
        // If no free position found, return original position
        Debug.LogWarning("No free position found, staying at current location");
        return transform.position;
    }
    
    Vector3 GetMouseWorldPosition()
    {
        if (_mainCamera == null) return Vector3.zero;
        
        // Cast ray from camera through mouse position to the plane
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Create a plane at Y = 0.5 (where our components sit)
        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
        
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return Vector3.zero;
    }
    
    public bool IsDragging()
    {
        return _isDragging;
    }
}
