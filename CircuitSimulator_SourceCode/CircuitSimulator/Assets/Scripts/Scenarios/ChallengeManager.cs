using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using CircuitSimulator;

/// <summary>
/// Manages a single challenge scenario within a scene
/// Replaces the free-form component creation with guided challenges
/// </summary>
public class ChallengeManager : MonoBehaviour
{
    [Header("Challenge Configuration")]
    [Tooltip("The challenge definition for this scene")]
    public ChallengeScenario challenge;

    [Header("Scene References")]
    [Tooltip("UI panel for displaying challenge info")]
    public GameObject challengeUI;

    [Tooltip("Text component for challenge description")]
    public Text challengeText;

    [Tooltip("Text component for current hint")]
    public Text hintText;

    [Tooltip("Button to get next hint")]
    public Button hintButton;

    [Tooltip("Panel for success message")]
    public GameObject successPanel;

    [Header("Component Management")]
    [Tooltip("Available component definitions for this challenge")]
    public ComponentDefinition[] availableComponentDefs;

    [Tooltip("Factory for creating components")]
    public ComponentFactoryManager componentFactory;

    [Tooltip("Current palette UI manager")]
    public PaletteUIManager paletteUI;

    [Header("Challenge State")]
    [Tooltip("Components placed by student")]
    public List<GameObject> placedComponents = new List<GameObject>();

    [Tooltip("Current hint index")]
    public int currentHintIndex = 0;

    [Tooltip("Has student completed the challenge?")]
    public bool challengeCompleted = false;

    [Tooltip("Number of attempts student has made")]
    public int attemptCount = 0;

    // Properties for external access
    public ChallengeScenario CurrentChallenge => challenge;

    // Events for external systems
    public System.Action<ChallengeScenario> OnChallengeStarted;
    public System.Action<ChallengeScenario> OnChallengeCompleted;
    public System.Action<CircuitSimulator.MisconceptionType, string> OnMisconceptionDetected;

    void Start()
    {
        InitializeChallenge();
    }

    void InitializeChallenge()
    {
        if (challenge == null)
        {
            Debug.LogError("No challenge scenario assigned to ChallengeManager!");
            return;
        }

        // Set up UI
        DisplayChallengeInfo();

        // Override the palette with challenge-specific components
        SetupChallengeComponentPalette();

        // Clear any existing components
        ClearPlacedComponents();

        // Trigger start event
        OnChallengeStarted?.Invoke(challenge);

        Debug.Log($"Challenge initialized: {challenge.challengeTitle}");
    }

    /// <summary>
    /// Load a specific challenge scenario
    /// </summary>
    public void LoadChallenge(ChallengeScenario newChallenge)
    {
        if (newChallenge == null)
        {
            Debug.LogError("Cannot load null challenge");
            return;
        }

        challenge = newChallenge;
        InitializeChallenge();
    }

