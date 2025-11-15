using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles serialization and deserialization of circuit data to/from JSON.
/// Uses Unity's JsonUtility for simplicity and compatibility.
/// </summary>
public static class CircuitSerializer
{
    /// <summary>
    /// Serializes the current circuit to a JSON string.
    /// </summary>
    /// <param name="components">List of all circuit components</param>
    /// <param name="wires">List of all wire GameObjects</param>
    /// <returns>JSON string representation of the circuit</returns>
    public static string SerializeCircuit(List<CircuitComponent3D> components, List<GameObject> wires)
    {
        var saveData = new CircuitSaveData
        {
            version = "1.0",
            components = SerializeComponents(components),
            wires = SerializeWires(wires)
        };

        string json = JsonUtility.ToJson(saveData, prettyPrint: true);
        Debug.Log($"✅ Circuit serialized: {components.Count} components, {wires.Count} wires");
        return json;
    }

    /// <summary>
    /// Deserializes a JSON string back into circuit save data.
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>CircuitSaveData object</returns>
    public static CircuitSaveData DeserializeCircuit(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("❌ Cannot deserialize empty JSON string");
            return null;
        }

        try
        {
            CircuitSaveData saveData = JsonUtility.FromJson<CircuitSaveData>(json);
            Debug.Log($"✅ Circuit deserialized: v{saveData.version}, {saveData.components.Length} components, {saveData.wires.Length} wires");
            return saveData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to deserialize circuit JSON: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts a list of 3D components to serializable component data.
    /// </summary>
    private static ComponentData[] SerializeComponents(List<CircuitComponent3D> components)
    {
        return components.Select(c => new ComponentData
        {
            id = c.name,
            type = c.ComponentType.ToString(),
            position = new float[]
            {
                c.transform.position.x,
                c.transform.position.y,
                c.transform.position.z
            },
            voltage = c.voltage,
            resistance = c.resistance,
            switchClosed = false // TODO: Add switch state property to CircuitComponent3D when implemented
        }).ToArray();
    }

    /// <summary>
    /// Converts a list of wire GameObjects to serializable wire data.
    /// </summary>
    private static WireData[] SerializeWires(List<GameObject> wires)
    {
        var wireDataList = new List<WireData>();

        foreach (var wireObj in wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire == null)
            {
                Debug.LogWarning($"⚠️ Wire GameObject '{wireObj.name}' has no CircuitWire component, skipping");
                continue;
            }

            // Check for valid component connections
            if (wire.startComponent == null || wire.endComponent == null)
            {
                Debug.LogWarning($"⚠️ Wire '{wireObj.name}' has null component references, skipping");
                continue;
            }

            // Check for valid terminal connections
            if (wire.startTerminal == null || wire.endTerminal == null)
            {
                Debug.LogWarning($"⚠️ Wire '{wireObj.name}' has null terminal references, skipping");
                continue;
            }

            wireDataList.Add(new WireData
            {
                startComponent = wire.startComponent.name,
                startTerminal = wire.startTerminal.name,
                endComponent = wire.endComponent.name,
                endTerminal = wire.endTerminal.name
            });
        }

        return wireDataList.ToArray();
    }
}
