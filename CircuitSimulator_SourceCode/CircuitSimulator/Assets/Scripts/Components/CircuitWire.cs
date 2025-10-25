using UnityEngine;

// Enhanced wire component that integrates with circuit solver
public class CircuitWire : MonoBehaviour
{
    [Header("Wire Properties")]
    public float resistance = 0.1f; // Small resistance for real wires
    public float current = 0f;
    public float voltageDrop = 0f;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.blue;
    public Color selectedColor = Color.cyan;
    public Color currentFlowColor = Color.yellow;
    public float wireWidth = 0.1f;
    
    private LineRenderer lineRenderer;
    private CircuitComponent3D component1;
    private CircuitComponent3D component2;
    private bool isSelected = false;
    private static CircuitWire currentlySelectedWire = null;

    // Wire dragging state
    private bool isDraggingWire = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    // Terminal-based connections (new architecture)
    public ComponentTerminal startTerminal;
    public ComponentTerminal endTerminal;

    // Draggable wire endpoints (newest architecture)
    public WireEndpoint startEndpoint;
    public WireEndpoint endEndpoint;

    // Educational: Current flow visualization for Grade 7 students
    private CurrentFlowVisualizer currentFlowVisualizer;
    
    public CircuitComponent3D Component1 => component1;
    public CircuitComponent3D Component2 => component2;
    public bool IsSelected => isSelected;
    
    // For CurrentFlowVisualizer access
    public CircuitComponent3D startComponent
    {
        get => component1;
        set => component1 = value;
    }
    public CircuitComponent3D endComponent
    {
        get => component2;
        set => component2 = value;
    }
    
    public void Initialize(CircuitComponent3D comp1, CircuitComponent3D comp2)
    {
        component1 = comp1;
        component2 = comp2;

        SetupVisual();
        SetupCurrentFlowVisualization();
        RegisterWithComponents();
        RegisterWithManager();

        // Mark as registered
        isRegisteredWithComponents = true;
        isRegisteredWithManager = true;

        name = $"Wire_{comp1.name}_to_{comp2.name}";
        Debug.Log($"Created circuit wire: {name}");
    }
    
    public void InitializeWithTerminals(ComponentTerminal terminal1, ComponentTerminal terminal2)
    {
        startTerminal = terminal1;
        endTerminal = terminal2;
        component1 = terminal1.ParentComponent;
        component2 = terminal2.ParentComponent;

        SetupVisual();
        SetupCurrentFlowVisualization();
        RegisterWithComponents();
        RegisterWithManager();

        // Mark as registered
        isRegisteredWithComponents = true;
        isRegisteredWithManager = true;

        name = $"Wire_{terminal1.name}_to_{terminal2.name}";
        Debug.Log($"Created terminal-based circuit wire: {name}");
    }

    // NEW: Initialize wire with draggable endpoints
    public void InitializeWithEndpoints(Vector3 startPosition, Vector3 endPosition)
    {
        // Setup visual first (LineRenderer, etc.)
        SetupVisual();
        SetupCurrentFlowVisualization();

        // Create start endpoint (NO parenting to avoid feedback loop!)
        GameObject startObj = new GameObject("StartEndpoint");
        startObj.transform.position = startPosition;
        startEndpoint = startObj.AddComponent<WireEndpoint>();
        startEndpoint.SetParentWire(this); // Set wire reference explicitly

        // Create end endpoint (NO parenting to avoid feedback loop!)
        GameObject endObj = new GameObject("EndEndpoint");
        endObj.transform.position = endPosition;
        endEndpoint = endObj.AddComponent<WireEndpoint>();
        endEndpoint.SetParentWire(this); // Set wire reference explicitly

        // Store reference to wire for cleanup
        startEndpoint.transform.SetParent(transform.parent); // Parent to same as wire
        endEndpoint.transform.SetParent(transform.parent);

        // NOW add the collider (after endpoints exist)
        CapsuleCollider wireCollider = GetComponent<CapsuleCollider>();
        if (wireCollider == null)
        {
            wireCollider = gameObject.AddComponent<CapsuleCollider>();
            wireCollider.isTrigger = false;
            wireCollider.direction = 2; // Z-axis
            wireCollider.radius = 0.2f; // Generous click area
            Debug.Log($"✅ Added CapsuleCollider to draggable wire: {name}");
        }

        // Update collider position
        UpdateWireCollider();

        name = "Draggable_Wire";
        Debug.Log($"Created draggable wire with endpoints at {startPosition} and {endPosition}");
    }

