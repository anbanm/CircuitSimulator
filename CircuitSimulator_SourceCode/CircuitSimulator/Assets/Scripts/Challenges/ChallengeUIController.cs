using UnityEngine;
using UnityEngine.UIElements;
using CircuitSimulator.Services;

namespace CircuitSimulator.Challenges
{
    /// <summary>
    /// UI controller for challenge-specific interface.
    /// Generates UI dynamically based on challenge configuration.
    /// </summary>
    public class ChallengeUIController : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private bool useUIToolkit = true;

        [Header("UI Elements")]
        [SerializeField] private Canvas legacyCanvas;
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private GameObject feedbackPanel;

        // UI Elements (UI Toolkit)
        private VisualElement rootElement;
        private Label challengeTitleLabel;
        private Label descriptionLabel;
        private VisualElement componentPalette;
        private VisualElement progressPanel;
        private VisualElement hintContainer;
        private VisualElement feedbackContainer;
        private Button hintButton;
        private Button resetButton;

        // Service Dependencies
        private IComponentFactory componentFactory;

        // State
        private ChallengeScenario currentChallenge;
        private bool uiInitialized = false;

        void Start()
        {
            InitializeServices();
            InitializeUI();
        }

        void InitializeServices()
        {
            componentFactory = ServiceLocator.Instance.TryGet<IComponentFactory>();
            if (componentFactory == null)
            {
                Debug.LogWarning("[ChallengeUIController] IComponentFactory service not found");
            }
        }

        void InitializeUI()
        {
            if (useUIToolkit)
            {
                InitializeUIToolkit();
            }
            else
            {
                InitializeLegacyUI();
            }

            uiInitialized = true;
        }

        void InitializeUIToolkit()
        {
            // Get or create UI Document
            if (uiDocument == null)
            {
                var uiGO = new GameObject("Challenge UI");
                uiDocument = uiGO.AddComponent<UIDocument>();

                // Create basic UXML structure
                var visualTree = CreateChallengeUITemplate();
                uiDocument.visualTreeAsset = visualTree;
            }

            rootElement = uiDocument.rootVisualElement;

            // Cache UI elements
            challengeTitleLabel = rootElement.Q<Label>("challenge-title");
            descriptionLabel = rootElement.Q<Label>("challenge-description");
            componentPalette = rootElement.Q<VisualElement>("component-palette");
            progressPanel = rootElement.Q<VisualElement>("progress-panel");
            hintContainer = rootElement.Q<VisualElement>("hint-container");
            feedbackContainer = rootElement.Q<VisualElement>("feedback-container");
            hintButton = rootElement.Q<Button>("hint-button");
            resetButton = rootElement.Q<Button>("reset-button");

            // Setup button callbacks
            if (hintButton != null)
                hintButton.clicked += RequestHint;

            if (resetButton != null)
                resetButton.clicked += ResetChallenge;
        }

