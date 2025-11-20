using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages loading and execution of circuit scenarios
/// Replaces the free-form component creation with structured educational content
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    [Header("Scenario Library")]
    [Tooltip("All available scenarios")]
    public List<CircuitScenario> availableScenarios = new List<CircuitScenario>();

    [Tooltip("Currently loaded scenario")]
    public CircuitScenario currentScenario;

    [Header("Dependencies")]
    [Tooltip("Factory for creating components")]
    public ComponentFactoryManager componentFactory;

    [Tooltip("Manager for circuit solving")]
    public CircuitSolverManager solverManager;

    [Tooltip("UI for scenario selection")]
    public GameObject selectionUI; // Placeholder - use generic GameObject for now

    [Header("Runtime State")]
    [Tooltip("Components created for current scenario")]
    public List<GameObject> scenarioComponents = new List<GameObject>();

    [Tooltip("Current scenario progress")]
    public ScenarioProgress progress = new ScenarioProgress();

    // Events for UI integration
    public System.Action<CircuitScenario> OnScenarioLoaded;
    public System.Action<ScenarioResults> OnScenarioCompleted;
    public System.Action<string> OnScenarioValidated;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Load all scenarios from Resources if not assigned
        if (availableScenarios.Count == 0)
        {
            LoadScenariosFromResources();
        }

        // Set up UI
        if (selectionUI != null)
        {
            // TODO: Add ScenarioSelectionUI component and initialize
            // var uiComponent = selectionUI.GetComponent<ScenarioSelectionUI>();
            // uiComponent?.Initialize(this, availableScenarios);
            selectionUI.SetActive(true);
        }

        Debug.Log($"ScenarioManager initialized with {availableScenarios.Count} scenarios");
    }

    void LoadScenariosFromResources()
    {
        CircuitScenario[] scenarios = Resources.LoadAll<CircuitScenario>("Scenarios");
        availableScenarios.AddRange(scenarios);
        Debug.Log($"Loaded {scenarios.Length} scenarios from Resources/Scenarios");
    }

    /// <summary>
    /// Load and display a specific scenario
    /// </summary>
    public void LoadScenario(CircuitScenario scenario)
    {
        if (scenario == null)
        {
            Debug.LogError("Cannot load null scenario");
            return;
        }

        // Clear existing scenario
        ClearCurrentScenario();

        // Set new scenario
        currentScenario = scenario;
        progress = new ScenarioProgress();
        progress.scenarioName = scenario.scenarioName;
        progress.startTime = System.DateTime.Now;

        // Create components
        CreateScenarioComponents();

        // Create connections
        CreateScenarioConnections();

        // Configure display settings
        ApplyDisplaySettings();

        // Trigger events
        OnScenarioLoaded?.Invoke(scenario);

        Debug.Log($"Loaded scenario: {scenario.scenarioName}");
    }

    /// <summary>
    /// Load scenario by name (for UI integration)
    /// </summary>
    public void LoadScenario(string scenarioName)
    {
        var scenario = availableScenarios.FirstOrDefault(s => s.scenarioName == scenarioName);
        if (scenario != null)
        {
            LoadScenario(scenario);
        }
        else
        {
            Debug.LogError($"Scenario not found: {scenarioName}");
        }
    }

    /// <summary>
    /// Get scenarios by grade level for filtered display
    /// </summary>
    public List<CircuitScenario> GetScenariosForGrade(GradeLevel grade)
    {
        return availableScenarios.Where(s => s.targetGrade == grade).ToList();
    }

    /// <summary>
    /// Get scenarios that teach specific concepts
    /// </summary>
    public List<CircuitScenario> GetScenariosForObjective(LearningObjective objective)
    {
        return availableScenarios.Where(s => s.objectives.Contains(objective)).ToList();
    }

    void CreateScenarioComponents()
    {
        foreach (var componentData in currentScenario.components)
        {
            GameObject component = CreateComponentFromData(componentData);
            if (component != null)
            {
                scenarioComponents.Add(component);

                // Configure component properties
                ConfigureComponent(component, componentData);
            }
        }
    }

    GameObject CreateComponentFromData(ComponentData data)
    {
        GameObject component = null;

        switch (data.type)
        {
            case ComponentType.Battery:
                component = componentFactory.CreateBattery();
                break;
            case ComponentType.Resistor:
                component = componentFactory.CreateResistor();
                break;
            case ComponentType.Bulb:
                component = componentFactory.CreateBulb();
                break;
            case ComponentType.Switch:
                component = componentFactory.CreateSwitch();
                break;
            case ComponentType.Junction:
                component = componentFactory.CreateJunction();
                break;
        }

        if (component != null)
        {
            // Set position
            component.transform.position = data.position;

            // Set name for identification
            component.name = data.componentId;

            // Add scenario component marker
            var marker = component.GetComponent<ScenarioComponent>();
            if (marker == null)
            {
                marker = component.AddComponent<ScenarioComponent>();
            }
            marker.Initialize(data);
        }

        return component;
    }

    void ConfigureComponent(GameObject component, ComponentData data)
    {
        var circuitComponent = component.GetComponent<CircuitComponent3D>();
        if (circuitComponent != null)
        {
            // Set electrical properties
            switch (data.type)
            {
                case ComponentType.Battery:
                    // Set voltage
                    circuitComponent.voltage = data.properties.voltage;
                    break;

                case ComponentType.Resistor:
                case ComponentType.Bulb:
                    // Set resistance
                    circuitComponent.resistance = data.properties.resistance;
                    break;

                case ComponentType.Switch:
                    // Set switch state
                    // TODO: Implement switch state setting
                    break;
            }

            // Set visual properties
            if (data.properties.componentColor != Color.white)
            {
                var renderer = component.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = data.properties.componentColor;
                }
            }

            // Set custom label if provided
            if (!string.IsNullOrEmpty(data.customLabel))
            {
                var labelComponent = component.GetComponent<ScreenSpaceLabels>();
                if (labelComponent != null)
                {
                    // TODO: Add custom label support to label component
                }
            }

            // Set interactivity
            var selectable = component.GetComponent<SelectableComponent>();
            if (selectable != null)
            {
                selectable.enabled = data.isInteractive;
            }

            var moveable = component.GetComponent<MoveableComponent>();
            if (moveable != null)
            {
                moveable.enabled = data.isInteractive;
            }
        }
    }

    void CreateScenarioConnections()
    {
        foreach (var connection in currentScenario.connections)
        {
            CreateConnection(connection);
        }
    }

    void CreateConnection(WireConnection connection)
    {
        // Find components by ID
        var fromComponent = scenarioComponents.FirstOrDefault(c => c.name == connection.fromComponent);
        var toComponent = scenarioComponents.FirstOrDefault(c => c.name == connection.toComponent);

        if (fromComponent == null || toComponent == null)
        {
            Debug.LogError($"Cannot create connection: component not found ({connection.fromComponent} -> {connection.toComponent})");
            return;
        }

        // Create wire connection
        var wireManager = FindFirstObjectByType<ComponentTerminalManager>();
        if (wireManager != null)
        {
            // TODO: Implement automatic wire creation between components
            // This would replace the manual wire creation UI
            wireManager.CreateWireBetweenComponents(
                fromComponent.GetComponent<CircuitComponent3D>(),
                toComponent.GetComponent<CircuitComponent3D>()
            );
        }
    }

    void ApplyDisplaySettings()
    {
        var settings = currentScenario.displaySettings;

        // Configure all label components
        var labelComponents = FindObjectsByType<ScreenSpaceLabels>(FindObjectsSortMode.None);
        foreach (var label in labelComponents)
        {
            // Apply display settings
            label.updateInterval = settings.updateFrequency;

            // TODO: Add support for different label styles
            // Configure what values to show based on settings
        }

        // Set initial visibility
        if (!currentScenario.showValuesAtStart)
        {
            foreach (var label in labelComponents)
            {
                label.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Solve the current scenario and validate results
    /// </summary>
    public void SolveAndValidateScenario()
    {
        if (currentScenario == null)
        {
            Debug.LogWarning("No scenario loaded to solve");
            return;
        }

        // Trigger circuit solving
        if (solverManager != null)
        {
            solverManager.SolveCircuit();
        }

        // Validate results after a short delay (allow solver to complete)
        Invoke(nameof(ValidateScenarioResults), 0.1f);
    }

    void ValidateScenarioResults()
    {
        var results = CalculateCircuitResults();
        var validation = ValidateAgainstExpected(results);

        progress.completed = validation.isValid;
        progress.completionTime = System.DateTime.Now;
        progress.accuracy = validation.accuracy;

        OnScenarioValidated?.Invoke(validation.message);

        if (validation.isValid)
        {
            OnScenarioCompleted?.Invoke(new ScenarioResults
            {
                scenario = currentScenario,
                actualResults = results,
                validationMessage = validation.message,
                timeToComplete = (float)(progress.completionTime - progress.startTime).TotalMinutes
            });
        }

        Debug.Log($"Scenario validation: {validation.message}");
    }

    ScenarioValidation ValidateAgainstExpected(CircuitResults actual)
    {
        var expected = currentScenario.expectedResults;
        var validation = new ScenarioValidation();

        // Check current
        float currentError = Mathf.Abs(actual.totalCurrent - expected.expectedCurrent) / expected.expectedCurrent * 100f;
        bool currentValid = currentError <= expected.tolerance;

        // Check resistance
        float resistanceError = Mathf.Abs(actual.totalResistance - expected.expectedResistance) / expected.expectedResistance * 100f;
        bool resistanceValid = resistanceError <= expected.tolerance;

        // Check power
        float powerError = Mathf.Abs(actual.totalPower - expected.expectedPower) / expected.expectedPower * 100f;
        bool powerValid = powerError <= expected.tolerance;

        validation.isValid = currentValid && resistanceValid && powerValid;
        validation.accuracy = 100f - (currentError + resistanceError + powerError) / 3f;

        validation.message = $"Current: {actual.totalCurrent:F2}A (expected {expected.expectedCurrent:F2}A, error: {currentError:F1}%)\n" +
                           $"Resistance: {actual.totalResistance:F2}Ω (expected {expected.expectedResistance:F2}Ω, error: {resistanceError:F1}%)\n" +
                           $"Power: {actual.totalPower:F2}W (expected {expected.expectedPower:F2}W, error: {powerError:F1}%)";

        return validation;
    }

    CircuitResults CalculateCircuitResults()
    {
        var results = new CircuitResults();

        // Get all circuit components
        var components = scenarioComponents
            .Select(go => go.GetComponent<CircuitComponent3D>())
            .Where(c => c != null)
            .ToList();

        if (components.Count > 0)
        {
            // Calculate totals
            results.totalCurrent = components.Where(c => c.ComponentType == ComponentType.Battery)
                                            .Sum(c => c.current);

            results.totalResistance = components.Where(c => c.ComponentType == ComponentType.Resistor || c.ComponentType == ComponentType.Bulb)
                                               .Sum(c => c.resistance);

            results.totalPower = components.Sum(c => c.voltageDrop * c.current);
        }

        return results;
    }

    /// <summary>
    /// Clear the current scenario and reset the workspace
    /// </summary>
    public void ClearCurrentScenario()
    {
        // Destroy all scenario components
        foreach (var component in scenarioComponents)
        {
            if (component != null)
            {
                DestroyImmediate(component);
            }
        }

        scenarioComponents.Clear();
        currentScenario = null;
        progress = new ScenarioProgress();

        Debug.Log("Scenario cleared");
    }

    /// <summary>
    /// Get available scenarios filtered by criteria
    /// </summary>
    public List<CircuitScenario> GetFilteredScenarios(GradeLevel? grade = null, LearningObjective? objective = null)
    {
        var filtered = availableScenarios.AsEnumerable();

        if (grade.HasValue)
        {
            filtered = filtered.Where(s => s.targetGrade == grade.Value);
        }

        if (objective.HasValue)
        {
            filtered = filtered.Where(s => s.objectives.Contains(objective.Value));
        }

        return filtered.ToList();
    }
}

// Supporting classes for scenario management
[System.Serializable]
public class ScenarioProgress
{
    public string scenarioName;
    public System.DateTime startTime;
    public System.DateTime completionTime;
    public bool completed;
    public float accuracy;
    public int questionsAnswered;
    public int questionsCorrect;
}

[System.Serializable]
public class ScenarioResults
{
    public CircuitScenario scenario;
    public CircuitResults actualResults;
    public string validationMessage;
    public float timeToComplete;
}


[System.Serializable]
public class ComponentMeasurement
{
    public string componentId;
    public float voltage;
    public float current;
    public float power;
}

[System.Serializable]
public class ScenarioValidation
{
    public bool isValid;
    public float accuracy;
    public string message;
}