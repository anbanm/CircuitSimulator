using System.Collections.Generic;
using UnityEngine;

namespace CircuitSimulator.Services
{
    /// <summary>
    /// Interface for circuit management operations.
    /// Replaces direct dependencies on CircuitManager singleton.
    /// </summary>
    public interface ICircuitManager
    {
        // Component Management
        void RegisterComponent(CircuitComponent3D component);
        void UnregisterComponent(CircuitComponent3D component);
        IReadOnlyList<CircuitComponent3D> GetAllComponents();
        CircuitComponent3D GetComponentById(int id);

        // Wire Management
        void RegisterWire(CircuitWire wire);
        void UnregisterWire(CircuitWire wire);
        IReadOnlyList<CircuitWire> GetAllWires();

        // Circuit Operations
        void MarkCircuitChanged();
        void SolveCircuit();
        void ResetCircuit();
        bool IsCircuitValid();

        // State Queries
        bool HasComponents();
        bool HasWires();
        int GetComponentCount();
        int GetWireCount();

        // Events
        System.Action<CircuitComponent3D> OnComponentRegistered { get; set; }
        System.Action<CircuitComponent3D> OnComponentUnregistered { get; set; }
        System.Action<CircuitWire> OnWireRegistered { get; set; }
        System.Action<CircuitWire> OnWireUnregistered { get; set; }
        System.Action OnCircuitChanged { get; set; }
    }
}