    void DisplayChallengeInfo()
    {
        if (challengeText != null)
        {
            challengeText.text = $"{challenge.challengeTitle}\n\n{challenge.description}";
        }

        if (hintButton != null)
        {
            hintButton.onClick.AddListener(ShowNextHint);
        }

        if (challengeUI != null)
        {
            challengeUI.SetActive(true);
        }

        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }
    }

    void SetupChallengeComponentPalette()
    {
        if (paletteUI == null || componentFactory == null)
        {
            Debug.LogWarning("Cannot setup challenge palette - missing references");
            return;
        }

        // Override the factory's component creation with challenge-specific components
        OverridePaletteWithChallengeComponents();
    }

    void OverridePaletteWithChallengeComponents()
    {
        // Clear existing palette
        if (paletteUI.paletteContainer != null)
        {
            foreach (Transform child in paletteUI.paletteContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Create buttons for available components
        foreach (var componentDef in challenge.availableComponents)
        {
            CreateChallengeComponentButton(componentDef);
        }

        // Add essential control buttons
        CreateControlButtons();
    }

    void CreateChallengeComponentButton(ComponentDefinition componentDef)
    {
        if (paletteUI.buttonPrefab == null || paletteUI.paletteContainer == null)
            return;

        // Create button
        Button newButton = Instantiate(paletteUI.buttonPrefab, paletteUI.paletteContainer);
        newButton.name = $"Button_{componentDef.displayName}";

        // Set button text
        Text buttonText = newButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = componentDef.displayName;
        }

        // Set button color based on component type
        var buttonImage = newButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = GetColorForComponentType(componentDef.baseType);
        }

        // Set button action
        newButton.onClick.AddListener(() => CreateChallengeComponent(componentDef));

        // Add tooltip
        var tooltip = newButton.gameObject.AddComponent<TooltipTrigger>();
        tooltip.tooltipText = $"{componentDef.displayName}\n{componentDef.description}";
    }

    void CreateControlButtons()
    {
        if (paletteUI.buttonPrefab == null || paletteUI.paletteContainer == null)
            return;

        // Solve button
        CreateControlButton("Check", Color.green, CheckSolution, "Check if your solution is correct");

        // Delete button
        CreateControlButton("Delete", Color.red, DeleteSelected, "Delete selected component");

        // Reset button
        CreateControlButton("Reset", Color.gray, ResetChallenge, "Start over");

        // Hint button
        CreateControlButton("Hint", Color.yellow, ShowNextHint, "Get a helpful hint");
    }

    void CreateControlButton(string label, Color color, System.Action action, string tooltip)
    {
        Button newButton = Instantiate(paletteUI.buttonPrefab, paletteUI.paletteContainer);
        newButton.name = $"Button_{label}";

        Text buttonText = newButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = label;
        }

        var buttonImage = newButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }

        newButton.onClick.AddListener(() => action());

        var tooltipTrigger = newButton.gameObject.AddComponent<TooltipTrigger>();
        tooltipTrigger.tooltipText = tooltip;
    }

    void CreateChallengeComponent(ComponentDefinition componentDef)
    {
        // Check if student has reached component limit
        if (placedComponents.Count >= challenge.maxComponentsAllowed)
        {
            ShowFeedback($"You can only use {challenge.maxComponentsAllowed} components for this challenge.");
            return;
        }

        // Create the component based on its base type
        GameObject newComponent = CreateComponentByType(componentDef);
        if (newComponent != null)
        {
            // Apply custom properties from the definition
            ApplyComponentDefinition(newComponent, componentDef);

            // Track the component
            placedComponents.Add(newComponent);

            Debug.Log($"Created challenge component: {componentDef.displayName}");
        }
    }

    GameObject CreateComponentByType(ComponentDefinition componentDef)
    {
        GameObject component = null;

        switch (componentDef.baseType)
        {
            case BaseComponentType.Battery:
                component = componentFactory.CreateBattery();
                break;
            case BaseComponentType.Bulb:
                component = componentFactory.CreateBulb();
                break;
            case BaseComponentType.Resistor:
                component = componentFactory.CreateResistor();
                break;
            case BaseComponentType.Switch:
                component = componentFactory.CreateSwitch();
                break;
            // Add more types as needed
        }

        return component;
    }

    void ApplyComponentDefinition(GameObject component, ComponentDefinition componentDef)
    {
        // Set electrical properties
        var circuitComponent = component.GetComponent<CircuitComponent3D>();
        if (circuitComponent != null)
        {
            switch (componentDef.baseType)
            {
                case BaseComponentType.Battery:
                    circuitComponent.voltage = componentDef.electricalProperties.voltage;
                    break;
                case BaseComponentType.Bulb:
                case BaseComponentType.Resistor:
                    circuitComponent.resistance = componentDef.electricalProperties.resistance;
                    break;
            }
        }

        // Apply visual properties
        if (componentDef.componentColor != Color.white)
        {
            var renderer = component.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = componentDef.componentColor;
            }
        }

        // Set display name
        component.name = componentDef.displayName;

        // Apply scaling
        if (componentDef.modelScale != 1f)
        {
            component.transform.localScale *= componentDef.modelScale;
        }

        // Set interaction permissions
        var selectable = component.GetComponent<SelectableComponent>();
        if (selectable != null)
        {
            selectable.enabled = componentDef.allowValueEditing;
        }

        var moveable = component.GetComponent<MoveableComponent>();
        if (moveable != null)
        {
            moveable.enabled = componentDef.allowMovement;
        }

        // Add challenge component marker
        var challengeMarker = component.AddComponent<ChallengeComponentMarker>();
        challengeMarker.sourceDefinition = componentDef;
    }

    void CheckSolution()
    {
        attemptCount++;

        // Solve the circuit first
        var solverManager = FindFirstObjectByType<CircuitSolverManager>();
        if (solverManager != null)
        {
            solverManager.SolveCircuit();
        }
        else
        {
            Debug.LogWarning("[ChallengeManager] CircuitSolverManager not found in scene");
        }

        // Check after a short delay to let solver complete
        Invoke(nameof(EvaluateSolution), 0.2f);
    }

    void EvaluateSolution()
    {
        bool allGoalsMet = true;
        List<string> feedback = new List<string>();

        // Check each goal
        foreach (var goal in challenge.goals)
        {
            bool goalMet = EvaluateGoal(goal);
            if (!goalMet && goal.isMandatory)
            {
                allGoalsMet = false;
                feedback.Add($"❌ {goal.description}");
            }
            else if (goalMet)
            {
                feedback.Add($"✅ {goal.description}");
            }
        }

        // Check for common mistakes
        CheckForCommonMistakes(feedback);

        if (allGoalsMet)
        {
            // Challenge completed!
            CompleteChallengeSuccess();
        }
        else
        {
            // Show feedback and allow retry
            ShowFeedback(string.Join("\n", feedback));
        }
    }

    bool EvaluateGoal(ChallengeGoal goal)
    {
        // Find the target component
        var targetComponent = placedComponents.FirstOrDefault(c => c.name == goal.targetComponentName);
        if (targetComponent == null)
        {
            return false;
        }

        var circuitComponent = targetComponent.GetComponent<CircuitComponent3D>();
        if (circuitComponent == null)
        {
            return false;
        }

        // Check goal based on type
        switch (goal.goalType)
        {
            case GoalType.ComponentPowered:
                return circuitComponent.current > 0.01f; // Has meaningful current

            case GoalType.SpecificVoltage:
                float voltageError = Mathf.Abs(circuitComponent.voltageDrop - goal.expectedValue) / goal.expectedValue * 100f;
                return voltageError <= challenge.tolerancePercent;

            case GoalType.SpecificCurrent:
                float currentError = Mathf.Abs(circuitComponent.current - goal.expectedValue) / goal.expectedValue * 100f;
                return currentError <= challenge.tolerancePercent;

            case GoalType.CircuitComplete:
                // Check if there's a complete circuit (current flowing)
                return circuitComponent.current > 0.001f;

            default:
                return false;
        }
    }

    void CheckForCommonMistakes(List<string> feedback)
    {
        foreach (var misconception in challenge.misconceptionChecks)
        {
            if (DetectMisconception(misconception))
            {
                feedback.Add($"💡 {misconception.feedback}");
                OnMisconceptionDetected?.Invoke((CircuitSimulator.MisconceptionType)misconception.type, misconception.feedback);
                break; // Only show one mistake at a time
            }
        }
    }

    bool DetectMisconception(MisconceptionCheck misconception)
    {
        // Simple misconception detection based on type
        switch (misconception.type)
        {
            case CircuitSimulator.MisconceptionType.M1_SinkModel:
                // Check for incomplete circuits (single wire connections)
                return DetectSinkModelMisconception();

            case CircuitSimulator.MisconceptionType.M2_CurrentAttenuation:
                // Check for current understanding issues
                return false; // Placeholder implementation

            case CircuitSimulator.MisconceptionType.M8_ConstantCurrentSource:
                // Check for battery misconceptions
                return false; // Placeholder implementation

            default:
                return false;
        }
    }

    bool DetectSinkModelMisconception()
    {
        // Simple check: look for components with only one wire connection
        foreach (var component in placedComponents)
        {
            var wireCount = CountWiresConnectedTo(component);
            if (wireCount == 1)
            {
                return true; // Found potential sink model thinking
            }
        }
        return false;
    }

    int CountWiresConnectedTo(GameObject component)
    {
        // Count wires connected to this component
        var circuitComponent = component.GetComponent<CircuitComponent3D>();
        if (circuitComponent != null && circuitComponent.connectedWires != null)
        {
            return circuitComponent.connectedWires.Count;
        }
        return 0;
    }

    void CompleteChallengeSuccess()
    {
        challengeCompleted = true;

        // Show success UI
        if (successPanel != null)
        {
            successPanel.SetActive(true);
            var successText = successPanel.GetComponentInChildren<Text>();
            if (successText != null)
            {
                successText.text = challenge.successMessage + "\n\nKey Learning Points:\n" +
                                  string.Join("\n• ", challenge.learningPoints);
            }
        }

        // Trigger completion event
        OnChallengeCompleted?.Invoke(challenge);

        Debug.Log($"Challenge completed: {challenge.challengeTitle} in {attemptCount} attempts");
    }

    void ShowFeedback(string message)
    {
        // Show feedback to student
        Debug.Log($"Challenge Feedback: {message}");

        // TODO: Integrate with proper UI feedback system
        // This could be a popup, status text, or other UI element
    }

    public void ShowNextHint()
    {
        if (currentHintIndex < challenge.hints.Length)
        {
            string hint = challenge.hints[currentHintIndex];
            currentHintIndex++;

            if (hintText != null)
            {
                hintText.text = $"Hint {currentHintIndex}: {hint}";
            }

            // Disable hint button if no more hints
            if (currentHintIndex >= challenge.hints.Length && hintButton != null)
            {
                hintButton.interactable = false;
                hintButton.GetComponentInChildren<Text>().text = "No More Hints";
            }

            Debug.Log($"Hint shown: {hint}");
        }
    }

    void DeleteSelected()
    {
        // Find selected component and remove it
        var allSelectables = FindObjectsByType<SelectableComponent>(FindObjectsSortMode.None);
        foreach (var selectable in allSelectables)
        {
            if (selectable.IsSelected())
            {
                GameObject toDelete = selectable.gameObject;
                placedComponents.Remove(toDelete);
                DestroyImmediate(toDelete);
                break; // Only delete one at a time
            }
        }
    }

    public void ResetChallenge()
    {
        ClearPlacedComponents();
        currentHintIndex = 0;
        attemptCount = 0;
        challengeCompleted = false;

        if (hintText != null)
        {
            hintText.text = "Click 'Hint' if you need help";
        }

        if (hintButton != null)
        {
            hintButton.interactable = true;
            hintButton.GetComponentInChildren<Text>().text = "Hint";
        }

        if (successPanel != null)
        {
            successPanel.SetActive(false);
        }

        Debug.Log("Challenge reset");
    }

    void ClearPlacedComponents()
    {
        foreach (var component in placedComponents)
        {
            if (component != null)
            {
                DestroyImmediate(component);
            }
        }
        placedComponents.Clear();
    }

    Color GetColorForComponentType(BaseComponentType type)
    {
        switch (type)
        {
            case BaseComponentType.Battery:
                return new Color(0.8f, 0.2f, 0.2f, 1f); // Red
            case BaseComponentType.Bulb:
                return new Color(0.9f, 0.9f, 0.3f, 1f); // Yellow
            case BaseComponentType.Resistor:
                return new Color(0.8f, 0.6f, 0.2f, 1f); // Orange
            case BaseComponentType.Switch:
                return new Color(0.3f, 0.3f, 0.4f, 1f); // Gray
            default:
                return Color.white;
        }
    }
}

/// <summary>
/// Marker component for objects created in challenges
/// </summary>
public class ChallengeComponentMarker : MonoBehaviour
{
    public ComponentDefinition sourceDefinition;
}