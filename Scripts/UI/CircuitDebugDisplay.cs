using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Inspector-viewable debug data for circuit topology and solver state.
/// Select this GameObject in the Hierarchy to see live circuit data in the Inspector.
/// </summary>
public class CircuitDebugDisplay : MonoBehaviour
{
    [Header("Settings")]
    public float updateInterval = 0.5f;

    [Header("TOPOLOGY - Electrical Node Groups")]
    [TextArea(10, 20)]
    public string topologyConnectivity = "Waiting for solve...";

    [Header("SOLVER STATE - Component Values")]
    [TextArea(15, 30)]
    public string solverState = "Waiting for solve...";

    [Header("Quick Stats")]
    public int totalEndpoints;
    public int totalNodes;
    public int totalComponents;
    public float batteryCurrent;
    public float totalPower;

    [Header("Component Data")]
    public List<ComponentDebugData> components = new List<ComponentDebugData>();

    [Header("Node Data")]
    public List<NodeDebugData> nodes = new List<NodeDebugData>();

    private float lastUpdateTime;

    [Serializable]
    public class ComponentDebugData
    {
        public string name;
        public string type;
        public float voltage;
        public float current;
        public float resistance;
        public float power;
    }

    [Serializable]
    public class NodeDebugData
    {
        public string nodeId;
        public float voltage;
        public List<string> connectedTerminals = new List<string>();
    }

    void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        Refresh();
    }

    public void Refresh()
    {
        UpdateTopology();
        UpdateSolverState();
    }

    void UpdateTopology()
    {
        var terminals = FindObjectsByType<ComponentTerminal>(FindObjectsSortMode.None);
        var endpoints = FindObjectsByType<WireEndpoint>(FindObjectsSortMode.None);

        totalEndpoints = endpoints.Length;

        // Group terminals by electrical node
        var nodeGroups = new Dictionary<CircuitNode, List<ComponentTerminal>>();

        foreach (var terminal in terminals)
        {
            if (terminal == null || terminal.electricalNode == null) continue;

            var node = terminal.electricalNode;
            if (!nodeGroups.ContainsKey(node))
                nodeGroups[node] = new List<ComponentTerminal>();
            nodeGroups[node].Add(terminal);
        }

        totalNodes = nodeGroups.Count;

        // Build topology string
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {nodeGroups.Count} ELECTRICAL NODES ===");
        sb.AppendLine($"=== {endpoints.Length} WIRE ENDPOINTS ===");
        sb.AppendLine();

        // Update nodes list
        nodes.Clear();
        int nodeIndex = 1;

        foreach (var kvp in nodeGroups)
        {
            var node = kvp.Key;
            var terminalList = kvp.Value;

            var nodeData = new NodeDebugData();
            nodeData.nodeId = $"Node{nodeIndex}";
            nodeData.voltage = node.Voltage;

            string voltageStr = node.Voltage != 0 ? $" @ {node.Voltage:F2}V" : "";
            sb.AppendLine($"[Node{nodeIndex}]{voltageStr}");

            foreach (var terminal in terminalList)
            {
                string compName = terminal.ParentComponent?.name ?? "?";
                string terminalStr = $"  {compName}.{terminal.name}";
                sb.AppendLine(terminalStr);
                nodeData.connectedTerminals.Add($"{compName}.{terminal.name}");
            }

            nodes.Add(nodeData);
            sb.AppendLine();
            nodeIndex++;
        }

        topologyConnectivity = sb.ToString();
    }

    void UpdateSolverState()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            solverState = "No CircuitManager";
            return;
        }

        var compList = circuitManager.Components;
        if (compList == null || compList.Count == 0)
        {
            solverState = "No components";
            return;
        }

        totalComponents = compList.Count;
        components.Clear();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {compList.Count} COMPONENTS ===");
        sb.AppendLine();

        batteryCurrent = 0;
        totalPower = 0;

        foreach (var comp3D in compList)
        {
            if (comp3D == null) continue;

            // Calculate power as V * I
            float compPower = comp3D.voltageDrop * comp3D.current;

            var data = new ComponentDebugData();
            data.name = comp3D.name;
            data.type = comp3D.ComponentType.ToString();
            data.voltage = comp3D.ComponentType == ComponentType.Battery ? comp3D.voltage : comp3D.voltageDrop;
            data.current = comp3D.current;
            data.resistance = comp3D.resistance;
            data.power = compPower;
            components.Add(data);

            switch (comp3D.ComponentType)
            {
                case ComponentType.Battery:
                    sb.AppendLine($"[BATTERY] {comp3D.name}");
                    sb.AppendLine($"  EMF: {comp3D.voltage:F2}V");
                    sb.AppendLine($"  I: {comp3D.current:F3}A");
                    batteryCurrent = comp3D.current;
                    break;

                case ComponentType.Bulb:
                    sb.AppendLine($"[BULB] {comp3D.name}");
                    sb.AppendLine($"  R: {comp3D.resistance:F1} ohm");
                    sb.AppendLine($"  V: {comp3D.voltageDrop:F2}V");
                    sb.AppendLine($"  I: {comp3D.current:F3}A");
                    sb.AppendLine($"  P: {compPower:F3}W");
                    totalPower += compPower;
                    break;

                case ComponentType.Resistor:
                    sb.AppendLine($"[RESISTOR] {comp3D.name}");
                    sb.AppendLine($"  R: {comp3D.resistance:F1} ohm");
                    sb.AppendLine($"  V: {comp3D.voltageDrop:F2}V");
                    sb.AppendLine($"  I: {comp3D.current:F3}A");
                    sb.AppendLine($"  P: {compPower:F3}W");
                    totalPower += compPower;
                    break;

                case ComponentType.Switch:
                    sb.AppendLine($"[SWITCH] {comp3D.name}");
                    // Switches don't have isClosed property directly - check resistance as proxy
                    bool isClosed = comp3D.resistance < 1f; // Low resistance = closed
                    sb.AppendLine($"  State: {(isClosed ? "CLOSED" : "OPEN")}");
                    break;
            }
            sb.AppendLine();
        }

        sb.AppendLine("=== TOTALS ===");
        sb.AppendLine($"Battery I: {batteryCurrent:F3}A");
        sb.AppendLine($"Total P: {totalPower:F3}W");

        solverState = sb.ToString();
    }
}
