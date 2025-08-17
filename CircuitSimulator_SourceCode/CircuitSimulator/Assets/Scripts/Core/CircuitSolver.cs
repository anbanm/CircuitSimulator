// Enhanced circuit solver with proper nodal analysis (Grade 7-level)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CircuitSolver
{
    public static bool EnableDebugLog = true;
    private const float TOLERANCE = 1e-6f;
    private const int MAX_ITERATIONS = 100;

    public void Solve(List<CircuitComponent> components)
    {
        if (EnableDebugLog)
        {
            Debug.Log($"=== CIRCUIT SOLVER START ({components.Count} components) ===");
        }
        
        var battery = components.OfType<Battery>().FirstOrDefault();
        if (battery == null)
        {
            Debug.LogWarning("No battery found.");
            return;
        }

        // Get all unique nodes
        var nodes = GetAllNodes(components);
        
        if (EnableDebugLog)
        {
            Debug.Log($"Found {nodes.Count} unique nodes, Battery: {battery.Voltage}V");
        }
        
        // Set ground reference (battery negative terminal)
        battery.NodeB.Voltage = 0f;
        
        // Use nodal analysis to solve the circuit
        SolveNodalAnalysis(components, nodes, battery);
        
        // Calculate component currents and voltage drops
        CalculateComponentValues(components);
        
        if (EnableDebugLog)
        {
            Debug.Log("=== CIRCUIT SOLVER COMPLETE ===");
        }
    }

    private List<CircuitNode> GetAllNodes(List<CircuitComponent> components)
    {
        var nodes = new HashSet<CircuitNode>();
        foreach (var component in components)
        {
            nodes.Add(component.NodeA);
            nodes.Add(component.NodeB);
        }
        return nodes.ToList();
    }

    private void SolveNodalAnalysis(List<CircuitComponent> components, List<CircuitNode> nodes, Battery battery)
    {
        // Find all nodes except ground (battery negative)
        var unknownNodes = nodes.Where(n => n != battery.NodeB).ToList();
        
        // Create conductance matrix and current vector
        int n = unknownNodes.Count;
        float[,] G = new float[n, n]; // Conductance matrix
        float[] I = new float[n];     // Current vector
        
        // Build the conductance matrix
        for (int i = 0; i < n; i++)
        {
            var node = unknownNodes[i];
            
            // Sum of conductances connected to this node (diagonal term)
            float totalConductance = 0f;
            
            foreach (var component in node.ConnectedComponents)
            {
                if (component.Resistance > 0f && component.Resistance != float.MaxValue)
                {
                    float conductance = 1f / component.Resistance;
                    totalConductance += conductance;
                    
                    // Find the other node this component connects to
                    var otherNode = component.NodeA == node ? component.NodeB : component.NodeA;
                    
                    // If other node is not ground, add off-diagonal term
                    if (otherNode != battery.NodeB)
                    {
                        int j = unknownNodes.IndexOf(otherNode);
                        if (j >= 0)
                        {
                            G[i, j] -= conductance; // Off-diagonal terms are negative
                        }
                    }
                }
            }
            
            G[i, i] = totalConductance; // Diagonal term
            
            // Handle current sources (batteries)
            foreach (var component in node.ConnectedComponents)
            {
                if (component is Battery bat)
                {
                    // Current flows from positive to negative terminal
                    // If this node is the positive terminal, add current
                    // If this node is the negative terminal, subtract current
                    if (node == bat.NodeA)
                    {
                        // This is handled by setting the voltage directly for the battery positive terminal
                        // We'll use a different approach for batteries
                    }
                }
            }
        }
        
        // Handle battery voltage constraint
        // For simplicity, we'll use a direct approach for circuits with one battery
        var batteryPositiveIndex = unknownNodes.IndexOf(battery.NodeA);
        if (batteryPositiveIndex >= 0)
        {
            // Set the battery positive terminal voltage directly
            battery.NodeA.Voltage = battery.Voltage;
            
            // Solve for other node voltages using successive approximation
            SolveBySuccessiveApproximation(components, unknownNodes, battery);
        }
    }

    private void SolveBySuccessiveApproximation(List<CircuitComponent> components, List<CircuitNode> unknownNodes, Battery battery)
    {
        // Initialize voltages
        foreach (var node in unknownNodes)
        {
            if (node == battery.NodeA)
            {
                node.Voltage = battery.Voltage;
            }
            else if (float.IsNaN(node.Voltage))
            {
                node.Voltage = 0f; // Initial guess
            }
        }

        // Iterative solver using Kirchhoff's Current Law
        for (int iteration = 0; iteration < MAX_ITERATIONS; iteration++)
        {
            bool converged = true;
            
            foreach (var node in unknownNodes)
            {
                if (node == battery.NodeA) continue; // Battery voltage is fixed
                
                float oldVoltage = node.Voltage;
                float totalCurrent = 0f;
                float totalConductance = 0f;
                
                // Apply KCL: sum of currents into node = 0
                foreach (var component in node.ConnectedComponents)
                {
                    if (component.Resistance > 0f && component.Resistance != float.MaxValue)
                    {
                        var otherNode = component.NodeA == node ? component.NodeB : component.NodeA;
                        float conductance = 1f / component.Resistance;
                        
                        totalCurrent += conductance * otherNode.Voltage;
                        totalConductance += conductance;
                    }
                }
                
                if (totalConductance > 0f)
                {
                    node.Voltage = totalCurrent / totalConductance;
                }
                
                if (Math.Abs(node.Voltage - oldVoltage) > TOLERANCE)
                {
                    converged = false;
                }
            }
            
            if (converged) break;
        }
    }

    private void CalculateComponentValues(List<CircuitComponent> components)
    {
        Battery battery = null;
        
        // First pass: calculate all component currents and voltage drops
        foreach (var component in components)
        {
            if (component is Battery bat)
            {
                battery = bat;
                bat.VoltageDrop = bat.Voltage;
                // Battery current will be calculated after other components
            }
            else if (component.Resistance > 0f && component.Resistance != float.MaxValue)
            {
                // Calculate voltage drop across component
                float voltageA = component.NodeA.Voltage;
                float voltageB = component.NodeB.Voltage;
                component.VoltageDrop = Math.Abs(voltageA - voltageB);
                
                // Calculate current through component using Ohm's law
                component.Current = component.VoltageDrop / component.Resistance;
            }
            else if (component is Wire || (component is Switch sw && sw.Resistance == 0f))
            {
                // Wire or closed switch - no voltage drop, but current flows through them
                component.VoltageDrop = 0f;
                // Current will be calculated based on connected components
                // For now, we'll handle this after all resistive components are calculated
            }
            else
            {
                // Open switch or unknown component
                component.VoltageDrop = 0f;
                component.Current = 0f;
            }
        }
        
        // Second pass: calculate wire currents
        // Wires carry the same current as the path they're on
        foreach (var component in components)
        {
            if (component is Wire || (component is Switch sw && sw.Resistance == 0f))
            {
                // For wires, current equals the sum of currents from connected resistive components
                float totalCurrent = 0f;
                int currentCount = 0;
                
                // Look at all components connected to the same nodes
                foreach (var otherComp in components)
                {
                    if (otherComp != component && otherComp.Resistance > 0f && otherComp.Resistance != float.MaxValue)
                    {
                        // If they share nodes, they're in the same current path
                        if ((otherComp.NodeA == component.NodeA || otherComp.NodeA == component.NodeB ||
                             otherComp.NodeB == component.NodeA || otherComp.NodeB == component.NodeB))
                        {
                            totalCurrent += otherComp.Current;
                            currentCount++;
                        }
                    }
                }
                
                // Use average current or current from battery if connected
                if (currentCount > 0)
                {
                    component.Current = totalCurrent / currentCount;
                }
                else
                {
                    // Check if directly connected to battery
                    if ((component.NodeA == battery?.NodeA || component.NodeA == battery?.NodeB ||
                         component.NodeB == battery?.NodeA || component.NodeB == battery?.NodeB))
                    {
                        component.Current = battery?.Current ?? 0f;
                    }
                }
                
                if (EnableDebugLog)
                {
                    Debug.Log($"Wire {component.Id}: {component.Current:F3}A");
                }
            }
        }
        
        // Third pass: calculate battery current
        // Battery current equals the current flowing through it
        if (battery != null)
        {
            // Method: Sum all currents leaving the positive terminal (or entering the negative terminal)
            float totalCurrent = 0f;
            
            // Find all components connected to the battery's positive terminal
            foreach (var component in battery.NodeA.ConnectedComponents)
            {
                if (component != battery)
                {
                    // Current flows from positive terminal through this component
                    totalCurrent += component.Current;
                }
            }
            
            // Alternatively, we can sum currents entering the negative terminal
            // This should give the same result due to conservation of current
            float negativeCurrent = 0f;
            foreach (var component in battery.NodeB.ConnectedComponents)
            {
                if (component != battery)
                {
                    negativeCurrent += component.Current;
                }
            }
            
            // Use the positive terminal calculation (both should be equal)
            battery.Current = totalCurrent;
            
            // Debug verification
            if (EnableDebugLog && Math.Abs(totalCurrent - negativeCurrent) > TOLERANCE)
            {
                Debug.LogWarning($"Current mismatch: positive terminal = {totalCurrent}A, negative terminal = {negativeCurrent}A");
            }
        }
    }
    
    private float CalculateEquivalentResistance(List<CircuitComponent> components)
    {
        var battery = components.OfType<Battery>().FirstOrDefault();
        if (battery == null) return 0f;
        
        var nonBatteryComponents = components.Where(c => !(c is Battery) && 
            c.Resistance > 0f && c.Resistance != float.MaxValue).ToList();
        
        if (nonBatteryComponents.Count == 0) return 0f;
        
        // For now, use the simple approach: total voltage divided by battery current
        // This will be calculated after we know the actual current flow
        return battery.Voltage / (battery.Current > 0f ? battery.Current : 1f);
    }
}
