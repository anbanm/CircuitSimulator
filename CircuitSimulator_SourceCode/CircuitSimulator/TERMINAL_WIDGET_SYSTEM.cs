// WIDGET-BASED TERMINAL SYSTEM
// Add this to ComponentFactoryManager.cs to replace the current SetupConnectionTerminals method

using UnityEngine;
using System.Linq;

public partial class ComponentFactoryManager
{
    // Replace the existing SetupConnectionTerminals method with this:
    private void SetupConnectionTerminals_WidgetBased(GameObject componentObject)
    {
        CircuitComponent3D circuitComp = componentObject.GetComponent<CircuitComponent3D>();
        if (circuitComp == null) return;

        // First, look for pre-defined connection point widgets in the prefab
        Transform[] connectionWidgets = FindConnectionPointWidgets(componentObject);

        if (connectionWidgets != null && connectionWidgets.Length > 0)
        {
            Debug.Log($"Found {connectionWidgets.Length} pre-defined connection widgets on {componentObject.name}");

            // Use the existing widgets and enhance them with visual indicators
            foreach (var widget in connectionWidgets)
            {
                EnhanceConnectionWidget(widget, circuitComp.ComponentType);
            }
        }
        else
        {
            Debug.Log($"No connection widgets found on {componentObject.name}, using default terminal creation");
            // Fall back to the original default terminal creation
            CreateDefaultTerminals(componentObject, circuitComp);
        }
    }

    private Transform[] FindConnectionPointWidgets(GameObject componentObject)
    {
        // Look for child objects that are marked as connection points
        // Multiple strategies to find them:

        // Strategy 1: By name pattern
        var byName = componentObject.GetComponentsInChildren<Transform>()
            .Where(t => t != componentObject.transform &&
                       (t.name.Contains("ConnectionPoint") ||
                        t.name.Contains("Terminal") ||
                        t.name.Contains("ConnectPoint") ||
                        t.name.Contains("Wire_Anchor") ||
                        t.name.Contains("CP_")))
            .ToArray();

        if (byName.Length > 0)
            return byName;

        // Strategy 2: By tag (if you want to use tags)
        var byTag = componentObject.GetComponentsInChildren<Transform>()
            .Where(t => t.tag == "ConnectionPoint" || t.tag == "Terminal")
            .ToArray();

        if (byTag.Length > 0)
            return byTag;

        // Strategy 3: By component marker (if you add a marker component)
        var byMarker = componentObject.GetComponentsInChildren<ConnectionPointMarker>()
            .Select(m => m.transform)
            .ToArray();

        return byMarker;
    }

    private void EnhanceConnectionWidget(Transform widget, ComponentType componentType)
    {
        // Determine terminal properties based on widget name or position
        bool isPositive = DeterminePolarity(widget);
        Color terminalColor = isPositive ? Color.blue : Color.red;
        string label = DetermineLabel(widget, componentType, isPositive);

        // Check if visual indicator already exists (in case of prefab with visuals)
        Transform existingIndicator = widget.Find("TerminalIndicator");

        if (existingIndicator == null)
        {
            // Create visual indicator sphere
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "TerminalIndicator";
            indicator.transform.SetParent(widget);
            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = Vector3.one * 0.15f;

            // Set terminal appearance
            Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
            if (indicatorRenderer != null)
            {
                Material terminalMaterial = new Material(Shader.Find("Standard"));
                terminalMaterial.color = terminalColor;
                terminalMaterial.SetFloat("_Metallic", 0.8f);
                terminalMaterial.SetFloat("_Smoothness", 0.9f);
                indicatorRenderer.material = terminalMaterial;
            }

            // Remove collider from indicator
            Collider indicatorCollider = indicator.GetComponent<Collider>();
            if (indicatorCollider != null)
            {
                Destroy(indicatorCollider);
            }
        }
        else
        {
            // Just update the color of existing indicator
            Renderer renderer = existingIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = terminalColor;
            }
        }

