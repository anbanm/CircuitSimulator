using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages spatial node system for circuit connectivity
/// Handles wire management and node sharing logic
/// </summary>
public class CircuitNodeManager : MonoBehaviour
{
    private CircuitDebugManager debugManager;
    
    void Start()
    {
        debugManager = GetComponent<CircuitDebugManager>();
    }
    
    public Dictionary<Vector3, CircuitNode> CreateSpatialNodeSystem()
    {
        var spatialNodes = new Dictionary<Vector3, CircuitNode>();
        float connectionTolerance = 0.5f; // Unity units - adjust for AR scale
        
        if (debugManager != null)
        {
            debugManager.LogToFile("=== CREATING SPATIAL NODE SYSTEM ===");
        }
        
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null) return spatialNodes;
        
        // Create component terminal positions
        var terminalPositions = new List<Vector3>();
        
        foreach (var comp in circuitManager.Components)
        {
            if (comp == null) continue;
            
            Vector3 pos = comp.transform.position;
            terminalPositions.Add(pos + Vector3.left * 0.3f);   // Input terminal
            terminalPositions.Add(pos + Vector3.right * 0.3f);  // Output terminal
        }
        
        // Add wire endpoint positions
        foreach (var wireObj in circuitManager.Wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire != null)
            {
                if (wire.Component1 != null)
                    terminalPositions.Add(wire.Component1.transform.position);
                if (wire.Component2 != null)
                    terminalPositions.Add(wire.Component2.transform.position);
            }
        }
        
        // Group nearby positions into shared nodes
        var processedPositions = new HashSet<Vector3>();
        int nodeCounter = 0;
        
        foreach (var pos in terminalPositions)
        {
            if (processedPositions.Contains(pos)) continue;
            
            // Find all positions within connection tolerance
            var nearbyPositions = terminalPositions
                .Where(p => !processedPositions.Contains(p) && Vector3.Distance(pos, p) <= connectionTolerance)
                .ToList();
            
            // Create shared node for this group
            var sharedNode = new CircuitNode($"Node_{nodeCounter++}");
            
            // Map all nearby positions to this shared node
            foreach (var nearbyPos in nearbyPositions)
            {
                spatialNodes[nearbyPos] = sharedNode;
                processedPositions.Add(nearbyPos);
            }
            
            if (debugManager != null)
            {
                debugManager.LogToFile($"Created {sharedNode.Id} for {nearbyPositions.Count} positions");
            }
        }
        
        if (debugManager != null)
        {
            debugManager.LogToFile($"Spatial node system: {spatialNodes.Count} mapped positions, {spatialNodes.Values.Distinct().Count()} unique nodes");
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