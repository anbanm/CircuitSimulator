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
    
    public CircuitComponent3D Component1 => component1;
    public CircuitComponent3D Component2 => component2;
    public bool IsSelected => isSelected;
    
    public void Initialize(CircuitComponent3D comp1, CircuitComponent3D comp2)
    {
        component1 = comp1;
        component2 = comp2;
        
        SetupVisual();
        RegisterWithComponents();
        RegisterWithManager();
        
        name = $"Wire_{comp1.name}_to_{comp2.name}";
        Debug.Log($"Created circuit wire: {name}");
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
        
        UpdateWirePosition();
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
            manager = FindFirstObjectByType<CircuitManager>();
        }
        
        if (manager != null)
        {
            manager.RegisterWire(gameObject);
        }
    }
    
    void Update()
    {
        UpdateWirePosition();
        UpdateVisualFromCircuitData();
        HandleInput();
    }
    
    void UpdateWirePosition()
    {
        if (component1 != null && component2 != null && lineRenderer != null)
        {
            Vector3 pos1 = component1.transform.position + Vector3.up * 0.6f;
            Vector3 pos2 = component2.transform.position + Vector3.up * 0.6f;
            
            lineRenderer.SetPosition(0, pos1);
            lineRenderer.SetPosition(1, pos2);
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
            manager = FindFirstObjectByType<CircuitManager>();
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