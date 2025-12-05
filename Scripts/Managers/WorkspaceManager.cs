using UnityEngine;

/// <summary>
/// Core workspace management - handles the main workspace setup and coordination
/// Replaces the core functionality of CircuitWorkspaceUI
/// </summary>
public class WorkspaceManager : MonoBehaviour
{
    [Header("Workspace Setup")]
    public Transform workspacePlane;
    public float workspaceSize = 10f;
    
    [Header("AR Optimization")]
    public bool optimizeForAR = true;
    public float arUIScale = 0.7f;
    public float maxViewDistance = 5f;

    [Header("AR Marker Tracking")]
    [Tooltip("If true, workspace will auto-parent to Vuforia ImageTarget")]
    public bool useMarkerTracking = false;
    [Tooltip("Optional: Assign ImageTarget manually. If empty, will search for one.")]
    public Transform imageTarget;
    
    // Component references
    private Camera playerCamera;
    private UILayoutManager layoutManager;
    private ARWorkspaceAdapter arAdapter;
    private MeasurementDisplayManager measurementManager;
    
    // Workspace state
    public Transform WorkspacePlane => workspacePlane;
    public bool IsARMode => optimizeForAR;
    public float ARScale => arUIScale;
    
    void Start()
    {
        InitializeWorkspace();
        InitializeManagers();
    }
    
    void Update()
    {
        // Update AR adaptations if needed
        arAdapter?.Update();
        
        // Update measurement displays
        measurementManager?.Update();
    }
    
    private void InitializeWorkspace()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("No main camera found! Make sure a camera is tagged as 'MainCamera'");
            // Try to find any camera as fallback
            playerCamera = FindFirstObjectByType<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("No camera found in scene! WorkspaceManager requires a camera.");
                enabled = false;
                return;
            }
        }

        
        // Create or setup workspace plane
        if (workspacePlane == null)
        {
            CreateWorkspacePlane();
        }
        
        // Configure for AR if needed
        if (optimizeForAR)
        {
            ConfigureForAR();
        }
        
    }


    private void InitializeManagers()
    {
        // Get or create sub-managers
        layoutManager = GetComponent<UILayoutManager>();
        if (layoutManager == null)
            layoutManager = gameObject.AddComponent<UILayoutManager>();
            
        if (optimizeForAR)
        {
            arAdapter = GetComponent<ARWorkspaceAdapter>();
            if (arAdapter == null)
                arAdapter = gameObject.AddComponent<ARWorkspaceAdapter>();
        }
        
        measurementManager = GetComponent<MeasurementDisplayManager>();
        if (measurementManager == null)
            measurementManager = gameObject.AddComponent<MeasurementDisplayManager>();
        
        // Initialize managers
        layoutManager.Initialize(this);
        arAdapter?.Initialize(this);
        measurementManager.Initialize(this);
    }
    
    private void CreateWorkspacePlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "CircuitWorkspace";
        plane.transform.localScale = Vector3.one * workspaceSize;
        workspacePlane = plane.transform;
        
        // Setup plane material for better visibility
        var renderer = plane.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
        }
        
    }
    
    private void ConfigureForAR()
    {
        // AR-specific workspace configuration
        if (workspacePlane == null) return;

        // If marker tracking enabled, parent workspace to Image Target
        if (useMarkerTracking)
        {
            ParentToImageTarget();
        }
        else
        {
            // Position appropriately for AR (floating in front of user)
            workspacePlane.position = new Vector3(0, 0, 2f);
        }

        // Set scale for AR viewing
        workspacePlane.localScale = Vector3.one * arUIScale;
    }

    private void ParentToImageTarget()
    {
        // Find Image Target if not assigned
        if (imageTarget == null)
        {
            // Try to find Vuforia ImageTarget by component
            var vuforiaTarget = FindFirstObjectByType<Vuforia.ImageTargetBehaviour>();
            if (vuforiaTarget != null)
            {
                imageTarget = vuforiaTarget.transform;
                Debug.Log($"[WorkspaceManager] Found ImageTarget: {imageTarget.name}");
            }
            else
            {
                Debug.LogWarning("[WorkspaceManager] useMarkerTracking enabled but no ImageTarget found!");
                return;
            }
        }

        // Parent workspace to Image Target
        workspacePlane.SetParent(imageTarget);
        workspacePlane.localPosition = new Vector3(0, 0.01f, 0); // Slightly above marker
        workspacePlane.localRotation = Quaternion.identity;

        Debug.Log($"[WorkspaceManager] Workspace parented to ImageTarget: {imageTarget.name}");
    }
    
    #region Public API
    
    public Vector3 GetWorldspacePosition(Vector2 workspaceCoords)
    {
        if (workspacePlane == null) return Vector3.zero;
        
        // Convert workspace coordinates to world position
        Vector3 localPos = new Vector3(workspaceCoords.x, 0.1f, workspaceCoords.y);
        return workspacePlane.TransformPoint(localPos);
    }
    
    public Vector2 GetWorkspaceCoords(Vector3 worldPosition)
    {
        if (workspacePlane == null) return Vector2.zero;
        
        // Convert world position to workspace coordinates
        Vector3 localPos = workspacePlane.InverseTransformPoint(worldPosition);
        return new Vector2(localPos.x, localPos.z);
    }
    
    public bool IsValidWorkspacePosition(Vector3 worldPosition)
    {
        Vector2 coords = GetWorkspaceCoords(worldPosition);
        float halfSize = workspaceSize * 0.5f;
        
        return Mathf.Abs(coords.x) <= halfSize && Mathf.Abs(coords.y) <= halfSize;
    }
    
    public void SetARMode(bool enabled)
    {
        optimizeForAR = enabled;
        
        if (enabled && arAdapter == null)
        {
            arAdapter = gameObject.AddComponent<ARWorkspaceAdapter>();
            arAdapter.Initialize(this);
        }
        
        arAdapter?.SetEnabled(enabled);
        ConfigureForAR();
        
    }
    
    public Camera GetPlayerCamera()
    {
        return playerCamera;
    }
    
    #endregion
    
    #region Component Actions
    
    public void ClearWorkspace()
    {
        
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            // Clear all components
            var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    if (Application.isPlaying)
                        Destroy(comp.gameObject);
                    else
                        DestroyImmediate(comp.gameObject);
                }
            }
            
            // Clear all wires
            var wires = FindObjectsByType<CircuitWire>(FindObjectsSortMode.None);
            foreach (var wire in wires)
            {
                if (wire != null)
                {
                    if (Application.isPlaying)
                        Destroy(wire.gameObject);
                    else
                        DestroyImmediate(wire.gameObject);
                }
            }
        }
    }
    
    public void ValidateWorkspace()
    {
        var circuitManager = CircuitManager.Instance;
        circuitManager?.ValidateAndTestCircuit();
    }
    
    #endregion
}