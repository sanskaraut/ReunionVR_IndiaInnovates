using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Project.MRI_Spawning.Scripts
{
    public class ModelListUIManager : MonoBehaviour
    {
        // -------------------------------------------------
        // API CONFIG
        // -------------------------------------------------

        [Header("API Base (Default Fallback)")]
        [SerializeField] private string apiBase = "http://192.168.161.240:5500/api/unity";

        [Header("Remote Config URL (Optional)")]
        [SerializeField] private string apiConfigUrl = "http://192.168.161.240:5500/api/config";

        // -------------------------------------------------
        // UI
        // -------------------------------------------------

        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private Button fetchButton;
        [SerializeField] private RawImage previewImage;

        [Header("Card UI")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform contentParent;

        // -------------------------------------------------
        // SPAWN
        // -------------------------------------------------

        [Header("Spawn References")]
        [SerializeField] private GameObject referenceModelPrefab;
        [SerializeField] private Transform placeholderLocation;
        [SerializeField] private Transform spawnedModelParent;

        private readonly List<GameObject> cards = new List<GameObject>();

        // -------------------------------------------------
        // UNITY
        // -------------------------------------------------

        private void Awake()
        {
            fetchButton.interactable = false;
            fetchButton.onClick.AddListener(OnFetchClicked);

            StartCoroutine(FetchApiBaseFromServer());
        }

        // -------------------------------------------------
        // STEP 0: FETCH API BASE FROM REMOTE CONFIG
        // -------------------------------------------------

        private IEnumerator FetchApiBaseFromServer()
        {
            if (string.IsNullOrEmpty(apiConfigUrl))
            {
                fetchButton.interactable = true;
                yield break;
            }

            using UnityWebRequest req = UnityWebRequest.Get(apiConfigUrl);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Remote config failed. Using default API Base.");
                fetchButton.interactable = true;
                yield break;
            }

            string response = req.downloadHandler.text.Trim();

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogWarning("Empty config response. Using default API Base.");
                fetchButton.interactable = true;
                yield break;
            }

            try
            {
                // Deserialize your actual structure
                ApiConfig config = JsonConvert.DeserializeObject<ApiConfig>(response);

                if (config != null && !string.IsNullOrEmpty(config.value))
                {
                    apiBase = config.value;
                    Debug.Log("API Base Updated From JSON VALUE: " + apiBase);
                }
                else
                {
                    Debug.LogWarning("JSON parsed but value empty. Using default.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Config parse failed: " + e.Message);
            }

            fetchButton.interactable = true;
        }

        // -------------------------------------------------
        // STEP 1: FETCH HISTORY
        // -------------------------------------------------

        private void OnFetchClicked()
        {
            if (string.IsNullOrWhiteSpace(emailInput.text))
            {
                Debug.LogWarning("Email empty");
                return;
            }

            StartCoroutine(FetchHistory(emailInput.text.Trim()));
        }

        private IEnumerator FetchHistory(string email)
        {
            ClearCards();

            string url = $"{apiBase}/history?email={UnityWebRequest.EscapeURL(email)}";

            using UnityWebRequest req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                yield break;
            }

            List<HistoryItem> items;

            try
            {
                items = JsonConvert.DeserializeObject<List<HistoryItem>>(req.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                yield break;
            }

            foreach (var item in items)
                yield return CreateCard(item);
        }

        // -------------------------------------------------
        // STEP 2: CREATE CARD
        // -------------------------------------------------

        private IEnumerator CreateCard(HistoryItem item)
        {
            var card = Instantiate(cardPrefab, contentParent);
            card.transform.localScale = Vector3.one;

            var nameText = card.transform.Find("Name Text")?.GetComponent<TMP_Text>();
            var dateText = card.transform.Find("Date Text")?.GetComponent<TMP_Text>();
            var icon = card.transform.Find("File Icon")?.GetComponent<RawImage>();
            var spawnBtn = card.transform.Find("Spawn Button")?.GetComponent<Button>();

            if (nameText)
                nameText.text = item.report_name;

            if (dateText)
                dateText.text = DateTime.Parse(item.created_at).ToString("dd MMM yyyy HH:mm");

            if (icon && !string.IsNullOrEmpty(item.xray_url))
                yield return LoadImage(item.xray_url, icon);

            if (spawnBtn != null)
            {
                spawnBtn.onClick.AddListener(() =>
                {
                    StartCoroutine(OnSpawnClicked(item));
                });
            }

            cards.Add(card);
        }

        // -------------------------------------------------
        // STEP 3: SPAWN CLICK → FETCH REPORT
        // -------------------------------------------------

        private IEnumerator OnSpawnClicked(HistoryItem item)
        {
            if (!string.IsNullOrEmpty(item.xray_url))
                yield return LoadImage(item.xray_url, previewImage);

            string url = $"{apiBase}/report/{item.session_id}";

            using UnityWebRequest req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                yield break;
            }

            ReportResponse report;

            try
            {
                report = JsonConvert.DeserializeObject<ReportResponse>(req.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                yield break;
            }

            SpawnModel(report.model_url);
        }

        // -------------------------------------------------
        // LOAD IMAGE
        // -------------------------------------------------

        private IEnumerator LoadImage(string url, RawImage target)
        {
            using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                target.texture = DownloadHandlerTexture.GetContent(req);
            else
                Debug.LogWarning(req.error);
        }

        // -------------------------------------------------
        // SPAWN MODEL
        // -------------------------------------------------

        public void SpawnModel(string modelUrl)
        {
            if (string.IsNullOrEmpty(modelUrl))
            {
                Debug.LogError("Model URL empty");
                return;
            }

            var instance = Instantiate(
                referenceModelPrefab,
                placeholderLocation.position,
                placeholderLocation.rotation,
                spawnedModelParent
            );

            var script = instance.GetComponent<ThreeDeeModel>();

            if (script != null)
                StartCoroutine(script.Init(modelUrl));
            else
                Debug.LogError("ThreeDeeModel missing");
        }

        // -------------------------------------------------
        // CLEAR UI
        // -------------------------------------------------

        private void ClearCards()
        {
            foreach (var c in cards)
                Destroy(c);

            cards.Clear();
        }

        // -------------------------------------------------
        // DTO CLASSES
        // -------------------------------------------------

        [Serializable]
        public class ApiConfig
        {
            public string id;
            public string key;
            public string value;
        }

        [Serializable]
        public class HistoryItem
        {
            public string session_id;
            public string report_name;
            public string created_at;
            public string xray_url;
        }

        [Serializable]
        public class ReportResponse
        {
            public string report_name;
            public string created_at;
            public string summary;
            public string model_url;
        }
    }
}