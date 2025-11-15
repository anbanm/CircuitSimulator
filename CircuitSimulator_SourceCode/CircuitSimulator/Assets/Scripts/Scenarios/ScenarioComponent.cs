using UnityEngine;

/// <summary>
/// Marker component for objects created by scenarios
/// Provides metadata and scenario-specific behavior
/// </summary>
public class ScenarioComponent : MonoBehaviour
{
    [Header("Scenario Data")]
    [Tooltip("The component data this object was created from")]
    public ComponentData sourceData;

    [Tooltip("Is this component locked from modification?")]
    public bool isLocked = false;

    [Tooltip("Custom behavior for this scenario component")]
    public ScenarioComponentBehavior behavior = ScenarioComponentBehavior.Normal;

    [Header("Educational Metadata")]
    [Tooltip("What concept this component demonstrates")]
    public string conceptTag = "";

    [Tooltip("Hint text for students")]
    [TextArea(2, 3)]
    public string hintText = "";

    [Tooltip("Should this component be highlighted?")]
    public bool isHighlighted = false;

    private Material originalMaterial;
    private Material highlightMaterial;

    void Start()
    {
        // Store original material for highlighting
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
        }

        // Apply scenario-specific settings
        ApplyScenarioBehavior();
    }

    /// <summary>
    /// Initialize with component data from scenario
    /// </summary>
    public void Initialize(ComponentData data)
    {
        sourceData = data;
        conceptTag = GetConceptTagForComponent(data.type);

        // Set locked state based on interactivity
        isLocked = !data.isInteractive;

        // Apply any custom behavior
        ApplyScenarioBehavior();
    }

    void ApplyScenarioBehavior()
    {
        switch (behavior)
        {
            case ScenarioComponentBehavior.Locked:
                SetComponentLocked(true);
                break;

            case ScenarioComponentBehavior.HighlightOnHover:
                AddHoverBehavior();
                break;

            case ScenarioComponentBehavior.ReadOnly:
                SetComponentReadOnly();
                break;

            case ScenarioComponentBehavior.TeachingFocus:
                SetTeachingFocus();
                break;
        }

        if (isHighlighted)
        {
            HighlightComponent(true);
        }
    }

    void SetComponentLocked(bool locked)
    {
        isLocked = locked;

        // Disable interaction components
        var selectable = GetComponent<SelectableComponent>();
        if (selectable != null)
        {
            selectable.enabled = !locked;
        }

        var moveable = GetComponent<MoveableComponent>();
        if (moveable != null)
        {
            moveable.enabled = !locked;
        }

        // Visual indication of locked state
        if (locked)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = renderer.material;
                var color = material.color;
                color.a = 0.7f; // Semi-transparent
                material.color = color;
            }
        }
    }

    void AddHoverBehavior()
    {
        // Add hover highlighting for educational emphasis
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
        }

        // Ensure we can detect mouse hover
        if (GetComponent<HoverDetector>() == null)
        {
            gameObject.AddComponent<HoverDetector>();
        }
    }

    void SetComponentReadOnly()
    {
        // Allow selection but not modification
        var selectable = GetComponent<SelectableComponent>();
        if (selectable != null)
        {
            selectable.enabled = true;
        }

        var moveable = GetComponent<MoveableComponent>();
        if (moveable != null)
        {
            moveable.enabled = false;
        }

        // TODO: Disable property editing
    }

    void SetTeachingFocus()
    {
        // Special highlighting for key teaching components
        isHighlighted = true;
        HighlightComponent(true);

        // Add pulsing effect for attention
        var pulser = GetComponent<ComponentPulser>();
        if (pulser == null)
        {
            gameObject.AddComponent<ComponentPulser>();
        }
    }

    /// <summary>
    /// Highlight or unhighlight this component
    /// </summary>
    public void HighlightComponent(bool highlight)
    {
        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        if (highlight)
        {
            if (highlightMaterial == null)
            {
                // Create highlight material
                highlightMaterial = new Material(originalMaterial);
                highlightMaterial.color = Color.yellow;
                highlightMaterial.SetFloat("_Metallic", 0.5f);
                highlightMaterial.SetFloat("_Glossiness", 0.8f);
            }
            renderer.material = highlightMaterial;
        }
        else
        {
            renderer.material = originalMaterial;
        }

        isHighlighted = highlight;
    }

    /// <summary>
    /// Show hint text for this component
    /// </summary>
    public void ShowHint()
    {
        if (!string.IsNullOrEmpty(hintText))
        {
            // TODO: Integrate with UI system to show hint popup
            Debug.Log($"Hint for {sourceData.displayName}: {hintText}");
        }
    }

    /// <summary>
    /// Get the educational concept this component type represents
    /// </summary>
    string GetConceptTagForComponent(ComponentType type)
    {
        switch (type)
        {
            case ComponentType.Battery:
                return "Voltage Source";
            case ComponentType.Resistor:
                return "Resistance/Ohm's Law";
            case ComponentType.Bulb:
                return "Load/Energy Conversion";
            case ComponentType.Switch:
                return "Circuit Control";
            case ComponentType.Junction:
                return "Parallel Connections";
            default:
                return "Circuit Element";
        }
    }

    /// <summary>
    /// Validate this component matches its expected behavior
    /// </summary>
    public bool ValidateComponent()
    {
        var circuitComponent = GetComponent<CircuitComponent3D>();
        if (circuitComponent == null) return false;

        // Check if component values match scenario expectations
        switch (sourceData.type)
        {
            case ComponentType.Battery:
                return Mathf.Approximately(circuitComponent.voltage, sourceData.properties.voltage);

            case ComponentType.Resistor:
            case ComponentType.Bulb:
                return Mathf.Approximately(circuitComponent.resistance, sourceData.properties.resistance);

            default:
                return true;
        }
    }

    void OnDestroy()
    {
        // Clean up materials
        if (highlightMaterial != null)
        {
            DestroyImmediate(highlightMaterial);
        }
    }
}

