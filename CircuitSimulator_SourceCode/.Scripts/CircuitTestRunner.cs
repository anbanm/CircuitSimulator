using System.Collections.Generic;
using UnityEngine;

public class CircuitTestRunner : MonoBehaviour
{
    void Start()
    {
        RunAllTests();
    }

    public void RunAllTests()
    {
        Debug.Log("==== BEGINNING CIRCUIT TESTS ====");
        RunSeriesTest();
        RunParallelTest();
        RunMixedTest();
        Debug.Log("==== CIRCUIT TESTS COMPLETE ====");
    }

    void RunSeriesTest()
    {
        var node1 = new CircuitNode("S_Node1");
        var node2 = new CircuitNode("S_Node2");
        var node3 = new CircuitNode("S_Node3");

        var battery = new Battery("Battery", node1, node3, 6f);
        var r1 = new Resistor("R1", node1, node2, 3f);
        var r2 = new Resistor("R2", node2, node3, 3f);

        SolveAndLog("SERIES TEST", new List<CircuitComponent> { battery, r1, r2 },
            new (CircuitComponent, CircuitNode, CircuitNode)[]
            {
                (r1, node1, node2),
                (r2, node2, node3),
                (battery, node1, node3)
            });
    }

    void RunParallelTest()
    {
        var node1 = new CircuitNode("P_Node1");
        var node2 = new CircuitNode("P_Node2");

        var battery = new Battery("Battery", node1, node2, 6f);
        var r1 = new Resistor("R1", node1, node2, 3f);
        var r2 = new Resistor("R2", node1, node2, 6f);

        SolveAndLog("PARALLEL TEST", new List<CircuitComponent> { battery, r1, r2 },
            new (CircuitComponent, CircuitNode, CircuitNode)[]
            {
                (r1, node1, node2),
                (r2, node1, node2),
                (battery, node1, node2)
            });
    }

    void RunMixedTest()
    {
        var node1 = new CircuitNode("M_Node1");
        var node2 = new CircuitNode("M_Node2");
        var node3 = new CircuitNode("M_Node3");
        var node4 = new CircuitNode("M_Node4");

        var battery = new Battery("Battery", node1, node4, 12f);
        var r1 = new Resistor("R1", node1, node2, 2f);
        var r2 = new Resistor("R2", node2, node3, 4f);
        var r3 = new Resistor("R3", node2, node3, 6f);
        var r4 = new Resistor("R4", node3, node4, 2f);

        SolveAndLog("MIXED SERIES-PARALLEL TEST", new List<CircuitComponent> { battery, r1, r2, r3, r4 },
            new (CircuitComponent, CircuitNode, CircuitNode)[]
            {
                (r1, node1, node2),
                (r2, node2, node3),
                (r3, node2, node3),
                (r4, node3, node4),
                (battery, node1, node4)
            });
    }

    void SolveAndLog(string label, List<CircuitComponent> components, (CircuitComponent comp, CircuitNode pos, CircuitNode neg)[] probes)
    {
        var solver = new CircuitSolver();
        solver.Solve(components);

        var multimeter = new Multimeter();
        var log = $"--- {label} ---\n";

        foreach (var (comp, pos, neg) in probes)
        {
            log += $"[Multimeter] Voltage across {comp.Id}: {multimeter.MeasureVoltage(pos, neg)} V\n";
            log += $"[Multimeter] Current through {comp.Id}: {multimeter.MeasureCurrent(comp)} A\n";
        }

        Debug.Log(log);
    }
}
