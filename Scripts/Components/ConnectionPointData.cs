using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime data component for connection points with electrical properties
/// Added automatically to terminal GameObjects
/// </summary>
public class ConnectionPointData : MonoBehaviour
{
    [Header("Electrical Properties")]
    public ElectricalPolarity polarity = ElectricalPolarity.Neutral;
    public ComponentType componentType;
    public string label = "";
    public Color terminalColor = Color.gray;

    [Header("Connection Management")]
    public List<GameObject> connectedWires = new List<GameObject>();
    public float connectionRadius = 0.5f;
    public bool allowMultipleConnections = true;

    [Header("Educational")]
    public string educationalTooltip = "";
    public bool showCurrentFlow = false;
    public float currentMagnitude = 0f;
    public bool isCurrentSource = false;
    public bool isCurrentSink = false;

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public bool CanAcceptConnection()
    {
        if (!allowMultipleConnections && connectedWires.Count > 0)
            return false;

        // Educational: Check for common mistakes
        if (componentType == ComponentType.Battery && polarity == ElectricalPolarity.Positive)
        {
            // Positive terminal of battery should typically connect to circuit
            return true;
        }

        return true;
    }

    public void AddWire(GameObject wire)
    {
        if (!connectedWires.Contains(wire))
        {
            connectedWires.Add(wire);
            Debug.Log($"Connected wire to {polarity} terminal of {componentType}");
        }
    }

    public void RemoveWire(GameObject wire)
    {
        connectedWires.Remove(wire);
        Debug.Log($"Disconnected wire from {polarity} terminal of {componentType}");
    }

    public string GetEducationalDescription()
    {
        switch (polarity)
        {
            case ElectricalPolarity.Positive:
                return "Positive terminal - conventional current flows FROM here";
            case ElectricalPolarity.Negative:
                return "Negative terminal - conventional current flows TO here";
            case ElectricalPolarity.Anode:
                return "Anode - positive terminal of LED/diode, current enters here";
            case ElectricalPolarity.Cathode:
                return "Cathode - negative terminal of LED/diode, current exits here";
            case ElectricalPolarity.Ground:
                return "Ground - reference point (0V) for the circuit";
            default:
                return "Connection point for circuit component";
        }
    }
}