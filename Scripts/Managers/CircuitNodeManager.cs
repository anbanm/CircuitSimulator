using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SOLVER LAYER: Creates circuit solver nodes from topology
/// Uses JunctionTopologyManager to discover connectivity
/// No longer uses position-based discovery
/// </summary>
public class CircuitNodeManager : MonoBehaviour
{
    private CircuitDebugManager debugManager;
    private JunctionTopologyManager topologyManager;

    void Start()
    {
        debugManager = GetComponent<CircuitDebugManager>();
        topologyManager = FindFirstObjectByType<JunctionTopologyManager>();

        if (topologyManager == null)
        {
            Debug.LogError("[CircuitNodeManager] JunctionTopologyManager not found! Adding it...");
            topologyManager = gameObject.AddComponent<JunctionTopologyManager>();
        }
    }
    
    /// <summary>
    /// Create spatial node system using TOPOLOGY instead of positions
    /// This is the new topology-based approach
    /// </summary>
    public Dictionary<Vector3, CircuitNode> CreateSpatialNodeSystem()
    {
        var spatialNodes = new Dictionary<Vector3, CircuitNode>();

        if (debugManager != null)
        {
            debugManager.LogToFile("=== CREATING SPATIAL NODE SYSTEM (TOPOLOGY-BASED) ===");
        }

        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null) return spatialNodes;

        if (topologyManager == null)
        {
            Debug.LogError("[CircuitNodeManager] Cannot create nodes without JunctionTopologyManager!");
            return spatialNodes;
        }

        // 1. Build topology by discovering junctions
        var topology = topologyManager.BuildTopology();

        if (debugManager != null)
        {
            debugManager.LogToFile($"Topology discovered: {topology.junctions.Count} junctions");
        }

        // 2. Create CircuitNode for each junction
        foreach (var junction in topology.junctions)
        {
            var node = new CircuitNode(junction.id);

            // Map this node to the junction position for spatial queries
            spatialNodes[junction.position] = node;

            // Also map each endpoint position to this node (for backward compatibility)
            foreach (var endpoint in junction.endpoints)
            {
                spatialNodes[endpoint.GetPosition()] = node;
            }

            // Log junction info
            if (debugManager != null)
            {
                var terminal = junction.GetConnectedTerminal();
                string terminalInfo = terminal != null ? terminal.name : "Floating";
                debugManager.LogToFile($"Created {node.Id} at {junction.position} with {junction.endpoints.Count} endpoints, terminal: {terminalInfo}");
            }
        }

        // 3. Also create nodes for component terminals that don't have junctions yet
        // (for components that aren't connected to wires)
        foreach (var comp in circuitManager.Components)
        {
            if (comp == null) continue;

            var terminals = comp.GetComponentsInChildren<ComponentTerminal>();
            foreach (var terminal in terminals)
            {
                Vector3 terminalPos = terminal.transform.position;

                // Check if this terminal already has a node (from junction)
                bool hasNode = false;
                foreach (var junction in topology.junctions)
                {
                    if (junction.GetConnectedTerminal() == terminal)
                    {
                        hasNode = true;
                        break;
                    }
                }

                // Create node for unconnected terminal
                if (!hasNode)
                {
                    var node = new CircuitNode($"Node_Terminal_{terminal.name}");
                    spatialNodes[terminalPos] = node;

                    if (debugManager != null)
                    {
                        debugManager.LogToFile($"Created {node.Id} for unconnected terminal {terminal.name}");
                    }
                }
            }
        }

        if (debugManager != null)
        {
            debugManager.LogToFile($"✅ Spatial node system: {spatialNodes.Count} mapped positions, {spatialNodes.Values.Distinct().Count()} unique nodes");
        }

        return spatialNodes;
    }
    
    public CircuitNode GetSpatialNode(Dictionary<Vector3, CircuitNode> spatialNodes, Vector3 position, float tolerance)
    {
        // First try exact match
        if (spatialNodes.ContainsKey(position))
        {
            return spatialNodes[position];
        }
        
        // Then try nearby positions
        foreach (var kvp in spatialNodes)
        {
            if (Vector3.Distance(kvp.Key, position) <= tolerance)
            {
                return kvp.Value;
            }
        }
        
        // Create new node if none found
        var newNode = new CircuitNode($"Node_Auto_{spatialNodes.Count}");
        spatialNodes[position] = newNode;
        
        if (debugManager != null)
        {
            debugManager.LogToFile($"Created new node {newNode.Id} at {position}");
        }
        
        return newNode;
    }
    
    public void ValidateNodeConnections()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null) return;
        
        var spatialNodes = CreateSpatialNodeSystem();
        var orphanedNodes = new List<CircuitNode>();
        
        foreach (var node in spatialNodes.Values.Distinct())
        {
            if (node.ConnectedComponents.Count == 0)
            {
                orphanedNodes.Add(node);
            }
        }
        
        if (orphanedNodes.Count > 0)
        {
            Debug.LogWarning($"Found {orphanedNodes.Count} orphaned nodes");
            if (debugManager != null)
            {
                foreach (var node in orphanedNodes)
                {
                    debugManager.LogToFile($"Orphaned node: {node.Id}");
                }
            }
        }
    }
    
    public int GetNodeCount()
    {
        var spatialNodes = CreateSpatialNodeSystem();
        return spatialNodes.Values.Distinct().Count();
    }
    
    public List<CircuitNode> GetAllNodes()
    {
        var spatialNodes = CreateSpatialNodeSystem();
        return spatialNodes.Values.Distinct().ToList();
    }
}