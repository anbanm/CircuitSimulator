// POLARITY-BASED TERMINAL SYSTEM (Educationally Correct)
// Uses proper electrical terminology: positive/negative, anode/cathode, etc.

using UnityEngine;
using System.Linq;

public partial class ComponentFactoryManager
{
    // Replace the existing SetupConnectionTerminals method with this:
    private void SetupConnectionTerminals_PolarityBased(GameObject componentObject)
    {
        CircuitComponent3D circuitComp = componentObject.GetComponent<CircuitComponent3D>();
        if (circuitComp == null) return;

        // Look for pre-defined connection point widgets in the prefab
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
            Debug.Log($"No connection widgets found on {componentObject.name}, creating default terminals");
            // Fall back to automatic terminal creation with proper electrical positions
            CreateElectricallyCorrectTerminals(componentObject, circuitComp);
        }
    }

    private Transform[] FindConnectionPointWidgets(GameObject componentObject)
    {
        // Look for child objects marked as connection points using ELECTRICAL terminology

        // Strategy 1: By electrical naming convention (PREFERRED)
        var byElectricalName = componentObject.GetComponentsInChildren<Transform>()
            .Where(t => t != componentObject.transform &&
                       (t.name.Contains("Positive") ||
                        t.name.Contains("Negative") ||
                        t.name.Contains("Anode") ||
                        t.name.Contains("Cathode") ||
                        t.name.Contains("Plus") ||
                        t.name.Contains("Minus") ||
                        t.name.Contains("Terminal") ||
                        t.name.Contains("ConnectionPoint") ||
                        t.name.Contains("CP_")))
            .OrderBy(t => t.name.Contains("Negative") || t.name.Contains("Cathode") ? 0 : 1) // Negative first
            .ToArray();

        if (byElectricalName.Length > 0)
            return byElectricalName;

        // Strategy 2: By tag
        var byTag = componentObject.GetComponentsInChildren<Transform>()
            .Where(t => t.tag == "ConnectionPoint" || t.tag == "Terminal")
            .ToArray();

        if (byTag.Length > 0)
            return byTag;

        // Strategy 3: By component marker
        var byMarker = componentObject.GetComponentsInChildren<ConnectionPointMarker>()
            .Select(m => m.transform)
            .ToArray();

        return byMarker;
    }

    private void EnhanceConnectionWidget(Transform widget, ComponentType componentType)
    {
        // Determine electrical properties based on widget name
        ElectricalPolarity polarity = DetermineElectricalPolarity(widget, componentType);
        Color terminalColor = GetPolarityColor(polarity);
        string label = GetPolarityLabel(polarity, componentType);

        // Check if visual indicator already exists
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
            // Update existing indicator color
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
        data.polarity = polarity;
        data.label = label;
        data.terminalColor = terminalColor;
        data.componentType = componentType;

        Debug.Log($"Enhanced connection widget '{widget.name}' as {polarity} terminal for {componentType}");
    }

    private ElectricalPolarity DetermineElectricalPolarity(Transform widget, ComponentType componentType)
    {
        string lowerName = widget.name.ToLower();

        // Check explicit electrical naming
        if (lowerName.Contains("positive") || lowerName.Contains("plus") || lowerName.Contains("+"))
            return ElectricalPolarity.Positive;
        if (lowerName.Contains("negative") || lowerName.Contains("minus") || lowerName.Contains("-"))
            return ElectricalPolarity.Negative;

        // LED/Diode specific
        if (lowerName.Contains("anode"))
            return ElectricalPolarity.Anode;
        if (lowerName.Contains("cathode"))
            return ElectricalPolarity.Cathode;

        // AC components
        if (lowerName.Contains("neutral"))
            return ElectricalPolarity.Neutral;
        if (lowerName.Contains("live") || lowerName.Contains("hot"))
            return ElectricalPolarity.Live;
        if (lowerName.Contains("ground") || lowerName.Contains("earth"))
            return ElectricalPolarity.Ground;

        // Component-specific defaults
        switch (componentType)
        {
            case ComponentType.Battery:
                // For batteries, if not specified, determine by position
                // Convention: positive terminal is typically on the right in schematics
                return widget.localPosition.x > 0 ? ElectricalPolarity.Positive : ElectricalPolarity.Negative;

            case ComponentType.Bulb:
            case ComponentType.Resistor:
                // Non-polarized components
                return ElectricalPolarity.Neutral;

            default:
                // Default to neutral for ambiguous cases
                return ElectricalPolarity.Neutral;
        }
    }

    private Color GetPolarityColor(ElectricalPolarity polarity)
    {
        switch (polarity)
        {
            case ElectricalPolarity.Positive:
            case ElectricalPolarity.Anode:
            case ElectricalPolarity.Live:
                return Color.red;  // Red for positive/hot

            case ElectricalPolarity.Negative:
            case ElectricalPolarity.Cathode:
                return Color.black; // Black for negative (educational standard)

            case ElectricalPolarity.Ground:
                return Color.green; // Green for ground

            case ElectricalPolarity.Neutral:
            default:
                return Color.gray;  // Gray for neutral/non-polarized
        }
    }

    private string GetPolarityLabel(ElectricalPolarity polarity, ComponentType componentType)
    {
        // Component-specific labels following educational standards
        switch (componentType)
        {
            case ComponentType.Battery:
                switch (polarity)
                {
                    case ElectricalPolarity.Positive: return "+";
                    case ElectricalPolarity.Negative: return "−"; // Proper minus sign
                    default: return "";
                }

            case ComponentType.Bulb:
                // Bulbs are typically non-polarized in basic circuits
                return "~";

            case ComponentType.Switch:
                // Switch terminals are typically numbered
                return polarity == ElectricalPolarity.Positive ? "2" : "1";

            case ComponentType.Resistor:
                // Resistors are non-polarized
                return "";

            default:
                // Generic polarity labels
                switch (polarity)
                {
                    case ElectricalPolarity.Positive: return "+";
                    case ElectricalPolarity.Negative: return "−";
                    case ElectricalPolarity.Ground: return "⏚"; // Ground symbol
                    default: return "";
                }
        }
    }

    private void CreateElectricallyCorrectTerminals(GameObject componentObject, CircuitComponent3D circuitComp)
    {
        // Create terminals with proper electrical positioning
        // Educational note: Position doesn't determine polarity, but we follow schematic conventions

        Renderer renderer = componentObject.GetComponent<Renderer>();
        if (renderer == null) return;

        Bounds bounds = renderer.bounds;
        Vector3 localBounds = bounds.size;

        switch (circuitComp.ComponentType)
        {
            case ComponentType.Battery:
                // Battery: Clear polarity marking is essential for education
                // Convention: Longer line = positive, shorter line = negative in schematics
                CreateTerminal("Terminal_Negative", componentObject.transform,
                    new Vector3(-localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Negative);

                CreateTerminal("Terminal_Positive", componentObject.transform,
                    new Vector3(localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Positive);
                break;

            case ComponentType.Resistor:
                // Resistors are non-polarized but we still need connection points
                CreateTerminal("Terminal_1", componentObject.transform,
                    new Vector3(-localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Neutral);

                CreateTerminal("Terminal_2", componentObject.transform,
                    new Vector3(localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Neutral);
                break;

            case ComponentType.Bulb:
                // Bulbs in basic circuits are typically non-polarized
                // But we can show them as having terminals for connection
                CreateTerminal("Terminal_1", componentObject.transform,
                    new Vector3(-localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Neutral);

                CreateTerminal("Terminal_2", componentObject.transform,
                    new Vector3(localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Neutral);
                break;

            case ComponentType.Switch:
                // Switch terminals - typically numbered
                CreateTerminal("Terminal_Input", componentObject.transform,
                    new Vector3(-localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Neutral);

                CreateTerminal("Terminal_Output", componentObject.transform,
                    new Vector3(localBounds.x * 0.5f, 0, 0),
                    ElectricalPolarity.Neutral);
                break;
        }

        Debug.Log($"Created electrically correct terminals for {componentObject.name} ({circuitComp.ComponentType})");
    }

    private GameObject CreateTerminal(string terminalName, Transform parent, Vector3 localPosition, ElectricalPolarity polarity)
    {
        // Create terminal with proper electrical identification
        GameObject terminal = new GameObject(terminalName);
        terminal.transform.SetParent(parent);
        terminal.transform.localPosition = localPosition;

        // Visual indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "TerminalIndicator";
        indicator.transform.SetParent(terminal.transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = Vector3.one * 0.15f;

        // Set appearance based on polarity
        Color terminalColor = GetPolarityColor(polarity);
        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
        if (indicatorRenderer != null)
        {
            Material terminalMaterial = new Material(Shader.Find("Standard"));
            terminalMaterial.color = terminalColor;
            terminalMaterial.SetFloat("_Metallic", 0.8f);
            terminalMaterial.SetFloat("_Smoothness", 0.9f);
            indicatorRenderer.material = terminalMaterial;
        }

        // Remove collider
        Collider indicatorCollider = indicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            Destroy(indicatorCollider);
        }

        // Add label
        ComponentType componentType = parent.GetComponent<CircuitComponent3D>().ComponentType;
        string label = GetPolarityLabel(polarity, componentType);
        if (!string.IsNullOrEmpty(label))
        {
            CreateOrUpdateTerminalLabel(terminal, label);
        }

        // Add metadata
        ConnectionPointData data = terminal.AddComponent<ConnectionPointData>();
        data.polarity = polarity;
        data.label = label;
        data.terminalColor = terminalColor;

        return terminal;
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

// Electrical polarity enum for proper terminology
public enum ElectricalPolarity
{
    Positive,    // Battery positive, DC positive
    Negative,    // Battery negative, DC negative
    Anode,       // LED/Diode anode (positive side)
    Cathode,     // LED/Diode cathode (negative side)
    Neutral,     // Non-polarized (resistors, bulbs in basic circuits)
    Live,        // AC live/hot wire
    Ground,      // Earth ground
    Reference    // Reference point (0V)
}

// Enhanced connection point data with electrical properties
public class ConnectionPointData : MonoBehaviour
{
    [Header("Electrical Properties")]
    public ElectricalPolarity polarity = ElectricalPolarity.Neutral;
    public ComponentType componentType;
    public string label = "";
    public Color terminalColor = Color.gray;

    [Header("Connection Management")]
    public List<GameObject> connectedWires = new List<GameObject>();
    public float connectionRadius = 0.5f;
    public bool allowMultipleConnections = true;

    [Header("Educational")]
    public string educationalTooltip = "";
    public bool showCurrentFlow = false;
    public float currentMagnitude = 0f;
    public bool isCurrentSource = false;
    public bool isCurrentSink = false;

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public bool CanAcceptConnection()
    {
        if (!allowMultipleConnections && connectedWires.Count > 0)
            return false;

        // Educational: Check for common mistakes
        if (componentType == ComponentType.Battery && polarity == ElectricalPolarity.Positive)
        {
            // Positive terminal of battery should typically connect to circuit
            return true;
        }

        return true;
    }

    public void AddWire(GameObject wire)
    {
        if (!connectedWires.Contains(wire))
        {
            connectedWires.Add(wire);
            Debug.Log($"Connected wire to {polarity} terminal of {componentType}");
        }
    }

    public void RemoveWire(GameObject wire)
    {
        connectedWires.Remove(wire);
        Debug.Log($"Disconnected wire from {polarity} terminal of {componentType}");
    }

    public string GetEducationalDescription()
    {
        switch (polarity)
        {
            case ElectricalPolarity.Positive:
                return "Positive terminal - electrons flow FROM here in conventional current";
            case ElectricalPolarity.Negative:
                return "Negative terminal - electrons flow TO here in conventional current";
            case ElectricalPolarity.Anode:
                return "Anode - positive terminal of LED/diode, current enters here";
            case ElectricalPolarity.Cathode:
                return "Cathode - negative terminal of LED/diode, current exits here";
            case ElectricalPolarity.Ground:
                return "Ground - reference point (0V) for the circuit";
            default:
                return "Connection point for circuit component";
        }
    }
}