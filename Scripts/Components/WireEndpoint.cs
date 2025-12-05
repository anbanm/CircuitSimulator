using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Draggable endpoint for circuit wires
/// Allows users to create connections by dragging endpoints to terminals
/// Provides visual feedback during snapping and connection
/// </summary>
public class WireEndpoint : MonoBehaviour
{
    [Header("Snap Settings")]
    public float snapRadius = 0.5f;
    public float endpointSize = 0.4f;  // Increased from 0.15f for better visibility

    [Header("Visual Feedback")]
    public Color disconnectedColor = Color.gray;
    public Color snapIndicatorColor = Color.yellow;
    public Color connectedColor = Color.blue;
    public Color draggingColor = Color.cyan;
    public Color junctionColor = Color.green;  // Color for junction points (multiple wires connected)

    // State
    private bool isDragging = false;
    private ComponentTerminal connectedTerminal = null;  // Physical terminal this endpoint is AT
    private WireEndpoint snappedToEndpoint = null;       // Another endpoint this is snapped to (visual junction)
    private CircuitWire parentWire;
    private Camera mainCamera;
    private Transform workspacePlane;

    // Public accessor for parent wire (needed by JunctionTopologyManager)
    public CircuitWire ParentWire => parentWire;

    // Flow direction (assigned after circuit solving)
    [Header("Flow Direction")]
    [Tooltip("True if this endpoint is the START of current flow (closer to Battery+)")]
    public bool isStart = false;

    // Visual components
    private MeshRenderer meshRenderer;
    private Material endpointMaterial;
    private GameObject snapIndicator;

    // Cached references
    private Vector3 dragOffset;
    private ComponentTerminal nearestTerminalWhileDragging = null;
    private WireEndpoint nearestWireEndpointWhileDragging = null;  // Track wire endpoint for visual feedback
    private ComponentTerminal lastLoggedSnapTarget = null;  // Track last logged snap to avoid spam

    public bool IsConnected => connectedTerminal != null || snappedToEndpoint != null;  // FIX: Also consider wire-to-wire junctions
    public ComponentTerminal ConnectedTerminal => connectedTerminal;
    public WireEndpoint SnappedToEndpoint => snappedToEndpoint;  // For topology discovery
    public bool IsDragging => isDragging;

    void Awake()
    {
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        // Find workspace plane for AR-compatible raycasting
        FindWorkspacePlane();

        // Setup visual appearance immediately in Awake() so endpoints are visible when created
        SetupVisualAppearance();
    }

    void FindWorkspacePlane()
    {
        // Try to find WorkspaceManager first (handles both AR and desktop)
        var workspaceManager = FindFirstObjectByType<WorkspaceManager>();
        if (workspaceManager != null && workspaceManager.WorkspacePlane != null)
        {
            workspacePlane = workspaceManager.WorkspacePlane;
            return;
        }

        // Fallback: Try to find CircuitWorkspace
        GameObject workspace = GameObject.Find("CircuitWorkspace");
        if (workspace != null)
        {
            workspacePlane = workspace.transform;
        }
    }

    void Start()
    {
        // Parent wire will be set via SetParentWire() method before Start() is called
    }

    /// <summary>
    /// Set the parent wire reference (called by CircuitWire after creation)
    /// </summary>
    public void SetParentWire(CircuitWire wire)
    {
        parentWire = wire;
    }

    void Update()
    {
        // If connected to a terminal, follow its position
        if (connectedTerminal != null && !isDragging)
        {
            Vector3 terminalPos = connectedTerminal.transform.position;
            if (Vector3.Distance(transform.position, terminalPos) > 0.01f)
            {
                transform.position = terminalPos;
            }
        }
    }

    void SetupVisualAppearance()
    {
        // Create endpoint sphere
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateSphereMesh();

        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Create material with emission for better visibility
        endpointMaterial = new Material(Shader.Find("Standard"));
        endpointMaterial.color = disconnectedColor;
        endpointMaterial.SetFloat("_Metallic", 0.5f);
        endpointMaterial.SetFloat("_Glossiness", 0.8f);
        // Add subtle emission to make endpoints always visible
        endpointMaterial.EnableKeyword("_EMISSION");
        endpointMaterial.SetColor("_EmissionColor", disconnectedColor * 0.3f);

        // Set rendering queue to render on top of other objects
        endpointMaterial.renderQueue = 3000; // Transparent queue for visibility

        meshRenderer.material = endpointMaterial;
        meshRenderer.enabled = true;  // Ensure renderer is enabled

        // Add collider for mouse interaction
        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = endpointSize * 2f; // Larger hit area

        // Scale endpoint
        transform.localScale = Vector3.one * endpointSize;

        // Create snap indicator (hidden by default)
        CreateSnapIndicator();

    }