        // Add or update label
        if (!string.IsNullOrEmpty(label))
        {
            CreateOrUpdateTerminalLabel(widget, label);
        }

        // Add connection metadata component
        ConnectionPointData data = widget.gameObject.GetComponent<ConnectionPointData>();
        if (data == null)
        {
            data = widget.gameObject.AddComponent<ConnectionPointData>();
        }
        data.isPositive = isPositive;
        data.label = label;
        data.terminalColor = terminalColor;

        Debug.Log($"Enhanced connection widget '{widget.name}' with {(isPositive ? "positive" : "negative")} terminal");
    }

    private bool DeterminePolarity(Transform widget)
    {
        string lowerName = widget.name.ToLower();

        // Check explicit naming
        if (lowerName.Contains("positive") || lowerName.Contains("plus") || lowerName.Contains("+"))
            return true;
        if (lowerName.Contains("negative") || lowerName.Contains("minus") || lowerName.Contains("-"))
            return false;

        // Check by position (right is typically positive)
        if (lowerName.Contains("right") || lowerName.Contains("output"))
            return true;
        if (lowerName.Contains("left") || lowerName.Contains("input"))
            return false;

        // Check by index
        if (lowerName.Contains("2") || lowerName.Contains("b"))
            return true;
        if (lowerName.Contains("1") || lowerName.Contains("a"))
            return false;

        // Default: determine by X position relative to parent
        return widget.localPosition.x > 0;
    }

    private string DetermineLabel(Transform widget, ComponentType componentType, bool isPositive)
    {
        // Check if widget name contains a specific label
        if (widget.name.Contains("_"))
        {
            string[] parts = widget.name.Split('_');
            if (parts.Length > 1 && parts[parts.Length - 1].Length <= 2)
            {
                return parts[parts.Length - 1]; // Use the suffix as label
            }
        }

        // Component-specific labels
        switch (componentType)
        {
            case ComponentType.Battery:
                return isPositive ? "+" : "-";
            case ComponentType.Bulb:
                return "~";
            case ComponentType.Switch:
                return isPositive ? "2" : "1";
            default:
                return ""; // No label for generic components
        }
    }

    private void CreateOrUpdateTerminalLabel(Transform widget, string labelText)
    {
        Transform existingLabel = widget.Find("TerminalLabel");
        TextMesh textMesh;

        if (existingLabel == null)
        {
            GameObject labelObj = new GameObject("TerminalLabel");
            labelObj.transform.SetParent(widget);
            labelObj.transform.localPosition = new Vector3(0, 0.3f, 0);
            textMesh = labelObj.AddComponent<TextMesh>();
        }
        else
        {
            textMesh = existingLabel.GetComponent<TextMesh>();
        }

        if (textMesh != null)
        {
            textMesh.text = labelText;
            textMesh.fontSize = 20;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.1f;
        }
    }
}

// Marker component to explicitly identify connection points
[System.Serializable]
public class ConnectionPointMarker : MonoBehaviour
{
    [Header("Connection Point Settings")]
    public bool isPositiveTerminal = false;
    public string customLabel = "";
    public Color terminalColor = Color.white;

    [Header("Visual Settings")]
    public bool showVisualIndicator = true;
    public float indicatorScale = 0.15f;
    public bool showLabel = true;

    [Header("Connection Settings")]
    public float connectionRadius = 0.5f;
    public bool allowMultipleConnections = false;
}

// Runtime data component for connection points
public class ConnectionPointData : MonoBehaviour
{
    public bool isPositive;
    public string label;
    public Color terminalColor;
    public List<GameObject> connectedWires = new List<GameObject>();

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public bool IsAvailable()
    {
        return connectedWires.Count == 0 || connectedWires.Any(w => w == null);
    }

    public void AddWire(GameObject wire)
    {
        if (!connectedWires.Contains(wire))
            connectedWires.Add(wire);
    }

    public void RemoveWire(GameObject wire)
    {
        connectedWires.Remove(wire);
    }
}