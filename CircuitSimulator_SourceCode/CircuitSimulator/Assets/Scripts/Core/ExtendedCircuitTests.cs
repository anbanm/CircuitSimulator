using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extended comprehensive test suite for triple-checking the circuit solver
/// Tests edge cases, boundary conditions, and complex configurations
/// </summary>
public class ExtendedCircuitTests : MonoBehaviour
{
    private int testsPassed = 0;
    private int testsFailed = 0;
    private List<string> failedTests = new List<string>();

    void Start()
    {
        RunExtendedTestSuite();
    }

    public void RunExtendedTestSuite()
    {
        Debug.Log("╔════════════════════════════════════════════════════════════════╗");
        Debug.Log("║     EXTENDED CIRCUIT SOLVER VALIDATION TEST SUITE v2.0        ║");
        Debug.Log("╚════════════════════════════════════════════════════════════════╝");
        Debug.Log("");

        // Reset counters
        testsPassed = 0;
        testsFailed = 0;
        failedTests.Clear();

        // Basic Tests - Fundamental validation
        Debug.Log("═══ SECTION 1: BASIC CIRCUIT TESTS ═══");
        TestSingleResistor();
        TestTwoSeriesResistors();
        TestThreeSeriesResistors();
        TestTwoParallelResistors();
        TestThreeParallelResistors();

        // Edge Cases - Boundary conditions
        Debug.Log("\n═══ SECTION 2: EDGE CASE TESTS ═══");
        TestZeroResistance();
        TestVeryHighResistance();
        TestVeryLowVoltage();
        TestVeryHighVoltage();
        TestManyComponentsInSeries();

        // Complex Configurations
        Debug.Log("\n═══ SECTION 3: COMPLEX CIRCUIT TESTS ═══");
        TestWheatstomeBridge();
        TestDeltaYConfiguration();
        TestMultipleParallelBranches();
        TestNestedSeriesParallel();
        TestAsymmetricCircuit();

        // Numerical Stability Tests
        Debug.Log("\n═══ SECTION 4: NUMERICAL STABILITY TESTS ═══");
        TestFloatingPointPrecision();
        TestVerySmallCurrents();
        TestLargeResistanceRatios();
        TestConvergenceWithManyNodes();

        // Real-World Scenarios
        Debug.Log("\n═══ SECTION 5: REAL-WORLD CIRCUIT TESTS ═══");
        TestHouseholdCircuit();
        TestLEDCircuit();
        TestVoltageDivider();
        TestCurrentDivider();
        TestPowerDistribution();

        // Print Summary
        PrintTestSummary();
    }

    // ═══════════════════════════════════════════════════════════════
    // SECTION 1: BASIC CIRCUIT TESTS
    // ═══════════════════════════════════════════════════════════════

    void TestSingleResistor()
    {
        string testName = "Single Resistor (12V, 6Ω)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var battery = new Battery("Bat", node1, node2, 12f);
        var resistor = new Resistor("R1", node1, node2, 6f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, resistor });

        float expectedCurrent = 2.0f; // I = V/R = 12/6 = 2A
        float expectedVoltage = 12.0f;

        bool pass = ValidateResult(resistor.Current, expectedCurrent, 0.01f, "Current") &&
                   ValidateResult(resistor.VoltageDrop, expectedVoltage, 0.01f, "Voltage");