    Mesh CreateSphereMesh()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = sphere.GetComponent<MeshFilter>().mesh;
        if (Application.isPlaying)
            Destroy(sphere);
        else
            DestroyImmediate(sphere);
        return mesh;
    }

    void CreateSnapIndicator()
    {
        snapIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        snapIndicator.name = "SnapIndicator";
        snapIndicator.transform.SetParent(transform);
        snapIndicator.transform.localPosition = Vector3.zero;
        snapIndicator.transform.localScale = Vector3.one * 2f; // Slightly larger

        // Remove collider (don't interfere with mouse)
        Destroy(snapIndicator.GetComponent<Collider>());

        // Make it glow
        var snapRenderer = snapIndicator.GetComponent<MeshRenderer>();
        var snapMat = new Material(Shader.Find("Standard"));
        snapMat.color = snapIndicatorColor;
        snapMat.EnableKeyword("_EMISSION");
        snapMat.SetColor("_EmissionColor", snapIndicatorColor * 0.8f);
        snapRenderer.material = snapMat;

        // Hide by default
        snapIndicator.SetActive(false);
    }

    void OnMouseDown()
    {
        StartDragging();
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            UpdateDragPosition();
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            StopDragging();
        }
    }

    void StartDragging()
    {
        isDragging = true;

        // Disconnect from current terminal if connected
        if (connectedTerminal != null)
        {
            DetachFromTerminal();
        }
        
        // Disconnect from snapped endpoint if snapped
        if (snappedToEndpoint != null)
        {
            DetachFromEndpoint();
        }

        // Calculate drag offset
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPos;

        // Visual feedback
        UpdateColor(draggingColor);
    }

    void UpdateDragPosition()
    {
        // Move endpoint with mouse
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        transform.position = mouseWorldPos + dragOffset;

        // Check for nearby terminals
        nearestTerminalWhileDragging = FindNearestTerminal();

        // Show snap indicator if near terminal OR wire endpoint
        bool hasSnapTarget = nearestTerminalWhileDragging != null || nearestWireEndpointWhileDragging != null;

        if (hasSnapTarget)
        {
            snapIndicator.SetActive(true);

            // Position snap indicator at the snap target
            if (nearestWireEndpointWhileDragging != null)
            {
                // Snapping to wire endpoint - show indicator at wire endpoint position
                snapIndicator.transform.position = nearestWireEndpointWhileDragging.transform.position;

                // Show the OTHER endpoint's snap indicator too for visual feedback
                if (nearestWireEndpointWhileDragging.snapIndicator != null)
                {
                    nearestWireEndpointWhileDragging.snapIndicator.SetActive(true);
                }
            }
            else if (nearestTerminalWhileDragging != null)
            {
                // Snapping to component terminal
                snapIndicator.transform.position = nearestTerminalWhileDragging.transform.position;
            }
        }
        else
        {
            snapIndicator.SetActive(false);
            // Hide any wire endpoint snap indicators we might have shown
            if (nearestWireEndpointWhileDragging != null && nearestWireEndpointWhileDragging.snapIndicator != null)
            {
                nearestWireEndpointWhileDragging.snapIndicator.SetActive(false);
            }
        }
    }

    void StopDragging()
    {
        isDragging = false;
        lastLoggedSnapTarget = null;  // Reset snap logging

        // Try to snap to nearest terminal
        ComponentTerminal nearestTerminal = FindNearestTerminal();

        if (nearestTerminal != null)
        {
            // Snap to component terminal (or terminal of connected wire endpoint)
            SnapToTerminal(nearestTerminal);
        }
        else if (nearestWireEndpointWhileDragging != null)
        {
            // Snap to unconnected wire endpoint - connect both to same position
            SnapToWireEndpoint(nearestWireEndpointWhileDragging);
        }
        else
        {
            // Check if there was a terminal nearby but invalid
            CheckForInvalidConnectionAttempt();

            // No terminal nearby - stay disconnected
            UpdateColor(disconnectedColor);
        }

        // Hide snap indicators on BOTH endpoints
        snapIndicator.SetActive(false);
        if (nearestWireEndpointWhileDragging != null && nearestWireEndpointWhileDragging.snapIndicator != null)
        {
            nearestWireEndpointWhileDragging.snapIndicator.SetActive(false);
        }

        nearestTerminalWhileDragging = null;
        nearestWireEndpointWhileDragging = null;
    }

    /// <summary>
    /// Check if user tried to connect to an invalid terminal and give feedback
    /// </summary>
    void CheckForInvalidConnectionAttempt()
    {
        ComponentTerminal[] allTerminals = FindObjectsByType<ComponentTerminal>(FindObjectsSortMode.None);

        foreach (var terminal in allTerminals)
        {
            float distance = Vector3.Distance(transform.position, terminal.transform.position);

            if (distance < snapRadius)
            {
                // Terminal is nearby but invalid - tell user why
                WireEndpoint otherEndpoint = GetOtherEndpoint();
                ComponentTerminal otherTerminal = otherEndpoint?.ConnectedTerminal;

                if (otherTerminal != null && terminal.ParentComponent == otherTerminal.ParentComponent)
                {
                    Debug.LogWarning($"⚠️ Cannot connect both wire ends to the same component ({terminal.ParentComponent.name})");
                    return;
                }
            }
        }
    }

    ComponentTerminal FindNearestTerminal()
    {
        // Clear previous wire endpoint reference
        nearestWireEndpointWhileDragging = null;

        ComponentTerminal nearest = null;
        float minDistance = snapRadius;
        float minWireEndpointDistance = float.MaxValue;
        float minTerminalDistance = float.MaxValue;

        // CHECK WIRE ENDPOINTS FIRST (priority for wire-to-wire junctions!)
        WireEndpoint[] allEndpoints = FindObjectsByType<WireEndpoint>(FindObjectsSortMode.None);

        foreach (var endpoint in allEndpoints)
        {
            // Skip self
            if (endpoint == this) continue;

            // Skip if on same wire
            if (endpoint.parentWire == this.parentWire) continue;

            // ALLOW SNAPPING TO UNCONNECTED ENDPOINTS (for wire-to-wire junctions)
            // If endpoint is connected, we'll use its terminal
            // If endpoint is unconnected, we'll just use it for positioning
            float distance = Vector3.Distance(transform.position, endpoint.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                minWireEndpointDistance = distance;
                nearestWireEndpointWhileDragging = endpoint;

                // If the endpoint is connected to a terminal, use that terminal
                // If not connected, nearest will remain null but we'll still track the wire endpoint
                if (endpoint.IsConnected)
                {
                    nearest = endpoint.ConnectedTerminal;
                }
            }
        }

        // Then check component terminals
        ComponentTerminal[] allTerminals = FindObjectsByType<ComponentTerminal>(FindObjectsSortMode.None);

        foreach (var terminal in allTerminals)
        {
            // Skip if this terminal is invalid for connection
            if (!IsValidTerminalForConnection(terminal))
                continue;

            float distance = Vector3.Distance(transform.position, terminal.transform.position);

            // Track minimum terminal distance for logging
            if (distance < minTerminalDistance)
            {
                minTerminalDistance = distance;
            }

            // Only use this terminal if it's closer than any wire endpoint
            // OR if distances are equal, prefer the wire endpoint (already set)
            if (distance < minWireEndpointDistance && distance < snapRadius)
            {
                minDistance = distance;
                nearest = terminal;
                nearestWireEndpointWhileDragging = null; // Clear wire endpoint since terminal is closer
            }
        }

        // Only log when snap target CHANGES (not every frame!)
        if (nearest != lastLoggedSnapTarget)
        {
            if (nearest != null)
            {
                if (nearestWireEndpointWhileDragging != null)
                {
                    string connStatus = nearestWireEndpointWhileDragging.IsConnected ? "connected" : "unconnected";
                    Debug.Log($"[SNAP] → Wire endpoint ({connStatus}): {nearestWireEndpointWhileDragging.name} at {minWireEndpointDistance:F2} units");
                }
                else
                {
                    Debug.Log($"[SNAP] → Terminal: {nearest.name} at {minTerminalDistance:F2} units");
                }
            }
            else if (nearestWireEndpointWhileDragging != null)
            {
                // Found wire endpoint but no terminal (unconnected wire endpoint)
                Debug.Log($"[SNAP] → Unconnected wire: {nearestWireEndpointWhileDragging.name} at {minWireEndpointDistance:F2} units");
            }
            else
            {
                if (lastLoggedSnapTarget != null)
                {
                    Debug.Log($"[SNAP] × Lost snap target");
                }
            }
            lastLoggedSnapTarget = nearest;
        }

        return nearest;
    }

    /// <summary>
    /// Check if a terminal is valid for connection
    /// Prevents connecting both endpoints to the same component
    /// </summary>
    bool IsValidTerminalForConnection(ComponentTerminal terminal)
    {
        if (terminal == null) return false;

        // Get the other endpoint of this wire
        WireEndpoint otherEndpoint = GetOtherEndpoint();
        if (otherEndpoint == null) return true; // No other endpoint yet, allow any connection

        // If other endpoint is not connected, allow any connection
        if (!otherEndpoint.IsConnected) return true;

        // Get the terminal the other endpoint is connected to
        ComponentTerminal otherTerminal = otherEndpoint.ConnectedTerminal;
        if (otherTerminal == null) return true;

        // Check if both terminals are on the same component
        if (terminal.ParentComponent == otherTerminal.ParentComponent)
        {
            // NOTE: Don't log here - this is called every frame during drag preview
            // Warning is shown in OnMouseUp when actually attempting connection
            return false; // Same component - invalid!
        }

        return true; // Different components - valid!
    }

    /// <summary>
    /// Get the other endpoint of this wire (start or end)
    /// </summary>
    WireEndpoint GetOtherEndpoint()
    {
        if (parentWire == null) return null;

        // If this is the start endpoint, return the end endpoint (and vice versa)
        if (parentWire.startEndpoint == this)
            return parentWire.endEndpoint;
        else
            return parentWire.startEndpoint;
    }

    /// <summary>
    /// Snap this endpoint to a component terminal (physical connection)
    /// VISUAL LAYER ONLY - no electrical logic here
    /// </summary>
    public void SnapToTerminal(ComponentTerminal terminal)
    {
        if (terminal == null) return;

        // Validate connection before snapping
        if (!IsValidTerminalForConnection(terminal))
        {
            Debug.LogWarning($"❌ Cannot snap to {terminal.name}: Both endpoints would be on same component!");
            UpdateColor(Color.red);
            return;
        }

        // Detach from old terminal if any
        if (connectedTerminal != null && connectedTerminal != terminal)
        {
            DetachFromTerminal();
        }
        
        // Detach from snapped endpoint if any (can't be both snapped and at terminal)
        if (snappedToEndpoint != null)
        {
            DetachFromEndpoint();
        }

        // Connect to terminal (physical connection only)
        connectedTerminal = terminal;

        // Move to terminal position
        transform.position = terminal.transform.position;

        // Visual feedback
        UpdateColor(connectedColor);
        UpdateJunctionColors(terminal);  // Green if multiple wires on same terminal

        // Notify parent wire
        if (parentWire != null)
        {
            parentWire.OnEndpointConnected(this);
        }
    }

    /// <summary>
    /// Snap to another wire endpoint (for wire-to-wire junctions)
    /// VISUAL LAYER ONLY - just stores the reference for topology discovery later
    /// </summary>
    public void SnapToWireEndpoint(WireEndpoint otherEndpoint)
    {
        if (otherEndpoint == null) return;

        // Detach from terminal if any (can't be both snapped and at terminal)
        if (connectedTerminal != null)
        {
            DetachFromTerminal();
        }

        // Store the snap reference (bidirectional)
        snappedToEndpoint = otherEndpoint;
        if (otherEndpoint.snappedToEndpoint != this)
        {
            otherEndpoint.snappedToEndpoint = this;
        }

        // Move to the other endpoint's position
        Vector3 junctionPosition = otherEndpoint.transform.position;
        transform.position = junctionPosition;

        // Visual feedback - green for junction
        UpdateColor(junctionColor);
        otherEndpoint.UpdateColor(junctionColor);

        // Make junction endpoints larger for visibility
        transform.localScale = Vector3.one * endpointSize * 1.5f;
        otherEndpoint.transform.localScale = Vector3.one * endpointSize * 1.5f;

        // Notify parent wire (for visual update)
        if (parentWire != null)
        {
            parentWire.OnEndpointConnected(this);
        }
    }

    /// <summary>
    /// Detach from snapped wire endpoint
    /// VISUAL LAYER ONLY - clears the snap reference
    /// </summary>
    public void DetachFromEndpoint()
    {
        if (snappedToEndpoint == null) return;

        // Clear bidirectional reference
        WireEndpoint other = snappedToEndpoint;
        snappedToEndpoint = null;
        if (other != null && other.snappedToEndpoint == this)
        {
            other.snappedToEndpoint = null;

            // FIX: Check if other endpoint is still part of terminal junction
            if (other.IsConnected)
            {
                // Update junction colors at terminal (may still be green if multiple wires)
                UpdateJunctionColors(other.ConnectedTerminal);
            }
            else
            {
                other.UpdateColor(disconnectedColor);
            }

            other.transform.localScale = Vector3.one * endpointSize;
        }

        // Reset visual feedback
        UpdateColor(disconnectedColor);
        transform.localScale = Vector3.one * endpointSize;

        // Notify parent wire
        if (parentWire != null)
        {
            parentWire.OnEndpointDisconnected(this);
        }
    }

    public void DetachFromTerminal()
    {
        if (connectedTerminal == null) return;

        connectedTerminal = null;

        // Visual feedback
        UpdateColor(disconnectedColor);

        // Notify parent wire
        if (parentWire != null)
        {
            parentWire.OnEndpointDisconnected(this);
        }
    }

    void UpdateColor(Color color)
    {
        if (endpointMaterial != null)
        {
            endpointMaterial.color = color;
            // Update emission color to match, maintaining the glow effect
            endpointMaterial.SetColor("_EmissionColor", color * 0.4f);  // Slightly stronger glow for different states
        }
    }

    /// <summary>
    /// Update colors for all endpoints connected to the given terminal
    /// Uses junction color if multiple wires are connected (parallel circuit junction)
    /// </summary>
    void UpdateJunctionColors(ComponentTerminal terminal)
    {
        if (terminal == null) return;

        // Find all wire endpoints connected to this terminal
        WireEndpoint[] allEndpoints = FindObjectsByType<WireEndpoint>(FindObjectsSortMode.None);
        List<WireEndpoint> endpointsOnThisTerminal = new List<WireEndpoint>();

        foreach (var endpoint in allEndpoints)
        {
            if (endpoint.connectedTerminal == terminal)
            {
                endpointsOnThisTerminal.Add(endpoint);
            }
        }

        // Determine color based on junction status
        Color colorToUse = endpointsOnThisTerminal.Count >= 2 ? junctionColor : connectedColor;

        // Update all endpoints on this terminal to the same color
        foreach (var endpoint in endpointsOnThisTerminal)
        {
            endpoint.UpdateColor(colorToUse);
        }

    }

    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return transform.position;

        // Ensure workspace plane is available
        if (workspacePlane == null)
        {
            FindWorkspacePlane();
        }

        // Cast ray from camera through mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Use the actual workspace plane if available (works for AR with tilted markers)
        Plane plane;
        if (workspacePlane != null)
        {
            // Create plane using workspace transform's up direction
            // The plane is at Y=0.5 in local space (where components sit)
            Vector3 planePoint = workspacePlane.TransformPoint(new Vector3(0, 0.5f, 0));
            plane = new Plane(workspacePlane.up, planePoint);
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
            if (workspacePlane != null)
            {
                // Convert to local, enforce Y=0.5, convert back to world
                Vector3 localHit = workspacePlane.InverseTransformPoint(hitPoint);
                localHit.y = 0.5f; // Clamp to component height
                return workspacePlane.TransformPoint(localHit);
            }
            return hitPoint;
        }

        return transform.position;
    }

    void OnDestroy()
    {
        // Cleanup materials
        if (endpointMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(endpointMaterial);
            else
                DestroyImmediate(endpointMaterial);
        }
    }

    // Public API for external control
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    // Visual feedback for hovering
    void OnMouseEnter()
    {
        if (!isDragging && !IsConnected)
        {
            UpdateColor(draggingColor);
        }
    }

    void OnMouseExit()
    {
        if (!isDragging && !IsConnected)
        {
            UpdateColor(disconnectedColor);
        }
    }
}
