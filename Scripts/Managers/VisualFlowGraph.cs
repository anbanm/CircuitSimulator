using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Builds a topological graph of wire connections for visual flow animation
/// COMPLETELY SEPARATE from the electrical solver's merged node graph
/// This represents the PHYSICAL component-to-component connections as the user sees them
/// </summary>
public class VisualFlowGraph
{
    /// <summary>
    /// Represents a wire connection in the visual graph
    /// </summary>
    public class WireConnection
    {
        public CircuitWire wire;
        public CircuitComponent3D fromComponent;
        public ComponentTerminal fromTerminal;
        public CircuitComponent3D toComponent;
        public ComponentTerminal toTerminal;

        public WireConnection(CircuitWire wire, CircuitComponent3D from, ComponentTerminal fromTerm,
                             CircuitComponent3D to, ComponentTerminal toTerm)
        {
            this.wire = wire;
            this.fromComponent = from;
            this.fromTerminal = fromTerm;
            this.toComponent = to;
            this.toTerminal = toTerm;
        }

        public override string ToString()
        {
            return $"{fromComponent.name}.{fromTerminal.name} → {toComponent.name}.{toTerminal.name}";
        }
    }

    // Graph structure: Component → List of outgoing wire connections
    private Dictionary<CircuitComponent3D, List<WireConnection>> outgoingConnections =
        new Dictionary<CircuitComponent3D, List<WireConnection>>();

    // Graph structure: Component → List of incoming wire connections
    private Dictionary<CircuitComponent3D, List<WireConnection>> incomingConnections =
        new Dictionary<CircuitComponent3D, List<WireConnection>>();

    /// <summary>
    /// Build the visual graph from the current wire connections in the scene
    /// </summary>
    public void BuildFromScene(List<GameObject> wires)
    {
        outgoingConnections.Clear();
        incomingConnections.Clear();

        Debug.Log("=== BUILDING VISUAL FLOW GRAPH ===");

        foreach (var wireObj in wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire == null) continue;

            // Get the terminals this wire connects
            var startTerminal = wire.startEndpoint?.ConnectedTerminal;
            var endTerminal = wire.endEndpoint?.ConnectedTerminal;

            if (startTerminal == null || endTerminal == null)
            {
                Debug.LogWarning($"⚠️ Wire {wire.name} has disconnected endpoints, skipping");
                continue;
            }

            // Get the components these terminals belong to
            var startComponent = startTerminal.ParentComponent;
            var endComponent = endTerminal.ParentComponent;

            if (startComponent == null || endComponent == null)
            {
                Debug.LogWarning($"⚠️ Wire {wire.name} has terminals without parent components, skipping");
                continue;
            }

            // Skip self-connections (both endpoints on same component)
            if (startComponent == endComponent)
            {
                Debug.LogWarning($"⚠️ Wire {wire.name} connects {startComponent.name} to itself, skipping");
                continue;
            }

            // Create bidirectional connection (wire can conduct current in either direction)
            var connectionAtoB = new WireConnection(wire, startComponent, startTerminal, endComponent, endTerminal);
            var connectionBtoA = new WireConnection(wire, endComponent, endTerminal, startComponent, startTerminal);

            // Add to outgoing connections
            if (!outgoingConnections.ContainsKey(startComponent))
                outgoingConnections[startComponent] = new List<WireConnection>();
            outgoingConnections[startComponent].Add(connectionAtoB);

            if (!outgoingConnections.ContainsKey(endComponent))
                outgoingConnections[endComponent] = new List<WireConnection>();
            outgoingConnections[endComponent].Add(connectionBtoA);

            // Add to incoming connections
            if (!incomingConnections.ContainsKey(endComponent))
                incomingConnections[endComponent] = new List<WireConnection>();
            incomingConnections[endComponent].Add(connectionAtoB);

            if (!incomingConnections.ContainsKey(startComponent))
                incomingConnections[startComponent] = new List<WireConnection>();
            incomingConnections[startComponent].Add(connectionBtoA);

            Debug.Log($"  ✅ {connectionAtoB}");
        }

