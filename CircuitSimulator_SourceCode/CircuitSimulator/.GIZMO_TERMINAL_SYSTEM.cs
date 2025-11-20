// GIZMO-BASED CONNECTION POINT SYSTEM
// Professional Unity Editor approach for marking connection points on prefabs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Component to add to prefabs for marking connection points with gizmos
/// Place this on your prefab root and configure connection points in Inspector
/// </summary>
public class ComponentConnectionPoints : MonoBehaviour
{
    [System.Serializable]
    public class ConnectionPoint
    {
        [Header("Position")]
        public Vector3 localPosition = Vector3.zero;

        [Header("Electrical Properties")]
        public ElectricalPolarity polarity = ElectricalPolarity.Neutral;
        public string label = "";

        [Header("Visual")]
        public Color gizmoColor = Color.red;
        public float gizmoSize = 0.2f;

        [Header("Runtime")]
        [Tooltip("Will be set automatically at runtime")]
        public Transform runtimeTerminal;
    }

    [Header("Connection Points")]
    [Tooltip("Define connection points with gizmos - these will become terminals at runtime")]
    public List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

    [Header("Gizmo Settings")]
    public bool showGizmosWhenSelected = true;
    public bool showGizmosAlways = false;
    public bool showLabels = true;

    // Called by ComponentFactoryManager when component is created
    public void CreateRuntimeTerminals()
    {
        if (connectionPoints == null || connectionPoints.Count == 0)
        {
            Debug.LogWarning($"No connection points defined on {gameObject.name}");
            return;
        }

        ComponentType componentType = GetComponent<CircuitComponent3D>()?.ComponentType ?? ComponentType.Resistor;

        foreach (var connectionPoint in connectionPoints)
        {
            // Create runtime terminal at gizmo position
            GameObject terminal = CreateRuntimeTerminal(connectionPoint, componentType);
            connectionPoint.runtimeTerminal = terminal.transform;
        }

        Debug.Log($"Created {connectionPoints.Count} runtime terminals for {gameObject.name}");
    }

    private GameObject CreateRuntimeTerminal(ConnectionPoint connectionPoint, ComponentType componentType)
    {
        // Create terminal GameObject
        string terminalName = string.IsNullOrEmpty(connectionPoint.label)
            ? $"Terminal_{connectionPoint.polarity}"
            : $"Terminal_{connectionPoint.label}";

        GameObject terminal = new GameObject(terminalName);
        terminal.transform.SetParent(transform);
        terminal.transform.localPosition = connectionPoint.localPosition;

        // Create visual indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "TerminalIndicator";
        indicator.transform.SetParent(terminal.transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = Vector3.one * 0.15f;

        // Set appearance based on polarity
        Color terminalColor = GetPolarityColor(connectionPoint.polarity);
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

        // Add text label if specified
        if (!string.IsNullOrEmpty(connectionPoint.label))
        {
            CreateTerminalLabel(terminal, connectionPoint.label);
        }

        // Add connection data component
        ConnectionPointData data = terminal.AddComponent<ConnectionPointData>();
        data.polarity = connectionPoint.polarity;
        data.label = connectionPoint.label;
        data.terminalColor = terminalColor;
        data.componentType = componentType;

        return terminal;
    }

    private Color GetPolarityColor(ElectricalPolarity polarity)
    {
        switch (polarity)
        {
            case ElectricalPolarity.Positive:
            case ElectricalPolarity.Anode:
                return Color.red;
            case ElectricalPolarity.Negative:
            case ElectricalPolarity.Cathode:
                return Color.black;
            case ElectricalPolarity.Ground:
                return Color.green;
            default:
                return Color.gray;
        }
    }

    private void CreateTerminalLabel(GameObject terminal, string labelText)
    {
        GameObject labelObj = new GameObject("TerminalLabel");
        labelObj.transform.SetParent(terminal.transform);
        labelObj.transform.localPosition = new Vector3(0, 0.3f, 0);

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = labelText;
        textMesh.fontSize = 20;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.1f;
    }

    // Gizmo drawing in Scene view
    void OnDrawGizmos()
    {
        if (!showGizmosAlways) return;
        DrawConnectionGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmosWhenSelected) return;
        DrawConnectionGizmos();
    }

