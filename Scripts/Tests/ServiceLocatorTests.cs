using UnityEngine;
using CircuitSimulator.Services;

namespace CircuitSimulator.Tests
{
    /// <summary>
    /// Runtime tests for the new ServiceLocator architecture.
    /// Add this to a GameObject in your test scene to verify the implementation.
    /// </summary>
    public class ServiceLocatorTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;

        void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== ServiceLocator Architecture Tests ===");

            TestServiceLocatorSingleton();
            TestServiceRegistration();
            TestServiceRetrieval();
            TestCircuitManagerInterface();
            TestServiceNotFound();

            Debug.Log("=== All Tests Completed ===");
        }

        void TestServiceLocatorSingleton()
        {
            Debug.Log("[TEST] ServiceLocator Singleton...");

            var instance1 = ServiceLocator.Instance;
            var instance2 = ServiceLocator.Instance;

            if (instance1 == instance2 && instance1 != null)
            {
                Debug.Log("✅ ServiceLocator singleton working correctly");
            }
            else
            {
                Debug.LogError("❌ ServiceLocator singleton failed");
            }
        }

        void TestServiceRegistration()
        {
            Debug.Log("[TEST] Service Registration...");

            var serviceLocator = ServiceLocator.Instance;

            // Test registering a service
            var testService = new TestService();
            serviceLocator.Register<ITestService>(testService);

            if (serviceLocator.IsRegistered<ITestService>())
            {
                Debug.Log("✅ Service registration working correctly");
            }
            else
            {
                Debug.LogError("❌ Service registration failed");
            }
        }

        void TestServiceRetrieval()
        {
            Debug.Log("[TEST] Service Retrieval...");

            var serviceLocator = ServiceLocator.Instance;

            try
            {
                var retrievedService = serviceLocator.Get<ITestService>();
                if (retrievedService != null)
                {
                    Debug.Log("✅ Service retrieval working correctly");
                }
                else
                {
                    Debug.LogError("❌ Service retrieval returned null");
                }
            }
            catch (ServiceNotFoundException)
            {
                Debug.LogError("❌ Service retrieval failed with exception");
            }
        }

        void TestCircuitManagerInterface()
        {
            Debug.Log("[TEST] CircuitManager Interface...");

            var serviceLocator = ServiceLocator.Instance;

            try
            {
                var circuitManager = serviceLocator.Get<ICircuitManager>();
                if (circuitManager != null)
                {
                    // Test basic interface methods
                    var components = circuitManager.GetAllComponents();
                    var wires = circuitManager.GetAllWires();
                    var hasComponents = circuitManager.HasComponents();

                    Debug.Log($"✅ ICircuitManager working: Components={components.Count}, Wires={wires.Count}, HasComponents={hasComponents}");
                }
                else
                {
                    Debug.LogError("❌ ICircuitManager retrieval returned null");
                }
            }
            catch (ServiceNotFoundException e)
            {
                Debug.LogWarning($"⚠️ ICircuitManager not registered yet: {e.Message}");
            }
        }

        void TestServiceNotFound()
        {
            Debug.Log("[TEST] Service Not Found Exception...");

            var serviceLocator = ServiceLocator.Instance;

            try
            {
                var nonExistentService = serviceLocator.Get<INonExistentService>();
                Debug.LogError("❌ Should have thrown ServiceNotFoundException");
            }
            catch (ServiceNotFoundException)
            {
                Debug.Log("✅ ServiceNotFoundException working correctly");
            }
        }

        [ContextMenu("Test Challenge System")]
        public void TestChallengeSystem()
        {
            Debug.Log("=== Challenge System Tests ===");

            TestChallengeManagerCreation();
            TestComponentDefinitionLoading();
            TestChallengeScenarioLoading();

            Debug.Log("=== Challenge Tests Completed ===");
        }

        void TestChallengeManagerCreation()
        {
            Debug.Log("[TEST] ChallengeManager Creation...");

            var challengeManagerGO = new GameObject("Test ChallengeManager");
            var challengeManager = challengeManagerGO.AddComponent<CircuitSimulator.Challenges.ChallengeManager>();

            if (challengeManager != null)
            {
                Debug.Log("✅ ChallengeManager created successfully");

                // Clean up
                if (Application.isPlaying)
                    Destroy(challengeManagerGO);
                else
                    DestroyImmediate(challengeManagerGO);
            }
            else
            {
                Debug.LogError("❌ ChallengeManager creation failed");
            }
        }

        void TestComponentDefinitionLoading()
        {
            Debug.Log("[TEST] ComponentDefinition Loading...");

            // Try to load example component definitions
            var houseDef = Resources.Load<ComponentDefinition>("ComponentDef_House");
            var batteryDef = Resources.Load<ComponentDefinition>("ComponentDef_CarBattery");

            if (houseDef != null && batteryDef != null)
            {
                Debug.Log($"✅ ComponentDefinitions loaded: House='{houseDef.displayName}', Battery='{batteryDef.displayName}'");
            }
            else
            {
                Debug.LogWarning("⚠️ ComponentDefinitions not found in Resources (expected if not created as assets yet)");
            }
        }

        void TestChallengeScenarioLoading()
        {
            Debug.Log("[TEST] ChallengeScenario Loading...");

            var challengeScenario = Resources.Load<ChallengeScenario>("Challenge_M1_PowerTheHouse");

            if (challengeScenario != null)
            {
                Debug.Log($"✅ ChallengeScenario loaded: '{challengeScenario.challengeTitle}'");

                if (challengeScenario.ValidateChallenge())
                {
                    Debug.Log("✅ Challenge validation passed");
                }
                else
                {
                    Debug.LogWarning("⚠️ Challenge validation failed");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ ChallengeScenario not found in Resources (expected if not created as asset yet)");
            }
        }

        [ContextMenu("Log Service Status")]
        public void LogServiceStatus()
        {
            var serviceLocator = ServiceLocator.Instance;
            serviceLocator.LogAllServices();
        }
    }

    // Test interfaces and implementations
    public interface ITestService
    {
        string GetTestMessage();
    }

    public class TestService : ITestService
    {
        public string GetTestMessage()
        {
            return "Test service working!";
        }
    }

    public interface INonExistentService
    {
        void DoNothing();
    }
}