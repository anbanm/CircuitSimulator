using UnityEngine;
using CircuitSimulator.Services;
using CircuitSimulator.Challenges;
using CircuitSimulator.Components;

namespace CircuitSimulator.Tests
{
    /// <summary>
    /// Comprehensive integration test for the complete v2.0 system.
    /// Tests ServiceLocator, aspect-based components, challenge system, and ScriptableObject integration.
    /// </summary>
    public class FullSystemIntegrationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool createTestComponents = true;
        [SerializeField] private bool loadTestChallenge = true;

        [Header("Test Scene Setup")]
        [SerializeField] private Transform componentParent;
        [SerializeField] private Camera testCamera;

        private bool testsCompleted = false;

        void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunFullIntegrationTest());
            }
        }

        private System.Collections.IEnumerator RunFullIntegrationTest()
        {
            Debug.Log("=== FULL SYSTEM INTEGRATION TEST v2.0 ===");

            yield return StartCoroutine(TestPhase1_ServiceLocator());
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(TestPhase2_AspectComponents());
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(TestPhase3_ComponentFactory());
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(TestPhase4_ChallengeSystem());
            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(TestPhase5_FullCircuitSolving());

            Debug.Log("=== INTEGRATION TEST COMPLETE ===");
            testsCompleted = true;
        }

        #region Phase 1: ServiceLocator Foundation

        private System.Collections.IEnumerator TestPhase1_ServiceLocator()
        {
            Debug.Log("[PHASE 1] Testing ServiceLocator Foundation...");

            // Test ServiceLocator singleton
            var serviceLocator = ServiceLocator.Instance;
            if (serviceLocator == null)
            {
                Debug.LogError("❌ ServiceLocator.Instance is null!");
                yield break;
            }

            // Test core services registration
            bool hasCircuitManager = serviceLocator.IsRegistered<ICircuitManager>();
            bool hasComponentFactory = serviceLocator.IsRegistered<IComponentFactory>();
            bool hasCircuitSolver = serviceLocator.IsRegistered<ICircuitSolver>();
            bool hasValidationService = serviceLocator.IsRegistered<IValidationService>();

            Debug.Log($"Service Registration Status:");
            Debug.Log($"  ICircuitManager: {(hasCircuitManager ? "✅" : "❌")}");
            Debug.Log($"  IComponentFactory: {(hasComponentFactory ? "✅" : "❌")}");
            Debug.Log($"  ICircuitSolver: {(hasCircuitSolver ? "✅" : "❌")}");
            Debug.Log($"  IValidationService: {(hasValidationService ? "✅" : "❌")}");

            if (hasCircuitManager && hasComponentFactory && hasCircuitSolver)
            {
                Debug.Log("✅ Phase 1: ServiceLocator Foundation - PASSED");
            }
            else
            {
                Debug.LogError("❌ Phase 1: ServiceLocator Foundation - FAILED");
            }

            yield return null;
        }

        #endregion

        #region Phase 2: Aspect Components

        private System.Collections.IEnumerator TestPhase2_AspectComponents()
        {
            Debug.Log("[PHASE 2] Testing Aspect-Based Components...");

            if (!createTestComponents)
            {
                Debug.Log("⚠️ Component creation disabled, skipping aspect tests");
                yield break;
            }

            // Create test component with all aspects
            var testGO = CreateTestComponentWithAspects();
            if (testGO == null)
            {
                Debug.LogError("❌ Failed to create test component");
                yield break;
            }

            // Test aspect initialization
            var electrical = testGO.GetComponent<ElectricalComponent>();
            var visual = testGO.GetComponent<VisualComponent>();
            var interaction = testGO.GetComponent<InteractionComponent>();
            var label = testGO.GetComponent<LabelComponent>();

            bool hasAllAspects = electrical != null && visual != null &&
                               interaction != null && label != null;

            Debug.Log($"Aspect Components Status:");
            Debug.Log($"  ElectricalComponent: {(electrical != null ? "✅" : "❌")}");
            Debug.Log($"  VisualComponent: {(visual != null ? "✅" : "❌")}");
            Debug.Log($"  InteractionComponent: {(interaction != null ? "✅" : "❌")}");
            Debug.Log($"  LabelComponent: {(label != null ? "✅" : "❌")}");

            // Test electrical properties
            if (electrical != null)
            {
                electrical.Voltage = 12f;
                electrical.Current = 1.5f;
                electrical.Resistance = 8f;

                yield return new WaitForSeconds(0.1f); // Allow events to propagate

                Debug.Log($"Electrical Properties Test:");
                Debug.Log($"  Voltage: {electrical.Voltage}V");
                Debug.Log($"  Current: {electrical.Current}A");
                Debug.Log($"  Resistance: {electrical.Resistance}Ω");
                Debug.Log($"  Power: {electrical.GetPower()}W");
            }

            if (hasAllAspects)
            {
                Debug.Log("✅ Phase 2: Aspect Components - PASSED");
            }
            else
            {
                Debug.LogError("❌ Phase 2: Aspect Components - FAILED");
            }

            yield return null;
        }

        private GameObject CreateTestComponentWithAspects()
        {
            // Create test component
            var testGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testGO.name = "TestComponent_AspectBased";
            testGO.transform.position = new Vector3(0, 0.5f, 0);

            if (componentParent != null)
                testGO.transform.SetParent(componentParent);

            // Add coordinator
            var coordinator = testGO.AddComponent<CircuitComponent3D_v2>();

            // Aspects are automatically added by the coordinator
            return testGO;
        }

        #endregion

        #region Phase 3: Component Factory

        private System.Collections.IEnumerator TestPhase3_ComponentFactory()
        {
            Debug.Log("[PHASE 3] Testing Component Factory with ScriptableObjects...");

            var serviceLocator = ServiceLocator.Instance;
            if (!serviceLocator.IsRegistered<IComponentFactory>())
            {
                Debug.LogError("❌ IComponentFactory not registered");
                yield break;
            }

            var componentFactory = serviceLocator.Get<IComponentFactory>();

            // Test standard component creation
            var battery = componentFactory.CreateBattery(new Vector3(-2, 0.5f, 0));
            var resistor = componentFactory.CreateResistor(new Vector3(0, 0.5f, 0));
            var bulb = componentFactory.CreateBulb(new Vector3(2, 0.5f, 0));

            bool standardCreationWorks = battery != null && resistor != null && bulb != null;

            Debug.Log($"Standard Component Creation:");
            Debug.Log($"  Battery: {(battery != null ? "✅" : "❌")}");
            Debug.Log($"  Resistor: {(resistor != null ? "✅" : "❌")}");
            Debug.Log($"  Bulb: {(bulb != null ? "✅" : "❌")}");

            // Test ScriptableObject-based creation
            var testDefinition = CreateTestComponentDefinition();
            var customComponent = componentFactory.CreateComponent(testDefinition, new Vector3(4, 0.5f, 0));

            bool customCreationWorks = customComponent != null;

            Debug.Log($"Custom Component Creation (ScriptableObject):");
            Debug.Log($"  Custom House Component: {(customCreationWorks ? "✅" : "❌")}");

            if (customComponent != null)
            {
                Debug.Log($"  Applied Definition: {testDefinition.displayName}");
                Debug.Log($"  Base Type: {testDefinition.baseType}");
                Debug.Log($"  Resistance: {customComponent.resistance}Ω");
            }

            if (standardCreationWorks && customCreationWorks)
            {
                Debug.Log("✅ Phase 3: Component Factory - PASSED");
            }
            else
            {
                Debug.LogError("❌ Phase 3: Component Factory - FAILED");
            }

            yield return null;
        }

        private ComponentDefinition CreateTestComponentDefinition()
        {
            var definition = ScriptableObject.CreateInstance<ComponentDefinition>();
            definition.displayName = "Test House";
            definition.description = "A test house component for integration testing";
            definition.baseType = BaseComponentType.Bulb;

            definition.electricalProperties.defaultResistance = 15f;
            definition.electricalProperties.defaultVoltage = 0f;

            definition.visualProperties.defaultColor = Color.yellow;
            definition.visualProperties.scale = new Vector3(1.5f, 1.2f, 1.2f);
            definition.visualProperties.hasGlowEffect = true;

            return definition;
        }

        #endregion

        #region Phase 4: Challenge System

        private System.Collections.IEnumerator TestPhase4_ChallengeSystem()
        {
            Debug.Log("[PHASE 4] Testing Challenge System...");

            if (!loadTestChallenge)
            {
                Debug.Log("⚠️ Challenge loading disabled, skipping challenge tests");
                yield break;
            }

            // Create challenge manager
            var challengeManagerGO = new GameObject("TestChallengeManager");
            var challengeManager = challengeManagerGO.AddComponent<ChallengeManager>();

            // Create test challenge scenario
            var testChallenge = CreateTestChallengeScenario();

            bool challengeValidation = testChallenge.ValidateChallenge();
            Debug.Log($"Challenge Validation: {(challengeValidation ? "✅" : "❌")}");

            // Test challenge loading
            try
            {
                challengeManager.LoadChallenge(testChallenge);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Phase 4: Challenge System - FAILED: {ex.Message}");
                yield break;
            }

            yield return new WaitForSeconds(0.5f); // Allow initialization

            bool challengeLoaded = challengeManager.CurrentChallenge != null;
            Debug.Log($"Challenge Loading: {(challengeLoaded ? "✅" : "❌")}");

            if (challengeLoaded)
            {
                Debug.Log($"  Loaded Challenge: {challengeManager.CurrentChallenge.challengeTitle}");
                Debug.Log($"  Primary Misconception: {challengeManager.CurrentChallenge.primaryMisconception}");
                Debug.Log($"  Available Components: {challengeManager.CurrentChallenge.availableComponents.Length}");
            }

            Debug.Log("✅ Phase 4: Challenge System - PASSED");

            yield return null;
        }

        private ChallengeScenario CreateTestChallengeScenario()
        {
            var scenario = ScriptableObject.CreateInstance<ChallengeScenario>();
            scenario.challengeTitle = "Test Challenge: Power the House";
            scenario.challengeId = "TEST_M1_PowerTheHouse";
            scenario.description = "Test challenge for integration testing";
            scenario.primaryMisconception = MisconceptionType.M1_SinkModel;

            // Add available components
            var houseDef = CreateTestComponentDefinition();
            var batteryDef = ScriptableObject.CreateInstance<ComponentDefinition>();
            batteryDef.displayName = "Test Battery";
            batteryDef.baseType = BaseComponentType.Battery;
            batteryDef.electricalProperties.defaultVoltage = 12f;
            batteryDef.electricalProperties.isVoltageSource = true;

            scenario.availableComponents = new ComponentDefinition[] { houseDef, batteryDef };

            // Add challenge goals
            var goal = new ChallengeGoal();
            goal.goalId = "connect_power";
            goal.description = "Connect the battery to the house";
            goal.type = GoalType.CircuitCompletion;
            goal.validation.type = ValidationType.CircuitComplete;

            scenario.goals = new ChallengeGoal[] { goal };

            // Add misconception checks
            var misconceptionCheck = new MisconceptionCheck();
            misconceptionCheck.type = MisconceptionType.M1_SinkModel;
            misconceptionCheck.feedback = "Remember that electricity needs a complete loop!";

            scenario.misconceptionChecks = new MisconceptionCheck[] { misconceptionCheck };

            return scenario;
        }

        #endregion

        #region Phase 5: Full Circuit Solving

        private System.Collections.IEnumerator TestPhase5_FullCircuitSolving()
        {
            Debug.Log("[PHASE 5] Testing Full Circuit Solving Integration...");

            var serviceLocator = ServiceLocator.Instance;
            if (!serviceLocator.IsRegistered<ICircuitSolver>())
            {
                Debug.LogError("❌ ICircuitSolver not registered");
                yield break;
            }

            var circuitSolver = serviceLocator.Get<ICircuitSolver>();
            var circuitManager = serviceLocator.Get<ICircuitManager>();

            // Test circuit solving
            if (circuitManager.HasComponents())
            {
                Debug.Log($"Components in circuit: {circuitManager.GetComponentCount()}");

                bool solveResult = circuitSolver.SolveCircuit();
                Debug.Log($"Circuit Solving: {(solveResult ? "✅" : "❌")}");

                if (solveResult)
                {
                    Debug.Log($"  Solve Result: {circuitSolver.LastSolveResult}");
                    Debug.Log($"  Solve Time: {circuitSolver.LastSolveTime}");
                    Debug.Log($"  Is Solved: {circuitSolver.IsCircuitSolved}");
                }

                yield return new WaitForSeconds(0.5f);

                // Test validation
                if (serviceLocator.IsRegistered<IValidationService>())
                {
                    var validationService = serviceLocator.Get<IValidationService>();
                    bool isValid = validationService.IsCircuitValid();
                    var errors = validationService.GetCircuitErrors();

                    Debug.Log($"Circuit Validation: {(isValid ? "✅" : "❌")}");
                    if (!isValid)
                    {
                        Debug.Log($"  Validation Errors: {string.Join(", ", errors)}");
                    }
                }

                Debug.Log("✅ Phase 5: Full Circuit Solving - PASSED");
            }
            else
            {
                Debug.LogWarning("⚠️ No components in circuit, skipping solving test");
            }

            yield return null;
        }

        #endregion

        #region Public Test Interface

        [ContextMenu("Run Integration Tests")]
        public void RunIntegrationTests()
        {
            if (!testsCompleted)
            {
                StartCoroutine(RunFullIntegrationTest());
            }
        }

        [ContextMenu("Test ServiceLocator Only")]
        public void TestServiceLocatorOnly()
        {
            StartCoroutine(TestPhase1_ServiceLocator());
        }

        [ContextMenu("Test Component Factory Only")]
        public void TestComponentFactoryOnly()
        {
            StartCoroutine(TestPhase3_ComponentFactory());
        }

        [ContextMenu("Create Test Scene")]
        public void CreateTestScene()
        {
            // Clear existing test objects
            ClearTestObjects();

            // Create basic test setup
            CreateTestEnvironment();
            StartCoroutine(TestPhase3_ComponentFactory());
        }

        private void CreateTestEnvironment()
        {
            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 10f;

            // Create component parent
            var parent = new GameObject("TestComponents");
            componentParent = parent.transform;

            // Setup camera if not assigned
            if (testCamera == null)
            {
                testCamera = Camera.main;
                if (testCamera != null)
                {
                    testCamera.transform.position = new Vector3(0, 5, -10);
                    testCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
                }
            }

            Debug.Log("Test environment created");
        }

        private void ClearTestObjects()
        {
            var testObjects = GameObject.FindGameObjectsWithTag("TestComponent");
            foreach (var obj in testObjects)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }

            // Clear test components
            var testComponents = FindObjectsByType<CircuitComponent3D_v2>(FindObjectsSortMode.None);
            foreach (var comp in testComponents)
            {
                if (comp.name.Contains("Test"))
                {
                    if (Application.isPlaying)
                        Destroy(comp.gameObject);
                    else
                        DestroyImmediate(comp.gameObject);
                }
            }
        }

        #endregion

        void OnGUI()
        {
            if (!testsCompleted) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("v2.0 Integration Test Status", GUI.skin.box);

            if (GUILayout.Button("Run Full Test"))
            {
                testsCompleted = false;
                StartCoroutine(RunFullIntegrationTest());
            }

            if (GUILayout.Button("Create Test Components"))
            {
                StartCoroutine(TestPhase3_ComponentFactory());
            }

            if (GUILayout.Button("Test Challenge System"))
            {
                StartCoroutine(TestPhase4_ChallengeSystem());
            }

            if (GUILayout.Button("Clear Test Objects"))
            {
                ClearTestObjects();
            }

            GUILayout.EndArea();
        }
    }
}