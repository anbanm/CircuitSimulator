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
    
    private SelectableComponent _firstComponent = null;
    private ComponentTerminal _firstTerminal = null;
    private List<GameObject> _wires = new List<GameObject>();
    
    public static ConnectTool Instance { get; private set; }
    public Mode CurrentMode => _currentMode;
    
    // Wire preview system
    private GameObject _wirePreview = null;
    private LineRenderer _previewLineRenderer = null;
    private Camera _mainCamera;
    
    void Start()
    {
        Instance = this;
        _mainCamera = Camera.main;
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
        
        // Clear any existing selection
        if (_firstComponent != null)
        {
            _firstComponent.SetHighlight(false);
            _firstComponent = null;
        }
        
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
        _firstComponent = null;
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
        
        // Get the CircuitComponent3D from the SelectableComponent
        CircuitComponent3D circuitComp = component.GetComponent<CircuitComponent3D>();
        if (circuitComp == null)
        {
            Debug.LogWarning("Component doesn't have CircuitComponent3D!");
            return;
        }
        
        if (_firstComponent == null)
        {
            // First component selected
            _firstComponent = component;
            component.SetHighlight(true);
            Debug.Log($"First component selected: {component.name}");
        }
        else if (_firstComponent == component)
        {
            // Clicked same component - deselect
            _firstComponent.SetHighlight(false);
            _firstComponent = null;
            Debug.Log("Deselected component");
        }
        else
        {
            // Second component selected - create wire
            CircuitComponent3D firstCircuitComp = _firstComponent.GetComponent<CircuitComponent3D>();
            CreateCircuitWire(firstCircuitComp, circuitComp);
            _firstComponent.SetHighlight(false);
            _firstComponent = null;
            
            // Hide wire preview after connection is made
            HideWirePreview();
        }
    }
    
    public void HandleTerminalClick(ComponentTerminal terminal)
    {
        if (_currentMode != Mode.Connect) return;
        
        if (_firstTerminal == null)
        {
            // First terminal selected
            _firstTerminal = terminal;
            terminal.SetHighlight(true);
            Debug.Log($"First terminal selected: {terminal.name}");
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
            var terminalManager = FindObjectOfType<ComponentTerminalManager>();
            if (terminalManager != null && terminalManager.CanConnectTerminals(_firstTerminal, terminal))
            {
                // Create terminal-to-terminal wire
                CreateTerminalWire(_firstTerminal, terminal);
                _firstTerminal.SetHighlight(false);
                _firstTerminal = null;
                HideWirePreview();
                Debug.Log($"Connected {_firstTerminal?.name} to {terminal.name}");
            }
            else
            {
                Debug.LogWarning("Cannot connect these terminals (same component or wrong type)");
            }
        }
    }
    
    void CreateCircuitWire(CircuitComponent3D comp1, CircuitComponent3D comp2)
    {
        // Create wire GameObject
        GameObject wireObj = new GameObject($"Wire_{comp1.name}_to_{comp2.name}");
        wireObj.transform.SetParent(transform);
        
        // Add LineRenderer
        LineRenderer line = wireObj.AddComponent<LineRenderer>();
        
        // Add CircuitWire component
        CircuitWire circuitWire = wireObj.AddComponent<CircuitWire>();
        circuitWire.Initialize(comp1, comp2);
        
        // Store wire reference
        _wires.Add(wireObj);
        
        Debug.Log($"Created circuit wire between {comp1.name} and {comp2.name}");
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
        
        // Initialize the wire with terminal connection
        circuitWire.InitializeWithTerminals(terminal1, terminal2);
        
        // Connect terminals electrically
        terminal1.ConnectToTerminal(terminal2, circuitWire);
        
        // Store wire reference
        _wires.Add(wireObj);
        
        Debug.Log($"Created terminal wire between {terminal1.name} and {terminal2.name}");
    }
    
    Material CreateWireMaterial()
    {
        // Create simple unlit material for wires
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = wireColor;
        return mat;
    }
    
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
        
        // ESC to cancel connection mode
        if (Input.GetKeyDown(KeyCode.Escape) && _currentMode == Mode.Connect)
        {
            if (_firstComponent != null)
            {
                _firstComponent.SetHighlight(false);
                _firstComponent = null;
                HideWirePreview();
                Debug.Log("Cancelled component connection");
            }
            if (_firstTerminal != null)
            {
                _firstTerminal.SetHighlight(false);
                _firstTerminal = null;
                HideWirePreview();
                Debug.Log("Cancelled terminal connection");
            }
        }
        
        // Update wire preview when in connect mode with first component or terminal selected
        if (_currentMode == Mode.Connect && (_firstComponent != null || _firstTerminal != null))
        {
            UpdateWirePreview();
        }
    }
    
    void UpdateWirePreview()
    {
        if (_previewLineRenderer == null || _mainCamera == null)
            return;
            
        if (_firstComponent == null && _firstTerminal == null)
            return;
        
        // Show preview if hidden
        if (!_wirePreview.activeInHierarchy)
        {
            _wirePreview.SetActive(true);
        }
        
        // Start position: first component or terminal
        Vector3 startPos;
        if (_firstTerminal != null)
        {
            startPos = _firstTerminal.GetConnectionPoint();
        }
        else if (_firstComponent != null)
        {
            startPos = _firstComponent.transform.position + Vector3.up * 0.6f;
        }
        else
        {
            return;
        }
        
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