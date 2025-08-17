using System;
using System.Collections.Generic;
using UnityEngine;

public class SolverValidation : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== CIRCUIT SOLVER VALIDATION ===");
        bool allPassed = true;
        
        allPassed &= ValidateSeriesCircuit();
        allPassed &= ValidateParallelCircuit();
        allPassed &= ValidateMixedCircuit();
        
        if (allPassed)
        {
            Debug.Log("<color=green>✅ ALL SOLVER TESTS PASSED!</color>");
        }
        else
        {
            Debug.LogError("❌ SOLVER VALIDATION FAILED!");
        }
    }
    
    bool ValidateSeriesCircuit()
    {
        Debug.Log("\n--- VALIDATING SERIES CIRCUIT ---");
        Debug.Log("Circuit: 6V Battery -> 3Ω -> 3Ω");
        
        var node1 = new CircuitNode("Node1");
        var node2 = new CircuitNode("Node2");
        var node3 = new CircuitNode("Node3");
        
        var battery = new Battery("Battery", node1, node3, 6f);
        var r1 = new Resistor("R1", node1, node2, 3f);
        var r2 = new Resistor("R2", node2, node3, 3f);
        
        var components = new List<CircuitComponent> { battery, r1, r2 };
        var solver = new CircuitSolver();
        solver.Solve(components);
        
        // Expected values from conversation
        bool passed = true;
        passed &= ValidateValue("Battery Current", battery.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R1 Current", r1.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R2 Current", r2.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R1 Voltage", r1.VoltageDrop, 3.0f, 0.01f);
        passed &= ValidateValue("R2 Voltage", r2.VoltageDrop, 3.0f, 0.01f);
        
        Debug.Log(passed ? "<color=green>✅ Series test PASSED</color>" : "<color=red>❌ Series test FAILED</color>");
        return passed;
    }
    
    bool ValidateParallelCircuit()
    {
        Debug.Log("\n--- VALIDATING PARALLEL CIRCUIT ---");
        Debug.Log("Circuit: 6V Battery -> (3Ω || 6Ω)");
        
        var node1 = new CircuitNode("Node1");
        var node2 = new CircuitNode("Node2");
        
        var battery = new Battery("Battery", node1, node2, 6f);
        var r1 = new Resistor("R1", node1, node2, 3f);
        var r2 = new Resistor("R2", node1, node2, 6f);
        
        var components = new List<CircuitComponent> { battery, r1, r2 };
        var solver = new CircuitSolver();
        solver.Solve(components);
        
        // Expected values from conversation
        bool passed = true;
        passed &= ValidateValue("Battery Current", battery.Current, 3.0f, 0.01f);
        passed &= ValidateValue("R1 Current", r1.Current, 2.0f, 0.01f);
        passed &= ValidateValue("R2 Current", r2.Current, 1.0f, 0.01f);
        passed &= ValidateValue("R1 Voltage", r1.VoltageDrop, 6.0f, 0.01f);
        passed &= ValidateValue("R2 Voltage", r2.VoltageDrop, 6.0f, 0.01f);
        
        Debug.Log(passed ? "<color=green>✅ Parallel test PASSED</color>" : "<color=red>❌ Parallel test FAILED</color>");
        return passed;
    }
    
    bool ValidateMixedCircuit()
    {
        Debug.Log("\n--- VALIDATING MIXED CIRCUIT ---");
        Debug.Log("Circuit: 12V -> 2Ω -> (4Ω || 6Ω) -> 2Ω");
        
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
        
        // Expected values from conversation (confirmed PERFECT)
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
        
        Debug.Log(passed ? "<color=green>✅ Mixed test PASSED</color>" : "<color=red>❌ Mixed test FAILED</color>");
        return passed;
    }
    
    bool ValidateValue(string name, float actual, float expected, float tolerance)
    {
        bool passed = Math.Abs(actual - expected) <= tolerance;
        string status = passed ? "✓" : "✗";
        string color = passed ? "green" : "red";
        Debug.Log($"  <color={color}>{status} {name}: {actual:F3} (expected {expected:F3})</color>");
        return passed;
    }
}