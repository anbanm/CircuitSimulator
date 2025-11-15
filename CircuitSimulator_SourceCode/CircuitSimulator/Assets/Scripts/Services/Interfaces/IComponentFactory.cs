using UnityEngine;

namespace CircuitSimulator.Services
{
    /// <summary>
    /// Interface for component creation operations.
    /// Replaces direct dependencies on ComponentFactoryManager singleton.
    /// </summary>
    public interface IComponentFactory
    {
        // Component Creation
        CircuitComponent3D CreateComponent(ComponentType type, Vector3 position);
        CircuitComponent3D CreateComponent(ComponentDefinition definition, Vector3 position);
        CircuitWire CreateWire(CircuitComponent3D startComponent, CircuitComponent3D endComponent);

        // Specific Component Factories
        CircuitComponent3D CreateBattery(Vector3 position = default);
        CircuitComponent3D CreateResistor(Vector3 position = default);
        CircuitComponent3D CreateBulb(Vector3 position = default);
        CircuitComponent3D CreateSwitch(Vector3 position = default);
        CircuitComponent3D CreateJunction(Vector3 position = default);

        // Custom Components (NEW - for ScriptableObject system)
        CircuitComponent3D CreateCustomComponent(ComponentDefinition definition, Vector3 position);

        // Configuration
        bool UseRandomizedPositioning { get; set; }
        float ComponentSpacing { get; set; }
        Transform ComponentParent { get; set; }

        // Events
        System.Action<CircuitComponent3D> OnComponentCreated { get; set; }
        System.Action<CircuitWire> OnWireCreated { get; set; }
    }
}