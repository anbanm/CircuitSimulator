using UnityEngine;

/// <summary>
/// AR-specific adaptations for the workspace
/// Handles AR scaling, positioning, and optimization
/// </summary>
public class ARWorkspaceAdapter : MonoBehaviour
{
    [Header("AR Configuration")]
    public float arScale = 0.7f;
    public float userDistance = 2f;
    public float maxInteractionDistance = 5f;
    
    [Header("AR Performance")]
    public bool enableLOD = true;
    public float lodDistance = 3f;
    public int maxVisibleComponents = 15;
    
    private WorkspaceManager workspaceManager;
    private Camera arCamera;
    private bool isARActive = false;
    
    // AR state tracking
    private Vector3 lastUserPosition;
    private float positionUpdateThreshold = 0.1f;
    
    public void Initialize(WorkspaceManager workspace)
    {
        workspaceManager = workspace;
        arCamera = workspace.GetPlayerCamera();
        
        if (workspaceManager.IsARMode)
        {
            EnableARMode();
        }
        
        Debug.Log("ARWorkspaceAdapter initialized");
    }
    
    public void Update()
    {
        if (!isARActive) return;
        
        UpdateAROptimizations();
        UpdateUserTracking();
        HandleARInteractions();
    }
    
    public void EnableARMode()
    {
        isARActive = true;
        
        // Configure workspace for AR
        ConfigureWorkspaceForAR();
        
        // Setup AR-specific optimizations
        SetupAROptimizations();
        
        Debug.Log("AR Mode enabled");
    }
    
    public void DisableARMode()
    {
        isARActive = false;
        
        // Restore desktop configuration
        RestoreDesktopConfiguration();
        
        Debug.Log("AR Mode disabled");
    }
    
    public void SetEnabled(bool enabled)
    {
        if (enabled)
            EnableARMode();
        else
            DisableARMode();
    }
    
    private void ConfigureWorkspaceForAR()
    {
        if (workspaceManager?.WorkspacePlane == null) return;
        
        // Position workspace in front of user
        Vector3 userForward = arCamera.transform.forward;
        Vector3 targetPosition = arCamera.transform.position + userForward * userDistance;
        targetPosition.y = arCamera.transform.position.y - 0.5f; // Slightly below eye level
        
        workspaceManager.WorkspacePlane.position = targetPosition;
        
        // Scale for AR viewing
        workspaceManager.WorkspacePlane.localScale = Vector3.one * arScale;
        
        // Orient toward user
        Vector3 lookDirection = arCamera.transform.position - workspaceManager.WorkspacePlane.position;
        lookDirection.y = 0; // Keep horizontal
        workspaceManager.WorkspacePlane.rotation = Quaternion.LookRotation(-lookDirection);
        
        Debug.Log($"Workspace configured for AR at position {targetPosition}");
    }
    
    private void SetupAROptimizations()
    {
        // Enable component LOD system
        if (enableLOD)
        {
            var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
            foreach (var comp in components)
            {
                var lodComponent = comp.GetComponent<ComponentLOD>();
                if (lodComponent == null)
                {
                    lodComponent = comp.gameObject.AddComponent<ComponentLOD>();
                    lodComponent.Initialize(arCamera, lodDistance);
                }
            }
        }
        
        // Limit visible components for performance
        ManageVisibleComponents();
    }
    
    private void UpdateAROptimizations()
    {
        if (enableLOD)
        {
            UpdateComponentLOD();
        }
        
        // Update workspace position based on user movement
        if (HasUserMoved())
        {
            UpdateWorkspacePosition();
        }
    }
    
    private void UpdateUserTracking()
    {
        lastUserPosition = arCamera.transform.position;
    }
    
    private void HandleARInteractions()
    {
        // Check if user is within interaction range
        float distanceToWorkspace = Vector3.Distance(
            arCamera.transform.position,
            workspaceManager.WorkspacePlane.position
        );
        
        bool canInteract = distanceToWorkspace <= maxInteractionDistance;
        
        // Enable/disable interaction based on distance
        var interactables = FindObjectsByType<UIButton3D>(FindObjectsSortMode.None);
        foreach (var button in interactables)
        {
            var collider = button.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = canInteract;
            }
        }
    }
    
    private void UpdateComponentLOD()
    {
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        
        foreach (var comp in components)
        {
            float distance = Vector3.Distance(arCamera.transform.position, comp.transform.position);
            
            // Simple LOD: hide components beyond certain distance
            bool shouldBeVisible = distance <= lodDistance;
            
            var renderer = comp.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = shouldBeVisible;
            }
            
            // Hide text labels for distant components
            var textMesh = comp.GetComponentInChildren<TMPro.TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.enabled = shouldBeVisible && distance <= lodDistance * 0.5f;
            }
        }
    }
    
    private void ManageVisibleComponents()
    {
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        
        if (components.Length <= maxVisibleComponents) return;
        
        // Sort by distance to camera
        System.Array.Sort(components, (a, b) => {
            float distA = Vector3.Distance(arCamera.transform.position, a.transform.position);
            float distB = Vector3.Distance(arCamera.transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        // Show only closest components
        for (int i = 0; i < components.Length; i++)
        {
            var renderer = components[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = i < maxVisibleComponents;
            }
        }
    }
    
    private bool HasUserMoved()
    {
        return Vector3.Distance(arCamera.transform.position, lastUserPosition) > positionUpdateThreshold;
    }
    
    private void UpdateWorkspacePosition()
    {
        // Smoothly update workspace position to stay in front of user
        Vector3 userForward = arCamera.transform.forward;
        Vector3 targetPosition = arCamera.transform.position + userForward * userDistance;
        targetPosition.y = arCamera.transform.position.y - 0.5f;
        
        // Smooth movement
        if (workspaceManager?.WorkspacePlane != null)
        {
            workspaceManager.WorkspacePlane.position = Vector3.Lerp(
                workspaceManager.WorkspacePlane.position,
                targetPosition,
                Time.deltaTime * 2f
            );
        }
    }
    
    private void RestoreDesktopConfiguration()
    {
        if (workspaceManager?.WorkspacePlane == null) return;
        
        // Reset to desktop position and scale
        workspaceManager.WorkspacePlane.position = Vector3.zero;
        workspaceManager.WorkspacePlane.rotation = Quaternion.identity;
        workspaceManager.WorkspacePlane.localScale = Vector3.one;
        
        // Re-enable all components
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        foreach (var comp in components)
        {
            var renderer = comp.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }
    
    #region Public API
    
    public bool IsARActive => isARActive;
    
    public void SetARScale(float scale)
    {
        arScale = scale;
        if (isARActive)
        {
            ConfigureWorkspaceForAR();
        }
    }
    
    public void SetUserDistance(float distance)
    {
        userDistance = distance;
        if (isARActive)
        {
            ConfigureWorkspaceForAR();
        }
    }
    
    #endregion
}

/// <summary>
/// Simple LOD component for circuit components in AR
/// </summary>
public class ComponentLOD : MonoBehaviour
{
    private Camera targetCamera;
    private float lodDistance;
    private Renderer componentRenderer;
    
    public void Initialize(Camera camera, float distance)
    {
        targetCamera = camera;
        lodDistance = distance;
        componentRenderer = GetComponent<Renderer>();
    }
    
    void Update()
    {
        if (targetCamera == null || componentRenderer == null) return;
        
        float distance = Vector3.Distance(targetCamera.transform.position, transform.position);
        componentRenderer.enabled = distance <= lodDistance;
    }
}