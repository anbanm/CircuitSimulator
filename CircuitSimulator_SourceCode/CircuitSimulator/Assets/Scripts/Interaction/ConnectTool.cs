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
    
    private bool _isConnectMode = false;
    private SelectableComponent _firstComponent = null;
    private List<GameObject> _wires = new List<GameObject>();
    
    // Wire preview system
    private GameObject _wirePreview = null;
    private LineRenderer _previewLineRenderer = null;
    private Camera _mainCamera;
    
    void Start()
    {
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
        _isConnectMode = false;
        
        // Clear any existing selection
        if (_firstComponent != null)
        {
            _firstComponent.SetHighlight(false);
            _firstComponent = null;
        }
        
        // Hide wire preview
        HideWirePreview();
        
        UpdateButtonColors();
        UpdateCursor(false);
        Debug.Log("[SELECT MODE] Click to select and move components");
    }
    
    public void SetConnectMode()
    {
        _isConnectMode = true;
        _firstComponent = null;
        
        UpdateButtonColors();
        UpdateCursor(true);
        Debug.Log("[CONNECT MODE] Click two components to connect them");
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
                img.color = _isConnectMode ? Color.white : Color.yellow;
        }
        
        if (connectButton != null)
        {
            Image img = connectButton.GetComponent<Image>();
            if (img != null)
                img.color = _isConnectMode ? Color.green : Color.white;
        }
    }
    
    public void OnComponentClicked(SelectableComponent component)
    {
        if (!_isConnectMode) return;
        
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
        if (Input.GetKeyDown(KeyCode.Escape) && _isConnectMode)
        {
            if (_firstComponent != null)
            {
                _firstComponent.SetHighlight(false);
                _firstComponent = null;
                HideWirePreview();
                Debug.Log("Cancelled connection");
            }
        }
        
        // Update wire preview when in connect mode with first component selected
        if (_isConnectMode && _firstComponent != null)
        {
            UpdateWirePreview();
        }
    }
    
    void UpdateWirePreview()
    {
        if (_previewLineRenderer == null || _mainCamera == null || _firstComponent == null)
            return;
        
        // Show preview if hidden
        if (!_wirePreview.activeInHierarchy)
        {
            _wirePreview.SetActive(true);
        }
        
        // Start position: first component
        Vector3 startPos = _firstComponent.transform.position + Vector3.up * 0.6f;
        
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
        return _isConnectMode;
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