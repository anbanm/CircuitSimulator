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
    public float terminalSize = 0.5f;  // Increased to 0.5f for maximum visibility
    
    private CircuitComponent3D parentComponent;
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Material highlightMaterial;
    private bool isHighlighted = false;
    
    public CircuitComponent3D ParentComponent => parentComponent;
    public bool IsConnected => electricalNode != null && electricalNode.ConnectedComponents.Count > 1;

    /// <summary>
    /// Initialize terminal immediately after creation (don't wait for Start)
    /// CRITICAL: This must be called by ComponentTerminalManager to avoid timing issues
    /// </summary>
    public void InitializeTerminal(CircuitComponent3D parent)
    {
        parentComponent = parent;

        // Setup visual appearance
        SetupVisualAppearance();

        // Create electrical node IMMEDIATELY
        CreateElectricalNode();

        Debug.Log($"✅ Terminal initialized: {name} (Input: {isInput}), ElectricalNode: {electricalNode?.Id}");
    }

    void Start()
    {
        // Fallback: If not already initialized, initialize now
        if (parentComponent == null)
        {
            parentComponent = GetComponentInParent<CircuitComponent3D>();
            if (parentComponent == null)
            {
                Debug.LogError($"ComponentTerminal {name} must be child of CircuitComponent3D!");
                return;
            }

            SetupVisualAppearance();
            CreateElectricalNode();

            Debug.Log($"⚠️ Terminal late-initialized in Start(): {name} (Input: {isInput})");
        }
    }
    
    void SetupVisualAppearance()
    {
        Debug.Log($"🎨 Setting up terminal visual: {name}, Color: {terminalColor}, Size: {terminalSize}");

        // Create terminal geometry (small sphere)
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateSphereMesh();

        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Create materials with emission for better visibility
        originalMaterial = new Material(Shader.Find("Standard"));
        originalMaterial.color = terminalColor;
        originalMaterial.SetFloat("_Metallic", 0.8f);
        originalMaterial.SetFloat("_Glossiness", 0.9f);
        // Add subtle emission to make terminals always visible
        originalMaterial.EnableKeyword("_EMISSION");
        originalMaterial.SetColor("_EmissionColor", terminalColor * 0.3f);

        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = highlightColor;
        highlightMaterial.SetFloat("_Metallic", 0.8f);
        highlightMaterial.SetFloat("_Glossiness", 0.9f);
        // Stronger emission for highlight
        highlightMaterial.EnableKeyword("_EMISSION");
        highlightMaterial.SetColor("_EmissionColor", highlightColor * 0.7f);

        meshRenderer.material = originalMaterial;
        meshRenderer.enabled = true;  // Ensure renderer is enabled

        // Scale terminal
        transform.localScale = Vector3.one * terminalSize;

        Debug.Log($"✅ Terminal visual complete: {name}, World Position: {transform.position}, Renderer enabled: {meshRenderer.enabled}");
    }
    
    Mesh CreateSphereMesh()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = sphere.GetComponent<MeshFilter>().mesh;
        if (Application.isPlaying)
            Destroy(sphere);
        else
            DestroyImmediate(sphere);
        return mesh;
    }
    
    void CreateElectricalNode()
    {
        // Create unique node for this terminal
        string nodeId = $"{parentComponent.name}_{(isInput ? "Input" : "Output")}_{GetInstanceID()}";
        electricalNode = new CircuitNode(nodeId);

        Debug.Log($"🔌 CREATED ELECTRICAL NODE: {nodeId} (HashCode: {electricalNode.GetHashCode()})");
        Debug.Log($"   → Terminal: {name}, IsInput: {isInput}, Parent: {parentComponent?.name}");
    }
    
    public void SetHighlight(bool highlight)
    {
        if (meshRenderer == null) return;

        isHighlighted = highlight;
        meshRenderer.material = highlight ? highlightMaterial : originalMaterial;
    }

    /// <summary>
    /// Debug method to check terminal status
    /// </summary>
    public void LogTerminalStatus(string context = "")
    {
        Debug.Log($"🔍 TERMINAL STATUS ({context}): {name}");
        Debug.Log($"   → Parent: {parentComponent?.name ?? "NULL"}");
        Debug.Log($"   → IsInput: {isInput}");
        Debug.Log($"   → ElectricalNode: {electricalNode?.Id ?? "NULL"} (HashCode: {(electricalNode != null ? electricalNode.GetHashCode().ToString() : "N/A")})");
        Debug.Log($"   → Transform Position: {transform.position}");
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

        string myOriginalNodeId = electricalNode?.Id ?? "NULL";
        string otherOriginalNodeId = otherTerminal.electricalNode?.Id ?? "NULL";

        // PREVENT SELF-CONNECTION BUG
        if (this.parentComponent == otherTerminal.parentComponent)
        {
            Debug.LogError($"❌ PREVENTED SELF-CONNECTION: {parentComponent.name}.{name} ({myOriginalNodeId}) to {otherTerminal.name} ({otherOriginalNodeId})");
            Debug.LogError($"   Stack trace: {System.Environment.StackTrace}");
            return;
        }

        var sharedNode = electricalNode ?? otherTerminal.electricalNode ?? new CircuitNode($"Shared_{GetInstanceID()}");

        electricalNode = sharedNode;
        otherTerminal.electricalNode = sharedNode;

        Debug.Log($"🔗 NODE MERGE: {parentComponent.name}.{name} (was {myOriginalNodeId}) + {otherTerminal.parentComponent.name}.{otherTerminal.name} (was {otherOriginalNodeId}) → SHARED NODE: {sharedNode.Id}");

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
        if (originalMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(originalMaterial);
            else
                DestroyImmediate(originalMaterial);
        }
        if (highlightMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(highlightMaterial);
            else
                DestroyImmediate(highlightMaterial);
        }
    }
}