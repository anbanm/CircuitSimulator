using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Linq;

/// <summary>
/// Handles all debugging, logging, and testing functionality
/// Manages debug file I/O and circuit validation
/// </summary>
public class CircuitDebugManager : MonoBehaviour
{
    private StringBuilder debugLog = new StringBuilder();
    private string debugFilePath;
    
    public void Initialize()
    {
        // Set up debug file path
        debugFilePath = Path.Combine(Application.persistentDataPath, "circuit_debug.log");
        LogToFile("=== CircuitDebugManager Initialized ===");
        LogToFile($"Debug file: {debugFilePath}");
    }
    
    public void LogToFile(string message)
    {
        string timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        debugLog.AppendLine(timestampedMessage);
        
        try
        {
            File.AppendAllText(debugFilePath, timestampedMessage + Environment.NewLine);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write to debug file: {e.Message}");
        }
    }
    
    public void LogCircuitChange()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            LogToFile($"Circuit changed: {circuitManager.ComponentCount} components, {circuitManager.WireCount} wires");
        }
    }
    
    public void SaveDebugReport()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            Debug.LogError("CircuitManager not found!");
            return;
        }
        
        string reportPath = Path.Combine(Application.persistentDataPath, $"circuit_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        
        try
        {
            using (var writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("=== CIRCUIT DEBUG REPORT ===");
                writer.WriteLine($"Generated: {DateTime.Now}");
                writer.WriteLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                writer.WriteLine();
                
                writer.WriteLine("Circuit Overview:");
                writer.WriteLine($"Components: {circuitManager.ComponentCount}");
                writer.WriteLine($"Wires: {circuitManager.WireCount}");
                writer.WriteLine($"Auto Solve: {circuitManager.autoSolve}");
                writer.WriteLine($"Solve Interval: {circuitManager.solveInterval}s");
                writer.WriteLine();
                
                writer.WriteLine("Component Details:");
                foreach (var comp in circuitManager.Components)
                {
                    if (comp != null)
                    {
                        writer.WriteLine($"- {comp.name}: Type={comp.ComponentType}, R={comp.resistance}Ω, V={comp.voltageDrop:F2}V, I={comp.current:F3}A");
                    }
                }
                
                writer.WriteLine();
                writer.WriteLine("Wire Details:");
                foreach (var wire in circuitManager.Wires)
                {
                    if (wire != null)
                    {
                        var wireComponent = wire.GetComponent<CircuitWire>();
                        if (wireComponent != null)
                        {
                            string comp1Name = wireComponent.Component1?.name ?? "NULL";
                            string comp2Name = wireComponent.Component2?.name ?? "NULL";
                            writer.WriteLine($"- {wire.name}: {comp1Name} <-> {comp2Name}");
                        }
                    }
                }
                
                writer.WriteLine();
                writer.WriteLine("=== END REPORT ===");
            }
            
            Debug.Log($"✅ Debug report saved: {reportPath}");
            LogToFile($"Debug report saved: {reportPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save debug report: {ex.Message}");
            LogToFile($"ERROR saving report: {ex.Message}");
        }
    }
    
    public void DebugComponentRegistration()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            Debug.LogError("CircuitManager not found!");
            return;
        }
        
        Debug.Log("=== Component Registration Debug ===");
        Debug.Log($"Total registered components: {circuitManager.ComponentCount}");
        Debug.Log($"Total registered wires: {circuitManager.WireCount}");
        
        Debug.Log("3D Components:");
        for (int i = 0; i < circuitManager.Components.Count; i++)
        {
            var comp = circuitManager.Components[i];
            if (comp != null)
            {
                Debug.Log($"  [{i}] {comp.name} - Type: {comp.ComponentType}, Position: {comp.transform.position}");
            }
            else
            {
                Debug.LogWarning($"  [{i}] NULL COMPONENT FOUND!");
            }
        }
        
        Debug.Log("Logical Components:");
        var logicalComponents = BuildLogicalCircuitForDebug();
        for (int i = 0; i < logicalComponents.Count; i++)
        {
            var comp = logicalComponents[i];
            if (comp != null)
            {
                Debug.Log($"  [{i}] {comp.Id} - Type: {comp.GetType().Name}, Resistance: {comp.Resistance}Ω");
            }
            else
            {
                Debug.LogWarning($"  [{i}] NULL LOGICAL COMPONENT FOUND!");
            }
        }
        
        Debug.Log("Wires:");
        for (int i = 0; i < circuitManager.Wires.Count; i++)
        {
            var wire = circuitManager.Wires[i];
            if (wire != null)
            {
                var wireComp = wire.GetComponent<CircuitWire>();
                string comp1 = wireComp?.Component1?.name ?? "NULL";
                string comp2 = wireComp?.Component2?.name ?? "NULL";
                Debug.Log($"  [{i}] {wire.name}: {comp1} <-> {comp2}");
            }
            else
            {
                Debug.LogWarning($"  [{i}] NULL WIRE FOUND!");
            }
        }
        
        Debug.Log("=== End Debug ===");
    }
    
    public void ValidateAndTestCircuit()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            Debug.LogError("CircuitManager not found!");
            return;
        }
        
        Debug.Log("=== Circuit Validation and Test ===");
        
        // Count components by type
        var batteryComps = circuitManager.Components.FindAll(c => c != null && c.ComponentType == ComponentType.Battery);
        var resistorComps = circuitManager.Components.FindAll(c => c != null && c.ComponentType == ComponentType.Resistor);
        var bulbComps = circuitManager.Components.FindAll(c => c != null && c.ComponentType == ComponentType.Bulb);
        var switchComps = circuitManager.Components.FindAll(c => c != null && c.ComponentType == ComponentType.Switch);
        
        Debug.Log($"Components found: {batteryComps.Count} batteries, {resistorComps.Count} resistors, {bulbComps.Count} bulbs, {switchComps.Count} switches");
        Debug.Log($"Wires: {circuitManager.WireCount}");
        
        // Basic validation
        if (batteryComps.Count == 0)
        {
            Debug.LogError("❌ No battery found - circuit cannot function");
            return;
        }
        
        if (resistorComps.Count + bulbComps.Count == 0)
        {
            Debug.LogError("❌ No resistive components found - circuit would have infinite current");
            return;
        }
        
        if (circuitManager.WireCount == 0)
        {
            Debug.LogWarning("⚠️ No wires found - components may not be connected");
        }
        
        // Test the current circuit
        Debug.Log("🔧 Testing current circuit configuration...");
        circuitManager.SolveCircuit();
        
        // Report results
        Debug.Log("📊 Circuit Test Results:");
        foreach (var comp in circuitManager.Components)
        {
            if (comp != null && comp.logicalComponent != null)
            {
                Debug.Log($"  {comp.name}: V={comp.voltageDrop:F2}V, I={comp.current:F3}A, R={comp.resistance:F1}Ω");
            }
        }
        
        Debug.Log("✅ Circuit test completed");
    }
    
    public void TestCircuitComponents()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            Debug.LogWarning("CircuitManager not found");
            return;
        }
        
        Debug.Log("=== Testing Circuit Components (No Wires Required) ===");
        
        if (circuitManager.Components.Count == 0)
        {
            Debug.LogWarning("No components found to test");
            return;
        }
        
        Debug.Log($"Found {circuitManager.Components.Count} components to test:");
        
        foreach (var comp in circuitManager.Components)
        {
            if (comp != null)
            {
                Debug.Log($"  ✓ {comp.name} (Type: {comp.ComponentType})");
                Debug.Log($"    Position: {comp.transform.position}");
                Debug.Log($"    Voltage: {comp.voltage}V, Resistance: {comp.resistance}Ω");
                
                // Test component properties
                if (comp.ComponentType == ComponentType.Battery && comp.voltage <= 0)
                {
                    Debug.LogWarning($"    ⚠️ Battery {comp.name} has invalid voltage: {comp.voltage}V");
                }
                
                if ((comp.ComponentType == ComponentType.Resistor || comp.ComponentType == ComponentType.Bulb) 
                    && comp.resistance <= 0)
                {
                    Debug.LogWarning($"    ⚠️ {comp.ComponentType} {comp.name} has invalid resistance: {comp.resistance}Ω");
                }
            }
            else
            {
                Debug.LogError("    ❌ Found null component in list!");
            }
        }
        
        Debug.Log("✅ Component test completed");
    }
    
    private List<CircuitComponent> BuildLogicalCircuitForDebug()
    {
        // This is a simplified version for debug purposes only
        var solverManager = GetComponent<CircuitSolverManager>();
        if (solverManager != null)
        {
            // We'd need to expose the BuildLogicalCircuit method or create a debug version
            // For now, return empty list
            return new List<CircuitComponent>();
        }
        return new List<CircuitComponent>();
    }
}