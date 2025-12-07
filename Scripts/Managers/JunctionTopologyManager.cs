using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// TOPOLOGY LAYER: Discovers electrical connectivity from wire endpoint connections
/// Builds junction graph WITHOUT moving any endpoints (pure discovery)
/// Creates GameObject hierarchy for visual debugging in Unity Editor
/// </summary>
public class JunctionTopologyManager : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool enableDebugVisualization = false;  // Disabled to prevent visual artifacts
    public Color junctionDebugColor = Color.green;
    public float junctionDebugSphereSize = 0.3f;

    [Header("Debug Logging")]
    public bool debugTopology = false; // Enable verbose console logging

    [Header("Junction Discovery")]
    public float positionTolerance = 0.2f; // For grouping endpoints at similar positions

    private CircuitTopology currentTopology;
    private GameObject topologyDebugRoot;
    private List<Material> debugMaterials = new List<Material>();  // FIX: Track materials for cleanup

    /// <summary>
    /// Represents a junction - multiple wire endpoints at the same electrical point
    /// </summary>
    public class Junction
    {
        public List<WireEndpoint> endpoints = new List<WireEndpoint>();
        public Vector3 position;
        public string id;
        public GameObject debugObject; // Visual representation in hierarchy

        /// <summary>
        /// Get the terminal that this junction connects to (if any)
        /// Returns null if junction is floating (not connected to any component)
        /// </summary>
        public ComponentTerminal GetConnectedTerminal()
        {
            // Check if any endpoint in this junction is physically at a terminal
            foreach (var endpoint in endpoints)
            {
                if (endpoint.IsConnected && endpoint.ConnectedTerminal != null)
                {
                    return endpoint.ConnectedTerminal;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if this junction is floating (not connected to any terminal)
        /// </summary>
        public bool IsFloating => GetConnectedTerminal() == null;

        /// <summary>
        /// Get all wires that meet at this junction
        /// </summary>
        public List<CircuitWire> GetConnectedWires()
        {
            var wires = new HashSet<CircuitWire>();
            foreach (var endpoint in endpoints)
            {
                if (endpoint == null) continue;  // FIX: Null check

                var wire = endpoint.GetComponent<CircuitWire>();
                if (wire == null && endpoint.transform.parent != null)
                {
                    wire = endpoint.transform.parent.GetComponent<CircuitWire>();
                }

                if (wire == null)
                {
                    Debug.LogWarning($"[JUNCTION] Cannot find CircuitWire for endpoint {endpoint.name}");
                    continue;
                }

                wires.Add(wire);
            }
            return wires.ToList();
        }
    }

    /// <summary>
    /// Complete circuit topology - all junctions and connections
    /// </summary>
    public class CircuitTopology
    {
        public List<Junction> junctions = new List<Junction>();
        public Dictionary<CircuitComponent3D, List<WireEndpoint>> componentConnections = new Dictionary<CircuitComponent3D, List<WireEndpoint>>();

        /// <summary>
        /// Find the junction that contains a specific endpoint
        /// </summary>
        public Junction FindJunctionForEndpoint(WireEndpoint endpoint)
        {
            return junctions.FirstOrDefault(j => j.endpoints.Contains(endpoint));
        }

        /// <summary>
        /// Get the electrical terminal for an endpoint (may be via junction)
        /// </summary>
        public ComponentTerminal GetElectricalTerminal(WireEndpoint endpoint)
        {
            // Direct connection to terminal
            if (endpoint.IsConnected && endpoint.ConnectedTerminal != null)
            {
                return endpoint.ConnectedTerminal;
            }

            // Check if endpoint is in a junction that connects to a terminal
            var junction = FindJunctionForEndpoint(endpoint);
            if (junction != null)
            {
                return junction.GetConnectedTerminal();
            }

            return null;
        }

        /// <summary>
        /// Get all endpoints that are electrically connected to the given endpoint
        /// (via junctions)
        /// </summary>
        public List<WireEndpoint> GetElectricallyConnectedEndpoints(WireEndpoint endpoint)
        {
            var junction = FindJunctionForEndpoint(endpoint);
            if (junction != null)
            {
                return junction.endpoints.Where(e => e != endpoint).ToList();
            }
            return new List<WireEndpoint>();
        }

        /// <summary>
        /// Find junction for a specific terminal
        /// </summary>
        public Junction FindJunctionForTerminal(ComponentTerminal terminal)
        {
            return junctions.FirstOrDefault(j => j.GetConnectedTerminal() == terminal);
        }

        /// <summary>
        /// CRITICAL FIX: Get junctions electrically connected to this junction via wires
        /// This enables traversal through wire-to-wire junctions for circuit solving
        ///
        /// Example: Battery+ → Junction_0 → Wire1 → Junction_1 (wire-to-wire) → Wire2 → Junction_2 → Bulb
        /// </summary>
        public List<Junction> GetConnectedJunctionsViaWires(Junction junction)
        {
            var connectedJunctions = new HashSet<Junction>();

            // For each endpoint in this junction
            foreach (var endpoint in junction.endpoints)
            {
                // Find the wire this endpoint belongs to
                var wire = GetWireForEndpoint(endpoint);
                if (wire == null) continue;

                // Find the OTHER endpoint of this wire (wires are non-directional!)
                WireEndpoint otherEndpoint = null;
                if (wire.startEndpoint == endpoint)
                    otherEndpoint = wire.endEndpoint;
                else if (wire.endEndpoint == endpoint)
                    otherEndpoint = wire.startEndpoint;

                if (otherEndpoint == null) continue;

                // Find the junction containing the other endpoint
                var otherJunction = FindJunctionForEndpoint(otherEndpoint);

                // If it's a different junction, they're electrically connected via this wire
                if (otherJunction != null && otherJunction != junction)
                {
                    connectedJunctions.Add(otherJunction);
                }
            }

            return connectedJunctions.ToList();
        }

        /// <summary>
        /// Find the wire that this endpoint belongs to
        /// </summary>
        CircuitWire GetWireForEndpoint(WireEndpoint endpoint)
        {
            if (endpoint == null) return null;

            // Use the direct reference stored in WireEndpoint (FIX: endpoints are NOT children of wire GameObject)
            var wire = endpoint.ParentWire;
            if (wire != null) return wire;

            // Wire not found - this is expected for some edge cases, no need to log
            return null;
        }

        /// <summary>
        /// Build complete electrical graph including wire connections
        /// Logs junction-to-junction connectivity for debugging
        /// Call this after BuildTopology() to verify electrical paths
        /// NOTE: This method is verbose - only call when actively debugging
        /// </summary>
        public void BuildElectricalGraph(bool enableLogging = false)
        {
            if (!enableLogging) return;

            Debug.Log("[TOPOLOGY] === Building Electrical Graph ===");

            foreach (var junction in junctions)
            {
                var connectedJunctions = GetConnectedJunctionsViaWires(junction);
                var terminal = junction.GetConnectedTerminal();
                string terminalInfo = terminal != null ? terminal.name : "Floating";

                Debug.Log($"[TOPOLOGY] {junction.id} (at {terminalInfo}) connects to {connectedJunctions.Count} junctions via wires:");
                foreach (var connectedJunction in connectedJunctions)
                {
                    var otherTerminal = connectedJunction.GetConnectedTerminal();
                    string otherInfo = otherTerminal != null ? otherTerminal.name : "Floating";
                    Debug.Log($"  → {connectedJunction.id} (at {otherInfo})");
                }
            }

            Debug.Log("[TOPOLOGY] === Electrical Graph Complete ===");
        }
    }

    /// <summary>
    /// Find the wire that an endpoint belongs to
    /// Checks endpoint GameObject and parent for CircuitWire component
    /// </summary>
    CircuitWire GetWireForEndpointHelper(WireEndpoint endpoint)
    {
        if (endpoint == null) return null;

        // Use the direct reference stored in WireEndpoint (FIX: endpoints are NOT children of wire GameObject)
        var wire = endpoint.ParentWire;
        if (wire != null) return wire;

        // Log warning if wire not found (helps debugging)
        if (debugTopology) Debug.LogWarning($"[TOPOLOGY] Cannot find CircuitWire for endpoint {endpoint.name}");
        return null;
    }

    /// <summary>
    /// Get the terminal that a wire endpoint connects to
    /// Wire endpoints attach to component terminals via CircuitWire references
    /// </summary>
    ComponentTerminal GetTerminalForWireEndpoint(CircuitWire wire, WireEndpoint endpoint)
    {
        if (wire == null || endpoint == null) return null;

        // CRITICAL FIX: In wire-to-wire junctions, the junction endpoint has connectedTerminal = NULL
        // because it's snapped to another wire, not a component terminal.
        // We need to check the OPPOSITE endpoint of the same wire to find the component connection!

        // First, check if THIS endpoint is directly connected to a component terminal
        if (endpoint.ConnectedTerminal != null)
        {
            return endpoint.ConnectedTerminal;
        }

        // If not, this endpoint is at a junction. Check the OPPOSITE endpoint of the same wire.
        // For example: Wire from Battery+ to Junction
        //   - Wire.START connected to Battery.PositiveTerminal (has ConnectedTerminal)
        //   - Wire.END at junction (ConnectedTerminal = NULL, but we want Battery.PositiveTerminal!)
        WireEndpoint oppositeEndpoint = null;
        if (wire.startEndpoint == endpoint)
        {
            oppositeEndpoint = wire.endEndpoint;
        }
        else if (wire.endEndpoint == endpoint)
        {
            oppositeEndpoint = wire.startEndpoint;
        }

        // Return the terminal from the opposite endpoint (the component this wire connects to)
        if (oppositeEndpoint != null && oppositeEndpoint.ConnectedTerminal != null)
        {
            return oppositeEndpoint.ConnectedTerminal;
        }

        return null;  // Wire is floating or disconnected
    }

    #region Path-Centric Traversal (NEW)

    /// <summary>
    /// Find all component terminals in the scene
    /// </summary>
    List<ComponentTerminal> FindAllComponentTerminals()
    {
        var terminals = new List<ComponentTerminal>();
        var allTerminals = FindObjectsByType<ComponentTerminal>(FindObjectsSortMode.None);
        terminals.AddRange(allTerminals);
        return terminals;
    }

    /// <summary>
    /// Find all wire endpoints connected to a terminal
    /// Checks endpoint.ConnectedTerminal and position proximity
    /// Note: Wire's startTerminal/endTerminal are NOT reliable for wire-to-wire junctions
    /// because they're only set when endpoint has ConnectedTerminal (not SnappedToEndpoint)
    /// </summary>
    List<WireEndpoint> FindEndpointsAtTerminal(ComponentTerminal terminal)
    {
        var result = new List<WireEndpoint>();
        if (terminal == null) return result;

        var allEndpoints = FindObjectsByType<WireEndpoint>(FindObjectsSortMode.None);
        foreach (var endpoint in allEndpoints)
        {
            if (endpoint == null) continue;

            // Check 1: Endpoint is directly connected to this terminal (primary check)
            if (endpoint.ConnectedTerminal == terminal)
            {
                if (!result.Contains(endpoint))
                    result.Add(endpoint);
                continue;
            }

            // Check 2: Position proximity (for endpoints that may be at terminal but not formally connected)
            // This handles edge cases where snapping didn't set ConnectedTerminal properly
            float distance = Vector3.Distance(endpoint.GetPosition(), terminal.transform.position);
            if (distance < positionTolerance)
            {
                if (!result.Contains(endpoint))
                    result.Add(endpoint);
            }
        }

        Debug.Log($"[TRACE] FindEndpointsAtTerminal({terminal.name}): found {result.Count} endpoints");
        return result;
    }

    /// <summary>
    /// Find all wire endpoints at a given position (within tolerance)
    /// Uses consistent tolerance for all endpoints at junctions
    /// </summary>
    List<WireEndpoint> FindEndpointsAtPosition(Vector3 position)
    {
        var result = new List<WireEndpoint>();
        var allEndpoints = FindObjectsByType<WireEndpoint>(FindObjectsSortMode.None);

        foreach (var endpoint in allEndpoints)
        {
            if (endpoint == null) continue;

            // Use consistent tolerance for position-based matching
            // When endpoints are snapped together, they're at exactly the same position
            float distance = Vector3.Distance(endpoint.GetPosition(), position);

            // Use positionTolerance (0.2f) for all endpoints at junction positions
            // This allows finding endpoints that are snapped together
            if (distance < positionTolerance)
            {
                result.Add(endpoint);
                Debug.Log($"[TRACE]     FindEndpointsAtPosition: {endpoint.name} at distance {distance:F4}");
            }
        }
        return result;
    }

    /// <summary>
    /// BFS traversal from a terminal through all connected wire paths.
    /// Returns all terminals reachable via wire connections.
    /// Handles wire chains through multiple wire-to-wire junctions.
    /// </summary>
    List<ComponentTerminal> TraceFromTerminal(ComponentTerminal startTerminal)
    {
        var result = new List<ComponentTerminal> { startTerminal };
        var visitedEndpoints = new HashSet<WireEndpoint>();
        var queue = new Queue<WireEndpoint>();

        Debug.Log($"[TRACE] === Starting BFS from terminal: {startTerminal.name} ===");

        // Seed BFS with endpoints at this terminal
        var startEndpoints = FindEndpointsAtTerminal(startTerminal);
        Debug.Log($"[TRACE] Found {startEndpoints.Count} endpoints at start terminal");
        foreach (var ep in startEndpoints)
        {
            if (ep != null)
            {
                Debug.Log($"[TRACE]   Seeding queue with: {ep.name} (wire: {ep.ParentWire?.name})");
                queue.Enqueue(ep);
                visitedEndpoints.Add(ep);
            }
        }

        while (queue.Count > 0)
        {
            var currentEndpoint = queue.Dequeue();
            if (currentEndpoint == null) continue;

            Debug.Log($"[TRACE] Processing: {currentEndpoint.name}");

            // Get the wire this endpoint belongs to
            var wire = currentEndpoint.ParentWire;
            if (wire == null)
            {
                if (debugTopology) Debug.LogWarning($"[TOPOLOGY] Endpoint {currentEndpoint.name} has null ParentWire!");
                continue;
            }

            // Get the OTHER endpoint of the same wire
            WireEndpoint otherEndpoint = null;
            if (wire.startEndpoint == currentEndpoint)
                otherEndpoint = wire.endEndpoint;
            else if (wire.endEndpoint == currentEndpoint)
                otherEndpoint = wire.startEndpoint;
            else
            {
                if (debugTopology) Debug.LogWarning($"[TOPOLOGY] Endpoint {currentEndpoint.name} not found in wire {wire.name}!");
                continue;
            }

            Debug.Log($"[TRACE]   Other endpoint of wire {wire.name}: {otherEndpoint?.name ?? "NULL"}");
            Debug.Log($"[TRACE]   Other endpoint position: {otherEndpoint?.GetPosition()}");
            Debug.Log($"[TRACE]   Other endpoint ConnectedTerminal: {otherEndpoint?.ConnectedTerminal?.name ?? "NULL"}");
            Debug.Log($"[TRACE]   Other endpoint SnappedToEndpoint: {otherEndpoint?.SnappedToEndpoint?.name ?? "NULL"}");

            if (otherEndpoint == null || visitedEndpoints.Contains(otherEndpoint))
            {
                Debug.Log($"[TRACE]   Skipping (null or already visited)");
                continue;
            }

            visitedEndpoints.Add(otherEndpoint);

            // Case 1: Other endpoint is at a component terminal
            if (otherEndpoint.ConnectedTerminal != null)
            {
                Debug.Log($"[TRACE]   Case 1: Connected to terminal {otherEndpoint.ConnectedTerminal.name}");
                if (!result.Contains(otherEndpoint.ConnectedTerminal))
                {
                    result.Add(otherEndpoint.ConnectedTerminal);
                    Debug.Log($"[TRACE]   Added terminal to result: {otherEndpoint.ConnectedTerminal.name}");
                }
                // Continue searching from this terminal (there may be more wires)
                var moreEndpoints = FindEndpointsAtTerminal(otherEndpoint.ConnectedTerminal);
                Debug.Log($"[TRACE]   Found {moreEndpoints.Count} more endpoints at terminal");
                foreach (var ep in moreEndpoints)
                {
                    if (!visitedEndpoints.Contains(ep))
                    {
                        Debug.Log($"[TRACE]   Queuing: {ep.name}");
                        queue.Enqueue(ep);
                        visitedEndpoints.Add(ep);
                    }
                }
            }
            // Case 2: Other endpoint is snapped to another wire endpoint (junction)
            else if (otherEndpoint.SnappedToEndpoint != null)
            {
                Debug.Log($"[TRACE]   Case 2: Snapped to endpoint {otherEndpoint.SnappedToEndpoint.name}");
                if (!visitedEndpoints.Contains(otherEndpoint.SnappedToEndpoint))
                {
                    Debug.Log($"[TRACE]   Queuing snapped endpoint: {otherEndpoint.SnappedToEndpoint.name}");
                    queue.Enqueue(otherEndpoint.SnappedToEndpoint);
                    visitedEndpoints.Add(otherEndpoint.SnappedToEndpoint);
                }

                // Also check for other endpoints at this junction position
                var junctionEndpoints = FindEndpointsAtPosition(otherEndpoint.GetPosition());
                Debug.Log($"[TRACE]   Found {junctionEndpoints.Count} endpoints at junction position {otherEndpoint.GetPosition()}");
                foreach (var ep in junctionEndpoints)
                {
                    if (!visitedEndpoints.Contains(ep))
                    {
                        Debug.Log($"[TRACE]   Queuing nearby: {ep.name}");
                        queue.Enqueue(ep);
                        visitedEndpoints.Add(ep);
                    }
                }
            }
            // Case 3: Other endpoint is near another endpoint (position-based junction)
            else
            {
                Debug.Log($"[TRACE]   Case 3: Position-based junction check at {otherEndpoint.GetPosition()}");
                var nearbyEndpoints = FindEndpointsAtPosition(otherEndpoint.GetPosition());
                Debug.Log($"[TRACE]   Found {nearbyEndpoints.Count} nearby endpoints");
                foreach (var ep in nearbyEndpoints)
                {
                    if (!visitedEndpoints.Contains(ep))
                    {
                        Debug.Log($"[TRACE]   Queuing nearby: {ep.name}");
                        queue.Enqueue(ep);
                        visitedEndpoints.Add(ep);
                    }
                    else
                    {
                        Debug.Log($"[TRACE]   Already visited: {ep.name}");
                    }
                }
            }
        }

        Debug.Log($"[TRACE] === BFS complete. Found {result.Count} terminals ===");
        return result;
    }

    /// <summary>
    /// Traces all wire paths from each component terminal using BFS.
    /// Terminals connected via wire paths (regardless of junction count) share the same electrical node.
    /// This is the NEW path-centric approach that handles wire chains correctly.
    /// </summary>
    void TraceTerminalPaths()
    {
        var allTerminals = FindAllComponentTerminals();
        var terminalGroups = new Dictionary<ComponentTerminal, CircuitNode>();

        if (debugTopology) Debug.Log($"[TOPOLOGY] === TraceTerminalPaths: Processing {allTerminals.Count} terminals ===");

        // NOTE: Node component list clearing is now handled in CircuitSolverManager.BuildLogicalCircuit()
        // AFTER this method runs (so nodes exist when we clear them)

        foreach (var startTerminal in allTerminals)
        {
            if (startTerminal == null) continue;

            // Skip if already assigned to a group
            if (terminalGroups.ContainsKey(startTerminal))
                continue;

            // Create new electrical node for this group (or reuse existing if valid)
            var sharedNode = new CircuitNode($"PathNode_{startTerminal.name}_{startTerminal.GetInstanceID()}");

            // BFS to find all terminals connected via wire paths
            var connectedTerminals = TraceFromTerminal(startTerminal);

            // Log the group
            var terminalNames = string.Join(", ", connectedTerminals.ConvertAll(t => t?.name ?? "NULL"));
            if (debugTopology) Debug.Log($"[TOPOLOGY] Terminal group: [{terminalNames}] ({connectedTerminals.Count} terminals share node {sharedNode.Id})");

            // Merge all connected terminals to share the same node
            foreach (var terminal in connectedTerminals)
            {
                if (terminal == null) continue;

                terminal.electricalNode = sharedNode;
                terminalGroups[terminal] = sharedNode;

                // Register component with the shared node
                if (terminal.ParentComponent?.logicalComponent != null)
                {
                    if (!sharedNode.ConnectedComponents.Contains(terminal.ParentComponent.logicalComponent))
                    {
                        sharedNode.ConnectedComponents.Add(terminal.ParentComponent.logicalComponent);
                    }
                }
                else
                {
                    if (debugTopology) Debug.LogWarning($"[TOPOLOGY] Terminal {terminal.name} has no parent component or logical component!");
                }
            }
        }

        // SINGLE SUMMARY LOG: Show all terminal groups with their shared nodes
        var nodeGroups = new Dictionary<CircuitNode, List<string>>();
        foreach (var kvp in terminalGroups)
        {
            if (!nodeGroups.ContainsKey(kvp.Value))
                nodeGroups[kvp.Value] = new List<string>();
            nodeGroups[kvp.Value].Add($"{kvp.Key.ParentComponent?.name}.{kvp.Key.name}");
        }
        var summary = new System.Text.StringBuilder();
        summary.Append($"[TOPOLOGY SUMMARY] {nodeGroups.Count} electrical nodes: ");
        foreach (var group in nodeGroups)
        {
            summary.Append($"[{string.Join(" + ", group.Value)}] ");
        }
        Debug.Log(summary.ToString());
    }

    #endregion

    /// <summary>
    /// DEPRECATED: Use TraceTerminalPaths() instead.
    /// This method fails on wire-to-wire junction chains because it only looks
    /// at terminals directly at junctions, not terminals reachable via wire paths.
    /// Kept for reference only.
    /// </summary>
    [System.Obsolete("Use TraceTerminalPaths() instead - this fails on wire chains")]
    void MergeJunctionTerminalNodes(List<Junction> junctions)
    {
        if (debugTopology) Debug.Log("[TOPOLOGY] === Merging Terminal Electrical Nodes ===");

        foreach (var junction in junctions)
        {
            var terminals = new List<ComponentTerminal>();

            // Collect all terminals at this junction
            foreach (var endpoint in junction.endpoints)
            {
                // DEBUG: Show what this endpoint is connected to
                if (debugTopology) Debug.Log($"[TOPOLOGY] {junction.id} endpoint: {endpoint.name}");
                Debug.Log($"  - endpoint.ConnectedTerminal = {endpoint.ConnectedTerminal?.name ?? "NULL"}");
                Debug.Log($"  - endpoint.SnappedToEndpoint = {endpoint.SnappedToEndpoint?.name ?? "NULL"}");

                var wire = GetWireForEndpointHelper(endpoint);
                if (wire != null)
                {
                    Debug.Log($"  - wire = {wire.name}");
                    Debug.Log($"  - wire.startTerminal = {wire.startTerminal?.name ?? "NULL"}");
                    Debug.Log($"  - wire.endTerminal = {wire.endTerminal?.name ?? "NULL"}");

                    var terminal = GetTerminalForWireEndpoint(wire, endpoint);
                    Debug.Log($"  - GetTerminalForWireEndpoint returned: {terminal?.name ?? "NULL"}");

                    if (terminal != null)
                    {
                        terminals.Add(terminal);
                    }
                }
            }

            // Merge all terminal nodes to first terminal's node
            if (terminals.Count >= 2)
            {
                // Use first terminal's node as shared node (or create new one if null)
                var sharedNode = terminals[0].electricalNode ?? new CircuitNode(junction.id);

                // Make all terminals reference the same node
                foreach (var terminal in terminals)
                {
                    var oldNodeId = terminal.electricalNode?.Id ?? "NULL";
                    terminal.electricalNode = sharedNode;

                    // Add terminal's component to shared node's connected components
                    if (terminal.ParentComponent?.logicalComponent != null)
                    {
                        if (!sharedNode.ConnectedComponents.Contains(terminal.ParentComponent.logicalComponent))
                        {
                            sharedNode.ConnectedComponents.Add(terminal.ParentComponent.logicalComponent);
                        }
                    }
                }

                var terminalNames = string.Join(", ", terminals.Select(t => t.name));
                if (debugTopology) Debug.Log($"[TOPOLOGY] ✅ Merged {terminals.Count} terminal nodes at {junction.id}: {terminalNames}");
                Debug.Log($"  Shared node ID: {sharedNode.Id} with {sharedNode.ConnectedComponents.Count} components");
            }
            else if (terminals.Count == 1)
            {
                if (debugTopology) Debug.Log($"[TOPOLOGY] {junction.id} has only 1 terminal: {terminals[0].name} (no merge needed)");
            }
            else
            {
                if (debugTopology) Debug.LogWarning($"[TOPOLOGY] {junction.id} has no terminals! (floating junction with no component connections)");
            }
        }

        if (debugTopology) Debug.Log("[TOPOLOGY] === Terminal Node Merging Complete ===");
    }

    void Awake()
    {
        // Create root GameObject for topology visualization
        CreateTopologyDebugRoot();
    }

    void CreateTopologyDebugRoot()
    {
        // Find or create debug root
        topologyDebugRoot = GameObject.Find("_CircuitTopology_Debug");
        if (topologyDebugRoot == null)
        {
            topologyDebugRoot = new GameObject("_CircuitTopology_Debug");
        }

        // Position at origin for easy viewing
        topologyDebugRoot.transform.position = Vector3.zero;

        // Add description component for Inspector visibility
        var description = topologyDebugRoot.AddComponent<TopologyDebugInfo>();
        description.manager = this;
    }

    /// <summary>
    /// Build complete circuit topology by discovering junctions
    /// This is the MAIN ENTRY POINT for topology discovery
    /// </summary>
    public CircuitTopology BuildTopology()
    {
        var topology = new CircuitTopology();

        // 1. Find all wire endpoints in the scene
        WireEndpoint[] allEndpoints = FindObjectsByType<WireEndpoint>(FindObjectsSortMode.None);

        if (allEndpoints.Length == 0)
        {
            if (debugTopology) Debug.Log("[TOPOLOGY] No wire endpoints found - empty circuit");
            ClearDebugVisualization();
            return topology;
        }

        if (debugTopology) Debug.Log($"[TOPOLOGY] Discovering junctions from {allEndpoints.Length} endpoints");

        // 2. Group endpoints into junctions
        var processedEndpoints = new HashSet<WireEndpoint>();
        int junctionCounter = 0;

        foreach (var endpoint in allEndpoints)
        {
            if (processedEndpoints.Contains(endpoint))
                continue;

            // Find all endpoints that form a junction with this one
            var junctionEndpoints = FindJunctionEndpoints(endpoint, allEndpoints, processedEndpoints);

            // Only create junction if multiple endpoints are at the same location (FIX: prevents isolated endpoints from creating junctions)
            if (junctionEndpoints.Count > 1)
            {
                // Create junction
                var junction = new Junction
                {
                    id = $"Junction_{junctionCounter++}",
                    endpoints = junctionEndpoints,
                    position = CalculateJunctionCenter(junctionEndpoints)
                };

                topology.junctions.Add(junction);

                // Mark all endpoints as processed
                foreach (var ep in junctionEndpoints)
                {
                    processedEndpoints.Add(ep);
                }

                if (debugTopology) Debug.Log($"[TOPOLOGY] Created {junction.id} with {junction.endpoints.Count} endpoints at {junction.position}");
            }
        }

        // 3. Build component connection map
        BuildComponentConnections(topology);

        // 4. Create debug visualization (DISABLED - causes performance issues)
        // if (enableDebugVisualization)
        // {
        //     CreateDebugVisualization(topology);
        // }
        // else
        // {
        //     ClearDebugVisualization();
        // }

        // Store current topology
        currentTopology = topology;

        // 5. Build electrical graph (wire-based connectivity) - for debugging
        topology.BuildElectricalGraph();

        // 6. NEW: Path-centric terminal merging
        // Uses BFS from each terminal through wire chains to find connected terminals
        // This replaces MergeJunctionTerminalNodes() which failed on wire-to-wire chains
        TraceTerminalPaths();

        if (debugTopology) Debug.Log($"[TOPOLOGY] ✅ Discovered {topology.junctions.Count} junctions (path-centric traversal complete)");

        return topology;
    }

    /// <summary>
    /// Find all endpoints that form a junction with the given endpoint
    /// Uses both explicit snaps (snappedToEndpoint) and position proximity
    /// </summary>
    List<WireEndpoint> FindJunctionEndpoints(WireEndpoint startEndpoint, WireEndpoint[] allEndpoints, HashSet<WireEndpoint> processedEndpoints)
    {
        var junctionEndpoints = new List<WireEndpoint> { startEndpoint };
        var toProcess = new Queue<WireEndpoint>();
        toProcess.Enqueue(startEndpoint);

        while (toProcess.Count > 0)
        {
            var current = toProcess.Dequeue();

            // Check explicit snap reference
            if (current.SnappedToEndpoint != null && !junctionEndpoints.Contains(current.SnappedToEndpoint))
            {
                junctionEndpoints.Add(current.SnappedToEndpoint);
                toProcess.Enqueue(current.SnappedToEndpoint);
            }

            // Check position-based proximity (for endpoints that are close but not explicitly snapped)
            // FIX: Use strict tolerance for floating endpoints to avoid false junctions
            foreach (var endpoint in allEndpoints)
            {
                if (junctionEndpoints.Contains(endpoint))
                    continue;

                if (processedEndpoints.Contains(endpoint))
                    continue;

                // Check if this endpoint is at the same position as any endpoint in current junction
                foreach (var junctionEndpoint in junctionEndpoints)
                {
                    // Only use position tolerance if BOTH endpoints are at terminals
                    // This prevents false junctions between nearby floating wire endpoints
                    bool bothAtTerminals = endpoint.IsConnected && junctionEndpoint.IsConnected;
                    float tolerance = bothAtTerminals ? positionTolerance : 0.01f;  // Strict for floating

                    float distance = Vector3.Distance(endpoint.GetPosition(), junctionEndpoint.GetPosition());
                    if (distance <= tolerance)
                    {
                        junctionEndpoints.Add(endpoint);
                        toProcess.Enqueue(endpoint);
                        break;
                    }
                }
            }
        }

        return junctionEndpoints;
    }

    /// <summary>
    /// Calculate the center position of a junction (average of endpoint positions)
    /// </summary>
    Vector3 CalculateJunctionCenter(List<WireEndpoint> endpoints)
    {
        if (endpoints.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var endpoint in endpoints)
        {
            sum += endpoint.GetPosition();
        }
        return sum / endpoints.Count;
    }

    /// <summary>
    /// Build map of which endpoints connect to which components
    /// </summary>
    void BuildComponentConnections(CircuitTopology topology)
    {
        topology.componentConnections.Clear();

        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
            return;

        // For each component, find all wire endpoints that connect to its terminals
        foreach (var component in circuitManager.Components)
        {
            if (component == null)
                continue;

            var terminals = component.GetComponentsInChildren<ComponentTerminal>();
            var connectedEndpoints = new List<WireEndpoint>();

            foreach (var terminal in terminals)
            {
                // Find all junctions that connect to this terminal
                foreach (var junction in topology.junctions)
                {
                    if (junction.GetConnectedTerminal() == terminal)
                    {
                        connectedEndpoints.AddRange(junction.endpoints);
                    }
                }
            }

            if (connectedEndpoints.Count > 0)
            {
                topology.componentConnections[component] = connectedEndpoints;
            }
        }
    }

    /// <summary>
    /// Create GameObject hierarchy to visualize topology in Unity Editor
    /// This replaces console logs with visible structure
    /// </summary>
    void CreateDebugVisualization(CircuitTopology topology)
    {
        // Clear old visualization
        ClearDebugVisualization();

        if (!enableDebugVisualization || topology == null)
            return;

        // Create GameObject for each junction
        foreach (var junction in topology.junctions)
        {
            // Create junction root GameObject
            GameObject junctionObj = new GameObject(junction.id);
            junctionObj.transform.SetParent(topologyDebugRoot.transform);
            junctionObj.transform.position = junction.position;

            // Add visual sphere at junction position
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "JunctionMarker";
            sphere.transform.SetParent(junctionObj.transform);
            sphere.transform.position = junction.position;
            sphere.transform.localScale = Vector3.one * junctionDebugSphereSize;

            // Color the sphere
            var renderer = sphere.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = junctionDebugColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", junctionDebugColor * 0.5f);
                renderer.material = mat;
                debugMaterials.Add(mat);  // FIX: Track material for cleanup
            }

            // Remove collider (debug only)
            Destroy(sphere.GetComponent<Collider>());

            // Add info component
            var info = junctionObj.AddComponent<JunctionDebugInfo>();
            info.junction = junction;

            // Store reference
            junction.debugObject = junctionObj;

            // Create child GameObjects for each endpoint
            foreach (var endpoint in junction.endpoints)
            {
                GameObject endpointDebug = new GameObject($"Endpoint_{endpoint.name}");
                endpointDebug.transform.SetParent(junctionObj.transform);
                endpointDebug.transform.position = endpoint.GetPosition();

                // Add line to connect debug object to actual endpoint
                LineRenderer line = endpointDebug.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;

                // FIX: Track line material for cleanup
                var lineMat = new Material(Shader.Find("Sprites/Default"));
                lineMat.color = Color.cyan;
                line.material = lineMat;
                debugMaterials.Add(lineMat);

                line.SetPosition(0, junction.position);
                line.SetPosition(1, endpoint.GetPosition());
            }
        }

        Debug.Log($"[TOPOLOGY DEBUG] Created visualization for {topology.junctions.Count} junctions in hierarchy");
    }

    /// <summary>
    /// Clear all debug visualization GameObjects
    /// FIX: Now properly cleans up materials to prevent memory leaks
    /// </summary>
    void ClearDebugVisualization()
    {
        if (topologyDebugRoot == null)
            return;

        // Destroy all children
        for (int i = topologyDebugRoot.transform.childCount - 1; i >= 0; i--)
        {
            var child = topologyDebugRoot.transform.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // FIX: Cleanup materials to prevent memory leaks
        foreach (var mat in debugMaterials)
        {
            if (mat != null)
            {
                if (Application.isPlaying)
                    Destroy(mat);
                else
                    DestroyImmediate(mat);
            }
        }
        debugMaterials.Clear();
    }

    /// <summary>
    /// Get the current topology (null if not built yet)
    /// </summary>
    public CircuitTopology GetCurrentTopology()
    {
        return currentTopology;
    }

    void OnDestroy()
    {
        ClearDebugVisualization();

        // Clean up debug root
        if (topologyDebugRoot != null)
        {
            if (Application.isPlaying)
                Destroy(topologyDebugRoot);
            else
                DestroyImmediate(topologyDebugRoot);
        }
    }
}

/// <summary>
/// Component attached to debug GameObjects to show junction info in Inspector
/// </summary>
public class JunctionDebugInfo : MonoBehaviour
{
    [HideInInspector]
    public JunctionTopologyManager.Junction junction;

    [Header("Junction Information")]
    public string junctionId;
    public int endpointCount;
    public bool isFloating;
    public string connectedTerminal;
    public Vector3 junctionPosition;

    void Update()
    {
        if (junction != null)
        {
            junctionId = junction.id;
            endpointCount = junction.endpoints.Count;
            isFloating = junction.IsFloating;
            junctionPosition = junction.position;

            var terminal = junction.GetConnectedTerminal();
            connectedTerminal = terminal != null ? terminal.name : "None (Floating)";
        }
    }
}

/// <summary>
/// Component attached to topology debug root to show summary in Inspector
/// </summary>
public class TopologyDebugInfo : MonoBehaviour
{
    [HideInInspector]
    public JunctionTopologyManager manager;

    [Header("Topology Summary")]
    public int totalJunctions;
    public int floatingJunctions;
    public int connectedJunctions;

    void Update()
    {
        if (manager != null)
        {
            var topology = manager.GetCurrentTopology();
            if (topology != null)
            {
                totalJunctions = topology.junctions.Count;
                floatingJunctions = topology.junctions.Count(j => j.IsFloating);
                connectedJunctions = topology.junctions.Count(j => !j.IsFloating);
            }
        }
    }
}