    private void DrawConnectionGizmos()
    {
        if (connectionPoints == null) return;

        foreach (var connectionPoint in connectionPoints)
        {
            // Set gizmo color
            Gizmos.color = connectionPoint.gizmoColor;

            // Draw sphere at connection point
            Vector3 worldPos = transform.TransformPoint(connectionPoint.localPosition);
            Gizmos.DrawSphere(worldPos, connectionPoint.gizmoSize);

            // Draw wireframe for better visibility
            Gizmos.color = connectionPoint.gizmoColor * 0.5f;
            Gizmos.DrawWireSphere(worldPos, connectionPoint.gizmoSize);

            // Draw label in Scene view
            if (showLabels && !string.IsNullOrEmpty(connectionPoint.label))
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                Vector3 labelPos = worldPos + Vector3.up * (connectionPoint.gizmoSize + 0.3f);
                UnityEditor.Handles.Label(labelPos, connectionPoint.label);
                #endif
            }
        }
    }

    // Context menu for easy setup
    [ContextMenu("Add Positive Terminal")]
    void AddPositiveTerminal()
    {
        connectionPoints.Add(new ConnectionPoint
        {
            polarity = ElectricalPolarity.Positive,
            label = "+",
            gizmoColor = Color.red,
            localPosition = new Vector3(0.5f, 0, 0)
        });
    }

    [ContextMenu("Add Negative Terminal")]
    void AddNegativeTerminal()
    {
        connectionPoints.Add(new ConnectionPoint
        {
            polarity = ElectricalPolarity.Negative,
            label = "-",
            gizmoColor = Color.black,
            localPosition = new Vector3(-0.5f, 0, 0)
        });
    }

    [ContextMenu("Add Neutral Terminal")]
    void AddNeutralTerminal()
    {
        connectionPoints.Add(new ConnectionPoint
        {
            polarity = ElectricalPolarity.Neutral,
            label = "~",
            gizmoColor = Color.gray,
            localPosition = Vector3.zero
        });
    }

    [ContextMenu("Auto-Setup Battery Terminals")]
    void AutoSetupBattery()
    {
        connectionPoints.Clear();
        connectionPoints.Add(new ConnectionPoint
        {
            polarity = ElectricalPolarity.Negative,
            label = "-",
            gizmoColor = Color.black,
            localPosition = new Vector3(-0.75f, 0, 0)
        });
        connectionPoints.Add(new ConnectionPoint
        {
            polarity = ElectricalPolarity.Positive,
            label = "+",
            gizmoColor = Color.red,
            localPosition = new Vector3(0.75f, 0, 0)
        });
    }

    [ContextMenu("Auto-Setup Resistor Terminals")]
    void AutoSetupResistor()
    {
        connectionPoints.Clear();
        connectionPoints.Add(new ConnectionPoint
        {
            polarity = ElectricalPolarity.Neutral,
            label = "",
            gizmoColor = Color.gray,
            localPosition = new Vector3(-0.6f, 0, 0)
        });
        connectionPoints.Add(new ConnectionPoint
        {
            polarity = ElectricalPolarity.Neutral,
            label = "",
            gizmoColor = Color.gray,
            localPosition = new Vector3(0.6f, 0, 0)
        });
    }
}

// Updated ComponentFactoryManager integration
public partial class ComponentFactoryManager
{
    private void SetupConnectionTerminals_GizmoBased(GameObject componentObject)
    {
        CircuitComponent3D circuitComp = componentObject.GetComponent<CircuitComponent3D>();
        if (circuitComp == null) return;

        // Check if component has gizmo-based connection points
        ComponentConnectionPoints connectionPoints = componentObject.GetComponent<ComponentConnectionPoints>();

        if (connectionPoints != null)
        {
            Debug.Log($"Using gizmo-based connection points for {componentObject.name}");
            connectionPoints.CreateRuntimeTerminals();
        }
        else
        {
            Debug.Log($"No gizmo connection points found, using default terminals for {componentObject.name}");
            CreateElectricallyCorrectTerminals(componentObject, circuitComp);
        }
    }

    // Keep the existing CreateElectricallyCorrectTerminals method as fallback
    // (Previous code from TERMINAL_WIDGET_SYSTEM_CORRECTED.cs)
}

// Electrical polarity enum (if not already defined)
public enum ElectricalPolarity
{
    Positive,    // Battery positive, DC positive
    Negative,    // Battery negative, DC negative
    Anode,       // LED/Diode anode (positive side)
    Cathode,     // LED/Diode cathode (negative side)
    Neutral,     // Non-polarized (resistors, bulbs)
    Live,        // AC live/hot wire
    Ground,      // Earth ground
    Reference    // Reference point (0V)
}