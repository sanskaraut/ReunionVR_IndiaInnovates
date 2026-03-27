// =============================================================================
// ReunionVR — Unity WebSocket Client  
// =============================================================================
// FEATURES:
//   - Multiple models load side by side (not overwritten)
//   - Models are saved to disk and auto-reload on game restart
//   - Position offset so models don't stack on top of each other
//
// SETUP:
//   1. Install NativeWebSocket: https://github.com/endel/NativeWebSocket.git#upm
//   2. Edit > Project Settings > Player > Other Settings >
//      "Allow downloads over HTTP" = "Always allowed"
//   3. Attach this script to an empty GameObject
//   4. Set serverIP to the therapist laptop's IP
// =============================================================================

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NativeWebSocket;

public class VRWebSocketClient : MonoBehaviour
{
    [Serializable] private class InMsg
    {
        public string type = "";
        public string url = "";
        public string name = "";
        public float intensity;
        public float sound;
        public string data = "";
    }

    [Serializable] private class OutMsg
    {
        public string type = "";
        public string name = "";
        public string data = "";
    }

    // Manifest to track saved models for persistence
    [Serializable] private class ModelEntry
    {
        public string name;
        public string localPath;
        public float posX, posY, posZ;
    }

    [Serializable] private class ModelManifest
    {
        public List<ModelEntry> models = new List<ModelEntry>();
    }

    [Header("Connection")]
    public string serverIP = "172.22.96.202";
    public int serverPort = 3001;

    [Header("Scene")]
    public Transform modelContainer;
    public AudioSource environmentAudio;
    public Light environmentLight;

    [Header("Model Placement")]
    [Tooltip("How far apart to space multiple models")]
    public float modelSpacing = 3.0f;

    [Header("Status")]
    [SerializeField] private bool isConnected;
    [SerializeField] private string lastCommand = "None";
    [SerializeField] private int loadedModelCount = 0;

    private WebSocket ws;
    private bool shouldReconnect = true;
    private ModelManifest manifest = new ModelManifest();
    private string manifestPath;
    private string modelSavePath;

    public static event Action<string, string> OnModelLoaded;
    public static event Action<float, float> OnEnvironmentChanged;
    public static event Action OnSessionStarted;
    public static event Action OnSessionStopped;