    // Track if wire is already registered to prevent duplicates
    private bool isRegisteredWithManager = false;
    private bool isRegisteredWithComponents = false;

    // Called when an endpoint connects to a terminal
    public void OnEndpointConnected(WireEndpoint endpoint)
    {
        Debug.Log($"🔌 Endpoint {endpoint.name} connected to terminal, Current registration state: Components={isRegisteredWithComponents}, Manager={isRegisteredWithManager}");

        // Update component references based on connected terminals
        if (startEndpoint != null && startEndpoint.IsConnected)
        {
            startTerminal = startEndpoint.ConnectedTerminal;
            component1 = startTerminal.ParentComponent;
            Debug.Log($"  → Start endpoint connected to {component1.name}, Wire count on component: {component1.connectedWires.Count}");
        }

        if (endEndpoint != null && endEndpoint.IsConnected)
        {
            endTerminal = endEndpoint.ConnectedTerminal;
            component2 = endTerminal.ParentComponent;
            Debug.Log($"  → End endpoint connected to {component2.name}, Wire count on component: {component2.connectedWires.Count}");
        }

        // If both endpoints connected, register with circuit system
        if (IsFullyConnected())
        {
            Debug.Log($"🔗 Both endpoints connected! Checking registration...");

            // CRITICAL FIX: Double-check component wire lists to prevent duplicate registration
            // This handles race conditions where OnEndpointConnected() is called multiple times
            bool alreadyInComponent1 = (component1 != null && component1.connectedWires.Contains(this));
            bool alreadyInComponent2 = (component2 != null && component2.connectedWires.Contains(this));
            bool actuallyRegisteredWithComponents = alreadyInComponent1 || alreadyInComponent2;

            // Only register if not already registered to prevent duplicates
            if (!isRegisteredWithComponents && !actuallyRegisteredWithComponents)
            {
                Debug.Log($"  → REGISTERING with components (was not registered)");
                // CRITICAL: Set flag IMMEDIATELY to prevent race conditions
                isRegisteredWithComponents = true;

                RegisterWithComponents();

                // Log wire counts AFTER registration
                if (component1 != null)
                    Debug.Log($"  → {component1.name} now has {component1.connectedWires.Count} wires");
                if (component2 != null)
                    Debug.Log($"  → {component2.name} now has {component2.connectedWires.Count} wires");
            }
            else
            {
                Debug.LogWarning($"⚠️ Wire ALREADY registered with components! Flag={isRegisteredWithComponents}, InComp1={alreadyInComponent1}, InComp2={alreadyInComponent2}");
                // Sync the flag if it's out of sync
                if (!isRegisteredWithComponents && actuallyRegisteredWithComponents)
                {
                    Debug.Log($"  → Syncing flag: isRegisteredWithComponents = true");
                    isRegisteredWithComponents = true;
                }
            }

            // CRITICAL FIX: Double-check CircuitManager wire list to prevent duplicate registration
            var manager = FindFirstObjectByType<CircuitManager>();
            bool actuallyRegisteredWithManager = (manager != null && manager.IsWireRegistered(gameObject));

            if (!isRegisteredWithManager && !actuallyRegisteredWithManager)
            {
                Debug.Log($"  → REGISTERING with CircuitManager");
                // CRITICAL: Set flag IMMEDIATELY to prevent race conditions
                isRegisteredWithManager = true;

                RegisterWithManager();
            }
            else
            {
                Debug.LogWarning($"⚠️ Wire ALREADY registered with CircuitManager! Flag={isRegisteredWithManager}, InManager={actuallyRegisteredWithManager}");
                // Sync the flag if it's out of sync
                if (!isRegisteredWithManager && actuallyRegisteredWithManager)
                {
                    Debug.Log($"  → Syncing flag: isRegisteredWithManager = true");
                    isRegisteredWithManager = true;
                }
            }

            // Connect terminals electrically (only once)
            if (startTerminal != null && endTerminal != null)
            {
                startTerminal.ConnectToTerminal(endTerminal, this);
            }

            name = $"Wire_{component1.name}_to_{component2.name}";
            Debug.Log($"✅ Wire fully connected: {name}");

            // Mark dirty to update immediately
            MarkDirty();
        }
        else
        {
            Debug.Log($"  → Not fully connected yet (waiting for other endpoint)");
        }
    }

