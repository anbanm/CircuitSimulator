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
    
    // Terminal-based connections (new architecture)
    public ComponentTerminal startTerminal;
    public ComponentTerminal endTerminal;
    
    // Educational: Current flow visualization for Grade 7 students
    private CurrentFlowVisualizer currentFlowVisualizer;
    
    public CircuitComponent3D Component1 => component1;
    public CircuitComponent3D Component2 => component2;
    public bool IsSelected => isSelected;
    
    // For CurrentFlowVisualizer access
    public CircuitComponent3D startComponent => component1;
    public CircuitComponent3D endComponent => component2;
    
    public void Initialize(CircuitComponent3D comp1, CircuitComponent3D comp2)
    {
        component1 = comp1;
        component2 = comp2;
        
        SetupVisual();
        SetupCurrentFlowVisualization();
        RegisterWithComponents();
        RegisterWithManager();
        
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
        
        name = $"Wire_{terminal1.name}_to_{terminal2.name}";
        Debug.Log($"Created terminal-based circuit wire: {name}");
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
        if (manager == null)
        {
            manager = ComponentRegistry.Instance.GetManager<CircuitManager>();
        }
        
        if (manager != null)
        {
            manager.RegisterWire(gameObject);
        }
    }
    
    void Update()
    {
        UpdateWirePosition();
        UpdateCurrentFromComponents();
        UpdateVisualFromCircuitData();
        HandleInput();
    }
    
    void UpdateCurrentFromComponents()
    {
        // Get current from connected components for educational visualization
        // This ensures the current flow dots show the correct speed
        if (component1 != null && component2 != null)
        {
            // Use the current from the first component (should be same in series)
            float componentCurrent = component1.current;
            
            // Only update if there's a significant change
            if (Mathf.Abs(componentCurrent - current) > 0.001f)
            {
                current = componentCurrent;
                
                // Debug info for educational purposes
                if (Mathf.Abs(current) > 0.01f)
                {
                    Debug.Log($"Wire {name}: Current updated to {current:F3}A");
                }
            }
        }
    }
    
    void UpdateWirePosition()
    {
        if (lineRenderer == null) return;
        
        Vector3 pos1, pos2;
        
        // Use terminal positions if available (new architecture)
        if (startTerminal != null && endTerminal != null)
        {
            pos1 = startTerminal.GetConnectionPoint();
            pos2 = endTerminal.GetConnectionPoint();
        }
        // Fallback to component positions (legacy)
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
        SelectWire();
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
        
        // Unregister from components
        if (component1 != null) component1.RemoveConnectedWire(gameObject);
        if (component2 != null) component2.RemoveConnectedWire(gameObject);
        
        // Unregister from manager
        CircuitManager manager = CircuitManager.Instance;
        if (manager == null)
        {
            manager = ComponentRegistry.Instance.GetManager<CircuitManager>();
        }
        if (manager != null)
        {
            manager.UnregisterWire(gameObject);
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
        // Clean up any remaining references
        if (component1 != null) component1.RemoveConnectedWire(gameObject);
        if (component2 != null) component2.RemoveConnectedWire(gameObject);
    }
}