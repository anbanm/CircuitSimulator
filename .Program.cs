using System;
using System.Collections.Generic;
using System.Linq;

// Mock Unity Debug class for testing
public static class Debug
{
    public static void Log(string message) => Console.WriteLine(message);
    public static void LogWarning(string message) => Console.WriteLine($"WARNING: {message}");
    public static void LogError(string message) => Console.WriteLine($"ERROR: {message}");
}

// Copy all the circuit classes and solver for standalone testing
public class CircuitNode
{
    public string Id { get; private set; }
    public List<CircuitComponent> ConnectedComponents { get; private set; }
    public float Voltage { get; set; }
    public float Current { get; set; }

    public CircuitNode(string id)
    {
        Id = id;
        ConnectedComponents = new List<CircuitComponent>();
        Voltage = float.NaN;
        Current = 0f;
    }

    public void AddComponent(CircuitComponent component)
    {
        if (!ConnectedComponents.Contains(component))
        {
            ConnectedComponents.Add(component);
        }
    }
}

public abstract class CircuitComponent
{
    public string Id { get; protected set; }
    public CircuitNode NodeA { get; set; }
    public CircuitNode NodeB { get; set; }
    public abstract float Resistance { get; }
    public virtual float VoltageDrop { get; set; }
    public virtual float Current { get; set; }

    protected CircuitComponent(string id, CircuitNode nodeA, CircuitNode nodeB)
    {
        Id = id;
        NodeA = nodeA;
        NodeB = nodeB;
        NodeA.AddComponent(this);
        NodeB.AddComponent(this);
    }
}

public class Battery : CircuitComponent
{
    public float Voltage { get; private set; }
    public override float Resistance => 0f;

    public Battery(string id, CircuitNode a, CircuitNode b, float voltage)
        : base(id, a, b)
    {
        Voltage = voltage;
    }
}

public class Resistor : CircuitComponent
{
    private float resistance;
    public override float Resistance => resistance;

    public Resistor(string id, CircuitNode a, CircuitNode b, float resistanceOhms)
        : base(id, a, b)
    {
        resistance = resistanceOhms;
    }
}

public class Wire : CircuitComponent
{
    public override float Resistance => 0f;

    public Wire(string id, CircuitNode a, CircuitNode b)
        : base(id, a, b) { }
}

public class Switch : CircuitComponent
{
    private bool isClosed;
    public override float Resistance => isClosed ? 0f : float.MaxValue;

    public Switch(string id, CircuitNode a, CircuitNode b, bool closed)
        : base(id, a, b)
    {
        isClosed = closed;
    }
}

public class Multimeter
{
    public float MeasureVoltage(CircuitNode positive, CircuitNode negative)
    {
        return Math.Abs(positive.Voltage - negative.Voltage);
    }

    public float MeasureCurrent(CircuitComponent component)
    {
        return component.Current;
    }
}

// INSERT THE COMPLETE CIRCUIT SOLVER CODE HERE
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
        
        // Handle battery voltage constraint
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
            }
            else
            {
                // Open switch or unknown component
                component.VoltageDrop = 0f;
                component.Current = 0f;
            }
        }
        
        // Second pass: calculate wire currents
        foreach (var component in components)
        {
            if (component is Wire || (component is Switch sw && sw.Resistance == 0f))
            {
                // For wires, find a connected resistive component and use its current
                foreach (var otherComp in components)
                {
                    if (otherComp != component && otherComp.Resistance > 0f && otherComp.Resistance != float.MaxValue)
                    {
                        // If they share a node, they carry the same current
                        if (otherComp.NodeA == component.NodeA || otherComp.NodeA == component.NodeB ||
                            otherComp.NodeB == component.NodeA || otherComp.NodeB == component.NodeB)
                        {
                            component.Current = otherComp.Current;
                            break;
                        }
                    }
                }
                
                if (EnableDebugLog)
                {
                    Debug.Log($"Wire {component.Id}: {component.Current:F3}A");
                }
            }
        }
        
        // Third pass: calculate battery current
        if (battery != null)
        {
            // Sum all currents leaving the positive terminal
            float totalCurrent = 0f;
            
            foreach (var component in battery.NodeA.ConnectedComponents)
            {
                if (component != battery)
                {
                    totalCurrent += component.Current;
                }
            }
            
            battery.Current = totalCurrent;
        }
    }
}

// Test program
class Program
{
    static void Main()
    {
        Console.WriteLine("=== CIRCUIT SOLVER VALIDATION ===\n");
        
        bool allPassed = true;
        allPassed &= TestSeriesCircuit();
        allPassed &= TestParallelCircuit();
        allPassed &= TestMixedCircuit();
        
        Console.WriteLine($"\n=== FINAL RESULT ===");
        if (allPassed)
        {
            Console.WriteLine("✅ ALL TESTS PASSED!");
        }
        else
        {
            Console.WriteLine("❌ SOME TESTS FAILED!");
        }
    }
    