    // Called when an endpoint disconnects from a terminal
    public void OnEndpointDisconnected(WireEndpoint endpoint)
    {
        Debug.Log($"🔓 Endpoint {endpoint.name} disconnected, Current registration state: Components={isRegisteredWithComponents}, Manager={isRegisteredWithManager}");

        // Check if wire WAS fully connected by checking if BOTH components are set
        // (The endpoint has already cleared its connectedTerminal by the time this is called)
        bool wasFullyConnected = (component1 != null && component2 != null);
        Debug.Log($"  → Was fully connected: {wasFullyConnected} (component1={component1?.name}, component2={component2?.name})");

        // Clear the disconnected terminal reference
        if (endpoint == startEndpoint)
        {
            if (component1 != null)
            {
                Debug.Log($"  → Removing wire from {component1.name} (had {component1.connectedWires.Count} wires)");
                if (isRegisteredWithComponents)
                {
                    component1.RemoveConnectedWire(gameObject);
                    Debug.Log($"  → {component1.name} now has {component1.connectedWires.Count} wires");
                }
            }
            startTerminal = null;
            component1 = null;
        }
        else if (endpoint == endEndpoint)
        {
            if (component2 != null)
            {
                Debug.Log($"  → Removing wire from {component2.name} (had {component2.connectedWires.Count} wires)");
                if (isRegisteredWithComponents)
                {
                    component2.RemoveConnectedWire(gameObject);
                    Debug.Log($"  → {component2.name} now has {component2.connectedWires.Count} wires");
                }
            }
            endTerminal = null;
            component2 = null;
        }

        // Unregister from manager if no longer fully connected
        if (wasFullyConnected && isRegisteredWithManager)
        {
            Debug.Log($"  → UNREGISTERING from CircuitManager");
            CircuitManager manager = CircuitManager.Instance;
            if (manager == null && ComponentRegistry.Instance != null)
            {
                manager = ComponentRegistry.Instance.GetManager<CircuitManager>();
            }
            if (manager != null)
            {
                manager.UnregisterWire(gameObject);
                isRegisteredWithManager = false;
                Debug.Log($"  → Unregistered from CircuitManager");
            }
        }

        // Clear component registration flag
        if (!IsFullyConnected())
        {
            Debug.Log($"  → Clearing component registration flag (no longer fully connected)");
            isRegisteredWithComponents = false;
        }

        Debug.Log($"🔓 Disconnect complete. New state: Components={isRegisteredWithComponents}, Manager={isRegisteredWithManager}");
        MarkDirty();
    }

    // Check if both endpoints are connected to terminals
    public bool IsFullyConnected()
    {
        // Endpoint-based connection
        if (startEndpoint != null && endEndpoint != null)
        {
            return startEndpoint.IsConnected && endEndpoint.IsConnected;
        }

        // Terminal-based connection (legacy)
        if (startTerminal != null && endTerminal != null)
        {
            return true;
        }

        // Component-based connection (oldest legacy)
        if (component1 != null && component2 != null)
        {
            return true;
        }

        return false;
    }
    
    void SetupVisual()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        // Configure LineRenderer
        lineRenderer.material = CreateWireMaterial();
        lineRenderer.startWidth = wireWidth;
        lineRenderer.endWidth = wireWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // Get main camera for dragging
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        // Add collider for wire body interaction (if using endpoints)
        // Using CapsuleCollider for simple cylinder-shaped click area
        if (startEndpoint != null || endEndpoint != null)
        {
            CapsuleCollider wireCollider = GetComponent<CapsuleCollider>();
            if (wireCollider == null)
            {
                wireCollider = gameObject.AddComponent<CapsuleCollider>();
            }
            wireCollider.isTrigger = false;
            wireCollider.direction = 2; // Z-axis
            wireCollider.radius = 0.2f; // Generous click area
            UpdateWireCollider();
        }

        // Add current display on wire
        WireValueDisplay valueDisplay = GetComponent<WireValueDisplay>();
        if (valueDisplay == null)
        {
            valueDisplay = gameObject.AddComponent<WireValueDisplay>();
        }

