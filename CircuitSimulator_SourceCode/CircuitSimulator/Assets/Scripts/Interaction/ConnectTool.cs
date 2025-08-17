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
    
    void Start()
    {
        SetupButtons();
        SetSelectMode();
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
        _firstComponent = null;
        
        UpdateButtonColors();
        Debug.Log("Select Mode Active");
    }
    
    public void SetConnectMode()
    {
        _isConnectMode = true;
        _firstComponent = null;
        
        UpdateButtonColors();
        Debug.Log("Connect Mode Active - Click two components to connect them");
    }
    
    void UpdateButtonColors()
    {
        if (selectButton != null)
        {
            Image img = selectButton.GetComponent<Image>();
            img.color = _isConnectMode ? Color.white : Color.yellow;
        }
        
        if (connectButton != null)
        {
            Image img = connectButton.GetComponent<Image>();
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
        // ESC to cancel connection mode
        if (Input.GetKeyDown(KeyCode.Escape) && _isConnectMode)
        {
            if (_firstComponent != null)
            {
                _firstComponent.SetHighlight(false);
                _firstComponent = null;
                Debug.Log("Cancelled connection");
            }
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