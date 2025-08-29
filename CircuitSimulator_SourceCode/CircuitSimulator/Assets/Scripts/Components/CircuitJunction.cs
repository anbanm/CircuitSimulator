using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Visual junction component for parallel circuit branching
/// Provides clear connection points - NOT an electrical component
/// The spatial node system automatically handles electrical connectivity
/// </summary>
public class CircuitJunction : MonoBehaviour
{
    [Header("Junction Properties")]
    public Color junctionColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public float junctionSize = 0.3f;
    
    [Header("Connected Components")]
    public List<GameObject> connectedWires = new List<GameObject>();
    
    private Renderer junctionRenderer;
    private bool isHighlighted = false;
    
    void Start()
    {
        SetupVisual();
        RegisterWithManager();
        
        // Add interaction capabilities
        gameObject.tag = "CircuitComponent";
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // Add collider for selection
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = junctionSize * 2f; // Slightly larger for easier clicking
        }
        
        Debug.Log($"Junction created at {transform.position}");
    }
    
    void SetupVisual()
    {
        // Create visual representation if not already present
        junctionRenderer = GetComponent<Renderer>();
        if (junctionRenderer == null)
        {
            // If no mesh, create a sphere
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            junctionRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        
        // Set scale
        transform.localScale = Vector3.one * junctionSize;
        
        // Set color
        if (junctionRenderer.material == null)
        {
            junctionRenderer.material = new Material(Shader.Find("Standard"));
        }
        junctionRenderer.material.color = junctionColor;
        
        // Make it slightly emissive so it stands out
        junctionRenderer.material.EnableKeyword("_EMISSION");
        junctionRenderer.material.SetColor("_EmissionColor", junctionColor * 0.3f);
    }
    
    void RegisterWithManager()
    {
        // Junctions are visual-only aids - they don't register as electrical components
        // They help the spatial node system by providing clear connection points
        // The CircuitNodeManager will automatically create shared nodes for components near this junction
        Debug.Log($"Junction {name} created as visual connection aid at {transform.position}");
    }
    
    public void AddConnectedWire(GameObject wire)
    {
        if (!connectedWires.Contains(wire))
        {
            connectedWires.Add(wire);
            Debug.Log($"Junction now has {connectedWires.Count} connections");
        }
    }
    
    public void RemoveConnectedWire(GameObject wire)
    {
        if (connectedWires.Contains(wire))
        {
            connectedWires.Remove(wire);
            Debug.Log($"Junction now has {connectedWires.Count} connections");
        }
    }
    
    void OnMouseEnter()
    {
        if (!isHighlighted)
        {
            isHighlighted = true;
            if (junctionRenderer != null)
            {
                junctionRenderer.material.color = Color.yellow;
            }
        }
    }
    
    void OnMouseExit()
    {
        if (isHighlighted)
        {
            isHighlighted = false;
            if (junctionRenderer != null)
            {
                junctionRenderer.material.color = junctionColor;
            }
        }
    }
    
    void OnMouseDown()
    {
        // FIXED: Use existing SelectableComponent (should already exist from factory)
        SelectableComponent selectable = GetComponent<SelectableComponent>();
        if (selectable != null)
        {
            // Let SelectableComponent handle the connection logic
            // This will properly handle both connect mode and select mode
            selectable.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogError($"Junction {name} missing SelectableComponent - cannot be connected!");
        }
    }
    
    void OnDestroy()
    {
        // Clean up connected wires
        foreach (var wire in connectedWires)
        {
            if (wire != null)
            {
                Destroy(wire);
            }
        }
    }
    
    /// <summary>
    /// Check if this junction can be used as a connection point
    /// </summary>
    public bool CanConnect()
    {
        // Junctions can always accept more connections
        return true;
    }
    
    /// <summary>
    /// Get the effective position for wire connections
    /// This is where the spatial node system will create shared electrical nodes
    /// </summary>
    public Vector3 GetConnectionPoint()
    {
        return transform.position;
    }
    
    /// <summary>
    /// Check if a component is close enough to share this junction's electrical node
    /// Uses the same tolerance as the spatial node system (0.5f units)
    /// </summary>
    public bool IsComponentNearJunction(Vector3 componentPosition)
    {
        float nodeShareTolerance = 0.5f; // Same as CircuitNodeManager
        return Vector3.Distance(transform.position, componentPosition) <= nodeShareTolerance;
    }
    
    /// <summary>
    /// Visual feedback when components connect through this junction
    /// </summary>
    public void ShowConnectionFeedback(bool connected)
    {
        if (junctionRenderer != null)
        {
            Color feedbackColor = connected ? Color.green : junctionColor;
            junctionRenderer.material.color = feedbackColor;
            junctionRenderer.material.SetColor("_EmissionColor", feedbackColor * 0.5f);
        }
    }
}