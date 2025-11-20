using UnityEngine;

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

    // State
    private bool isDragging = false;
    private ComponentTerminal connectedTerminal = null;
    private CircuitWire parentWire;
    private Camera mainCamera;

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

    public bool IsConnected => connectedTerminal != null;
    public ComponentTerminal ConnectedTerminal => connectedTerminal;
    public bool IsDragging => isDragging;

    void Start()
    {
        // Parent wire will be set via SetParentWire() method
        // No need to find it via GetComponentInParent since endpoints are siblings

        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        // Setup visual appearance
        SetupVisualAppearance();

        Debug.Log($"WireEndpoint created: {name}, ParentWire: {(parentWire != null ? parentWire.name : "null")}");
    }

    /// <summary>
    /// Set the parent wire reference (called by CircuitWire after creation)
    /// </summary>
    public void SetParentWire(CircuitWire wire)
    {
        parentWire = wire;
        Debug.Log($"✅ WireEndpoint {name} parent wire set to: {wire.name}");
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
        Debug.Log($"🔌 Setting up wire endpoint visual: {name}, Size: {endpointSize}");

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

        meshRenderer.material = endpointMaterial;
        meshRenderer.enabled = true;  // Ensure renderer is enabled

        // Add collider for mouse interaction
        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = endpointSize * 2f; // Larger hit area

        // Scale endpoint
        transform.localScale = Vector3.one * endpointSize;

        // Create snap indicator (hidden by default)
        CreateSnapIndicator();

        Debug.Log($"✅ Wire endpoint visual complete: {name}, World Position: {transform.position}, Size: {endpointSize}, Renderer enabled: {meshRenderer.enabled}");
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

        // Calculate drag offset
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPos;

        // Visual feedback
        UpdateColor(draggingColor);

        Debug.Log($"Started dragging endpoint: {name}");
    }

    void UpdateDragPosition()
    {
        // Move endpoint with mouse
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        transform.position = mouseWorldPos + dragOffset;

        // Check for nearby terminals
        nearestTerminalWhileDragging = FindNearestTerminal();

        // Show snap indicator if near terminal
        if (nearestTerminalWhileDragging != null)
        {
            snapIndicator.SetActive(true);
            snapIndicator.transform.position = nearestTerminalWhileDragging.transform.position;
        }
        else
        {
            snapIndicator.SetActive(false);
        }
    }

    void StopDragging()
    {
        isDragging = false;

        // Try to snap to nearest terminal
        ComponentTerminal nearestTerminal = FindNearestTerminal();

        if (nearestTerminal != null)
        {
            SnapToTerminal(nearestTerminal);
        }
        else
        {
            // Check if there was a terminal nearby but invalid
            CheckForInvalidConnectionAttempt();

            // No terminal nearby - stay disconnected
            UpdateColor(disconnectedColor);
        }

        // Hide snap indicator
        snapIndicator.SetActive(false);
        nearestTerminalWhileDragging = null;

        Debug.Log($"Stopped dragging endpoint: {name} (Connected: {IsConnected})");
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
        // Find all terminals in scene
        ComponentTerminal[] allTerminals = FindObjectsByType<ComponentTerminal>(FindObjectsSortMode.None);

        ComponentTerminal nearest = null;
        float minDistance = snapRadius;

        foreach (var terminal in allTerminals)
        {
            // Skip if this terminal is invalid for connection
            if (!IsValidTerminalForConnection(terminal))
                continue;

            float distance = Vector3.Distance(transform.position, terminal.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = terminal;
            }
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

    public void SnapToTerminal(ComponentTerminal terminal)
    {
        if (terminal == null) return;

        // Validate connection before snapping
        if (!IsValidTerminalForConnection(terminal))
        {
            Debug.LogWarning($"❌ Cannot snap to {terminal.name}: Both endpoints would be on same component!");
            UpdateColor(Color.red); // Show red to indicate invalid connection
            return; // Don't snap!
        }

        // Detach from old terminal if any
        if (connectedTerminal != null && connectedTerminal != terminal)
        {
            DetachFromTerminal();
        }

        // Connect to new terminal
        connectedTerminal = terminal;

        // Move to terminal position
        transform.position = terminal.transform.position;

        // Visual feedback
        UpdateColor(connectedColor);

        // Notify parent wire
        if (parentWire != null)
        {
            parentWire.OnEndpointConnected(this);
        }

        Debug.Log($"✅ Endpoint snapped to terminal: {terminal.name}");
    }

    public void DetachFromTerminal()
    {
        if (connectedTerminal == null) return;

        Debug.Log($"Endpoint detached from terminal: {connectedTerminal.name}");

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

    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return transform.position;

        // Raycast to workspace plane (Y = 0.5)
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
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