        void InitializeLegacyUI()
        {
            // Create legacy UI canvas if needed
            if (legacyCanvas == null)
            {
                var canvasGO = new GameObject("Challenge Canvas");
                legacyCanvas = canvasGO.AddComponent<Canvas>();
                legacyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        public void SetupChallengeUI(ChallengeScenario challenge)
        {
            if (!uiInitialized) return;

            currentChallenge = challenge;

            // Update challenge info
            UpdateChallengeInfo();

            // Generate component palette
            GenerateComponentPalette();

            // Setup progress tracking
            SetupProgressTracking();

            // Configure hint system
            ConfigureHintSystem();

            Debug.Log($"[ChallengeUIController] UI setup for: {challenge.challengeTitle}");
        }

        void UpdateChallengeInfo()
        {
            if (challengeTitleLabel != null)
                challengeTitleLabel.text = currentChallenge.challengeTitle;

            if (descriptionLabel != null)
                descriptionLabel.text = currentChallenge.description;
        }

        void GenerateComponentPalette()
        {
            if (componentPalette == null) return;

            // Clear existing palette
            componentPalette.Clear();

            // Add title
            var paletteTitle = new Label("Available Components");
            paletteTitle.AddToClassList("palette-title");
            componentPalette.Add(paletteTitle);

            // Create component buttons
            foreach (var componentDef in currentChallenge.availableComponents)
            {
                var button = new Button(() => CreateComponent(componentDef))
                {
                    text = componentDef.displayName
                };

                button.AddToClassList("component-button");
                button.tooltip = componentDef.description;

                // Add visual styling based on component type
                button.AddToClassList($"component-{componentDef.baseType.ToString().ToLower()}");

                componentPalette.Add(button);
            }
        }

        void SetupProgressTracking()
        {
            if (progressPanel == null) return;

            progressPanel.Clear();

            var progressTitle = new Label("Challenge Goals");
            progressTitle.AddToClassList("progress-title");
            progressPanel.Add(progressTitle);

            // Create goal tracking elements
            foreach (var goal in currentChallenge.goals)
            {
                var goalElement = new VisualElement();
                goalElement.AddToClassList("goal-item");

                var goalLabel = new Label(goal.description);
                goalLabel.AddToClassList("goal-description");

                var statusIndicator = new VisualElement();
                statusIndicator.AddToClassList("goal-status");
                statusIndicator.AddToClassList("goal-pending");

                goalElement.Add(goalLabel);
                goalElement.Add(statusIndicator);
                progressPanel.Add(goalElement);
            }
        }

        void ConfigureHintSystem()
        {
            if (hintButton == null) return;

            var hintSystem = currentChallenge.hintSystem;

            if (hintSystem.hintsEnabled)
            {
                hintButton.style.display = DisplayStyle.Flex;
                hintButton.text = $"Hint ({hintSystem.hints.Length} available)";
            }
            else
            {
                hintButton.style.display = DisplayStyle.None;
            }
        }

        public void ShowHint(Hint hint)
        {
            if (hintContainer == null) return;

            // Create hint display
            var hintElement = new VisualElement();
            hintElement.AddToClassList("hint-display");

            var hintText = new Label(hint.text);
            hintText.AddToClassList("hint-text");

            var closeButton = new Button(() => hintElement.RemoveFromHierarchy())
            {
                text = "×"
            };
            closeButton.AddToClassList("hint-close");

            hintElement.Add(hintText);
            hintElement.Add(closeButton);
            hintContainer.Add(hintElement);

            // Auto-hide after delay
            StartCoroutine(AutoHideHint(hintElement, 10f));
        }

        public void ShowMisconceptionFeedback(MisconceptionCheck misconception)
        {
            if (feedbackContainer == null) return;

            var feedbackElement = new VisualElement();
            feedbackElement.AddToClassList("misconception-feedback");

            var feedbackText = new Label(misconception.feedback);
            feedbackText.AddToClassList("misconception-text");

            var closeButton = new Button(() => feedbackElement.RemoveFromHierarchy())
            {
                text = "OK"
            };
            closeButton.AddToClassList("feedback-close");

            feedbackElement.Add(feedbackText);
            feedbackElement.Add(closeButton);
            feedbackContainer.Add(feedbackElement);
        }

        public void ShowCompletionFeedback(ChallengeResult result, float completionTime, int hintsUsed)
        {
            if (feedbackContainer == null) return;

            var completionElement = new VisualElement();
            completionElement.AddToClassList("completion-feedback");

            var titleLabel = new Label("Challenge Complete!");
            titleLabel.AddToClassList("completion-title");

            var timeLabel = new Label($"Time: {completionTime:F1} seconds");
            var hintsLabel = new Label($"Hints used: {hintsUsed}");
            var scoreLabel = new Label($"Completion: {result.completionPercentage:P0}");

            var continueButton = new Button(() => completionElement.RemoveFromHierarchy())
            {
                text = "Continue"
            };

            completionElement.Add(titleLabel);
            completionElement.Add(timeLabel);
            completionElement.Add(hintsLabel);
            completionElement.Add(scoreLabel);
            completionElement.Add(continueButton);

            feedbackContainer.Add(completionElement);
        }

        public void UpdateProgress(ChallengeResult result)
        {
            if (progressPanel == null) return;

            var goalItems = progressPanel.Query<VisualElement>(className: "goal-item").ToList();

            for (int i = 0; i < goalItems.Count && i < currentChallenge.goals.Length; i++)
            {
                var goalItem = goalItems[i];
                var statusIndicator = goalItem.Q<VisualElement>(className: "goal-status");

                if (result.completedGoals.Contains(currentChallenge.goals[i].goalId))
                {
                    statusIndicator.RemoveFromClassList("goal-pending");
                    statusIndicator.AddToClassList("goal-completed");
                }
            }
        }

        public void ResetUI()
        {
            if (hintContainer != null)
                hintContainer.Clear();

            if (feedbackContainer != null)
                feedbackContainer.Clear();

            if (progressPanel != null)
                SetupProgressTracking();
        }

        void CreateComponent(ComponentDefinition definition)
        {
            if (componentFactory == null)
            {
                Debug.LogWarning("[ChallengeUIController] Cannot create component - factory not available");
                return;
            }

            var mousePos = Input.mousePosition;
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = FindFirstObjectByType<Camera>();
                if (camera == null)
                {
                    Debug.LogError("[ChallengeUIController] No camera found in scene");
                    return;
                }
            }

            var worldPos = camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
            componentFactory.CreateComponent(definition, worldPos);
        }

        void RequestHint()
        {
            var challengeManager = FindFirstObjectByType<global::ChallengeManager>();
            if (challengeManager != null)
            {
                challengeManager.ShowNextHint();
            }
            else
            {
                Debug.LogWarning("[ChallengeUIController] ChallengeManager not found");
            }
        }

        void ResetChallenge()
        {
            var challengeManager = FindFirstObjectByType<global::ChallengeManager>();
            if (challengeManager != null)
            {
                challengeManager.ResetChallenge();
            }
            else
            {
                Debug.LogWarning("[ChallengeUIController] ChallengeManager not found");
            }
        }

        private System.Collections.IEnumerator AutoHideHint(VisualElement hintElement, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (hintElement.parent != null)
                hintElement.RemoveFromHierarchy();
        }

        // Create basic UXML template programmatically
        private VisualTreeAsset CreateChallengeUITemplate()
        {
            // This would normally be a .uxml file, but for rapid implementation
            // we'll create the structure programmatically

            // In a real implementation, you'd create a proper .uxml file
            // For now, return null and handle in code
            return null;
        }
    }
}