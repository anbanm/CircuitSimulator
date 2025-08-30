using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the creation and positioning of input/output terminals on circuit components
/// Replaces spatial node sharing with explicit terminal connections
/// </summary>
public class ComponentTerminalManager : MonoBehaviour
{
    [Header("Terminal Settings")]
    public float terminalDistance = 0.4f;
    public Color inputColor = Color.green;
    public Color outputColor = Color.red;
    public Color terminalHighlightColor = Color.yellow;
    
    private CircuitManager circuitManager;
    private Dictionary<CircuitComponent3D, List<ComponentTerminal>> componentTerminals = new Dictionary<CircuitComponent3D, List<ComponentTerminal>>();
    
    public void Initialize()
    {
        circuitManager = CircuitManager.Instance;
        Debug.Log("ComponentTerminalManager initialized");
    }
    
    public void SetupComponentTerminals(CircuitComponent3D component)
    {
        if (component == null) return;
        
        RemoveComponentTerminals(component);
        var terminals = new List<ComponentTerminal>();
        
        switch (component.ComponentType)
        {
            case ComponentType.Battery:
                terminals.Add(CreateTerminal(component, Vector3.left * terminalDistance, true, "NegativeTerminal"));
                terminals.Add(CreateTerminal(component, Vector3.right * terminalDistance, false, "PositiveTerminal"));
                break;
                
            case ComponentType.Resistor:
            case ComponentType.Bulb:
                terminals.Add(CreateTerminal(component, Vector3.left * terminalDistance, true, "InputTerminal"));
                terminals.Add(CreateTerminal(component, Vector3.right * terminalDistance, false, "OutputTerminal"));
                break;
                
            case ComponentType.Switch:
                terminals.Add(CreateTerminal(component, Vector3.left * terminalDistance, true, "InputTerminal"));
                terminals.Add(CreateTerminal(component, Vector3.right * terminalDistance, false, "OutputTerminal"));
                break;
        }
        
        componentTerminals[component] = terminals;
        Debug.Log($"Created {terminals.Count} terminals for {component.name}");
    }
    
    ComponentTerminal CreateTerminal(CircuitComponent3D component, Vector3 localPosition, bool isInput, string terminalName)
    {
        GameObject terminalObj = new GameObject(terminalName);
        terminalObj.transform.SetParent(component.transform);
        terminalObj.transform.localPosition = localPosition;
        
        var terminal = terminalObj.AddComponent<ComponentTerminal>();
        terminal.isInput = isInput;
        terminal.terminalColor = isInput ? inputColor : outputColor;
        terminal.highlightColor = terminalHighlightColor;
        
        var collider = terminalObj.AddComponent<SphereCollider>();
        collider.radius = 0.3f;
        
        return terminal;
    }
    
    public void RemoveComponentTerminals(CircuitComponent3D component)
    {
        if (componentTerminals.ContainsKey(component))
        {
            foreach (var terminal in componentTerminals[component])
            {
                if (terminal != null)
                    DestroyImmediate(terminal.gameObject);
            }
            componentTerminals.Remove(component);
        }
    }
    
    public List<ComponentTerminal> GetComponentTerminals(CircuitComponent3D component)
    {
        if (componentTerminals.ContainsKey(component))
            return componentTerminals[component];
        return new List<ComponentTerminal>();
    }
    
    public ComponentTerminal GetClosestTerminal(Vector3 worldPosition, float maxDistance = 1.0f)
    {
        ComponentTerminal closest = null;
        float closestDistance = maxDistance;
        
        foreach (var terminals in componentTerminals.Values)
        {
            foreach (var terminal in terminals)
            {
                if (terminal == null) continue;
                
                float distance = Vector3.Distance(terminal.transform.position, worldPosition);
                if (distance < closestDistance)
                {
                    closest = terminal;
                    closestDistance = distance;
                }
            }
        }
        
        return closest;
    }
    
    public bool CanConnectTerminals(ComponentTerminal terminal1, ComponentTerminal terminal2)
    {
        if (terminal1 == null || terminal2 == null) return false;
        if (terminal1 == terminal2) return false;
        if (terminal1.ParentComponent == terminal2.ParentComponent) return false;
        
        return terminal1.isInput != terminal2.isInput;
    }
    
    public ComponentTerminal GetInputTerminal(CircuitComponent3D component)
    {
        var terminals = GetComponentTerminals(component);
        return terminals.Find(t => t.isInput);
    }
    
    public ComponentTerminal GetOutputTerminal(CircuitComponent3D component)
    {
        var terminals = GetComponentTerminals(component);
        return terminals.Find(t => !t.isInput);
    }
    
    public void OnComponentRegistered(CircuitComponent3D component)
    {
        SetupComponentTerminals(component);
    }
    
    public void OnComponentUnregistered(CircuitComponent3D component)
    {
        RemoveComponentTerminals(component);
    }
    
    public void UpdateLogicalConnections()
    {
        foreach (var componentPair in componentTerminals)
        {
            var component = componentPair.Key;
            var terminals = componentPair.Value;
            
            if (component.logicalComponent == null) continue;
            
            if (terminals.Count >= 2)
            {
                var inputTerminal = terminals.Find(t => t.isInput);
                var outputTerminal = terminals.Find(t => !t.isInput);
                
                if (inputTerminal?.electricalNode != null && outputTerminal?.electricalNode != null)
                {
                    component.logicalComponent.NodeA = inputTerminal.electricalNode;
                    component.logicalComponent.NodeB = outputTerminal.electricalNode;
                }
            }
        }
        
        Debug.Log("Updated logical connections for all components");
    }
}