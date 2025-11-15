using System.Collections.Generic;
using UnityEngine;
using CircuitSimulator.Services;

namespace CircuitSimulator.Challenges
{
    /// <summary>
    /// Scene-based challenge controller that manages educational scenarios.
    /// Replaces free-form simulator with guided learning experiences.
    /// </summary>
    public class ChallengeManager : MonoBehaviour
    {
        [Header("Challenge Configuration")]
        [SerializeField] private ChallengeScenario currentChallenge;
        [SerializeField] private bool autoStartChallenge = true;
        [SerializeField] private bool enableDebugMode = false;

        [Header("Challenge State")]
        [SerializeField] private bool challengeActive = false;
        [SerializeField] private float challengeStartTime = 0f;
        [SerializeField] private int hintsUsed = 0;

        // Service Dependencies
        private ICircuitManager circuitManager;
        private IComponentFactory componentFactory;
        private IValidationService validationService;
        private ICircuitSolver circuitSolver;

        // Challenge Components
        private List<GameObject> preplacedObjects = new List<GameObject>();
        private List<GameObject> studentComponents = new List<GameObject>();
        private ChallengeUIController uiController;

        // Events
        public System.Action<ChallengeScenario> OnChallengeStarted;
        public System.Action<ChallengeResult> OnChallengeCompleted;
        public System.Action<string> OnHintRequested;
        public System.Action<MisconceptionType> OnMisconceptionDetected;

        void Start()
        {
            InitializeServices();
            InitializeChallenge();

            if (autoStartChallenge && currentChallenge != null)
            {
                StartChallenge();
            }
        }

        void InitializeServices()
        {
            var serviceLocator = ServiceLocator.Instance;

            // Get required services
            circuitManager = serviceLocator.Get<ICircuitManager>();
            componentFactory = serviceLocator.Get<IComponentFactory>();
            validationService = serviceLocator.Get<IValidationService>();
            circuitSolver = serviceLocator.Get<ICircuitSolver>();

            // Subscribe to circuit events
            circuitManager.OnCircuitChanged += OnCircuitChanged;
            validationService.OnChallengeCompleted += OnChallengeValidationCompleted;

            if (enableDebugMode)
                Debug.Log("[ChallengeManager] Services initialized successfully");
        }

        void InitializeChallenge()
        {
            // Find or create UI controller
            uiController = FindFirstObjectByType<ChallengeUIController>();
            if (uiController == null)
            {
                var uiGO = new GameObject("ChallengeUIController");
                uiController = uiGO.AddComponent<ChallengeUIController>();
            }

            // Validate current challenge
            if (currentChallenge != null && !currentChallenge.ValidateChallenge())
            {
                Debug.LogError($"[ChallengeManager] Challenge '{currentChallenge.challengeTitle}' failed validation!");
            }
        }

        public void LoadChallenge(ChallengeScenario challenge)
        {
            if (challengeActive)
            {
                StopChallenge();
            }

            currentChallenge = challenge;
            InitializeChallenge();

            if (enableDebugMode)
                Debug.Log($"[ChallengeManager] Loaded challenge: {challenge.challengeTitle}");
        }

        public void StartChallenge()
        {
            if (currentChallenge == null)
            {
                Debug.LogError("[ChallengeManager] No challenge loaded!");
                return;
            }

            challengeActive = true;
            challengeStartTime = Time.time;
            hintsUsed = 0;

            // Clear existing circuit
            circuitManager.ResetCircuit();

            // Create pre-placed objects
            CreatePreplacedObjects();

            // Setup UI for this challenge
            uiController.SetupChallengeUI(currentChallenge);

            // Start monitoring for misconceptions
            StartMisconceptionMonitoring();

            OnChallengeStarted?.Invoke(currentChallenge);

            if (enableDebugMode)
                Debug.Log($"[ChallengeManager] Started challenge: {currentChallenge.challengeTitle}");
        }

        public void StopChallenge()
        {
            if (!challengeActive) return;

            challengeActive = false;

            // Clean up challenge objects
            CleanupChallengeObjects();

            // Reset UI
            uiController?.ResetUI();

            if (enableDebugMode)
                Debug.Log("[ChallengeManager] Challenge stopped");
        }

        public ChallengeResult CheckCompletion()
        {
            if (!challengeActive || currentChallenge == null)
                return null;

            return validationService.CheckChallengeCompletion(currentChallenge);
        }