/// <summary>
/// Behavior modes for scenario components
/// </summary>
public enum ScenarioComponentBehavior
{
    Normal,           // Standard component behavior
    Locked,           // Cannot be moved or modified
    HighlightOnHover, // Highlights when mouse hovers
    ReadOnly,         // Can be selected but not modified
    TeachingFocus     // Special emphasis for key learning components
}

/// <summary>
/// Simple hover detection for educational emphasis
/// </summary>
public class HoverDetector : MonoBehaviour
{
    private ScenarioComponent scenarioComponent;

    void Start()
    {
        scenarioComponent = GetComponent<ScenarioComponent>();
    }

    void OnMouseEnter()
    {
        if (scenarioComponent != null)
        {
            scenarioComponent.HighlightComponent(true);
            scenarioComponent.ShowHint();
        }
    }

    void OnMouseExit()
    {
        if (scenarioComponent != null && !scenarioComponent.isHighlighted)
        {
            scenarioComponent.HighlightComponent(false);
        }
    }
}

/// <summary>
/// Pulsing effect for teaching focus components
/// </summary>
public class ComponentPulser : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float minAlpha = 0.6f;
    public float maxAlpha = 1f;

    private Renderer componentRenderer;
    private Material pulseMaterial;

    void Start()
    {
        componentRenderer = GetComponent<Renderer>();
        if (componentRenderer != null)
        {
            pulseMaterial = componentRenderer.material;
        }
    }

    void Update()
    {
        if (pulseMaterial != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            var color = pulseMaterial.color;
            color.a = alpha;
            pulseMaterial.color = color;
        }
    }

    void OnDestroy()
    {
        // Reset alpha when destroyed
        if (pulseMaterial != null)
        {
            var color = pulseMaterial.color;
            color.a = 1f;
            pulseMaterial.color = color;
        }
    }
}