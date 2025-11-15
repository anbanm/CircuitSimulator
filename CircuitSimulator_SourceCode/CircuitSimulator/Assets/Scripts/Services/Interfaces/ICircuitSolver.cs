using System.Collections.Generic;

namespace CircuitSimulator.Services
{
    /// <summary>
    /// Interface for circuit solving operations.
    /// Replaces direct dependencies on CircuitSolverManager singleton.
    /// </summary>
    public interface ICircuitSolver
    {
        // Solving Operations
        bool SolveCircuit();
        void MarkCircuitChanged();
        void EnableAutoSolve(bool enabled);
        void SetSolveInterval(float interval);

        // Circuit Building
        IReadOnlyList<CircuitComponent> BuildLogicalCircuit();
        void UpdateComponentsFromSolver();

        // Configuration
        bool IsAutoSolveEnabled { get; }
        float SolveInterval { get; }
        bool IsDebugEnabled { get; set; }

        // Status
        bool IsCircuitSolved { get; }
        float LastSolveTime { get; }
        string LastSolveResult { get; }

        // Events
        System.Action OnCircuitSolved { get; set; }
        System.Action<string> OnSolveError { get; set; }
        System.Action OnSolveStarted { get; set; }
    }
}