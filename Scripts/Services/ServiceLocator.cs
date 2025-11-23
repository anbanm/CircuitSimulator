using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircuitSimulator.Services
{
    /// <summary>
    /// Centralized dependency injection container that replaces all singleton patterns.
    /// Provides O(1) service lookups and proper lifecycle management.
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ServiceLocator>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ServiceLocator");
                        _instance = go.AddComponent<ServiceLocator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private Dictionary<Type, object> services = new Dictionary<Type, object>();
        private readonly object lockObject = new object();

        [Header("Service Configuration")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool validateOnRegister = true;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Initialize()
        {
            if (enableDebugLogging)

            // Register core services - will be called by individual managers
            RegisterCoreServices();
        }

        /// <summary>
        /// Register a service instance with the container
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Cannot register null service of type {typeof(T).Name}");
                return;
            }

            var serviceType = typeof(T);

            lock (lockObject)
            {
                if (services.ContainsKey(serviceType))
                {
                    if (enableDebugLogging)
                        Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} already registered, replacing");
                }

                services[serviceType] = service;
            }

            if (enableDebugLogging)

            if (validateOnRegister)
                ValidateService(service);
        }

        /// <summary>
        /// Get a service from the container
        /// </summary>
        public T Get<T>() where T : class
        {
            var serviceType = typeof(T);

            lock (lockObject)
            {
                if (services.TryGetValue(serviceType, out var service))
                {
                    // Validate service is still valid
                    if (service is MonoBehaviour mb && mb == null)
                    {
                        services.Remove(serviceType);
                        Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} was destroyed, removing from registry");
                    }
                    else
                    {
                        return service as T;
                    }
                }
            }

            // Try to find and auto-register if it's a MonoBehaviour
            if (typeof(MonoBehaviour).IsAssignableFrom(serviceType))
            {
                var foundService = FindFirstObjectByType(serviceType) as T;
                if (foundService != null)
                {
                    Register(foundService);
                    if (enableDebugLogging)
                    return foundService;
                }
            }

            throw new ServiceNotFoundException($"Service {serviceType.Name} not registered. Available services: {string.Join(", ", GetRegisteredServiceNames())}");
        }

        /// <summary>
        /// Try to get a service without throwing exception
        /// </summary>
        public T TryGet<T>() where T : class
        {
            try
            {
                return Get<T>();
            }
            catch (ServiceNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsRegistered<T>() where T : class
        {
            lock (lockObject)
            {
                return services.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void Unregister<T>() where T : class
        {
            var serviceType = typeof(T);
            lock (lockObject)
            {
                services.Remove(serviceType);
            }
        }

        /// <summary>
        /// Get all registered service names for debugging
        /// </summary>
        public string[] GetRegisteredServiceNames()
        {
            lock (lockObject)
            {
                var names = new string[services.Count];
                int i = 0;
                foreach (var kvp in services)
                {
                    names[i++] = kvp.Key.Name;
                }
                return names;
            }
        }

        /// <summary>
        /// Register core services automatically
        /// </summary>
        private void RegisterCoreServices()
        {
            // Services will self-register when they initialize
            // This method exists for any manual registrations needed
        }

        /// <summary>
        /// Validate service on registration
        /// </summary>
        private void ValidateService<T>(T service) where T : class
        {
            if (service is MonoBehaviour mb && mb == null)
            {
                Debug.LogError($"[ServiceLocator] Registered MonoBehaviour service {typeof(T).Name} is destroyed!");
            }
        }

        /// <summary>
        /// Clean shutdown - dispose all services
        /// </summary>
        void OnDestroy()
        {
            if (_instance == this)
            {
                if (enableDebugLogging)

                foreach (var service in services.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                lock (lockObject)
                {
                    services.Clear();
                }
                _instance = null;
            }
        }

        /// <summary>
        /// Debug method to log all registered services
        /// </summary>
        [ContextMenu("Log All Services")]
        public void LogAllServices()
        {
            lock (lockObject)
            {
                foreach (var kvp in services)
                {
                }
            }
        }
    }

    /// <summary>
    /// Exception thrown when a requested service is not found
    /// </summary>
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(string message) : base(message) { }
    }
}