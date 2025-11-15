using System;
using UnityEngine;

/// <summary>
/// Root container for circuit save data.
/// Contains all components and wire connections needed to recreate a circuit.
/// Designed for Unity JsonUtility serialization.
/// </summary>
[Serializable]
public class CircuitSaveData
{
    public string version = "1.0";
    public ComponentData[] components;
    public WireData[] wires;
}

/// <summary>
/// Serializable data for a single circuit component.
/// Stores position in world coordinates and electrical properties.
/// </summary>
[Serializable]
public class ComponentData
{
    public string id;           // Component name (e.g., "Battery_0", "Bulb_1")
    public string type;         // Component type (e.g., "Battery", "Bulb", "Resistor", "Switch")
    public float[] position;    // World coordinates [x, y, z]

    // Electrical properties (only relevant fields will be used based on type)
    public float voltage;       // For Battery components
    public float resistance;    // For Resistor, Bulb components
    public bool switchClosed;   // For Switch components (true = closed/on, false = open/off)
}

/// <summary>
/// Serializable data for a wire connection between two component terminals.
/// </summary>
[Serializable]
public class WireData
{
    public string startComponent;  // Start component ID (e.g., "Battery_0")
    public string startTerminal;   // Start terminal name (e.g., "PositiveTerminal", "TerminalA")
    public string endComponent;    // End component ID (e.g., "Bulb_1")
    public string endTerminal;     // End terminal name (e.g., "TerminalB", "NegativeTerminal")
}
