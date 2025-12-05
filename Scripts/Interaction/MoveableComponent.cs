using UnityEngine;

public class MoveableComponent : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public int groundLayer = 1; // Default layer (LayerMask simplified)
    
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    private Camera _mainCamera;
    private SelectableComponent _selectableComponent;
    private Transform _workspacePlane;
    
    void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError($"No MainCamera found for MoveableComponent on {gameObject.name}!");
            // Try to find any camera as fallback
            _mainCamera = FindFirstObjectByType<Camera>();
            if (_mainCamera == null)
            {
                Debug.LogError("No camera found in scene! MoveableComponent requires a camera.");
                enabled = false;
                return;
            }
        }
        
        _selectableComponent = GetComponent<SelectableComponent>();
        if (_selectableComponent == null)
        {
            Debug.LogWarning($"No SelectableComponent found on {gameObject.name}. Component will not be moveable.");
            enabled = false;
        }

        // Find the actual workspace plane for accurate raycasting
        FindWorkspacePlane();
    }

    void FindWorkspacePlane()
    {
        // Try to find WorkspaceManager first (handles both AR and desktop)
        var workspaceManager = FindFirstObjectByType<WorkspaceManager>();
        if (workspaceManager != null && workspaceManager.WorkspacePlane != null)
        {
            _workspacePlane = workspaceManager.WorkspacePlane;
            return;
        }

        // Fallback: Try to find CircuitWorkspace (created by WorkspaceManager)
        GameObject workspace = GameObject.Find("CircuitWorkspace");
        if (workspace != null)
        {
            _workspacePlane = workspace.transform;
            return;
        }

        // Fallback: Find Components GameObject
        GameObject components = GameObject.Find("Components");
        if (components != null)
        {
            _workspacePlane = components.transform;
            return;
        }

        Debug.LogWarning("[MoveableComponent] No workspace plane found, will use default plane at Y=0.5");
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

        // Only calculate offset for X and Z (horizontal plane), Y is fixed
        _dragOffset = transform.position - mouseWorldPos;
        _dragOffset.y = 0; // Ignore Y offset since we lock Y during dragging

    }
    
    void Update()
    {
        // Early exit if not dragging to save performance
        if (!_isDragging) return;
        
        HandleDragging();
        
        // Stop dragging on mouse release
        if (Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
        
        // ESC to cancel dragging
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopDragging();
        }
    }
    
    void HandleDragging()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector3.zero) return; // Ray didn't hit plane

        Vector3 targetPosition = mouseWorldPos + _dragOffset;

        if (_workspacePlane != null)
        {
            // Convert to local space, constrain to plane, convert back to world
            Vector3 localPos = _workspacePlane.InverseTransformPoint(targetPosition);
            localPos.y = 0.5f; // Fixed height above workspace surface in local space
            targetPosition = _workspacePlane.TransformPoint(localPos);
        }
        else
        {
            // Fallback: Keep Y position fixed at 0.5
            targetPosition.y = 0.5f;
        }

        // Direct movement for responsive dragging (no lerp lag)
        transform.position = targetPosition;
    }
    
    void StopDragging()
    {
        _isDragging = false;
        
        // Snap to grid (optional)
        SnapToGrid();
        
    }
    
    void SnapToGrid()
    {
        // Snap to nearest grid position (AR-optimized spacing)
        float gridSize = 1f;
        Vector3 snappedPos;

        if (_workspacePlane != null)
        {
            // Snap in local workspace space (works for tilted AR markers)
            Vector3 localPos = _workspacePlane.InverseTransformPoint(transform.position);
            localPos.x = Mathf.Round(localPos.x / gridSize) * gridSize;
            localPos.z = Mathf.Round(localPos.z / gridSize) * gridSize;
            localPos.y = 0.5f; // Maintain height above workspace
            snappedPos = _workspacePlane.TransformPoint(localPos);
        }
        else
        {
            // Fallback: Snap in world space
            snappedPos = transform.position;
            snappedPos.x = Mathf.Round(snappedPos.x / gridSize) * gridSize;
            snappedPos.z = Mathf.Round(snappedPos.z / gridSize) * gridSize;
        }

        // Check if this position is occupied by another component
        if (IsPositionOccupied(snappedPos))
        {
            // Find nearest free position
            snappedPos = FindNearestFreePosition(snappedPos, gridSize);
        }

        transform.position = snappedPos;

        // Trigger circuit re-solve when component moves (only if not in manual mode)
        var manager = CircuitManager.Instance;
        if (manager != null)
        {
            // Check if manual solve mode is enabled
            var solverManager = FindFirstObjectByType<CircuitSolverManager>();
            if (solverManager != null && !solverManager.manualSolveMode)
            {
                manager.SolveCircuit();
            }
            else if (solverManager == null)
            {
                // Fallback: if no solver manager, allow auto-solve
                manager.SolveCircuit();
            }
            // Otherwise, in manual mode - wait for user to press Solve button
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

        // Ensure workspace plane is available
        if (_workspacePlane == null)
        {
            FindWorkspacePlane();
        }

        // Cast ray from camera through mouse position to the plane
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // Use the actual workspace plane if available
        Plane plane;
        if (_workspacePlane != null)
        {
            // Create plane using the workspace transform's up direction
            // The plane is at Y=0.5 in local space (where components sit)
            Vector3 planePoint = _workspacePlane.TransformPoint(new Vector3(0, 0.5f, 0));
            plane = new Plane(_workspacePlane.up, planePoint);
        }
        else
        {
            // Fallback: Create a plane at Y = 0.5 (default behavior)
            plane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
        }

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);

            // For AR: Ensure the hit point is on the workspace plane at correct height
            if (_workspacePlane != null)
            {
                // Convert to local, enforce Y=0.5, convert back to world
                Vector3 localHit = _workspacePlane.InverseTransformPoint(hitPoint);
                localHit.y = 0.5f; // Clamp to component height
                return _workspacePlane.TransformPoint(localHit);
            }
            return hitPoint;
        }

        return Vector3.zero;
    }
    
    public bool IsDragging()
    {
        return _isDragging;
    }
}
