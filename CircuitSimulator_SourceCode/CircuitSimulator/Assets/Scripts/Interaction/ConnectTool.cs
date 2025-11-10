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
        
        SetupButtons();
        SetupWirePreview();
        SetSelectMode();
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
        Debug.Log("[SELECT MODE] Click to select and move components");
    }
    
    public void SetConnectMode()
    {
        _currentMode = Mode.Connect;
        _firstTerminal = null;

        UpdateButtonColors();
        UpdateCursor(true);
        Debug.Log("[CONNECT MODE] Click terminals to connect components");
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

            Debug.Log($"First terminal selected: {terminal.name} on {terminal.ParentComponent.name}");
        }
        else if (_firstTerminal == terminal)
        {
            // Clicked same terminal - deselect
            _firstTerminal.SetHighlight(false);
            _firstTerminal = null;
            HideWirePreview();
            Debug.Log("Deselected terminal");
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
                Debug.Log($"Connected {_firstTerminal.name} to {terminal.name}");
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
        // Create wire GameObject
        GameObject wireObj = new GameObject($"Wire_{terminal1.ParentComponent.name}_to_{terminal2.ParentComponent.name}");
        wireObj.transform.SetParent(transform);

        // Add CircuitWire component and configure for terminal-to-terminal connection
        CircuitWire circuitWire = wireObj.AddComponent<CircuitWire>();
        circuitWire.startTerminal = terminal1;
        circuitWire.endTerminal = terminal2;

        // CRITICAL DEBUG: Log terminal assignment order
        Debug.Log($"🔌 WIRE CREATED: Start={terminal1.name} (Input:{terminal1.isInput}), End={terminal2.name} (Input:{terminal2.isInput})");
        Debug.Log($"   Start Component: {terminal1.ParentComponent.name}, End Component: {terminal2.ParentComponent.name}");

        // Initialize the wire with terminal connection
        circuitWire.InitializeWithTerminals(terminal1, terminal2);

        // Connect terminals electrically
        terminal1.ConnectToTerminal(terminal2, circuitWire);

        // Store wire reference
        _wires.Add(wireObj);

        Debug.Log($"✅ Terminal wire created - animation should flow from START to END based on click order");
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
                Debug.Log("Cancelled terminal connection");
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
        
        // Cast ray from camera through mouse position to the workspace plane
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Create a plane at Y = 0.5 (where our components sit)
        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));
        
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
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
        // Get cursor position in world space
        Vector3 cursorPos = GetMouseWorldPosition();

        // Create wire slightly offset so both endpoints are visible
        Vector3 startPos = cursorPos + Vector3.left * 0.5f;
        Vector3 endPos = cursorPos + Vector3.right * 0.5f;

        // Create wire GameObject
        GameObject wireObj = new GameObject("Draggable_Wire");
        wireObj.transform.SetParent(transform);

        // Add CircuitWire component
        CircuitWire circuitWire = wireObj.AddComponent<CircuitWire>();

        // Initialize with draggable endpoints
        circuitWire.InitializeWithEndpoints(startPos, endPos);

        // Store wire reference
        _wires.Add(wireObj);

        Debug.Log($"[WIRE CREATED] Press W to create draggable wires. Drag endpoints to terminals to connect.");
        Debug.Log($"Created draggable wire at {cursorPos} - drag endpoints to connect to component terminals");
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