        Debug.Log($"✅ Visual graph built: {outgoingConnections.Count} components, {wires.Count} wires");
    }

    /// <summary>
    /// Get all wire connections leaving a component
    /// Sorted by wire name for deterministic iteration
    /// </summary>
    public List<WireConnection> GetOutgoingConnections(CircuitComponent3D component)
    {
        if (outgoingConnections.ContainsKey(component))
        {
            // Sort by wire name for deterministic BFS traversal
            var connections = outgoingConnections[component];
            connections.Sort((a, b) => string.Compare(a.wire.name, b.wire.name));
            return connections;
        }
        return new List<WireConnection>();
    }

    /// <summary>
    /// Get all wire connections entering a component
    /// </summary>
    public List<WireConnection> GetIncomingConnections(CircuitComponent3D component)
    {
        if (incomingConnections.ContainsKey(component))
            return incomingConnections[component];
        return new List<WireConnection>();
    }

    /// <summary>
    /// Assign flow directions starting from Battery+ using BFS on the visual graph
    /// This is INDEPENDENT of the electrical solver's merged nodes
    /// </summary>
    public void AssignFlowDirectionsFromBattery(CircuitComponent3D battery, ComponentTerminalManager terminalManager)
    {
        if (battery == null || battery.ComponentType != ComponentType.Battery)
        {
            Debug.LogWarning("⚠️ Invalid battery component");
            return;
        }

        // Get battery's positive terminal (the START of current flow)
        var terminals = terminalManager.GetComponentTerminals(battery);
        var batteryPositive = terminals.Find(t => !t.isInput); // Positive = output

        if (batteryPositive == null)
        {
            Debug.LogWarning("⚠️ Battery positive terminal not found");
            return;
        }

        Debug.Log($"🔋 Starting BFS from Battery+ ({batteryPositive.name})");

        // BFS state
        var visitedComponents = new HashSet<CircuitComponent3D>();
        var visitedWires = new HashSet<CircuitWire>();
        var queue = new Queue<(CircuitComponent3D component, ComponentTerminal exitTerminal)>();

        // Start BFS from battery - will exit FROM PositiveTerminal
        queue.Enqueue((battery, batteryPositive));
        visitedComponents.Add(battery);

        int wiresProcessed = 0;

        while (queue.Count > 0)
        {
            var (currentComponent, exitFromTerminal) = queue.Dequeue();
            Debug.Log($"  📍 Visiting {currentComponent.name}, exiting from {exitFromTerminal.name}");

            // Find all outgoing connections from this component
            var connections = GetOutgoingConnections(currentComponent);
            Debug.Log($"    Found {connections.Count} outgoing connections");

            foreach (var connection in connections)
            {
                // Only process connections that leave FROM the exit terminal
                if (connection.fromTerminal != exitFromTerminal)
                    continue;

                // Skip if we already processed this wire
                if (visitedWires.Contains(connection.wire))
                    continue;

                visitedWires.Add(connection.wire);

                // Assign flow direction: current flows FROM this component TO the other
                // The endpoint connected to currentTerminal is the START of flow
                if (connection.wire.startEndpoint?.ConnectedTerminal == connection.fromTerminal)
                {
                    connection.wire.startEndpoint.isStart = true;
                    connection.wire.endEndpoint.isStart = false;
                    Debug.Log($"    🔗 {connection.wire.name}: START endpoint is flow start");
                }
                else if (connection.wire.endEndpoint?.ConnectedTerminal == connection.fromTerminal)
                {
                    connection.wire.endEndpoint.isStart = true;
                    connection.wire.startEndpoint.isStart = false;
                    Debug.Log($"    🔗 {connection.wire.name}: END endpoint is flow start");
                }

                wiresProcessed++;

                // Queue the destination component if not visited
                if (!visitedComponents.Contains(connection.toComponent))
                {
                    visitedComponents.Add(connection.toComponent);

                    // Current enters via toTerminal, so find the OTHER terminal to exit from
                    var destTerminals = terminalManager.GetComponentTerminals(connection.toComponent);

                    // For 2-terminal components, pick the terminal that isn't the entry point
                    // Sort by name for deterministic selection if multiple exit terminals exist
                    var exitTerminal = destTerminals
                        .Where(t => t != connection.toTerminal)
                        .OrderBy(t => t.name)
                        .FirstOrDefault();

                    if (exitTerminal != null)
                    {
                        queue.Enqueue((connection.toComponent, exitTerminal));
                        Debug.Log($"      ➡️ Queuing {connection.toComponent.name}, will exit from {exitTerminal.name}");
                    }
                }
            }
        }

        Debug.Log($"✅ Flow directions assigned: {wiresProcessed} wires processed, {visitedComponents.Count} components visited");
    }

    /// <summary>
    /// Debug: Print the entire visual graph
    /// </summary>
    public void PrintGraph()
    {
        Debug.Log("=== VISUAL FLOW GRAPH ===");
        foreach (var kvp in outgoingConnections)
        {
            Debug.Log($"{kvp.Key.name}:");
            foreach (var conn in kvp.Value)
            {
                Debug.Log($"  → {conn}");
            }
        }
    }
}
