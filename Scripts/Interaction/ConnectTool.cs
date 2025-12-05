using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ConnectTool : MonoBehaviour
{
    [Header("Tool Buttons")]
    public Button selectButton;
    public Button connectButton;
    
    [Header("Wire Settings")]
    public Color wireColor = Color.blue;
    public float wireWidth = 0.1f;
    
    public enum Mode { Select, Connect }
    private Mode _currentMode = Mode.Select;
    
    // Terminal-to-terminal connection system (ONLY system used)
    private ComponentTerminal _firstTerminal = null;
    private List<GameObject> _wires = new List<GameObject>();
    
    public static ConnectTool Instance { get; private set; }
    public Mode CurrentMode => _currentMode;
    public bool IsInConnectMode => _currentMode == Mode.Connect;

    // Wire preview system
    private GameObject _wirePreview = null;
    private LineRenderer _previewLineRenderer = null;
    private Camera _mainCamera;
    private Transform _workspacePlane;
    private Transform _wiresContainer;
    private static int _wireCounter = 0; // Counter for unique wire names
    
    void Start()
    {
        // Singleton pattern with safety check
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ConnectTool instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("No MainCamera found for ConnectTool!");
            _mainCamera = FindFirstObjectByType<Camera>();
            if (_mainCamera == null)
            {
                Debug.LogError("No camera found in scene! ConnectTool requires a camera.");
                enabled = false;
                return;
            }
        }
        
        FindWorkspacePlane();
        SetupButtons();
        SetupWirePreview();
        SetSelectMode();
    }

    void FindWorkspacePlane()
    {
        // Try to find WorkspaceManager first (handles both AR and desktop)
        var workspaceManager = FindFirstObjectByType<WorkspaceManager>();
        if (workspaceManager != null && workspaceManager.WorkspacePlane != null)
        {
            _workspacePlane = workspaceManager.WorkspacePlane;

            // Create/find Wires container as child of workspace
            Transform existingWires = _workspacePlane.Find("Wires");
            if (existingWires != null)
            {
                _wiresContainer = existingWires;
            }
            else
            {
                GameObject wiresObj = new GameObject("Wires");
                wiresObj.transform.SetParent(_workspacePlane);
                wiresObj.transform.localPosition = Vector3.zero;
                wiresObj.transform.localRotation = Quaternion.identity;
                _wiresContainer = wiresObj.transform;
            }
            Debug.Log($"[ConnectTool] Using workspace-parented Wires container");
            return;
        }

        // Fallback: Try to find CircuitWorkspace
        GameObject workspace = GameObject.Find("CircuitWorkspace");
        if (workspace != null)
        {
            _workspacePlane = workspace.transform;
        }

        // Fallback wires container
        if (_wiresContainer == null)
        {
            GameObject wiresObj = GameObject.Find("Wires");
            if (wiresObj != null)
            {
                _wiresContainer = wiresObj.transform;
            }
            else
            {
                wiresObj = new GameObject("Wires");
                _wiresContainer = wiresObj.transform;
            }
        }
    }

    /// <summary>
    /// Lazy initialization of wires container - called when creating wires
    /// to handle race condition where ConnectTool.Start() runs before WorkspaceManager.Start()
    /// </summary>
    void EnsureWiresContainer()
    {
        // If we already have a container parented to workspace, we're good
        if (_wiresContainer != null && _workspacePlane != null && _wiresContainer.parent == _workspacePlane)
        {
            return;
        }

        // Try to find WorkspaceManager now (it should be ready if user is creating wires)
        var workspaceManager = FindFirstObjectByType<WorkspaceManager>();
        if (workspaceManager != null && workspaceManager.WorkspacePlane != null)
        {
            _workspacePlane = workspaceManager.WorkspacePlane;

            // Check if we have an existing Wires container that needs to be reparented
            if (_wiresContainer != null && _wiresContainer.parent != _workspacePlane)
            {
                Debug.Log($"[ConnectTool] Reparenting existing Wires container from {_wiresContainer.parent?.name ?? "ROOT"} to {_workspacePlane.name}");
                _wiresContainer.SetParent(_workspacePlane);
                _wiresContainer.localPosition = Vector3.zero;
                _wiresContainer.localRotation = Quaternion.identity;
                return;
            }

            // Check for orphaned Wires object that might exist from previous runs
            GameObject existingWiresObj = GameObject.Find("Wires");
            if (existingWiresObj != null && existingWiresObj.transform.parent != _workspacePlane)
            {
                Debug.Log($"[ConnectTool] Found orphaned Wires container, reparenting to workspace");
                existingWiresObj.transform.SetParent(_workspacePlane);
                existingWiresObj.transform.localPosition = Vector3.zero;
                existingWiresObj.transform.localRotation = Quaternion.identity;
                _wiresContainer = existingWiresObj.transform;
                return;
            }

            // Create/find Wires container as child of workspace
            Transform workspaceWires = _workspacePlane.Find("Wires");
            if (workspaceWires != null)
            {
                _wiresContainer = workspaceWires;
            }
            else
            {
                GameObject wiresObj = new GameObject("Wires");
                wiresObj.transform.SetParent(_workspacePlane);
                wiresObj.transform.localPosition = Vector3.zero;
                wiresObj.transform.localRotation = Quaternion.identity;
                _wiresContainer = wiresObj.transform;
            }
            Debug.Log($"[ConnectTool] EnsureWiresContainer: Using workspace-parented Wires container");
        }
        else if (_wiresContainer == null)
        {
            // Fallback: Create standalone container
            GameObject wiresObj = new GameObject("Wires");
            _wiresContainer = wiresObj.transform;
            Debug.LogWarning("[ConnectTool] EnsureWiresContainer: No workspace found, wires container not parented!");
        }
    }

    void SetupWirePreview()
    {
        // Create wire preview object
        _wirePreview = new GameObject("WirePreview");
        _wirePreview.transform.SetParent(transform);
        
        // Add LineRenderer for preview
        _previewLineRenderer = _wirePreview.AddComponent<LineRenderer>();
        _previewLineRenderer.material = CreateWireMaterial();
        _previewLineRenderer.startWidth = wireWidth;
        _previewLineRenderer.endWidth = wireWidth;
        _previewLineRenderer.positionCount = 2;
        _previewLineRenderer.useWorldSpace = true;
        
        // Make preview wire slightly transparent and different color
        Color previewColor = Color.cyan;
        previewColor.a = 0.7f;
        _previewLineRenderer.material.color = previewColor;
        
        // Start hidden
        _wirePreview.SetActive(false);
    }
    
    void SetupButtons()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(SetSelectMode);
        
        if (connectButton != null)
            connectButton.onClick.AddListener(SetConnectMode);
    }
    
    public void SetSelectMode()
    {
        _currentMode = Mode.Select;

        // Clear any existing terminal selection
        if (_firstTerminal != null)
        {
            _firstTerminal.SetHighlight(false);
            _firstTerminal = null;
        }

        // Hide wire preview
        HideWirePreview();
        
        UpdateButtonColors();
        UpdateCursor(false);
    }
    
    public void SetConnectMode()
    {
        _currentMode = Mode.Connect;
        _firstTerminal = null;

        UpdateButtonColors();
        UpdateCursor(true);
    }
    
    void UpdateCursor(bool isConnectMode)
    {
        // Visual feedback for mode change
        // In a real implementation, you could change the cursor sprite here
        // For now, we'll just use the wire preview color to indicate mode
        if (_previewLineRenderer != null)
        {
            Color previewColor = isConnectMode ? Color.cyan : Color.green;
            previewColor.a = 0.7f;
            _previewLineRenderer.material.color = previewColor;
        }
    }
    
    void UpdateButtonColors()
    {
        if (selectButton != null)
        {
            Image img = selectButton.GetComponent<Image>();
            if (img != null)
                img.color = (_currentMode == Mode.Select) ? Color.yellow : Color.white;
        }
        
        if (connectButton != null)
        {
            Image img = connectButton.GetComponent<Image>();
            if (img != null)
                img.color = (_currentMode == Mode.Connect) ? Color.green : Color.white;
        }
    }
    
    public void OnComponentClicked(SelectableComponent component)
    {
        if (_currentMode != Mode.Connect) return;

        // Find the nearest terminal to the mouse click position
        ComponentTerminal nearestTerminal = FindNearestTerminal(component.gameObject);

        if (nearestTerminal == null)
        {
            Debug.LogWarning($"Component {component.name} has no terminals! Ensure ComponentTerminalManager creates terminals.");
            return;
        }

        // Route to terminal-based connection
        HandleTerminalClick(nearestTerminal);
    }

    private ComponentTerminal FindNearestTerminal(GameObject componentObject)
    {
        // Get all terminals that are children of this component
        ComponentTerminal[] terminals = componentObject.GetComponentsInChildren<ComponentTerminal>();

        if (terminals.Length == 0)
            return null;

        // Find nearest terminal to mouse position
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        ComponentTerminal nearest = terminals[0];
        float minDistance = Vector3.Distance(mouseWorldPos, nearest.transform.position);

        foreach (var terminal in terminals)
        {
            float distance = Vector3.Distance(mouseWorldPos, terminal.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = terminal;
            }
        }

        return nearest;
    }

    
    public void HandleTerminalClick(ComponentTerminal terminal)
    {
        if (_currentMode != Mode.Connect) return;

        if (_firstTerminal == null)
        {
            // First terminal selected
            _firstTerminal = terminal;
            terminal.SetHighlight(true);

            // Show wire preview from this terminal
            ShowWirePreview(terminal.GetConnectionPoint());

        }
        else if (_firstTerminal == terminal)
        {
            // Clicked same terminal - deselect
            _firstTerminal.SetHighlight(false);
            _firstTerminal = null;
            HideWirePreview();
        }
        else
        {
            // Check if terminals can be connected
            var terminalManager = FindFirstObjectByType<ComponentTerminalManager>();
            if (terminalManager != null && terminalManager.CanConnectTerminals(_firstTerminal, terminal))
            {
                // Create terminal-to-terminal wire
                CreateTerminalWire(_firstTerminal, terminal);
                _firstTerminal.SetHighlight(false);
                _firstTerminal = null;
                HideWirePreview();
            }
            else
            {
                Debug.LogWarning("Cannot connect these terminals (same component or invalid connection)");
                // Still allow deselection
                _firstTerminal.SetHighlight(false);
                _firstTerminal = null;
                HideWirePreview();
            }
        }
    }
    
    
    void CreateTerminalWire(ComponentTerminal terminal1, ComponentTerminal terminal2)
    {
        // Ensure wires container exists (lazy initialization for AR timing)
        EnsureWiresContainer();

        // Create wire GameObject
        GameObject wireObj = new GameObject($"Wire_{terminal1.ParentComponent.name}_to_{terminal2.ParentComponent.name}");

        // Parent to wires container (follows workspace/marker in AR)
        if (_wiresContainer != null)
            wireObj.transform.SetParent(_wiresContainer);
        else
            wireObj.transform.SetParent(transform);

        // Add CircuitWire component and configure for terminal-to-terminal connection
        CircuitWire circuitWire = wireObj.AddComponent<CircuitWire>();
        circuitWire.startTerminal = terminal1;
        circuitWire.endTerminal = terminal2;

        // Initialize the wire with terminal connection
        circuitWire.InitializeWithTerminals(terminal1, terminal2);

        // Connect terminals electrically
        terminal1.ConnectToTerminal(terminal2, circuitWire);

        // Store wire reference
        _wires.Add(wireObj);
    }
    
    Material CreateWireMaterial()
    {
        // Create simple unlit material for wires
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = wireColor;
        return mat;
    }
    
    // Performance optimization for wire preview
    private float wirePreviewUpdateInterval = 0.02f; // 50 FPS for smooth preview
    private float nextWirePreviewUpdate = 0f;
    
    void Update()
    {
        // Keyboard controls for Unity 6 (no UI buttons needed)
        if (Input.GetKeyDown(KeyCode.C))
        {
            SetConnectMode();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            SetSelectMode();
        }

        // W key to create draggable wire
        if (Input.GetKeyDown(KeyCode.W))
        {
            CreateDraggableWire();
        }

        // ESC to cancel connection mode
        if (Input.GetKeyDown(KeyCode.Escape) && _currentMode == Mode.Connect)
        {
            if (_firstTerminal != null)
            {
                _firstTerminal.SetHighlight(false);
                _firstTerminal = null;
                HideWirePreview();
            }
        }

        // Update wire preview when in connect mode with first terminal selected
        // Throttle updates for performance
        if (_currentMode == Mode.Connect && _firstTerminal != null)
        {
            if (Time.time >= nextWirePreviewUpdate)
            {
                UpdateWirePreview();
                nextWirePreviewUpdate = Time.time + wirePreviewUpdateInterval;
            }
        }
    }
    
    void UpdateWirePreview()
    {
        if (_previewLineRenderer == null || _mainCamera == null)
            return;

        if (_firstTerminal == null)
            return;

        // Show preview if hidden
        if (!_wirePreview.activeInHierarchy)
        {
            _wirePreview.SetActive(true);
        }

        // Start position: first terminal
        Vector3 startPos = _firstTerminal.GetConnectionPoint();

        // End position: mouse cursor in world space
        Vector3 endPos = GetMouseWorldPosition();

        // Update line renderer
        _previewLineRenderer.SetPosition(0, startPos);
        _previewLineRenderer.SetPosition(1, endPos);
    }
    
    Vector3 GetMouseWorldPosition()
    {
        if (_mainCamera == null) return Vector3.zero;

        // Ensure workspace plane is available
        if (_workspacePlane == null)
        {
            var workspaceManager = FindFirstObjectByType<WorkspaceManager>();
            if (workspaceManager != null && workspaceManager.WorkspacePlane != null)
            {
                _workspacePlane = workspaceManager.WorkspacePlane;
            }
        }

        // Cast ray from camera through mouse position to the workspace plane
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // Use the actual workspace plane if available (works for AR with tilted markers)
        Plane plane;
        if (_workspacePlane != null)
        {
            // Create plane using workspace transform's up direction
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
    
    void HideWirePreview()
    {
        if (_wirePreview != null)
        {
            _wirePreview.SetActive(false);
        }
    }
    
    public bool IsConnectMode()
    {
        return _currentMode == Mode.Connect;
    }


    private void ShowWirePreview(Vector3 startPosition)
    {
        if (_wirePreview != null)
        {
            _wirePreview.SetActive(true);
            if (_previewLineRenderer != null)
            {
                _previewLineRenderer.SetPosition(0, startPosition);
                _previewLineRenderer.SetPosition(1, startPosition); // Will be updated in UpdateWirePreview
            }
        }
    }

    void CreateDraggableWire()
    {
        // Use LOCAL position approach like components (simpler, works for AR)
        CreateDraggableWireAtLocalPosition(Vector3.zero);
    }

    /// <summary>
    /// Create draggable wire at a LOCAL position on the workspace (like components).
    /// Uses TransformPoint to convert to world space - simple and reliable for AR.
    /// </summary>
    public void CreateDraggableWireAtLocalPosition(Vector3 localCenter)
    {
        // Ensure wires container exists (lazy initialization for AR timing)
        EnsureWiresContainer();

        // Ensure workspace plane is available
        if (_workspacePlane == null)
        {
            var workspaceManager = FindFirstObjectByType<WorkspaceManager>();
            if (workspaceManager != null && workspaceManager.WorkspacePlane != null)
            {
                _workspacePlane = workspaceManager.WorkspacePlane;
            }
        }

        Vector3 startPos, endPos;

        if (_workspacePlane != null)
        {
            // SAME APPROACH AS COMPONENTS: Use local position with TransformPoint
            // Wire endpoints at Y=0.5 (same as components), offset in local X
            Vector3 localStart = new Vector3(localCenter.x - 0.5f, 0.5f, localCenter.z);
            Vector3 localEnd = new Vector3(localCenter.x + 0.5f, 0.5f, localCenter.z);

            startPos = _workspacePlane.TransformPoint(localStart);
            endPos = _workspacePlane.TransformPoint(localEnd);

            Debug.Log($"[ConnectTool] Wire spawn: localStart={localStart} → worldStart={startPos}");
        }
        else
        {
            // Fallback: Use world coordinates at Y=0.5
            startPos = new Vector3(localCenter.x - 0.5f, 0.5f, localCenter.z);
            endPos = new Vector3(localCenter.x + 0.5f, 0.5f, localCenter.z);
            Debug.LogWarning("[ConnectTool] No workspace plane, using world coordinates for wire");
        }

        // Create wire GameObject with unique name
        _wireCounter++;
        GameObject wireObj = new GameObject($"Draggable_Wire_{_wireCounter}");

        // Parent to wires container (follows workspace/marker in AR)
        if (_wiresContainer != null)
            wireObj.transform.SetParent(_wiresContainer);
        else
            wireObj.transform.SetParent(transform);

        // Add CircuitWire component and initialize with draggable endpoints
        CircuitWire circuitWire = wireObj.AddComponent<CircuitWire>();
        circuitWire.InitializeWithEndpoints(startPos, endPos);

        // Store wire reference
        _wires.Add(wireObj);
    }

    /// <summary>
    /// Legacy method for backwards compatibility - converts world position to local
    /// </summary>
    public void CreateDraggableWireAtPosition(Vector3 centerPos)
    {
        // Convert world position to local if workspace is available
        Vector3 localCenter = Vector3.zero;

        if (_workspacePlane != null)
        {
            localCenter = _workspacePlane.InverseTransformPoint(centerPos);
            localCenter.y = 0; // Ignore Y since we use 0.5 for all components
        }

        CreateDraggableWireAtLocalPosition(localCenter);
    }
}

// Simple Wire3D component to track connections
public class Wire3D : MonoBehaviour
{
    private SelectableComponent _component1;
    private SelectableComponent _component2;
    private LineRenderer _lineRenderer;
    
    public void Initialize(SelectableComponent comp1, SelectableComponent comp2, LineRenderer line)
    {
        _component1 = comp1;
        _component2 = comp2;
        _lineRenderer = line;
    }
    
    void Update()
    {
        // Update wire positions if components move
        if (_component1 != null && _component2 != null && _lineRenderer != null)
        {
            Vector3 pos1 = _component1.transform.position + Vector3.up * 0.6f;
            Vector3 pos2 = _component2.transform.position + Vector3.up * 0.6f;
            
            _lineRenderer.SetPosition(0, pos1);
            _lineRenderer.SetPosition(1, pos2);
        }
    }
    
    void OnDestroy()
    {
        // Clean up if needed
    }
}