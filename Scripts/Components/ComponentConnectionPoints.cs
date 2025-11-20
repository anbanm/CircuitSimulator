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

        // Add ComponentTerminal script for electrical functionality
        ComponentTerminal terminalComponent = terminal.AddComponent<ComponentTerminal>();

        // Get terminal color based on polarity
        Color terminalColor = GetPolarityColor(connectionPoint.polarity);

        // Configure the ComponentTerminal fields (before Start() is called)
        // Only batteries have true polarity - all other components are bidirectional
        CircuitComponent3D circuitComp = GetComponent<CircuitComponent3D>();
        bool isBattery = circuitComp != null && circuitComp.ComponentType == ComponentType.Battery;
        terminalComponent.isInput = isBattery && connectionPoint.polarity == ElectricalPolarity.Negative;
        terminalComponent.terminalColor = terminalColor;
        terminalComponent.terminalSize = 0.15f;

        // ComponentTerminal will handle its own visuals and electrical nodes
        // No need to create duplicate visual indicators

        // Add text label if specified
        if (!string.IsNullOrEmpty(connectionPoint.label))
        {
            CreateTerminalLabel(terminal, connectionPoint.label);
        }

        // Add connection data component for compatibility
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

    // Force gizmo refresh when Inspector values change
    void OnValidate()
    {
        // This is called when Inspector values change
        // Forces Unity to redraw gizmos
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () => {
            if (this != null)
                UnityEditor.SceneView.RepaintAll();
        };
        #endif
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

            // Draw label in Scene view (Unity Editor only)
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

    [ContextMenu("Refresh Gizmos")]
    void RefreshGizmos()
    {
        #if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
        Debug.Log("Gizmos refreshed!");
        #endif
    }

    [ContextMenu("Test Color Changes")]
    void TestColorChanges()
    {
        if (connectionPoints.Count > 0)
        {
            connectionPoints[0].gizmoColor = Color.red;
            connectionPoints[0].gizmoSize = 0.3f;
        }
        if (connectionPoints.Count > 1)
        {
            connectionPoints[1].gizmoColor = Color.blue;
            connectionPoints[1].gizmoSize = 0.25f;
        }
        RefreshGizmos();
    }
}