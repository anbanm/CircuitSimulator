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

    [Header("Flow Direction (Set on Solve)")]
    [Tooltip("Terminal where current flow STARTS (source end)")]
    public string flowStartTerminal = "Not set";
    [Tooltip("Terminal where current flow ENDS (destination end)")]
    public string flowEndTerminal = "Not set";
    
    private LineRenderer lineRenderer;
    private CircuitComponent3D component1;
    private CircuitComponent3D component2;
    private bool isSelected = false;
    private static CircuitWire currentlySelectedWire = null;

    // Public accessor for other systems to check currently selected wire
    public static CircuitWire GetCurrentlySelectedWire()
    {
        return currentlySelectedWire;
    }

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

    void Awake()
    {
        // Subscribe to circuit solved event for dynamic direction updates
        CircuitEventManager.CircuitSolved += OnCircuitSolvedHandler;
    }

    void OnCircuitSolvedHandler()
    {
        // Force immediate update of current direction after circuit is solved
        Debug.Log($"🔄 Wire {name}: CircuitSolved event received, updating direction...");

        // DISABLED: OrientWireTowardSource() conflicts with VisualFlowGraph BFS
        // VisualFlowGraph now handles flow direction assignment via isStart flags
        // OrientWireTowardSource() was physically swapping endpoints, fighting with BFS
        // OrientWireTowardSource();

        UpdateCurrentFromComponents(debugDetails: true); // Enable detailed logging for circuit solve events only
        MarkDirty(); // Ensure visual update happens immediately
        Debug.Log($"   → Current after update: {current:F3}A");

        // Log the complete flow path for this wire
        LogFlowPath();
    }

    void LogFlowPath()
    {
        if (!IsFullyConnected()) return;

        // Get terminal names with better formatting
        string startCompName = component1?.name ?? "NULL";
        string endCompName = component2?.name ?? "NULL";
        string startTermName = startTerminal?.name ?? "NULL";
        string endTermName = endTerminal?.name ?? "NULL";

        // Format component type and terminal info
        string startInfo = $"{startCompName} ({startTermName})";
        string endInfo = $"{endCompName} ({endTermName})";

        // Show flow direction based on current
        string flowDirection = current >= 0 ? "→" : "←";
        string flowPath = current >= 0 ?
            $"{startInfo} {flowDirection} {endInfo}" :
            $"{endInfo} {flowDirection} {startInfo}";

        // Set Inspector properties for visual feedback
        if (current >= 0)
        {
            flowStartTerminal = startInfo;
            flowEndTerminal = endInfo;
        }
        else
        {
            flowStartTerminal = endInfo;
            flowEndTerminal = startInfo;
        }

        Debug.Log($"⚡ FLOW PATH: {flowPath} | Current: {Mathf.Abs(current):F3}A");
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
        // Update component references based on connected terminals
        if (startEndpoint != null && startEndpoint.IsConnected)
        {
            startTerminal = startEndpoint.ConnectedTerminal;
            component1 = startTerminal.ParentComponent;
        }

        if (endEndpoint != null && endEndpoint.IsConnected)
        {
            endTerminal = endEndpoint.ConnectedTerminal;
            component2 = endTerminal.ParentComponent;
        }

        // If both endpoints connected, register with circuit system
        if (IsFullyConnected())
        {

            // CRITICAL FIX: Double-check component wire lists to prevent duplicate registration
            bool alreadyInComponent1 = (component1 != null && component1.connectedWires.Contains(this));
            bool alreadyInComponent2 = (component2 != null && component2.connectedWires.Contains(this));
            bool actuallyRegisteredWithComponents = alreadyInComponent1 || alreadyInComponent2;

            // Only register if not already registered to prevent duplicates
            if (!isRegisteredWithComponents && !actuallyRegisteredWithComponents)
            {
                isRegisteredWithComponents = true;
                RegisterWithComponents();
            }
            else if (!isRegisteredWithComponents && actuallyRegisteredWithComponents)
            {
                isRegisteredWithComponents = true;
            }

            // CRITICAL FIX: Double-check CircuitManager wire list to prevent duplicate registration
            var manager = FindFirstObjectByType<CircuitManager>();
            bool actuallyRegisteredWithManager = (manager != null && manager.IsWireRegistered(gameObject));

            if (!isRegisteredWithManager && !actuallyRegisteredWithManager)
            {
                isRegisteredWithManager = true;
                RegisterWithManager();
            }
            else if (!isRegisteredWithManager && actuallyRegisteredWithManager)
            {
                isRegisteredWithManager = true;
            }

            // Connect terminals electrically (only once)
            if (startTerminal != null && endTerminal != null)
            {
                startTerminal.ConnectToTerminal(endTerminal, this);
            }

            // Force circuit to re-solve so animation direction updates
            var circuitManager = FindFirstObjectByType<CircuitManager>();
            if (circuitManager != null)
            {
                circuitManager.MarkCircuitChanged();
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

        // Clear current values when disconnected
        current = 0f;
        voltageDrop = 0f;

        // Trigger circuit re-solve to update all component values
        var circuitManager = FindFirstObjectByType<CircuitManager>();
        if (circuitManager != null)
        {
            circuitManager.MarkCircuitChanged();
            Debug.Log("  → Triggered circuit re-solve after disconnection");
        }

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
    
    void UpdateCurrentFromComponents(bool debugDetails = false) // Debug only when requested
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
                    // OrientWireTowardSource() is now called ONLY on circuit solve (not every frame)
                    // This prevents expensive BFS pathfinding from running constantly

                    // EDUCATIONAL PHYSICS: Kirchhoff's Current Law (KCL)
                    // Current through a wire MUST equal current through connected components
                    // Any difference indicates a solver error that we should detect

                    // Now that wire is properly oriented, use component1's current
                    float current1 = component1.current;
                    float current2 = component2.current;

                    if (debugDetails)
                    {
                        Debug.Log($"🔍 === WIRE DIRECTION DEBUG: {name} ===");
                        Debug.Log($"  Component1: {component1.name}, Current: {current1:F3}A");
                        Debug.Log($"  Component2: {component2.name}, Current: {current2:F3}A");
                    }

                    // BATTERY-BASED DIRECTION: Use battery as directional reference for conventional current flow
                    // Conventional current flows FROM battery + terminal THROUGH circuit TO battery - terminal
                    // This creates an educational current loop that students can visualize
                    float wireCurrent = current1;
                    bool directionDetermined = false;

                    // Check component types for special handling
                    bool comp1IsBattery = component1.ComponentType == ComponentType.Battery;
                    bool comp2IsBattery = component2.ComponentType == ComponentType.Battery;
                    bool comp1IsJunction = component1.ComponentType == ComponentType.Junction;
                    bool comp2IsJunction = component2.ComponentType == ComponentType.Junction;

                    if (debugDetails)
                    {
                        Debug.Log($"  Component1: {component1.ComponentType}, Component2: {component2.ComponentType}");
                        Debug.Log($"  StartTerminal: {startTerminal?.name} (on {startTerminal?.ParentComponent?.name}), EndTerminal: {endTerminal?.name} (on {endTerminal?.ParentComponent?.name})");
                    }

                    // CASE 0: Wire connects to a Junction - junctions just pass current through
                    // Use the direction from the non-junction component
                    if (comp1IsJunction && !comp2IsJunction)
                    {
                        // Junction at start, real component at end
                        // Wire direction comes from the end component
                        bool endIsOutput = !endTerminal.isInput;
                        if (endIsOutput)
                        {
                            // Connected to component's OUTPUT → current exits component, flows toward junction
                            wireCurrent = -current2; // Flows toward start (negative)
                            if (debugDetails)
                                Debug.Log($"  ✓ Junction at START, component OUTPUT at END → current flows toward junction (negative) = {wireCurrent:F3}A");
                        }
                        else
                        {
                            // Connected to component's INPUT → current enters component, flows from junction
                            wireCurrent = current2; // Flows away from start (positive)
                            if (debugDetails)
                                Debug.Log($"  ✓ Junction at START, component INPUT at END → current flows from junction (positive) = {wireCurrent:F3}A");
                        }
                        directionDetermined = true;
                    }
                    else if (comp2IsJunction && !comp1IsJunction)
                    {
                        // Real component at start, junction at end
                        // Wire direction comes from the start component
                        bool startIsOutput = !startTerminal.isInput;
                        if (startIsOutput)
                        {
                            // Connected to component's OUTPUT → current exits component toward junction
                            wireCurrent = current1; // Flows away from start (positive)
                            if (debugDetails)
                                Debug.Log($"  ✓ Component OUTPUT at START, junction at END → current flows toward junction (positive) = {wireCurrent:F3}A");
                        }
                        else
                        {
                            // Connected to component's INPUT → current enters component from junction
                            wireCurrent = -current1; // Flows toward start (negative)
                            if (debugDetails)
                                Debug.Log($"  ✓ Component INPUT at START, junction at END → current flows from junction (negative) = {wireCurrent:F3}A");
                        }
                        directionDetermined = true;
                    }
                    else if (comp1IsJunction && comp2IsJunction)
                    {
                        // Junction-to-junction wire - just use magnitude
                        wireCurrent = Mathf.Abs(current1);
                        if (debugDetails)
                            Debug.Log($"  ✓ Junction-to-junction wire → use current magnitude = {wireCurrent:F3}A");
                        directionDetermined = true;
                    }

                    // CASE 1: Battery wires - use terminal polarity to determine direction
                    if (comp1IsBattery && !directionDetermined)
                    {
                        // Battery at START - check which terminal we're connected to
                        bool connectedToPositive = startTerminal != null && startTerminal.name.Contains("Positive");

                        if (connectedToPositive)
                        {
                            // Connected to + terminal: current flows OUT (away from start)
                            wireCurrent = current1;  // Positive = start→end
                        }
                        else
                        {
                            // Connected to - terminal: current flows IN (toward start)
                            wireCurrent = -current1;  // Negative = end→start
                        }

                        if (debugDetails)
                            Debug.Log($"  ✓ Battery at START, terminal={startTerminal?.name}, wireCurrent={wireCurrent:F3}A");

                        directionDetermined = true;
                    }
                    else if (comp2IsBattery && !directionDetermined)
                    {
                        // Battery at END - check which terminal we're connected to
                        bool connectedToPositive = endTerminal != null && endTerminal.name.Contains("Positive");

                        if (connectedToPositive)
                        {
                            // Connected to + terminal: current flows OUT (toward start)
                            wireCurrent = -current2;  // Negative = end→start
                        }
                        else
                        {
                            // Connected to - terminal: current flows IN (away from start)
                            wireCurrent = current2;  // Positive = start→end
                        }

                        if (debugDetails)
                            Debug.Log($"  ✓ Battery at END, terminal={endTerminal?.name}, wireCurrent={wireCurrent:F3}A");

                        directionDetermined = true;
                    }

                    // CASE 2: Non-battery components
                    // Wire is already oriented (component1 closer to battery), so just use solver current
                    if (!directionDetermined)
                    {
                        // Simple! Wire is oriented correctly, current flows component1→component2
                        wireCurrent = Mathf.Abs(current1); // Always positive (flows start→end)

                        if (debugDetails)
                            Debug.Log($"  ✓ Symmetric component (oriented) → wireCurrent = {wireCurrent:F3}A (always positive)");

                        directionDetermined = true;
                    }

                    if (debugDetails && directionDetermined)
                    {
                        Debug.Log($"✅ Wire {name}: Direction determined, INITIAL wireCurrent = {wireCurrent:F3}A");
                    }

                    // No swapping! CurrentDot handles positive/negative current naturally
                    // Positive current: dots flow start→end (position 0→1)
                    // Negative current: dots flow end→start (position 1→0)
                    if (debugDetails)
                    {
                        Debug.Log($"✅ Wire {name}: FINAL wireCurrent = {wireCurrent:F3}A (direction={(wireCurrent >= 0 ? "forward" : "reverse")})");
                    }

                    // EDUCATIONAL CHECK: Verify Kirchhoff's Current Law
                    // In a properly solved circuit, current magnitudes should be equal
                    float currentDifference = Mathf.Abs(Mathf.Abs(current1) - Mathf.Abs(current2));
                    if (currentDifference > 0.01f) // Tolerance for numerical precision
                    {
                        // Use the average magnitude to handle small solver errors
                        float avgMagnitude = (Mathf.Abs(current1) + Mathf.Abs(current2)) / 2f;
                        wireCurrent = (wireCurrent >= 0) ? avgMagnitude : -avgMagnitude;
                    }

                    // Only update if there's a significant change
                    if (Mathf.Abs(wireCurrent - current) > 0.001f)
                    {
                        current = wireCurrent;
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
        // CRITICAL FIX: Shorten collider to leave gaps for endpoints to be clickable!
        // Each endpoint has size 0.4f, so we leave 0.5f gap on each end
        float endpointGap = 0.5f; // Leave room for endpoints
        float colliderHeight = Mathf.Max(0.1f, length - (endpointGap * 2)); // Min 0.1f to avoid zero height

        wireCollider.center = Vector3.zero; // Centered on GameObject
        wireCollider.height = colliderHeight;
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

        // Deselect any selected components (OLD SYSTEM - v1.0)
        SelectableComponent selectedComp = SelectableComponent.GetCurrentlySelected();
        if (selectedComp != null)
        {
            selectedComp.Deselect();
        }

        // CRITICAL FIX: Deselect ALL InteractionComponents (NEW SYSTEM - v2.0)
        CircuitSimulator.Components.InteractionComponent.DeselectAll();

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
    
    /// <summary>
    /// Orient wire based on electrical node connections (NodeA/NodeB from solver)
    /// This creates consistent flow direction regardless of component instance names
    /// Called on every solve to handle topology changes
    /// </summary>
    void OrientWireTowardSource()
    {
        // Skip if either component is a battery (battery wires already oriented by terminal polarity)
        bool comp1IsBattery = component1 != null && component1.ComponentType == ComponentType.Battery;
        bool comp2IsBattery = component2 != null && component2.ComponentType == ComponentType.Battery;

        if (comp1IsBattery || comp2IsBattery)
        {
            return; // Battery wires use terminal polarity for direction
        }

        // ====== NEW APPROACH: Use ONLY electrical node connections ======
        // Ideal wire orientation: START→OUTPUT (NodeB), END→INPUT (NodeA)
        // This ensures current flows: Component1.OUTPUT → Wire → Component2.INPUT

        bool needsSwap = false;
        string reason = "";

        // Validate we have all required data
        if (startTerminal == null || endTerminal == null ||
            startTerminal.electricalNode == null || endTerminal.electricalNode == null)
        {
            Debug.LogWarning($"⚠️ {name}: Missing terminal or node data, cannot orient");
            return;
        }

        if (component1.logicalComponent == null || component2.logicalComponent == null)
        {
            Debug.LogWarning($"⚠️ {name}: Missing logical components, cannot orient");
            return;
        }

        // Check current wire orientation
        CircuitNode startNode = startTerminal.electricalNode;
        CircuitNode endNode = endTerminal.electricalNode;
        CircuitNode comp1NodeA = component1.logicalComponent.NodeA;
        CircuitNode comp1NodeB = component1.logicalComponent.NodeB;
        CircuitNode comp2NodeA = component2.logicalComponent.NodeA;
        CircuitNode comp2NodeB = component2.logicalComponent.NodeB;

        Debug.Log($"🔍 NODE-BASED Orient check: {name}");
        Debug.Log($"  START terminal → Node {startNode.Id} (HashCode={startNode.GetHashCode()})");
        Debug.Log($"  END terminal → Node {endNode.Id} (HashCode={endNode.GetHashCode()})");
        Debug.Log($"  {component1.name}: NodeA={comp1NodeA.Id} (Hash={comp1NodeA.GetHashCode()}), NodeB={comp1NodeB.Id} (Hash={comp1NodeB.GetHashCode()})");
        Debug.Log($"  {component2.name}: NodeA={comp2NodeA.Id} (Hash={comp2NodeA.GetHashCode()}), NodeB={comp2NodeB.Id} (Hash={comp2NodeB.GetHashCode()})");

        // Determine correct orientation based on node connections
        // IDEAL: startTerminal connects to comp1's OUTPUT (NodeB), endTerminal connects to comp2's INPUT (NodeA)

        bool startAtComp1Output = (startNode == comp1NodeB);
        bool startAtComp1Input = (startNode == comp1NodeA);
        bool endAtComp2Input = (endNode == comp2NodeA);
        bool endAtComp2Output = (endNode == comp2NodeB);

        if (startAtComp1Output && endAtComp2Input)
        {
            // PERFECT: Wire flows from comp1's OUTPUT to comp2's INPUT
            needsSwap = false;
            reason = $"✓ Correct: START at {component1.name}.OUTPUT → END at {component2.name}.INPUT";
        }
        else if (startAtComp1Input && endAtComp2Output)
        {
            // BACKWARDS: Wire flows from comp1's INPUT to comp2's OUTPUT (wrong direction!)
            needsSwap = true;
            reason = $"⚠️ SWAPPING! Wire is backwards: START at {component1.name}.INPUT → END at {component2.name}.OUTPUT";
        }
        else
        {
            // Ambiguous or mixed connection - try checking from the other direction
            bool endAtComp1Output = (endNode == comp1NodeB);
            bool endAtComp1Input = (endNode == comp1NodeA);
            bool startAtComp2Input = (startNode == comp2NodeA);
            bool startAtComp2Output = (startNode == comp2NodeB);

            if (endAtComp1Output && startAtComp2Input)
            {
                // Wire is BACKWARDS: comp1.OUTPUT is at END, comp2.INPUT is at START
                needsSwap = true;
                reason = $"⚠️ SWAPPING! Wire is backwards: END at {component1.name}.OUTPUT ← START at {component2.name}.INPUT";
            }
            else if (endAtComp1Input && startAtComp2Output)
            {
                // Current orientation is correct: comp1.INPUT at END, comp2.OUTPUT at START
                needsSwap = false;
                reason = $"✓ Correct: END at {component1.name}.INPUT ← START at {component2.name}.OUTPUT";
            }
            else
            {
                // Cannot determine orientation - nodes don't match expected pattern
                Debug.LogWarning($"  ⚠️ Cannot determine orientation for {name}: Node connections don't match expected patterns");
                Debug.LogWarning($"    startAtComp1Out={startAtComp1Output}, startAtComp1In={startAtComp1Input}");
                Debug.LogWarning($"    endAtComp2In={endAtComp2Input}, endAtComp2Out={endAtComp2Output}");
                Debug.LogWarning($"    endAtComp1Out={endAtComp1Output}, endAtComp1In={endAtComp1Input}");
                Debug.LogWarning($"    startAtComp2In={startAtComp2Input}, startAtComp2Out={startAtComp2Output}");
                return; // Cannot orient, skip swap
            }
        }

        Debug.Log($"  {reason}");

        if (!needsSwap)
        {
            Debug.Log($"  ✅ Wire orientation is CORRECT, no changes needed");
            return; // Already correct, no swap needed
        }

        // If we get here, needsSwap is true, so perform the swap
        Debug.Log($"  🔄 Performing wire endpoint swap...");

        // Swap components
        var tempComp = component1;
        component1 = component2;
        component2 = tempComp;

        // Swap terminals
        var tempTerm = startTerminal;
        startTerminal = endTerminal;
        endTerminal = tempTerm;

        // Swap endpoints
        var tempEnd = startEndpoint;
        startEndpoint = endEndpoint;
        endEndpoint = tempEnd;

        // Swap visual components
        var tempVisual = startComponent;
        startComponent = endComponent;
        endComponent = tempVisual;

        // Update LineRenderer
        if (lineRenderer != null && lineRenderer.positionCount >= 2)
        {
            Vector3 pos0 = lineRenderer.GetPosition(0);
            Vector3 pos1 = lineRenderer.GetPosition(1);
            lineRenderer.SetPosition(0, pos1);
            lineRenderer.SetPosition(1, pos0);
        }

        Debug.Log($"  ✅ Wire endpoints swapped successfully!");
    }

    /// <summary>
    /// Calculate distance from a component to the nearest battery (breadth-first search)
    /// NOTE: This method is now DEPRECATED - we use connection-order-based flow direction instead
    /// </summary>
    int GetDistanceToBattery(CircuitComponent3D component)
    {
        if (component == null) return 999; // Infinite distance

        Debug.Log($"🔍 BFS START from {component.name} (looking for battery)");

        // BFS to find shortest path to battery
        var visited = new System.Collections.Generic.HashSet<CircuitComponent3D>();
        var queue = new System.Collections.Generic.Queue<(CircuitComponent3D comp, int dist)>();

        queue.Enqueue((component, 0));
        visited.Add(component);

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();
            Debug.Log($"  📍 Visiting {current.name} at distance {distance}, Type={current.ComponentType}");

            // Found a battery!
            if (current.ComponentType == ComponentType.Battery)
            {
                Debug.Log($"  ✅ Found battery {current.name} at distance {distance}!");
                return distance;
            }

            // Explore connected components through wires
            if (current.connectedWires != null)
            {
                Debug.Log($"  🔌 {current.name} has {current.connectedWires.Count} connected wires");
                foreach (var wireObj in current.connectedWires)
                {
                    // Check if wire GameObject still exists before accessing it
                    if (wireObj == null)
                    {
                        Debug.Log($"    ⚠️ Skipping null wire GameObject");
                        continue; // Skip destroyed wires
                    }

                    var wire = wireObj.GetComponent<CircuitWire>();
                    if (wire == null)
                    {
                        Debug.Log($"    ⚠️ Wire GameObject has no CircuitWire component");
                        continue;
                    }
                    if (wire == this)
                    {
                        Debug.Log($"    ⚠️ Skipping self-wire {wire.name}");
                        continue; // Skip this wire
                    }

                    Debug.Log($"    🔗 Checking wire {wire.name} (comp1={wire.component1?.name}, comp2={wire.component2?.name})");

                    // Get the other component on this wire
                    CircuitComponent3D nextComp = null;
                    if (wire.component1 == current) nextComp = wire.component2;
                    else if (wire.component2 == current) nextComp = wire.component1;

                    if (nextComp != null && !visited.Contains(nextComp))
                    {
                        Debug.Log($"    ➡️ Found next component: {nextComp.name} (distance will be {distance + 1})");
                        visited.Add(nextComp);
                        queue.Enqueue((nextComp, distance + 1));
                    }
                    else if (nextComp == null)
                    {
                        Debug.Log($"    ⚠️ Could not find next component (current={current.name} not in wire's components)");
                    }
                    else if (visited.Contains(nextComp))
                    {
                        Debug.Log($"    ⏭️ Already visited {nextComp.name}");
                    }
                }
            }
            else
            {
                Debug.Log($"  ⚠️ {current.name} has NULL connectedWires list!");
            }
        }

        Debug.Log($"❌ BFS FAILED: No battery found from {component.name} (returning 999)");
        return 999; // No battery found
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        CircuitEventManager.CircuitSolved -= OnCircuitSolvedHandler;

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