    void Start()
    {
        if (modelContainer == null)
        {
            modelContainer = new GameObject("__Models").transform;
            modelContainer.position = Vector3.zero;
        }

        // Setup persistent save paths
        modelSavePath = Path.Combine(Application.persistentDataPath, "VRModels");
        manifestPath = Path.Combine(Application.persistentDataPath, "VRModels", "manifest.json");

        if (!Directory.Exists(modelSavePath))
            Directory.CreateDirectory(modelSavePath);

        // Reload previously saved models
        LoadSavedModels();

        // Connect to server
        ConnectToServer();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null) ws.DispatchMessageQueue();
#endif
    }

    // ===================== CONNECTION =====================

    async void ConnectToServer()
    {
        string url = "ws://" + serverIP + ":" + serverPort + "/ws";
        Debug.Log("[ReunionVR] Connecting to " + url);

        ws = new WebSocket(url);

        ws.OnOpen += () =>
        {
            Debug.Log("[ReunionVR] Connected!");
            isConnected = true;
            var reg = new OutMsg();
            reg.type = "REGISTER_HMD";
            reg.name = SystemInfo.deviceName;
            SendMsg(reg);
        };

        ws.OnMessage += (bytes) =>
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            ProcessMessage(json);
        };

        ws.OnClose += (code) =>
        {
            Debug.Log("[ReunionVR] Disconnected");
            isConnected = false;
            if (shouldReconnect) StartCoroutine(Reconnect());
        };

        ws.OnError += (e) => Debug.LogError("[ReunionVR] Error: " + e);

        await ws.Connect();
    }

    IEnumerator Reconnect()
    {
        yield return new WaitForSeconds(3f);
        ConnectToServer();
    }

    // ===================== MESSAGES =====================

    void ProcessMessage(string json)
    {
        InMsg msg;
        try { msg = JsonUtility.FromJson<InMsg>(json); }
        catch { return; }

        if (msg == null || string.IsNullOrEmpty(msg.type)) return;
        lastCommand = msg.type;

        if (msg.type == "LOAD_MODEL")
        {
            Debug.Log("[ReunionVR] LOAD_MODEL: " + msg.name + " -> " + msg.url);
            StartCoroutine(DownloadAndLoadGLB(msg.url, msg.name));
        }
        else if (msg.type == "ENV_CONTROL")
        {
            SetEnvironment(msg.intensity, msg.sound);
        }
        else if (msg.type == "SESSION_START")
        {
            Debug.Log("[ReunionVR] Session STARTED");
            if (OnSessionStarted != null) OnSessionStarted();
            var ack = new OutMsg(); ack.type = "SESSION_ACK"; ack.data = "started";
            SendMsg(ack);
        }
        else if (msg.type == "SESSION_STOP")
        {
            Debug.Log("[ReunionVR] Session STOPPED - clearing all models");
            ClearAllModels(true); // clears and deletes saved data
            ResetEnvironment();
            if (OnSessionStopped != null) OnSessionStopped();
            var ack = new OutMsg(); ack.type = "SESSION_ACK"; ack.data = "stopped";
            SendMsg(ack);
        }
        else if (msg.type == "SESSION_PAUSE")
        {
            var ack = new OutMsg(); ack.type = "SESSION_ACK"; ack.data = "paused";
            SendMsg(ack);
        }
        else if (msg.type == "BIOMETRIC_DATA" || msg.type == "connection" ||
                 msg.type == "MODEL_SENT" || msg.type == "CLIENT_DISCONNECTED" ||
                 msg.type == "HMD_CONNECTED")
        {
            // ignore on HMD
        }
        else
        {
            Debug.Log("[ReunionVR] Unknown: " + msg.type);
        }
    }

    // ===================== GLB DOWNLOAD, SAVE & LOAD =====================

    IEnumerator DownloadAndLoadGLB(string url, string modelName)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("[ReunionVR] URL is empty!");
            yield break;
        }

        Debug.Log("[ReunionVR] Downloading " + url);

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[ReunionVR] Download FAILED: " + www.error);
            var err = new OutMsg(); err.type = "MODEL_ERROR"; err.data = www.error;
            SendMsg(err);
            www.Dispose();
            yield break;
        }

        byte[] glbData = www.downloadHandler.data;
        Debug.Log("[ReunionVR] Downloaded " + glbData.Length + " bytes");
        www.Dispose();

        // Save file permanently (not just temp)
        string safeName = SanitizeFilename(modelName);
        string localFile = Path.Combine(modelSavePath, safeName + ".glb");

        // If same name exists, add a number suffix
        int counter = 1;
        while (File.Exists(localFile))
        {
            localFile = Path.Combine(modelSavePath, safeName + "_" + counter + ".glb");
            counter++;
        }

        File.WriteAllBytes(localFile, glbData);
        Debug.Log("[ReunionVR] Saved permanently: " + localFile);

        // Calculate position — spread models out so they don't overlap
        float posX = loadedModelCount * modelSpacing;
        Vector3 position = new Vector3(posX, 0, 3);

        // Spawn the model (or placeholder)
        SpawnModel(localFile, modelName, position);

        // Save to manifest for persistence
        ModelEntry entry = new ModelEntry();
        entry.name = modelName;
        entry.localPath = localFile;
        entry.posX = position.x;
        entry.posY = position.y;
        entry.posZ = position.z;
        manifest.models.Add(entry);
        SaveManifest();

        loadedModelCount++;

        if (OnModelLoaded != null) OnModelLoaded(modelName, url);
        var ok = new OutMsg(); ok.type = "MODEL_LOADED"; ok.data = modelName;
        SendMsg(ok);
        Debug.Log("[ReunionVR] Model '" + modelName + "' added! Total: " + loadedModelCount);
    }

    // Spawn a model or placeholder at a given position
    void SpawnModel(string localPath, string displayName, Vector3 position)
    {
        // TODO: Replace with GLTFast loading when installed:
        // var gltf = new GLTFast.GltfImport();
        // byte[] data = File.ReadAllBytes(localPath);
        // yield return gltf.LoadGltfBinary(data);
        // gltf.InstantiateMainSceneAsync(go.transform);

        // Placeholder cube — proves the model was received and saved
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = string.IsNullOrEmpty(displayName) ? "Model" : displayName;
        cube.transform.SetParent(modelContainer);
        cube.transform.localPosition = position;
        cube.transform.localScale = Vector3.one;

        // Different colors for different models
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            float hue = (loadedModelCount * 0.15f) % 1f;
            rend.material.color = Color.HSVToRGB(hue, 0.7f, 0.9f);
        }

        Debug.Log("[ReunionVR] Spawned '" + displayName + "' at " + position);
    }

    // ===================== PERSISTENCE =====================

    void SaveManifest()
    {
        try
        {
            string json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(manifestPath, json);
            Debug.Log("[ReunionVR] Manifest saved: " + manifest.models.Count + " models");
        }
        catch (Exception e)
        {
            Debug.LogError("[ReunionVR] Failed to save manifest: " + e.Message);
        }
    }

    void LoadSavedModels()
    {
        if (!File.Exists(manifestPath))
        {
            Debug.Log("[ReunionVR] No saved models found. Starting fresh.");
            return;
        }

        try
        {
            string json = File.ReadAllText(manifestPath);
            manifest = JsonUtility.FromJson<ModelManifest>(json);

            if (manifest == null || manifest.models == null || manifest.models.Count == 0)
            {
                manifest = new ModelManifest();
                Debug.Log("[ReunionVR] Manifest empty. Starting fresh.");
                return;
            }

            Debug.Log("[ReunionVR] Restoring " + manifest.models.Count + " saved models...");

            // Reload each saved model
            List<ModelEntry> validModels = new List<ModelEntry>();

            foreach (ModelEntry entry in manifest.models)
            {
                if (File.Exists(entry.localPath))
                {
                    Vector3 pos = new Vector3(entry.posX, entry.posY, entry.posZ);
                    SpawnModel(entry.localPath, entry.name, pos);
                    validModels.Add(entry);
                    loadedModelCount++;
                    Debug.Log("[ReunionVR] Restored: " + entry.name);
                }
                else
                {
                    Debug.LogWarning("[ReunionVR] File missing, skipping: " + entry.localPath);
                }
            }

            // Update manifest to only include valid entries
            manifest.models = validModels;
            SaveManifest();

            Debug.Log("[ReunionVR] Restore complete! " + loadedModelCount + " models loaded.");
        }
        catch (Exception e)
        {
            Debug.LogError("[ReunionVR] Failed to load manifest: " + e.Message);
            manifest = new ModelManifest();
        }
    }

    // ===================== CLEAR / ENVIRONMENT =====================

    void ClearAllModels(bool deleteFiles)
    {
        // Remove from scene
        if (modelContainer != null)
        {
            foreach (Transform child in modelContainer)
                Destroy(child.gameObject);
        }

        if (deleteFiles)
        {
            // Delete saved .glb files
            foreach (ModelEntry entry in manifest.models)
            {
                if (File.Exists(entry.localPath))
                {
                    try { File.Delete(entry.localPath); }
                    catch (Exception e) { Debug.LogWarning("Could not delete: " + e.Message); }
                }
            }
            manifest.models.Clear();
            SaveManifest();
        }

        loadedModelCount = 0;
        Debug.Log("[ReunionVR] All models cleared" + (deleteFiles ? " (files deleted)" : ""));
    }

    void SetEnvironment(float intensity, float sound)
    {
        if (environmentLight != null)
            environmentLight.intensity = Mathf.Lerp(0f, 2f, intensity / 100f);
        if (environmentAudio != null)
            environmentAudio.volume = sound / 100f;
        if (OnEnvironmentChanged != null)
            OnEnvironmentChanged(intensity, sound);
    }

    void ResetEnvironment()
    {
        if (environmentLight != null) environmentLight.intensity = 1f;
        if (environmentAudio != null) environmentAudio.volume = 0.5f;
    }

    // ===================== HELPERS =====================

    string SanitizeFilename(string name)
    {
        if (string.IsNullOrEmpty(name)) return "model";
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            name = name.Replace(c.ToString(), "_");
        return name.Replace(" ", "_");
    }

    async void SendMsg(OutMsg msg)
    {
        if (ws == null || ws.State != WebSocketState.Open) return;
        await ws.SendText(JsonUtility.ToJson(msg));
    }

    async void OnApplicationQuit()
    {
        shouldReconnect = false;
        if (ws != null && ws.State == WebSocketState.Open) await ws.Close();
    }

    void OnDestroy()
    {
        shouldReconnect = false;
    }
}