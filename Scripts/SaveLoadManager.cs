using System.IO;
using UnityEngine;

/// <summary>
/// Manages saving and loading circuits to/from disk.
/// Provides file I/O operations and directory management.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    private CircuitManager circuitManager;
    private CircuitLoader loader;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardShortcuts = true;

    void Start()
    {
        circuitManager = CircuitManager.Instance;
        loader = GetComponent<CircuitLoader>();

        // Add CircuitLoader component if not present
        if (loader == null)
        {
            loader = gameObject.AddComponent<CircuitLoader>();
            Debug.Log("Added CircuitLoader component to SaveLoadManager");
        }

        if (circuitManager == null)
        {
            Debug.LogError("❌ SaveLoadManager: CircuitManager not found!");
        }

        Debug.Log($"SaveLoadManager initialized. Save directory: {GetSaveDirectory()}");
    }

    void Update()
    {
        if (!enableKeyboardShortcuts)
            return;

        // F5: Quick save (common game shortcut)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveCircuit("quicksave");
            Debug.Log("⚡ Quick saved (F5)");
        }

        // F6: Quick load (common game shortcut)
        if (Input.GetKeyDown(KeyCode.F6))
        {
            LoadCircuit("quicksave");
            Debug.Log("⚡ Quick loaded (F6)");
        }
    }

    /// <summary>
    /// Saves the current circuit to a file.
    /// </summary>
    /// <param name="filename">Name of the save file (extension optional)</param>
    public void SaveCircuit(string filename)
    {
        if (circuitManager == null)
        {
            Debug.LogError("❌ Cannot save: CircuitManager is null");
            return;
        }

        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogError("❌ Cannot save: filename is empty");
            return;
        }

        try
        {
            // Get current circuit state
            var components = circuitManager.Components;
            var wires = circuitManager.Wires;

            if (components.Count == 0)
            {
                Debug.LogWarning("⚠️ Circuit is empty, nothing to save");
                return;
            }

            // Serialize to JSON
            string json = CircuitSerializer.SerializeCircuit(components, wires);

            // Write to file
            string filepath = GetFilePath(filename);
            File.WriteAllText(filepath, json);

            Debug.Log($"✅ Circuit saved: {filepath}");
            Debug.Log($"   File size: {json.Length} bytes");
            Debug.Log($"   Components: {components.Count}, Wires: {wires.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to save circuit: {e.Message}");
        }
    }

    /// <summary>
    /// Loads a circuit from a file.
    /// </summary>
    /// <param name="filename">Name of the file to load (extension optional)</param>
    public void LoadCircuit(string filename)
    {
        if (loader == null)
        {
            Debug.LogError("❌ Cannot load: CircuitLoader is null");
            return;
        }

        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogError("❌ Cannot load: filename is empty");
            return;
        }

        try
        {
            string filepath = GetFilePath(filename);

            if (!File.Exists(filepath))
            {
                Debug.LogError($"❌ File not found: {filepath}");
                return;
            }

            // Read JSON from file
            string json = File.ReadAllText(filepath);

            // Deserialize
            var saveData = CircuitSerializer.DeserializeCircuit(json);

            if (saveData == null)
            {
                Debug.LogError("❌ Failed to deserialize circuit data");
                return;
            }

            // Load into scene
            loader.LoadCircuit(saveData);

            Debug.Log($"✅ Circuit loaded from: {filepath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to load circuit: {e.Message}");
        }
    }

    /// <summary>
    /// Gets a list of all saved circuit files.
    /// </summary>
    /// <returns>Array of circuit filenames (without extension)</returns>
    public string[] GetSavedCircuits()
    {
        string dir = GetSaveDirectory();

        if (!Directory.Exists(dir))
        {
            Debug.LogWarning($"⚠️ Save directory does not exist: {dir}");
            return new string[0];
        }

        try
        {
            var files = Directory.GetFiles(dir, "*.circuit");

            // Remove extension from filenames
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            Debug.Log($"Found {files.Length} saved circuits");
            return files;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to list saved circuits: {e.Message}");
            return new string[0];
        }
    }

    /// <summary>
    /// Gets the full file path for a circuit save file.
    /// Ensures .circuit extension and creates directory if needed.
    /// </summary>
    private string GetFilePath(string filename)
    {
        // Ensure .circuit extension
        if (!filename.EndsWith(".circuit"))
        {
            filename += ".circuit";
        }

        string dir = GetSaveDirectory();

        // Create directory if it doesn't exist
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            Debug.Log($"Created save directory: {dir}");
        }

        return Path.Combine(dir, filename);
    }

    /// <summary>
    /// Gets the directory where circuit saves are stored.
    /// Uses Application.persistentDataPath for cross-platform compatibility.
    /// </summary>
    private string GetSaveDirectory()
    {
        // Cross-platform save location:
        // Windows: C:\Users\[User]\AppData\LocalLow\[Company]\CircuitSimulator\CircuitSaves
        // Mac: ~/Library/Application Support/[Company]/CircuitSimulator/CircuitSaves
        // WebGL: PlayerPrefs (not implemented yet)
        return Path.Combine(Application.persistentDataPath, "CircuitSaves");
    }

    /// <summary>
    /// Debug helper: Prints the save directory path to console.
    /// Use via right-click on component in Inspector → Show Save Directory
    /// </summary>
    [ContextMenu("Show Save Directory")]
    public void ShowSaveDirectory()
    {
        string dir = GetSaveDirectory();
        Debug.Log($"📁 Save directory: {dir}");

        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, "*.circuit");
            Debug.Log($"   Found {files.Length} saved circuits:");
            foreach (var file in files)
            {
                Debug.Log($"   - {Path.GetFileName(file)}");
            }
        }
        else
        {
            Debug.Log("   Directory does not exist yet (will be created on first save)");
        }
    }

    /// <summary>
    /// Debug helper: Saves the current circuit with a test filename.
    /// Use via right-click on component in Inspector → Save Test Circuit
    /// </summary>
    [ContextMenu("Save Test Circuit")]
    public void SaveTestCircuit()
    {
        SaveCircuit("test");
    }

    /// <summary>
    /// Debug helper: Loads the test circuit.
    /// Use via right-click on component in Inspector → Load Test Circuit
    /// </summary>
    [ContextMenu("Load Test Circuit")]
    public void LoadTestCircuit()
    {
        LoadCircuit("test");
    }
}