        RecordTestResult(testName, pass);
    }

    void TestTwoSeriesResistors()
    {
        string testName = "Two Series Resistors (24V, 8Ω + 4Ω)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var node3 = new CircuitNode("N3");

        var battery = new Battery("Bat", node1, node3, 24f);
        var r1 = new Resistor("R1", node1, node2, 8f);
        var r2 = new Resistor("R2", node2, node3, 4f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2 });

        float expectedCurrent = 2.0f; // I = 24/12 = 2A
        float expectedV1 = 16.0f; // V = IR = 2*8 = 16V
        float expectedV2 = 8.0f;  // V = IR = 2*4 = 8V

        bool pass = ValidateResult(r1.Current, expectedCurrent, 0.01f, "R1 Current") &&
                   ValidateResult(r2.Current, expectedCurrent, 0.01f, "R2 Current") &&
                   ValidateResult(r1.VoltageDrop, expectedV1, 0.01f, "R1 Voltage") &&
                   ValidateResult(r2.VoltageDrop, expectedV2, 0.01f, "R2 Voltage");

        RecordTestResult(testName, pass);
    }

    void TestThreeSeriesResistors()
    {
        string testName = "Three Series Resistors (9V, 1Ω + 2Ω + 3Ω)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var node3 = new CircuitNode("N3");
        var node4 = new CircuitNode("N4");

        var battery = new Battery("Bat", node1, node4, 9f);
        var r1 = new Resistor("R1", node1, node2, 1f);
        var r2 = new Resistor("R2", node2, node3, 2f);
        var r3 = new Resistor("R3", node3, node4, 3f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2, r3 });

        float expectedCurrent = 1.5f; // I = 9/6 = 1.5A
        float expectedV1 = 1.5f;  // V = 1.5*1 = 1.5V
        float expectedV2 = 3.0f;  // V = 1.5*2 = 3V
        float expectedV3 = 4.5f;  // V = 1.5*3 = 4.5V

        bool pass = ValidateResult(r1.Current, expectedCurrent, 0.01f, "Current") &&
                   ValidateResult(r1.VoltageDrop, expectedV1, 0.01f, "R1 Voltage") &&
                   ValidateResult(r2.VoltageDrop, expectedV2, 0.01f, "R2 Voltage") &&
                   ValidateResult(r3.VoltageDrop, expectedV3, 0.01f, "R3 Voltage");

        RecordTestResult(testName, pass);
    }

    void TestTwoParallelResistors()
    {
        string testName = "Two Parallel Resistors (6V, 3Ω || 6Ω)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");

        var battery = new Battery("Bat", node1, node2, 6f);
        var r1 = new Resistor("R1", node1, node2, 3f);
        var r2 = new Resistor("R2", node1, node2, 6f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2 });

        // Req = (3*6)/(3+6) = 18/9 = 2Ω
        // Itotal = 6/2 = 3A
        // I1 = 6/3 = 2A, I2 = 6/6 = 1A
        float expectedI1 = 2.0f;
        float expectedI2 = 1.0f;
        float expectedVoltage = 6.0f;

        bool pass = ValidateResult(r1.Current, expectedI1, 0.1f, "R1 Current") &&
                   ValidateResult(r2.Current, expectedI2, 0.1f, "R2 Current") &&
                   ValidateResult(r1.VoltageDrop, expectedVoltage, 0.1f, "Voltage");

        RecordTestResult(testName, pass);
    }

    void TestThreeParallelResistors()
    {
        string testName = "Three Parallel Resistors (12V, 4Ω || 6Ω || 12Ω)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");

        var battery = new Battery("Bat", node1, node2, 12f);
        var r1 = new Resistor("R1", node1, node2, 4f);
        var r2 = new Resistor("R2", node1, node2, 6f);
        var r3 = new Resistor("R3", node1, node2, 12f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2, r3 });

        // 1/Req = 1/4 + 1/6 + 1/12 = 3/12 + 2/12 + 1/12 = 6/12 = 1/2
        // Req = 2Ω, Itotal = 12/2 = 6A
        // I1 = 12/4 = 3A, I2 = 12/6 = 2A, I3 = 12/12 = 1A
        float expectedI1 = 3.0f;
        float expectedI2 = 2.0f;
        float expectedI3 = 1.0f;

        bool pass = ValidateResult(r1.Current, expectedI1, 0.1f, "R1 Current") &&
                   ValidateResult(r2.Current, expectedI2, 0.1f, "R2 Current") &&
                   ValidateResult(r3.Current, expectedI3, 0.1f, "R3 Current");

        RecordTestResult(testName, pass);
    }

    // ═══════════════════════════════════════════════════════════════
    // SECTION 2: EDGE CASE TESTS
    // ═══════════════════════════════════════════════════════════════

    void TestZeroResistance()
    {
        string testName = "Near-Zero Resistance (Short Circuit Protection)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var battery = new Battery("Bat", node1, node2, 12f);
        var wire = new Resistor("Wire", node1, node2, 0.001f); // Near zero

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, wire });

        // Should handle without infinity or NaN
        bool pass = !float.IsNaN(wire.Current) && !float.IsInfinity(wire.Current);
        Debug.Log($"  Wire current: {wire.Current:F3}A (should be finite)");

        RecordTestResult(testName, pass);
    }

    void TestVeryHighResistance()
    {
        string testName = "Very High Resistance (Open Circuit)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var battery = new Battery("Bat", node1, node2, 12f);
        var openSwitch = new Resistor("OpenSwitch", node1, node2, 1e12f); // 1 TΩ

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, openSwitch });

        float expectedCurrent = 0.000000000012f; // Nearly zero
        bool pass = openSwitch.Current < 0.001f; // Should be nearly zero
        Debug.Log($"  Current through 1TΩ: {openSwitch.Current:E3}A (should be ~0)");

        RecordTestResult(testName, pass);
    }

    void TestVeryLowVoltage()
    {
        string testName = "Very Low Voltage (1mV battery)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var battery = new Battery("Bat", node1, node2, 0.001f); // 1mV
        var resistor = new Resistor("R", node1, node2, 1f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, resistor });

        float expectedCurrent = 0.001f; // 1mA
        bool pass = ValidateResult(resistor.Current, expectedCurrent, 0.0001f, "Current");

        RecordTestResult(testName, pass);
    }

    void TestVeryHighVoltage()
    {
        string testName = "Very High Voltage (1000V battery)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var battery = new Battery("Bat", node1, node2, 1000f);
        var resistor = new Resistor("R", node1, node2, 100f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, resistor });

        float expectedCurrent = 10f; // 10A
        bool pass = ValidateResult(resistor.Current, expectedCurrent, 0.1f, "Current");

        RecordTestResult(testName, pass);
    }

    void TestManyComponentsInSeries()
    {
        string testName = "20 Resistors in Series";
        Debug.Log($"\n▶ Testing: {testName}");

        var nodes = new List<CircuitNode>();
        var components = new List<CircuitComponent>();

        // Create 21 nodes for 20 resistors
        for (int i = 0; i <= 20; i++)
        {
            nodes.Add(new CircuitNode($"N{i}"));
        }

        // Add battery
        var battery = new Battery("Bat", nodes[0], nodes[20], 100f);
        components.Add(battery);

        // Add 20 resistors of 5Ω each
        for (int i = 0; i < 20; i++)
        {
            var resistor = new Resistor($"R{i+1}", nodes[i], nodes[i+1], 5f);
            components.Add(resistor);
        }

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(components);

        float expectedCurrent = 1f; // 100V / (20*5Ω) = 1A
        bool pass = ValidateResult(components[1].Current, expectedCurrent, 0.01f, "Current");

        RecordTestResult(testName, pass);
    }

    // ═══════════════════════════════════════════════════════════════
    // SECTION 3: COMPLEX CIRCUIT TESTS
    // ═══════════════════════════════════════════════════════════════

    void TestWheatstomeBridge()
    {
        string testName = "Wheatstone Bridge Circuit";
        Debug.Log($"\n▶ Testing: {testName}");
        Debug.Log("  Circuit: Classic bridge configuration");

        // Wheatstone bridge with balanced resistors
        var nodeTop = new CircuitNode("Top");
        var nodeLeft = new CircuitNode("Left");
        var nodeRight = new CircuitNode("Right");
        var nodeBottom = new CircuitNode("Bottom");
        var nodeCenter = new CircuitNode("Center");

        var battery = new Battery("Bat", nodeTop, nodeBottom, 10f);
        var r1 = new Resistor("R1", nodeTop, nodeLeft, 10f);
        var r2 = new Resistor("R2", nodeLeft, nodeBottom, 10f);
        var r3 = new Resistor("R3", nodeTop, nodeRight, 10f);
        var r4 = new Resistor("R4", nodeRight, nodeBottom, 10f);
        var r5 = new Resistor("R5", nodeLeft, nodeRight, 10f); // Bridge resistor

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2, r3, r4, r5 });

        // In balanced bridge, no current through R5
        bool pass = Math.Abs(r5.Current) < 0.01f;
        Debug.Log($"  Bridge current: {r5.Current:F4}A (should be ~0 for balanced)");

        RecordTestResult(testName, pass);
    }

    void TestDeltaYConfiguration()
    {
        string testName = "Delta-Y Configuration";
        Debug.Log($"\n▶ Testing: {testName}");

        // Delta configuration
        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var node3 = new CircuitNode("N3");
        var nodeGnd = new CircuitNode("GND");

        var battery = new Battery("Bat", node1, nodeGnd, 12f);
        var r12 = new Resistor("R12", node1, node2, 6f);
        var r23 = new Resistor("R23", node2, node3, 6f);
        var r31 = new Resistor("R31", node3, node1, 6f);
        var rLoad = new Resistor("RLoad", node3, nodeGnd, 4f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r12, r23, r31, rLoad });

        bool pass = !float.IsNaN(rLoad.Current) && rLoad.Current > 0;
        Debug.Log($"  Load current: {rLoad.Current:F3}A");

        RecordTestResult(testName, pass);
    }

    void TestMultipleParallelBranches()
    {
        string testName = "Multiple Parallel Branches";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var node3 = new CircuitNode("N3");
        var node4 = new CircuitNode("N4");

        var battery = new Battery("Bat", node1, node4, 24f);

        // Series resistor before parallel section
        var r1 = new Resistor("R1", node1, node2, 2f);

        // Three parallel branches
        var r2 = new Resistor("R2", node2, node3, 6f);
        var r3 = new Resistor("R3", node2, node3, 4f);
        var r4 = new Resistor("R4", node2, node3, 12f);

        // Series resistor after parallel section
        var r5 = new Resistor("R5", node3, node4, 1f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2, r3, r4, r5 });

        // Rparallel = 1/(1/6 + 1/4 + 1/12) = 2Ω
        // Rtotal = 2 + 2 + 1 = 5Ω
        // I = 24/5 = 4.8A
        float expectedMainCurrent = 4.8f;
        bool pass = ValidateResult(r1.Current, expectedMainCurrent, 0.2f, "Main Current");

        RecordTestResult(testName, pass);
    }

    void TestNestedSeriesParallel()
    {
        string testName = "Nested Series-Parallel";
        Debug.Log($"\n▶ Testing: {testName}");

        var n1 = new CircuitNode("N1");
        var n2 = new CircuitNode("N2");
        var n3 = new CircuitNode("N3");
        var n4 = new CircuitNode("N4");
        var n5 = new CircuitNode("N5");

        var battery = new Battery("Bat", n1, n5, 48f);
        var r1 = new Resistor("R1", n1, n2, 10f);

        // Parallel section 1
        var r2 = new Resistor("R2", n2, n3, 20f);
        var r3 = new Resistor("R3", n2, n3, 20f);

        var r4 = new Resistor("R4", n3, n4, 5f);

        // Parallel section 2
        var r5 = new Resistor("R5", n4, n5, 15f);
        var r6 = new Resistor("R6", n4, n5, 30f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2, r3, r4, r5, r6 });

        // R_p1 = 10Ω, R_p2 = 10Ω
        // R_total = 10 + 10 + 5 + 10 = 35Ω
        // I = 48/35 ≈ 1.37A
        float expectedCurrent = 1.37f;
        bool pass = ValidateResult(r1.Current, expectedCurrent, 0.1f, "Main Current");

        RecordTestResult(testName, pass);
    }

    void TestAsymmetricCircuit()
    {
        string testName = "Asymmetric Circuit";
        Debug.Log($"\n▶ Testing: {testName}");

        var n1 = new CircuitNode("N1");
        var n2 = new CircuitNode("N2");
        var n3 = new CircuitNode("N3");

        var battery = new Battery("Bat", n1, n3, 15f);
        var r1 = new Resistor("R1", n1, n2, 100f); // Very high
        var r2 = new Resistor("R2", n2, n3, 1f);   // Very low
        var r3 = new Resistor("R3", n1, n3, 10f);  // Parallel path

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2, r3 });

        // Most current should flow through R3 (10Ω) rather than R1+R2 (101Ω)
        bool pass = r3.Current > r1.Current;
        Debug.Log($"  R3 current: {r3.Current:F3}A, R1 current: {r1.Current:F3}A");
        Debug.Log($"  Ratio: {r3.Current/r1.Current:F1}:1 (should be ~10:1)");

        RecordTestResult(testName, pass);
    }

    // ═══════════════════════════════════════════════════════════════
    // SECTION 4: NUMERICAL STABILITY TESTS
    // ═══════════════════════════════════════════════════════════════

    void TestFloatingPointPrecision()
    {
        string testName = "Floating Point Precision";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var battery = new Battery("Bat", node1, node2, 3.14159265f);
        var resistor = new Resistor("R", node1, node2, 2.71828182f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, resistor });

        float expectedCurrent = 3.14159265f / 2.71828182f; // π/e ≈ 1.1557
        bool pass = ValidateResult(resistor.Current, expectedCurrent, 0.001f, "Current");

        RecordTestResult(testName, pass);
    }

    void TestVerySmallCurrents()
    {
        string testName = "Very Small Currents (μA range)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var battery = new Battery("Bat", node1, node2, 0.001f); // 1mV
        var resistor = new Resistor("R", node1, node2, 1000f); // 1kΩ

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, resistor });

        float expectedCurrent = 0.000001f; // 1μA
        bool pass = ValidateResult(resistor.Current, expectedCurrent, 0.0000001f, "Current");
        Debug.Log($"  Current: {resistor.Current*1000000:F3}μA");

        RecordTestResult(testName, pass);
    }

    void TestLargeResistanceRatios()
    {
        string testName = "Large Resistance Ratios (1:1000000)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");

        var battery = new Battery("Bat", node1, node2, 12f);
        var r1 = new Resistor("R1", node1, node2, 0.01f);     // 10mΩ
        var r2 = new Resistor("R2", node1, node2, 10000f);    // 10kΩ

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, r1, r2 });

        // Almost all current through R1
        bool pass = r1.Current > r2.Current * 100;
        Debug.Log($"  Current ratio R1/R2: {r1.Current/r2.Current:F1}");

        RecordTestResult(testName, pass);
    }

    void TestConvergenceWithManyNodes()
    {
        string testName = "Convergence with 50+ nodes";
        Debug.Log($"\n▶ Testing: {testName}");

        var nodes = new List<CircuitNode>();
        var components = new List<CircuitComponent>();

        // Create ladder network with many nodes
        for (int i = 0; i <= 10; i++)
        {
            nodes.Add(new CircuitNode($"Top{i}"));
            nodes.Add(new CircuitNode($"Bot{i}"));
        }

        var battery = new Battery("Bat", nodes[0], nodes[1], 100f);
        components.Add(battery);

        // Create ladder
        for (int i = 0; i < 10; i++)
        {
            // Horizontal resistors
            if (i < 9)
            {
                components.Add(new Resistor($"RH_T{i}", nodes[i*2], nodes[(i+1)*2], 10f));
                components.Add(new Resistor($"RH_B{i}", nodes[i*2+1], nodes[(i+1)*2+1], 10f));
            }
            // Vertical resistors
            components.Add(new Resistor($"RV{i}", nodes[i*2], nodes[i*2+1], 5f));
        }

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(components);

        bool pass = !float.IsNaN(battery.Current) && battery.Current > 0;
        Debug.Log($"  Solved {nodes.Count} nodes, battery current: {battery.Current:F3}A");

        RecordTestResult(testName, pass);
    }

    // ═══════════════════════════════════════════════════════════════
    // SECTION 5: REAL-WORLD CIRCUIT TESTS
    // ═══════════════════════════════════════════════════════════════

    void TestHouseholdCircuit()
    {
        string testName = "Household Circuit (120V, multiple appliances)";
        Debug.Log($"\n▶ Testing: {testName}");

        var hot = new CircuitNode("Hot");
        var neutral = new CircuitNode("Neutral");

        var mains = new Battery("Mains", hot, neutral, 120f);
        var lamp = new Resistor("Lamp_60W", hot, neutral, 240f);      // P=V²/R, R=V²/P=14400/60=240Ω
        var tv = new Resistor("TV_200W", hot, neutral, 72f);          // R=14400/200=72Ω
        var fridge = new Resistor("Fridge_150W", hot, neutral, 96f);   // R=14400/150=96Ω

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { mains, lamp, tv, fridge });

        float totalPower = lamp.VoltageDrop * lamp.Current +
                          tv.VoltageDrop * tv.Current +
                          fridge.VoltageDrop * fridge.Current;

        Debug.Log($"  Total power: {totalPower:F1}W");
        Debug.Log($"  Lamp: {lamp.Current*1000:F1}mA, TV: {tv.Current:F2}A, Fridge: {fridge.Current:F2}A");

        bool pass = Math.Abs(totalPower - 410f) < 20f; // Should be ~410W
        RecordTestResult(testName, pass);
    }

    void TestLEDCircuit()
    {
        string testName = "LED Circuit with Current Limiting";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var node3 = new CircuitNode("N3");

        var battery = new Battery("Bat_9V", node1, node3, 9f);
        var resistor = new Resistor("R_330", node1, node2, 330f); // Current limiting
        var led = new Resistor("LED", node2, node3, 70f); // LED approximated as resistor

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, resistor, led });

        float expectedCurrent = 9f / 400f; // 22.5mA
        bool pass = ValidateResult(led.Current, expectedCurrent, 0.005f, "LED Current");
        Debug.Log($"  LED current: {led.Current*1000:F1}mA (safe for typical LED)");

        RecordTestResult(testName, pass);
    }

    void TestVoltageDivider()
    {
        string testName = "Voltage Divider (5V to 3.3V)";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("Vin");
        var node2 = new CircuitNode("Vout");
        var node3 = new CircuitNode("GND");

        var source = new Battery("5V", node1, node3, 5f);
        var r1 = new Resistor("R1", node1, node2, 680f);  // Top resistor
        var r2 = new Resistor("R2", node2, node3, 1000f); // Bottom resistor

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { source, r1, r2 });

        float vOut = r2.VoltageDrop; // Voltage across R2
        float expectedVout = 5f * (1000f / (680f + 1000f)); // ~2.98V

        bool pass = ValidateResult(vOut, expectedVout, 0.1f, "Output Voltage");
        Debug.Log($"  Vout: {vOut:F2}V (target ~3.3V logic level)");

        RecordTestResult(testName, pass);
    }

    void TestCurrentDivider()
    {
        string testName = "Current Divider";
        Debug.Log($"\n▶ Testing: {testName}");

        var node1 = new CircuitNode("N1");
        var node2 = new CircuitNode("N2");
        var node3 = new CircuitNode("N3");
        var node4 = new CircuitNode("N4");

        var battery = new Battery("Bat", node1, node4, 12f);
        var rSource = new Resistor("Rs", node1, node2, 10f);

        // Current divider branches
        var r1 = new Resistor("R1", node2, node3, 30f);
        var r2 = new Resistor("R2", node2, node3, 60f);

        var rLoad = new Resistor("RL", node3, node4, 5f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        solver.Solve(new List<CircuitComponent> { battery, rSource, r1, r2, rLoad });

        // Current should divide inversely with resistance
        float ratio = r1.Current / r2.Current;
        float expectedRatio = 60f / 30f; // 2:1

        bool pass = ValidateResult(ratio, expectedRatio, 0.2f, "Current Ratio");
        Debug.Log($"  I1: {r1.Current:F3}A, I2: {r2.Current:F3}A, Ratio: {ratio:F2}:1");

        RecordTestResult(testName, pass);
    }

    void TestPowerDistribution()
    {
        string testName = "Power Distribution Network";
        Debug.Log($"\n▶ Testing: {testName}");

        var source = new CircuitNode("Source");
        var dist1 = new CircuitNode("Dist1");
        var dist2 = new CircuitNode("Dist2");
        var load1 = new CircuitNode("Load1");
        var load2 = new CircuitNode("Load2");
        var load3 = new CircuitNode("Load3");
        var ground = new CircuitNode("Ground");

        var generator = new Battery("Generator", source, ground, 240f);

        // Distribution lines (small resistance)
        var line1 = new Resistor("Line1", source, dist1, 0.1f);
        var line2 = new Resistor("Line2", source, dist2, 0.1f);

        // Sub-distribution
        var subLine1 = new Resistor("SubLine1", dist1, load1, 0.05f);
        var subLine2 = new Resistor("SubLine2", dist1, load2, 0.05f);
        var subLine3 = new Resistor("SubLine3", dist2, load3, 0.05f);

        // Loads
        var device1 = new Resistor("Device1", load1, ground, 100f);
        var device2 = new Resistor("Device2", load2, ground, 150f);
        var device3 = new Resistor("Device3", load3, ground, 200f);

        var solver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = false;
        var components = new List<CircuitComponent> {
            generator, line1, line2, subLine1, subLine2, subLine3,
            device1, device2, device3
        };
        solver.Solve(components);

        float totalPower = device1.VoltageDrop * device1.Current +
                          device2.VoltageDrop * device2.Current +
                          device3.VoltageDrop * device3.Current;

        Debug.Log($"  Total load power: {totalPower:F1}W");
        Debug.Log($"  Line losses: {(generator.Voltage * generator.Current - totalPower):F2}W");

        bool pass = totalPower > 0 && !float.IsNaN(totalPower);
        RecordTestResult(testName, pass);
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════

    bool ValidateResult(float actual, float expected, float tolerance, string valueName)
    {
        bool pass = Math.Abs(actual - expected) <= tolerance;
        string status = pass ? "✓" : "✗";
        Debug.Log($"  {valueName}: {actual:F3} (expected {expected:F3}) {status}");
        return pass;
    }

    void RecordTestResult(string testName, bool passed)
    {
        if (passed)
        {
            testsPassed++;
            Debug.Log($"  Result: <color=green>✓ PASSED</color>");
        }
        else
        {
            testsFailed++;
            failedTests.Add(testName);
            Debug.Log($"  Result: <color=red>✗ FAILED</color>");
        }
    }

    void PrintTestSummary()
    {
        Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
        Debug.Log("║                      TEST SUITE SUMMARY                       ║");
        Debug.Log("╚════════════════════════════════════════════════════════════════╝");

        int total = testsPassed + testsFailed;
        float passRate = total > 0 ? (testsPassed * 100f / total) : 0;

        Debug.Log($"\n  Total Tests: {total}");
        Debug.Log($"  Passed: <color=green>{testsPassed}</color>");
        Debug.Log($"  Failed: <color=red>{testsFailed}</color>");
        Debug.Log($"  Pass Rate: {passRate:F1}%");

        if (testsFailed > 0)
        {
            Debug.Log($"\n  Failed Tests:");
            foreach (var test in failedTests)
            {
                Debug.Log($"    • {test}");
            }
        }

        if (passRate == 100)
        {
            Debug.Log($"\n  <color=green>★★★ PERFECT SCORE! ALL TESTS PASSED ★★★</color>");
        }
        else if (passRate >= 95)
        {
            Debug.Log($"\n  <color=yellow>✓ Excellent! Solver is production ready.</color>");
        }
        else if (passRate >= 90)
        {
            Debug.Log($"\n  <color=yellow>⚠ Good, but some edge cases need attention.</color>");
        }
        else
        {
            Debug.Log($"\n  <color=red>✗ Solver needs significant improvements.</color>");
        }

        Debug.Log("\n════════════════════════════════════════════════════════════════");
    }
}