        UpdateWirePosition();
    }
    
    void SetupCurrentFlowVisualization()
    {
        // Add current flow visualization for educational purposes
        // This helps Grade 7 students SEE electricity flowing
        currentFlowVisualizer = GetComponent<CurrentFlowVisualizer>();
        if (currentFlowVisualizer == null)
        {
            currentFlowVisualizer = gameObject.AddComponent<CurrentFlowVisualizer>();
            Debug.Log($"Added current flow visualization to wire: {name}");
        }
    }
    
    void RegisterWithComponents()
    {
        // Tell components about this wire connection
        if (component1 != null) component1.AddConnectedWire(gameObject);
        if (component2 != null) component2.AddConnectedWire(gameObject);
    }
    
    void RegisterWithManager()
    {
        CircuitManager manager = CircuitManager.Instance;
        if (manager == null && ComponentRegistry.Instance != null)
        {
            manager = ComponentRegistry.Instance.GetManager<CircuitManager>();
        }
        
        if (manager != null)
        {
            manager.RegisterWire(gameObject);
        }
        else
        {
            Debug.LogWarning($"CircuitManager not found! Wire {name} will not be registered.");
        }
    }
    
    // Performance optimization: Use frame throttling
    private float updateInterval = 0.1f; // Update 10 times per second instead of 60
    private float nextUpdateTime = 0f;
    private bool isDirty = true; // Flag to force immediate update when needed
    
    void Update()
    {
        // Always handle input for responsiveness
        HandleInput();
        
        // Throttle expensive updates
        if (isDirty || Time.time >= nextUpdateTime)
        {
            UpdateWirePosition();
            UpdateCurrentFromComponents();
            UpdateVisualFromCircuitData();
            
            nextUpdateTime = Time.time + updateInterval;
            isDirty = false;
        }
    }
    
    // Call this when immediate update is needed
    public void MarkDirty()
    {
        isDirty = true;
    }
    
    void UpdateCurrentFromComponents()
    {
        // Get current from circuit solver for accurate educational visualization
        // This ensures the current flow dots show the correct speed based on actual circuit analysis

        if (!IsFullyConnected())
        {
            current = 0f;
            return;
        }

        // Priority 1: Get current from circuit solver through CircuitManager
        CircuitManager manager = CircuitManager.Instance;
        if (manager == null && ComponentRegistry.Instance != null)
        {
            manager = ComponentRegistry.Instance.GetManager<CircuitManager>();
        }

        if (manager != null)
        {
            // Try to get current from the logical wire component in the solver
            // The CircuitSolverManager should have updated wire currents after solving
            var solverManager = FindFirstObjectByType<CircuitSolverManager>();
            if (solverManager != null)
            {
                // Check if this wire has solved current data
                // Wire current flows between the two connected components
                if (component1 != null && component2 != null)
                {
                    // In a series circuit, wire current = component current
                    // Use average if components have different currents (parallel branches)
                    float current1 = Mathf.Abs(component1.current);
                    float current2 = Mathf.Abs(component2.current);
                    float wireCurrent = (current1 + current2) / 2f;

                    // Only update if there's a significant change
                    if (Mathf.Abs(wireCurrent - current) > 0.001f)
                    {
                        current = wireCurrent;

                        // Debug info for educational purposes
                        if (Mathf.Abs(current) > 0.01f)
                        {
                            Debug.Log($"Wire {name}: Current updated to {current:F3}A (from components: {current1:F3}A, {current2:F3}A)");
                        }
                    }
                }
            }
        }

        // Fallback: Use component current directly
        if (component1 != null && Mathf.Abs(current) < 0.001f)
        {
            current = Mathf.Abs(component1.current);
        }
    }
    
    void UpdateWirePosition()
    {
        if (lineRenderer == null) return;

        Vector3 pos1, pos2;

        // Priority 1: Use draggable endpoint positions (newest architecture)
        if (startEndpoint != null && endEndpoint != null)
        {
            pos1 = startEndpoint.GetPosition();
            pos2 = endEndpoint.GetPosition();
        }
        // Priority 2: Use terminal positions (new architecture)
        else if (startTerminal != null && endTerminal != null)
        {
            pos1 = startTerminal.GetConnectionPoint();
            pos2 = endTerminal.GetConnectionPoint();
        }
        // Priority 3: Fallback to component positions (legacy)
        else if (component1 != null && component2 != null)
        {
            pos1 = component1.transform.position + Vector3.up * 0.6f;
            pos2 = component2.transform.position + Vector3.up * 0.6f;
        }
        else
        {
            return; // Can't position wire without valid endpoints
        }

        lineRenderer.SetPosition(0, pos1);
        lineRenderer.SetPosition(1, pos2);

        // Update collider to match wire position (for dragging)
        if (startEndpoint != null && endEndpoint != null)
        {
            UpdateWireCollider();
        }
    }
    
    void UpdateVisualFromCircuitData()
    {
        if (lineRenderer != null && lineRenderer.material != null)
        {
            Color wireColor = normalColor;
            
            if (isSelected)
            {
                wireColor = selectedColor;
            }
            else if (Mathf.Abs(current) > 0.01f)
            {
                // Show current flow with color intensity
                float intensity = Mathf.Clamp01(Mathf.Abs(current) / 2f);
                wireColor = Color.Lerp(normalColor, currentFlowColor, intensity);
            }
            
            lineRenderer.material.color = wireColor;
        }
    }
    
    void HandleInput()
    {
        // Delete key to remove selected wire
        if (Input.GetKeyDown(KeyCode.Delete) && isSelected)
        {
            DeleteWire();
        }
    }
    
    void OnMouseDown()
    {
        Debug.Log($"🖱️ Wire OnMouseDown triggered: {name}");

        SelectWire();

        // Start dragging wire body if it has endpoints
        if (startEndpoint != null && endEndpoint != null)
        {
            isDraggingWire = true;

            // Calculate offset between mouse position and wire center
            Vector3 wireCenterPos = (startEndpoint.GetPosition() + endEndpoint.GetPosition()) / 2f;
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            dragOffset = wireCenterPos - mouseWorldPos;

            Debug.Log($"✅ Started dragging wire body: {name}");
        }
        else
        {
            Debug.Log($"⚠️ Wire has no endpoints, cannot drag: {name}");
        }
    }

    void OnMouseDrag()
    {
        if (isDraggingWire && startEndpoint != null && endEndpoint != null)
        {
            // Get mouse world position
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3 targetCenter = mouseWorldPos + dragOffset;

            // Calculate the vector between endpoints
            Vector3 wireVector = endEndpoint.GetPosition() - startEndpoint.GetPosition();

            // Move both endpoints to maintain the wire shape
            Vector3 newStartPos = targetCenter - wireVector / 2f;
            Vector3 newEndPos = targetCenter + wireVector / 2f;

            startEndpoint.SetPosition(newStartPos);
            endEndpoint.SetPosition(newEndPos);

            // If endpoints were connected, disconnect them
            if (startEndpoint.IsConnected)
            {
                startEndpoint.DetachFromTerminal();
            }
            if (endEndpoint.IsConnected)
            {
                endEndpoint.DetachFromTerminal();
            }

            MarkDirty();
        }
    }

    void OnMouseUp()
    {
        if (isDraggingWire)
        {
            isDraggingWire = false;
            Debug.Log($"Stopped dragging wire: {name}");

            // Try to snap endpoints to nearby terminals
            if (startEndpoint != null)
            {
                ComponentTerminal nearestTerminal = FindNearestTerminalForEndpoint(startEndpoint.GetPosition());
                if (nearestTerminal != null)
                {
                    startEndpoint.SnapToTerminal(nearestTerminal);
                }
            }

            if (endEndpoint != null)
            {
                ComponentTerminal nearestTerminal = FindNearestTerminalForEndpoint(endEndpoint.GetPosition());
                if (nearestTerminal != null)
                {
                    endEndpoint.SnapToTerminal(nearestTerminal);
                }
            }
        }
    }

    ComponentTerminal FindNearestTerminalForEndpoint(Vector3 position)
    {
        ComponentTerminal[] allTerminals = FindObjectsByType<ComponentTerminal>(FindObjectsSortMode.None);
        ComponentTerminal nearest = null;
        float minDistance = 0.5f; // Same as endpoint snap radius

        foreach (var terminal in allTerminals)
        {
            float distance = Vector3.Distance(position, terminal.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = terminal;
            }
        }

        return nearest;
    }

    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;

        // Raycast to workspace plane (Y = 0.5)
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    void UpdateWireCollider()
    {
        if (startEndpoint == null || endEndpoint == null) return;

        CapsuleCollider wireCollider = GetComponent<CapsuleCollider>();
        if (wireCollider == null)
        {
            Debug.LogWarning($"⚠️ Wire {name} has no CapsuleCollider!");
            return;
        }

        Vector3 start = startEndpoint.GetPosition();
        Vector3 end = endEndpoint.GetPosition();
        Vector3 center = (start + end) / 2f;
        float length = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        // NOW SAFE: Endpoints aren't children, so we can move wire GameObject
        transform.position = center;

        // Rotate wire to point along the line
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Update capsule collider
        wireCollider.center = Vector3.zero; // Centered on GameObject
        wireCollider.height = length;
        wireCollider.direction = 2; // Z-axis
        wireCollider.enabled = true;

        // Debug: Log collider info periodically
        if (Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
        {
            Debug.Log($"🔧 Wire collider: {name} - Length: {length:F2}, Center: {center}, Radius: {wireCollider.radius}");
        }
    }
    
    public void SelectWire()
    {
        // Deselect previous wire
        if (currentlySelectedWire != null && currentlySelectedWire != this)
        {
            currentlySelectedWire.DeselectWire();
        }
        
        // Deselect any selected components
        SelectableComponent selectedComp = SelectableComponent.GetCurrentlySelected();
        if (selectedComp != null)
        {
            selectedComp.Deselect();
        }
        
        isSelected = true;
        currentlySelectedWire = this;
        
        Debug.Log($"Selected wire: {name} (R={resistance}Ω, I={current:F2}A)");
    }
    
    public void DeselectWire()
    {
        isSelected = false;
        if (currentlySelectedWire == this)
        {
            currentlySelectedWire = null;
        }
    }
    
    public void DeleteWire()
    {
        Debug.Log($"Deleting wire: {name}");

        // Destroy endpoint GameObjects first (before wire is destroyed)
        if (startEndpoint != null && startEndpoint.gameObject != null)
        {
            if (startEndpoint.IsConnected)
            {
                startEndpoint.DetachFromTerminal();
            }
            Destroy(startEndpoint.gameObject);
        }
        if (endEndpoint != null && endEndpoint.gameObject != null)
        {
            if (endEndpoint.IsConnected)
            {
                endEndpoint.DetachFromTerminal();
            }
            Destroy(endEndpoint.gameObject);
        }

        // Unregister from components (only if registered)
        if (isRegisteredWithComponents)
        {
            if (component1 != null) component1.RemoveConnectedWire(gameObject);
            if (component2 != null) component2.RemoveConnectedWire(gameObject);
            isRegisteredWithComponents = false;
        }

        // Unregister from manager (only if registered)
        if (isRegisteredWithManager)
        {
            CircuitManager manager = CircuitManager.Instance;
            if (manager == null && ComponentRegistry.Instance != null)
            {
                manager = ComponentRegistry.Instance.GetManager<CircuitManager>();
            }
            if (manager != null)
            {
                manager.UnregisterWire(gameObject);
                isRegisteredWithManager = false;
                Debug.Log($"🔴 Wire deleted and unregistered: {name}");
            }
        }

        // Clear selection
        if (currentlySelectedWire == this)
        {
            currentlySelectedWire = null;
        }

        Destroy(gameObject);
    }
    
    public bool IsConnectedToComponent(GameObject component)
    {
        if (component == null) return false;
        
        return (component1 != null && component1.gameObject == component) ||
               (component2 != null && component2.gameObject == component);
    }
    
    Material CreateWireMaterial()
    {
        // Try URP shader first, fallback to legacy
        Shader wireShader = Shader.Find("Universal Render Pipeline/Lit");
        if (wireShader == null)
        {
            wireShader = Shader.Find("Standard");
        }
        if (wireShader == null)
        {
            wireShader = Shader.Find("Sprites/Default");
        }
        
        Material mat = new Material(wireShader);
        mat.color = normalColor;
        return mat;
    }
    
    void OnDestroy()
    {
        // Destroy endpoint GameObjects (they're not children, so need manual cleanup)
        if (startEndpoint != null && startEndpoint.gameObject != null)
        {
            if (startEndpoint.IsConnected)
            {
                startEndpoint.DetachFromTerminal();
            }
            Destroy(startEndpoint.gameObject);
        }
        if (endEndpoint != null && endEndpoint.gameObject != null)
        {
            if (endEndpoint.IsConnected)
            {
                endEndpoint.DetachFromTerminal();
            }
            Destroy(endEndpoint.gameObject);
        }

        // Clean up any remaining references (only if registered)
        if (isRegisteredWithComponents)
        {
            if (component1 != null) component1.RemoveConnectedWire(gameObject);
            if (component2 != null) component2.RemoveConnectedWire(gameObject);
            isRegisteredWithComponents = false;
        }

        // Unregister from manager if still registered
        if (isRegisteredWithManager)
        {
            CircuitManager manager = CircuitManager.Instance;
            if (manager == null && ComponentRegistry.Instance != null)
            {
                manager = ComponentRegistry.Instance.GetManager<CircuitManager>();
            }
            if (manager != null)
            {
                manager.UnregisterWire(gameObject);
                isRegisteredWithManager = false;
            }
        }

        // Clean up static reference to prevent memory leak
        if (currentlySelectedWire == this.gameObject)
        {
            currentlySelectedWire = null;
        }
    }
}