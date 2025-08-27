using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Junction/Node component that allows branching for parallel circuits
/// Makes it easy to create connection points where multiple wires meet
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
        // Junctions don't need to register as components
        // They just facilitate connections
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
        // Check if connect tool is active
        ConnectTool connectTool = FindFirstObjectByType<ConnectTool>();
        if (connectTool != null && connectTool.IsConnectMode())
        {
            // Treat junction like a component for connections
            SelectableComponent selectable = GetComponent<SelectableComponent>();
            if (selectable == null)
            {
                selectable = gameObject.AddComponent<SelectableComponent>();
            }
            connectTool.OnComponentClicked(selectable);
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
    /// </summary>
    public Vector3 GetConnectionPoint()
    {
        return transform.position;
    }
}