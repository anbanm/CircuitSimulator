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
        Debug.Log("==== BEGINNING EXTENDED CIRCUIT TESTS ====");
        RunSeriesTest();
        RunParallelTest();
        RunMixedTest();
        RunDoubleParallelTest();
        RunLadderCircuitTest();
        RunComplexMixedTest();
        RunSwitchTest();
        Debug.Log("==== CIRCUIT TESTS COMPLETE ====");
    }

    void RunSeriesTest()
    {
        Debug.Log("\n--- SERIES TEST CIRCUIT ---");
        Debug.Log("     R1(3Ω)    R2(3Ω)");
        Debug.Log("  +----[===]----[===]----+");
        Debug.Log("  |                     |");
        Debug.Log(" [+] 6V              [-]");
        Debug.Log("  +---------------------+");
        Debug.Log("\nExpected: Total R=6Ω, I=1A");
        Debug.Log("R1: 3V, 1A | R2: 3V, 1A | Battery: 6V, 1A");

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
        Debug.Log("\n--- PARALLEL TEST CIRCUIT ---");
        Debug.Log("     +-------[R1=3Ω]-------+");
        Debug.Log("     |                     |");
        Debug.Log("   [+] 6V               [-]");
        Debug.Log("     |                     |");
        Debug.Log("     +-------[R2=6Ω]-------+");
        Debug.Log("\nExpected: R1||R2 = 2Ω, I_total=3A");
        Debug.Log("R1: 6V, 2A | R2: 6V, 1A | Battery: 6V, 3A");

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
        Debug.Log("\n--- MIXED SERIES-PARALLEL TEST ---");
        Debug.Log("         R1(2Ω)           R4(2Ω)");
        Debug.Log("    +----[===]----+--------[===]----+");
        Debug.Log("    |             |                 |");
        Debug.Log("  [+] 12V         |               [-]");
        Debug.Log("    |             +--[R2=4Ω]--+     |");
        Debug.Log("    |             |           |     |");
        Debug.Log("    |             +--[R3=6Ω]--+     |");
        Debug.Log("    +-----------------------------+");
        Debug.Log("\nExpected: R2||R3=2.4Ω, Total=6.4Ω, I=1.875A");
        Debug.Log("R1: 3.75V, 1.875A | R2: 4.5V, 1.125A | R3: 4.5V, 0.75A");
        Debug.Log("R4: 3.75V, 1.875A | Battery: 12V, 1.875A");

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

    void RunDoubleParallelTest()
    {
        Debug.Log("\n--- DOUBLE PARALLEL TEST ---");
        Debug.Log("     +---[R1=2Ω]---+---[R3=3Ω]---+");
        Debug.Log("     |             |             |");
        Debug.Log("   [+] 12V         |           [-]");
        Debug.Log("     |             |             |");
        Debug.Log("     +---[R2=4Ω]---+---[R4=6Ω]---+");
        Debug.Log("\nExpected: Left: R1||R2=4/3Ω, Right: R3||R4=2Ω");
        Debug.Log("Total: 4/3 + 2 = 10/3Ω ≈ 3.33Ω, I=3.6A");
        Debug.Log("Left voltage: 4.8V, Right voltage: 7.2V");
        Debug.Log("R1: 4.8V, 2.4A | R2: 4.8V, 1.2A");
        Debug.Log("R3: 7.2V, 2.4A | R4: 7.2V, 1.2A");

        var node1 = new CircuitNode("DP_Node1");
        var node2 = new CircuitNode("DP_Node2");
        var node3 = new CircuitNode("DP_Node3");

        var battery = new Battery("Battery", node1, node3, 12f);
        var r1 = new Resistor("R1", node1, node2, 2f);
        var r2 = new Resistor("R2", node1, node2, 4f);
        var r3 = new Resistor("R3", node2, node3, 3f);
        var r4 = new Resistor("R4", node2, node3, 6f);

        SolveAndLog("DOUBLE PARALLEL TEST", new List<CircuitComponent> { battery, r1, r2, r3, r4 },
            new (CircuitComponent, CircuitNode, CircuitNode)[]
            {
                (r1, node1, node2),
                (r2, node1, node2),
                (r3, node2, node3),
                (r4, node2, node3),
                (battery, node1, node3)
            });
    }

    void RunLadderCircuitTest()
    {
        Debug.Log("\n--- LADDER CIRCUIT TEST ---");
        Debug.Log("         R1(1Ω)           R3(1Ω)");
        Debug.Log("    +----[===]----+--------[===]----+");
        Debug.Log("    |             |                 |");
        Debug.Log("  [+] 10V         |               [-]");
        Debug.Log("    |            [R2=2Ω]            |");
        Debug.Log("    |             |                 |");
        Debug.Log("    +-----------------------------+");
        Debug.Log("\nExpected: R2 in parallel with (R3 + 0), then in series with R1");
        Debug.Log("Right equivalent: R3||R2 = (1×2)/(1+2) = 2/3Ω");
        Debug.Log("Total: 1 + 2/3 = 5/3Ω ≈ 1.67Ω, I_main = 6A");
        Debug.Log("V_R1 = 6V, V_right = 4V");
        Debug.Log("R1: 6V, 6A | R2: 4V, 2A | R3: 4V, 4A");

        var node1 = new CircuitNode("L_Node1");
        var node2 = new CircuitNode("L_Node2");
        var node3 = new CircuitNode("L_Node3");

        var battery = new Battery("Battery", node1, node3, 10f);
        var r1 = new Resistor("R1", node1, node2, 1f);
        var r2 = new Resistor("R2", node2, node3, 2f);
        var r3 = new Resistor("R3", node2, node3, 1f);

        SolveAndLog("LADDER CIRCUIT TEST", new List<CircuitComponent> { battery, r1, r2, r3 },
            new (CircuitComponent, CircuitNode, CircuitNode)[]
            {
                (r1, node1, node2),
                (r2, node2, node3),
                (r3, node2, node3),
                (battery, node1, node3)
            });
    }

    void RunComplexMixedTest()
    {
        Debug.Log("\n--- COMPLEX MIXED CIRCUIT TEST ---");
        Debug.Log("       R1(1Ω)       R4(2Ω)       R6(1Ω)");
        Debug.Log("  +----[===]----+----[===]----+----[===]----+");
        Debug.Log("  |             |             |             |");
        Debug.Log("[+] 15V         |             |           [-]");
        Debug.Log("  |            [R2=3Ω]       [R5=4Ω]       |");
        Debug.Log("  |             |             |             |");
        Debug.Log("  |             +----[R3=6Ω]--+             |");
        Debug.Log("  +-------------------------------------------+");
        Debug.Log("\nExpected: Complex parallel combinations");
        Debug.Log("Middle: R2||R3 = 2Ω, Right: R5 = 4Ω");
        Debug.Log("Total: 1 + 2 + 2 + 4 + 1 = 10Ω, I = 1.5A");

        var node1 = new CircuitNode("C_Node1");
        var node2 = new CircuitNode("C_Node2");
        var node3 = new CircuitNode("C_Node3");
        var node4 = new CircuitNode("C_Node4");
        var node5 = new CircuitNode("C_Node5");

        var battery = new Battery("Battery", node1, node5, 15f);
        var r1 = new Resistor("R1", node1, node2, 1f);
        var r2 = new Resistor("R2", node2, node3, 3f);
        var r3 = new Resistor("R3", node2, node3, 6f);
        var r4 = new Resistor("R4", node3, node4, 2f);
        var r5 = new Resistor("R5", node4, node5, 4f);
        var r6 = new Resistor("R6", node4, node5, 1f);

        SolveAndLog("COMPLEX MIXED TEST", new List<CircuitComponent> { battery, r1, r2, r3, r4, r5, r6 },
            new (CircuitComponent, CircuitNode, CircuitNode)[]
            {
                (r1, node1, node2),
                (r2, node2, node3),
                (r3, node2, node3),
                (r4, node3, node4),
                (r5, node4, node5),
                (r6, node4, node5),
                (battery, node1, node5)
            });
    }

    void RunSwitchTest()
    {
        Debug.Log("\n--- SWITCH CIRCUIT TEST ---");
        Debug.Log("     R1(4Ω)     SW(OPEN)    R2(2Ω)");
        Debug.Log("  +----[===]----[ / ]----[===]----+");
        Debug.Log("  |                               |");
        Debug.Log("[+] 12V                        [-]");
        Debug.Log("  |                               |");
        Debug.Log("  +----------[R3=6Ω]--------------+");
        Debug.Log("\nExpected: Switch open, only R3 has current");
        Debug.Log("I = 12V / 6Ω = 2A through R3 only");
        Debug.Log("R1 & R2: 0V, 0A | R3: 12V, 2A");

        var node1 = new CircuitNode("SW_Node1");
        var node2 = new CircuitNode("SW_Node2");
        var node3 = new CircuitNode("SW_Node3");
        var node4 = new CircuitNode("SW_Node4");

        var battery = new Battery("Battery", node1, node4, 12f);
        var r1 = new Resistor("R1", node1, node2, 4f);
        var sw = new Switch("SW", node2, node3, false); // Open switch
        var r2 = new Resistor("R2", node3, node4, 2f);
        var r3 = new Resistor("R3", node1, node4, 6f);

        SolveAndLog("SWITCH TEST (OPEN)", new List<CircuitComponent> { battery, r1, sw, r2, r3 },
            new (CircuitComponent, CircuitNode, CircuitNode)[]
            {
                (r1, node1, node2),
                (sw, node2, node3),
                (r2, node3, node4),
                (r3, node1, node4),
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
            float voltage = multimeter.MeasureVoltage(pos, neg);
            float current = multimeter.MeasureCurrent(comp);
            log += $"[Multimeter] {comp.Id}: {voltage:F3}V, {current:F3}A\n";
        }

        Debug.Log(log);
    }
}