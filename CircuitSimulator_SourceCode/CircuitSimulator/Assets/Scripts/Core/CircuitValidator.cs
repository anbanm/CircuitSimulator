using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CircuitValidator
{
    public ValidationResult ValidateCircuit(List<CircuitComponent> components)
    {
        var result = new ValidationResult();
        
        if (components == null || components.Count == 0)
        {
            result.AddError("No components found in circuit");
            return result;
        }
        
        // Check for battery
        var batteries = components.OfType<Battery>().ToList();
        if (batteries.Count == 0)
        {
            result.AddError("Circuit must contain at least one battery");
        }
        else if (batteries.Count > 1)
        {
            result.AddWarning($"Multiple batteries detected ({batteries.Count}) - this may cause unexpected behavior");
        }
        
        // Check for resistive components
        var resistiveComponents = components.Where(c => !(c is Battery) && c.Resistance > 0f && c.Resistance != float.MaxValue).ToList();
        if (resistiveComponents.Count == 0)
        {
            result.AddWarning("Circuit has no resistive components - may cause infinite current");
        }
        
        // Check for complete paths
        if (batteries.Count > 0)
        {
            var primaryBattery = batteries[0];
            if (!HasCompletePath(components, primaryBattery))
            {
                result.AddError("Circuit is not complete - components are not properly connected");
                
                // Provide helpful details about disconnected components
                var disconnected = GetDisconnectedComponents(components, primaryBattery);
                foreach (var comp in disconnected)
                {
                    result.AddError($"Component '{comp.Id}' is not connected to the main circuit");
                }
            }
        }
        
        // Check for floating components (components with no connections)
        var floating = GetFloatingComponents(components);
        foreach (var comp in floating)
        {
            result.AddWarning($"Component '{comp.Id}' appears to be floating (no connections to other components)");
        }
        
        // Check for short circuits (zero resistance paths)
        var shortCircuits = DetectShortCircuits(components);
        foreach (var shortCircuit in shortCircuits)
        {
            result.AddWarning($"Potential short circuit detected involving components: {string.Join(", ", shortCircuit.Select(c => c.Id))}");
        }
        
        // AR-specific validations
        ValidateForAR(components, result);
        
        return result;
    }
    
    private bool HasCompletePath(List<CircuitComponent> components, Battery battery)
    {
        if (battery == null) return false;
        
        // Use graph traversal to check if we can reach all components from the battery
        var visited = new HashSet<CircuitNode>();
        var toVisit = new Queue<CircuitNode>();
        toVisit.Enqueue(battery.NodeA);
        
        while (toVisit.Count > 0)
        {
            var node = toVisit.Dequeue();
            if (visited.Contains(node)) continue;
            visited.Add(node);
            
            // Visit all connected components
            foreach (var comp in node.ConnectedComponents)
            {
                var otherNode = comp.NodeA == node ? comp.NodeB : comp.NodeA;
                
                // Only traverse through components with valid connections (not open switches)
                if (comp.Resistance != float.MaxValue && !visited.Contains(otherNode))
                {
                    toVisit.Enqueue(otherNode);
                }
            }
        }
        
        // Check if all component nodes were visited
        var allNodes = new HashSet<CircuitNode>();
        foreach (var comp in components)
        {
            allNodes.Add(comp.NodeA);
            allNodes.Add(comp.NodeB);
        }
        
        // A circuit is complete if we can reach all nodes from the battery
        var reachableNodes = visited.Count;
        var totalNodes = allNodes.Count;
        
        // Allow for some unreachable nodes (due to open switches, etc.)
        return reachableNodes >= totalNodes * 0.8f; // 80% reachability threshold
    }
    
    private List<CircuitComponent> GetDisconnectedComponents(List<CircuitComponent> components, Battery battery)
    {
        var disconnected = new List<CircuitComponent>();
        
        if (battery == null) return components.ToList();
        
        var visited = new HashSet<CircuitNode>();
        var toVisit = new Queue<CircuitNode>();
        toVisit.Enqueue(battery.NodeA);
        
        while (toVisit.Count > 0)
        {
            var node = toVisit.Dequeue();
            if (visited.Contains(node)) continue;
            visited.Add(node);
            
            foreach (var comp in node.ConnectedComponents)
            {
                var otherNode = comp.NodeA == node ? comp.NodeB : comp.NodeA;
                if (comp.Resistance != float.MaxValue && !visited.Contains(otherNode))
                {
                    toVisit.Enqueue(otherNode);
                }
            }
        }
        
        // Find components whose nodes were not visited
        foreach (var comp in components)
        {
            if (!visited.Contains(comp.NodeA) && !visited.Contains(comp.NodeB))
            {
                disconnected.Add(comp);
            }
        }
        
        return disconnected;
    }
    
    private List<CircuitComponent> GetFloatingComponents(List<CircuitComponent> components)
    {
        var floating = new List<CircuitComponent>();
        
        foreach (var comp in components)
        {
            // A component is floating if its nodes have no other connected components
            int nodeAConnections = comp.NodeA.ConnectedComponents.Count;
            int nodeBConnections = comp.NodeB.ConnectedComponents.Count;
            
            // Each component connects to its own nodes, so we need at least 2 connections per node
            if (nodeAConnections < 2 && nodeBConnections < 2)
            {
                floating.Add(comp);
            }
        }
        
        return floating;
    }
    
    private List<List<CircuitComponent>> DetectShortCircuits(List<CircuitComponent> components)
    {
        var shortCircuits = new List<List<CircuitComponent>>();
        
        // Look for paths with zero total resistance (excluding batteries)
        var nonBatteryComponents = components.Where(c => !(c is Battery)).ToList();
        
        foreach (var battery in components.OfType<Battery>())
        {
            var zeroResistancePath = FindZeroResistancePath(battery.NodeA, battery.NodeB, components);
            if (zeroResistancePath.Count > 0)
            {
                shortCircuits.Add(zeroResistancePath);
            }
        }
        
        return shortCircuits;
    }
    
    private List<CircuitComponent> FindZeroResistancePath(CircuitNode start, CircuitNode end, List<CircuitComponent> components)
    {
        // Simple DFS to find if there's a zero-resistance path
        var visited = new HashSet<CircuitNode>();
        var path = new List<CircuitComponent>();
        
        if (FindZeroResistancePathDFS(start, end, visited, path, components))
        {
            return path;
        }
        
        return new List<CircuitComponent>();
    }
    
    private bool FindZeroResistancePathDFS(CircuitNode current, CircuitNode target, HashSet<CircuitNode> visited, List<CircuitComponent> path, List<CircuitComponent> components)
    {
        if (current == target && path.Count > 0)
        {
            // Check if total resistance is essentially zero
            float totalResistance = path.Where(c => !(c is Battery)).Sum(c => c.Resistance);
            return totalResistance < 0.1f; // Very low resistance threshold
        }
        
        if (visited.Contains(current)) return false;
        visited.Add(current);
        
        foreach (var comp in current.ConnectedComponents)
        {
            if (comp.Resistance == 0f || comp is Wire) // Zero resistance components
            {
                var nextNode = comp.NodeA == current ? comp.NodeB : comp.NodeA;
                path.Add(comp);
                
                if (FindZeroResistancePathDFS(nextNode, target, visited, path, components))
                {
                    return true;
                }
                
                path.RemoveAt(path.Count - 1);
            }
        }
        
        visited.Remove(current);
        return false;
    }
    
    private void ValidateForAR(List<CircuitComponent> components, ValidationResult result)
    {
        // AR-specific validations
        
        // Check component spacing (important for AR tracking)
        // TODO: Implement spatial validation when 3D position data is available
        // This would check minimum spacing between components for AR tracking
        
        // Validate component count for AR performance
        if (components.Count > 20)
        {
            result.AddWarning($"Large number of components ({components.Count}) may impact AR performance");
        }
        
        // Check for complexity that might be hard to visualize in AR
        var maxConnectionsPerNode = 0;
        var allNodes = new HashSet<CircuitNode>();
        foreach (var comp in components)
        {
            allNodes.Add(comp.NodeA);
            allNodes.Add(comp.NodeB);
        }
        
        foreach (var node in allNodes)
        {
            maxConnectionsPerNode = Mathf.Max(maxConnectionsPerNode, node.ConnectedComponents.Count);
        }
        
        if (maxConnectionsPerNode > 4)
        {
            result.AddWarning($"Complex connection point detected ({maxConnectionsPerNode} components) - may be hard to visualize clearly in AR");
        }
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();
    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;
    
    public void AddError(string error)
    {
        Errors.Add(error);
        Debug.LogError($"Circuit Validation Error: {error}");
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
        Debug.LogWarning($"Circuit Validation Warning: {warning}");
    }
    
    public string GetSummary()
    {
        if (IsValid && !HasWarnings)
        {
            return "✅ Circuit validation passed with no issues";
        }
        
        var summary = $"Circuit Validation Results:\n";
        
        if (Errors.Count > 0)
        {
            summary += $"❌ {Errors.Count} Error(s):\n";
            foreach (var error in Errors)
            {
                summary += $"  • {error}\n";
            }
        }
        
        if (Warnings.Count > 0)
        {
            summary += $"⚠️ {Warnings.Count} Warning(s):\n";
            foreach (var warning in Warnings)
            {
                summary += $"  • {warning}\n";
            }
        }
        
        return summary;
    }
}