        public void RequestHint()
        {
            if (!challengeActive || currentChallenge == null) return;

            var hintSystem = currentChallenge.hintSystem;
            if (!hintSystem.hintsEnabled) return;

            if (hintsUsed < hintSystem.hints.Length)
            {
                var hint = hintSystem.hints[hintsUsed];
                uiController.ShowHint(hint);
                hintsUsed++;

                OnHintRequested?.Invoke(hint.text);

                if (enableDebugMode)
                    Debug.Log($"[ChallengeManager] Hint {hintsUsed}: {hint.text}");
            }
        }

        private void CreatePreplacedObjects()
        {
            foreach (var preplacedObj in currentChallenge.preplacedObjects)
            {
                var component = componentFactory.CreateComponent(
                    preplacedObj.componentDefinition,
                    preplacedObj.position
                );

                if (component != null)
                {
                    component.transform.rotation = Quaternion.Euler(preplacedObj.rotation);

                    // Make fixed objects immovable
                    if (preplacedObj.isFixed)
                    {
                        var moveable = component.GetComponent<MoveableComponent>();
                        if (moveable != null)
                            moveable.enabled = false;
                    }

                    // Add label if specified
                    if (!string.IsNullOrEmpty(preplacedObj.label))
                    {
                        // Add label component or update existing one
                        // This would integrate with the existing label system
                    }

                    preplacedObjects.Add(component.gameObject);

                    if (enableDebugMode)
                        Debug.Log($"[ChallengeManager] Created preplaced: {preplacedObj.componentDefinition.displayName}");
                }
            }
        }

        private void CleanupChallengeObjects()
        {
            // Clean up pre-placed objects
            foreach (var obj in preplacedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            preplacedObjects.Clear();

            // Clean up student-created components
            foreach (var obj in studentComponents)
            {
                if (obj != null)
                    Destroy(obj);
            }
            studentComponents.Clear();
        }

        private void OnCircuitChanged()
        {
            if (!challengeActive) return;

            // Check for immediate misconceptions
            CheckForMisconceptions();

            // Check for goal completion
            var result = CheckCompletion();
            if (result != null && result.completed)
            {
                OnChallengeValidationCompleted(result);
            }
        }

        private void CheckForMisconceptions()
        {
            foreach (var misconceptionCheck in currentChallenge.misconceptionChecks)
            {
                bool detected = false;

                switch (misconceptionCheck.type)
                {
                    case MisconceptionType.M1_SinkModel:
                        detected = validationService.DetectSinkModelMisconception();
                        break;
                    case MisconceptionType.M2_CurrentAttenuation:
                        detected = validationService.DetectCurrentAttenuationMisconception();
                        break;
                    case MisconceptionType.M8_ConstantCurrentSource:
                        detected = validationService.DetectConstantCurrentMisconception();
                        break;
                }

                if (detected)
                {
                    OnMisconceptionDetected?.Invoke(misconceptionCheck.type);

                    if (misconceptionCheck.showImmediateFeedback)
                    {
                        uiController.ShowMisconceptionFeedback(misconceptionCheck);
                    }
                }
            }
        }

        private void StartMisconceptionMonitoring()
        {
            // Enable detailed monitoring for educational feedback
            if (circuitSolver != null)
            {
                circuitSolver.IsDebugEnabled = true;
            }
        }

        private void OnChallengeValidationCompleted(ChallengeResult result)
        {
            if (result.completed)
            {
                challengeActive = false;

                // Calculate final score and time
                float completionTime = Time.time - challengeStartTime;

                // Show success feedback
                uiController.ShowCompletionFeedback(result, completionTime, hintsUsed);

                OnChallengeCompleted?.Invoke(result);

                if (enableDebugMode)
                    Debug.Log($"[ChallengeManager] Challenge completed! Time: {completionTime:F1}s, Hints: {hintsUsed}");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (circuitManager != null)
                circuitManager.OnCircuitChanged -= OnCircuitChanged;

            if (validationService != null)
                validationService.OnChallengeCompleted -= OnChallengeValidationCompleted;
        }

        // Public getters for UI and other systems
        public ChallengeScenario CurrentChallenge => currentChallenge;
        public bool IsChallengeActive => challengeActive;
        public float ChallengeElapsedTime => challengeActive ? Time.time - challengeStartTime : 0f;
        public int HintsUsed => hintsUsed;
    }
}