    static bool TestSeriesCircuit()
    {
        Console.WriteLine("--- TESTING SERIES CIRCUIT ---");
        Console.WriteLine("Circuit: 6V Battery -> 3Ω -> 3Ω");
        
        var node1 = new CircuitNode("Node1");
        var node2 = new CircuitNode("Node2");
        var node3 = new CircuitNode("Node3");
        
        var battery = new Battery("Battery", node1, node3, 6f);
        var r1 = new Resistor("R1", node1, node2, 3f);
        var r2 = new Resistor("R2", node2, node3, 3f);
        
        var components = new List<CircuitComponent> { battery, r1, r2 };
        var solver = new CircuitSolver();
        solver.Solve(components);
        
        // Expected values from conversation: all 1A, R1=3V, R2=3V
        bool passed = true;
        passed &= ValidateValue("Battery Current", battery.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R1 Current", r1.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R2 Current", r2.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R1 Voltage", r1.VoltageDrop, 3.0f, 0.01f);
        passed &= ValidateValue("R2 Voltage", r2.VoltageDrop, 3.0f, 0.01f);
        
        Console.WriteLine(passed ? "✅ Series test PASSED\n" : "❌ Series test FAILED\n");
        return passed;
    }
    
    static bool TestParallelCircuit()
    {
        Console.WriteLine("--- TESTING PARALLEL CIRCUIT ---");
        Console.WriteLine("Circuit: 6V Battery -> (3Ω || 6Ω)");
        
        var node1 = new CircuitNode("Node1");
        var node2 = new CircuitNode("Node2");
        
        var battery = new Battery("Battery", node1, node2, 6f);
        var r1 = new Resistor("R1", node1, node2, 3f);
        var r2 = new Resistor("R2", node1, node2, 6f);
        
        var components = new List<CircuitComponent> { battery, r1, r2 };
        var solver = new CircuitSolver();
        solver.Solve(components);
        
        // Expected: R1=2A, R2=1A, Battery=3A, both resistors=6V
        bool passed = true;
        passed &= ValidateValue("Battery Current", battery.Current, 3.0f, 0.01f);
        passed &= ValidateValue("R1 Current", r1.Current, 2.0f, 0.01f);
        passed &= ValidateValue("R2 Current", r2.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R1 Voltage", r1.VoltageDrop, 6.0f, 0.01f);
        passed &= ValidateValue("R2 Voltage", r2.VoltageDrop, 6.0f, 0.01f);
        
        Console.WriteLine(passed ? "✅ Parallel test PASSED\n" : "❌ Parallel test FAILED\n");
        return passed;
    }
    
    static bool TestMixedCircuit()
    {
        Console.WriteLine("--- TESTING MIXED CIRCUIT ---");
        Console.WriteLine("Circuit: 12V -> 2Ω -> (4Ω || 6Ω) -> 2Ω");
        
        var node1 = new CircuitNode("Node1");
        var node2 = new CircuitNode("Node2");
        var node3 = new CircuitNode("Node3");
        var node4 = new CircuitNode("Node4");
        
        var battery = new Battery("Battery", node1, node4, 12f);
        var r1 = new Resistor("R1", node1, node2, 2f);
        var r2 = new Resistor("R2", node2, node3, 4f);
        var r3 = new Resistor("R3", node2, node3, 6f);
        var r4 = new Resistor("R4", node3, node4, 2f);
        
        var components = new List<CircuitComponent> { battery, r1, r2, r3, r4 };
        var solver = new CircuitSolver();
        solver.Solve(components);
        
        // Expected values from conversation (marked as PERFECT)
        bool passed = true;
        passed &= ValidateValue("Battery Current", battery.Current, 1.875f, 0.01f);
        passed &= ValidateValue("R1 Current", r1.Current, 1.875f, 0.01f);
        passed &= ValidateValue("R1 Voltage", r1.VoltageDrop, 3.75f, 0.01f);
        passed &= ValidateValue("R2 Current", r2.Current, 1.125f, 0.01f);
        passed &= ValidateValue("R2 Voltage", r2.VoltageDrop, 4.5f, 0.01f);
        passed &= ValidateValue("R3 Current", r3.Current, 0.75f, 0.01f);
        passed &= ValidateValue("R3 Voltage", r3.VoltageDrop, 4.5f, 0.01f);
        passed &= ValidateValue("R4 Current", r4.Current, 1.875f, 0.01f);
        passed &= ValidateValue("R4 Voltage", r4.VoltageDrop, 3.75f, 0.01f);
        
        Console.WriteLine(passed ? "✅ Mixed test PASSED\n" : "❌ Mixed test FAILED\n");
        return passed;
    }
    
    static bool ValidateValue(string name, float actual, float expected, float tolerance)
    {
        bool passed = Math.Abs(actual - expected) <= tolerance;
        string status = passed ? "✓" : "✗";
        Console.WriteLine($"  {status} {name}: {actual:F3} (expected {expected:F3})");
        return passed;
    }
}