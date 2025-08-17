// Core data model for circuit simulation (Grade 7-level)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public float GetVoltage() => Voltage;
    public float GetCurrent() => Current;
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

    public float GetVoltageDrop() => VoltageDrop;
    public float GetCurrent() => Current;
}

public class Bulb : CircuitComponent
{
    private float resistance;
    public override float Resistance => resistance;

    public Bulb(string id, CircuitNode a, CircuitNode b, float resistanceOhms)
        : base(id, a, b)
    {
        resistance = resistanceOhms;
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

    public void Toggle() => isClosed = !isClosed;
    public bool IsClosed() => isClosed;
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

public class Multimeter
{
    public float MeasureVoltage(CircuitNode nodeA, CircuitNode nodeB)
    {
        if (float.IsNaN(nodeA.Voltage) || float.IsNaN(nodeB.Voltage))
        {
            Debug.LogWarning("Multimeter probes not connected to a valid circuit reference.");
            return 0f;
        }
        return nodeA.GetVoltage() - nodeB.GetVoltage();
    }

    public float MeasureCurrent(CircuitComponent component)
    {
        return component.GetCurrent();
    }
}
