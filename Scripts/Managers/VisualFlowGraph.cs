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


        foreach (var wireObj in wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire == null) continue;

            // Get the terminals this wire connects (junction-aware)
            var startTerminal = GetTerminalForEndpoint(wire, wire.startEndpoint);
            var endTerminal = GetTerminalForEndpoint(wire, wire.endEndpoint);

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

        }

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
    /// Get terminal for wire endpoint (junction-aware)
    /// For junction endpoints, traverses across the junction to find the component on the other side
    /// Uses BFS to handle multi-level junction chains
    /// </summary>
    private ComponentTerminal GetTerminalForEndpoint(CircuitWire wire, WireEndpoint endpoint)
    {
        if (wire == null || endpoint == null) return null;

        // First check if endpoint is directly connected to a terminal
        if (endpoint.ConnectedTerminal != null)
        {
            return endpoint.ConnectedTerminal;
        }

        // If at junction, use BFS to traverse through junction chain until we find a component terminal
        if (endpoint.SnappedToEndpoint != null)
        {
            var visitedEndpoints = new HashSet<WireEndpoint> { endpoint };
            var queue = new Queue<WireEndpoint>();
            queue.Enqueue(endpoint.SnappedToEndpoint);

            while (queue.Count > 0)
            {
                var currentEndpoint = queue.Dequeue();
                if (currentEndpoint == null || visitedEndpoints.Contains(currentEndpoint))
                    continue;

                visitedEndpoints.Add(currentEndpoint);

                // Check if this endpoint is connected to a component terminal
                if (currentEndpoint.ConnectedTerminal != null)
                {
                    return currentEndpoint.ConnectedTerminal;
                }

                // Get the wire this endpoint belongs to
                CircuitWire currentWire = currentEndpoint.ParentWire;
                if (currentWire == null) continue;

                // Get the OPPOSITE endpoint of this wire
                WireEndpoint oppositeEndpoint = null;
                if (currentWire.startEndpoint == currentEndpoint)
                    oppositeEndpoint = currentWire.endEndpoint;
                else if (currentWire.endEndpoint == currentEndpoint)
                    oppositeEndpoint = currentWire.startEndpoint;

                if (oppositeEndpoint != null && !visitedEndpoints.Contains(oppositeEndpoint))
                {
                    // Check if opposite endpoint has a terminal
                    if (oppositeEndpoint.ConnectedTerminal != null)
                    {
                        return oppositeEndpoint.ConnectedTerminal;
                    }

                    // If opposite endpoint is snapped to another junction, continue BFS
                    if (oppositeEndpoint.SnappedToEndpoint != null)
                    {
                        queue.Enqueue(oppositeEndpoint.SnappedToEndpoint);
                    }
                }

                // Also check if current endpoint is snapped to another endpoint (multi-wire junction)
                if (currentEndpoint.SnappedToEndpoint != null && !visitedEndpoints.Contains(currentEndpoint.SnappedToEndpoint))
                {
                    queue.Enqueue(currentEndpoint.SnappedToEndpoint);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Assign flow directions starting from Battery+ using BFS on wire endpoints
    /// This traverses through ALL wires including junction-to-junction wires
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

        Debug.Log($"[FLOW] Starting flow assignment from {battery.name}.{batteryPositive.name}");

        // Find all wire endpoints connected to battery positive terminal
        var allEndpoints = Object.FindObjectsByType<WireEndpoint>(FindObjectsSortMode.None);
        var startEndpoints = new List<WireEndpoint>();

        foreach (var endpoint in allEndpoints)
        {
            if (endpoint.ConnectedTerminal == batteryPositive)
            {
                startEndpoints.Add(endpoint);
            }
        }

        if (startEndpoints.Count == 0)
        {
            Debug.LogWarning("⚠️ No wire endpoints connected to Battery+");
            return;
        }

        Debug.Log($"[FLOW] Found {startEndpoints.Count} endpoints at Battery+");

        // BFS through wire endpoints
        var visitedEndpoints = new HashSet<WireEndpoint>();
        var visitedWires = new HashSet<CircuitWire>();
        var queue = new Queue<WireEndpoint>();

        // Seed BFS with endpoints at Battery+
        foreach (var ep in startEndpoints)
        {
            ep.isStart = true; // This endpoint is the START of current flow on its wire
            visitedEndpoints.Add(ep);
            queue.Enqueue(ep);
        }

        int wiresProcessed = 0;

        while (queue.Count > 0)
        {
            var currentEndpoint = queue.Dequeue();
            if (currentEndpoint == null) continue;

            var wire = currentEndpoint.ParentWire;
            if (wire == null) continue;

            // Mark this wire as processed
            if (!visitedWires.Contains(wire))
            {
                visitedWires.Add(wire);
                wiresProcessed++;

                // The endpoint we came FROM is the start, the other endpoint is the end
                WireEndpoint otherEndpoint = null;
                if (wire.startEndpoint == currentEndpoint)
                {
                    otherEndpoint = wire.endEndpoint;
                    currentEndpoint.isStart = true;
                    if (otherEndpoint != null) otherEndpoint.isStart = false;
                }
                else if (wire.endEndpoint == currentEndpoint)
                {
                    otherEndpoint = wire.startEndpoint;
                    currentEndpoint.isStart = true;
                    if (otherEndpoint != null) otherEndpoint.isStart = false;
                }

                Debug.Log($"[FLOW] Wire {wire.name}: flow from {currentEndpoint.name} to {otherEndpoint?.name}");

                // Continue BFS from the other endpoint
                if (otherEndpoint != null && !visitedEndpoints.Contains(otherEndpoint))
                {
                    visitedEndpoints.Add(otherEndpoint);

                    // If other endpoint is at a component terminal, find wires leaving that terminal
                    if (otherEndpoint.ConnectedTerminal != null)
                    {
                        var terminal = otherEndpoint.ConnectedTerminal;
                        var component = terminal.ParentComponent;

                        if (component != null)
                        {
                            // Find the OTHER terminal of this component (current exits from there)
                            var compTerminals = terminalManager.GetComponentTerminals(component);
                            var exitTerminal = compTerminals.FirstOrDefault(t => t != terminal);

                            if (exitTerminal != null)
                            {
                                // Find all wire endpoints at the exit terminal
                                foreach (var ep in allEndpoints)
                                {
                                    if (ep.ConnectedTerminal == exitTerminal && !visitedEndpoints.Contains(ep))
                                    {
                                        visitedEndpoints.Add(ep);
                                        queue.Enqueue(ep);
                                    }
                                }
                            }
                        }
                    }
                    // If other endpoint is at a junction (snapped to another endpoint)
                    else if (otherEndpoint.SnappedToEndpoint != null)
                    {
                        // Find ALL endpoints at this junction position
                        var junctionPos = otherEndpoint.GetPosition();
                        foreach (var ep in allEndpoints)
                        {
                            if (ep == otherEndpoint) continue;
                            if (visitedEndpoints.Contains(ep)) continue;

                            // Check if this endpoint is at the junction (either snapped or position-based)
                            bool atJunction = (ep.SnappedToEndpoint == otherEndpoint) ||
                                            (otherEndpoint.SnappedToEndpoint == ep) ||
                                            (Vector3.Distance(ep.GetPosition(), junctionPos) < 0.2f);

                            if (atJunction)
                            {
                                visitedEndpoints.Add(ep);
                                queue.Enqueue(ep);
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"[FLOW] Processed {wiresProcessed} wires, visited {visitedEndpoints.Count} endpoints");
    }

    /// <summary>
    /// Debug: Print the entire visual graph
    /// </summary>
    public void PrintGraph()
    {
        foreach (var kvp in outgoingConnections)
        {
            foreach (var conn in kvp.Value)
            {
            }
        }
    }
}
