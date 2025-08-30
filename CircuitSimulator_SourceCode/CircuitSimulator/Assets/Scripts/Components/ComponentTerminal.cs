using UnityEngine;

/// <summary>
/// Represents an input or output terminal on a circuit component
/// Provides explicit connection points for wires
/// </summary>
public class ComponentTerminal : MonoBehaviour
{
    [Header("Terminal Configuration")]
    public bool isInput = true;
    public CircuitNode electricalNode;
    
    [Header("Visual Settings")]
    public Color terminalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public float terminalSize = 0.2f;
    
    private CircuitComponent3D parentComponent;
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Material highlightMaterial;
    private bool isHighlighted = false;
    
    public CircuitComponent3D ParentComponent => parentComponent;
    public bool IsConnected => electricalNode != null && electricalNode.ConnectedComponents.Count > 1;
    
    void Start()
    {
        // Get parent component
        parentComponent = GetComponentInParent<CircuitComponent3D>();
        if (parentComponent == null)
        {
            Debug.LogError($"ComponentTerminal {name} must be child of CircuitComponent3D!");
            return;
        }
        
        // Setup visual appearance
        SetupVisualAppearance();
        
        // Create electrical node
        CreateElectricalNode();
        
        Debug.Log($"Terminal created: {name} (Input: {isInput})");
    }
    
    void SetupVisualAppearance()
    {
        // Create terminal geometry (small sphere)
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateSphereMesh();
        
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        // Create materials
        originalMaterial = new Material(Shader.Find("Standard"));
        originalMaterial.color = terminalColor;
        originalMaterial.SetFloat("_Metallic", 0.8f);
        originalMaterial.SetFloat("_Glossiness", 0.9f);
        
        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = highlightColor;
        highlightMaterial.SetFloat("_Metallic", 0.8f);
        highlightMaterial.SetFloat("_Glossiness", 0.9f);
        highlightMaterial.EnableKeyword("_EMISSION");
        highlightMaterial.SetColor("_EmissionColor", highlightColor * 0.5f);
        
        meshRenderer.material = originalMaterial;
        
        // Scale terminal
        transform.localScale = Vector3.one * terminalSize;
    }
    
    Mesh CreateSphereMesh()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = sphere.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(sphere);
        return mesh;
    }
    
    void CreateElectricalNode()
    {
        // Create unique node for this terminal
        string nodeId = $"{parentComponent.name}_{(isInput ? "Input" : "Output")}_{GetInstanceID()}";
        electricalNode = new CircuitNode(nodeId);
        
        Debug.Log($"Created electrical node: {nodeId}");
    }
    
    public void SetHighlight(bool highlight)
    {
        if (meshRenderer == null) return;
        
        isHighlighted = highlight;
        meshRenderer.material = highlight ? highlightMaterial : originalMaterial;
    }
    
    /// <summary>
    /// Connect this terminal to another terminal via wire
    /// </summary>
    public void ConnectToTerminal(ComponentTerminal otherTerminal, CircuitWire wire)
    {
        if (otherTerminal == null || wire == null) return;
        
        // Connect the electrical nodes
        ConnectElectricalNodes(otherTerminal);
        
        // Store wire reference
        wire.startTerminal = this;
        wire.endTerminal = otherTerminal;
        
        Debug.Log($"Connected {name} to {otherTerminal.name} via wire");
    }
    
    void ConnectElectricalNodes(ComponentTerminal otherTerminal)
    {
        // For now, merge the nodes by having both terminals reference the same node
        // In a more sophisticated system, we might have explicit connections
        var sharedNode = electricalNode ?? otherTerminal.electricalNode ?? new CircuitNode($"Shared_{GetInstanceID()}");
        
        electricalNode = sharedNode;
        otherTerminal.electricalNode = sharedNode;
        
        // Add components to the shared node
        if (!sharedNode.ConnectedComponents.Contains(parentComponent.logicalComponent))
            sharedNode.ConnectedComponents.Add(parentComponent.logicalComponent);
            
        if (!sharedNode.ConnectedComponents.Contains(otherTerminal.parentComponent.logicalComponent))
            sharedNode.ConnectedComponents.Add(otherTerminal.parentComponent.logicalComponent);
    }
    
    void OnMouseEnter()
    {
        // Only highlight during Connect mode
        if (ConnectTool.Instance != null && ConnectTool.Instance.IsConnectMode())
        {
            SetHighlight(true);
        }
    }
    
    void OnMouseExit()
    {
        SetHighlight(false);
    }
    
    void OnMouseDown()
    {
        // Forward click to ConnectTool if in Connect mode
        if (ConnectTool.Instance != null && ConnectTool.Instance.IsConnectMode())
        {
            ConnectTool.Instance.HandleTerminalClick(this);
        }
    }
    
    public Vector3 GetConnectionPoint()
    {
        return transform.position;
    }
    
    void OnDestroy()
    {
        // Clean up materials
        if (originalMaterial != null) DestroyImmediate(originalMaterial);
        if (highlightMaterial != null) DestroyImmediate(highlightMaterial);
    }
}