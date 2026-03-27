using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ApiHandling
{
    [Serializable]
    public class BaseUrlContainer : ISerializationCallbackReceiver
    {
        [SerializeField] private int _activeIndex;
        [SerializeField] private string[] _urls;

        private string _activeUrl;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Refresh();
        }

        public bool StartsWith(string s)
        {
            if (string.IsNullOrEmpty(_activeUrl))
                Refresh();
            return _activeUrl.StartsWith(s);
        }

        public bool EndsWith(string s)
        {
            if (string.IsNullOrEmpty(_activeUrl))
                Refresh();
            return _activeUrl.EndsWith(s);
        }

        public void Refresh()
        {
            if (_activeIndex < 0 || _activeIndex >= _urls.Length) return;

            var url = _urls[_activeIndex];
            if (string.IsNullOrEmpty(url) == false)
                _activeUrl = url;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_activeUrl))
                Refresh();
            return _activeUrl;
        }

        public static implicit operator string(BaseUrlContainer obj)
        {
            if (string.IsNullOrEmpty(obj._activeUrl))
                obj.Refresh();
            return obj._activeUrl;
        }
    }

    [ExecuteAlways]
    public class ApiHandler : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        public class ApiMappings
        {
            public string key;
            public string endPoint;
        }

        public static ApiHandler Instance { get; private set; }
        public static string LastResponseJson { get; private set; }

        [SerializeField] private BaseUrlContainer _baseUrl;
        [SerializeField] private ApiMappings[] _apis;
        [SerializeField] private string _version;

        public string BaseUrl => _baseUrl;

        private void Awake()
        {
            Instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        public static string CombineUrls(string url1, string url2, string url3)
        {
            return $"{url1.TrimEnd('/')}/{url2.TrimStart('/').TrimEnd('/')}/{url3.TrimStart('/')}";
        }

        // public string Decrypt(byte[] data)
        // {
        //     // return data;
        //
        //     using Aes aes = Aes.Create();
        //     aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
        //     aes.IV = Encoding.UTF8.GetBytes(_encryptionIV);
        //
        //     using MemoryStream ms = new MemoryStream(data);
        //     using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        //     var output = new MemoryStream();
        //     cs.CopyTo(output);
        //     return Encoding.UTF8.GetString(output.ToArray());
        // }

        public string GetApiEndpoint(string endPointKey)
        {
            foreach (ApiMappings api in _apis)
            {
                if (string.Equals(api.key, endPointKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(api.endPoint))
                        return CombineUrls(_baseUrl, _version, api.endPoint);
                }
            }

            Debug.LogError("No end point found for key: " + endPointKey);
            return null;
        }

        public static void Send<TRequest, TResponse>(TRequest request, Action<TRequest, TResponse> onSuccess,
            Action<TRequest> onFail = null)
            where TRequest : BaseRequest<TResponse>
            where TResponse : BaseResponse
        {
            ApiHandler.Instance.StartCoroutine(SendCr(request, onSuccess, onFail));
        }

        private static IEnumerable<string> GetQueryParams(object request)
        {
            Type stringType = typeof(string);
            foreach (FieldInfo field in request.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType != stringType)
                    continue;

                if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                string key;
                if (field.GetCustomAttribute<JsonPropertyAttribute>() != null) key = field.Name;
                else key = field.Name;
                var value = field.GetValue(request) as string;

                if (!string.IsNullOrEmpty(value))
                {
                    var escapedKey = UnityWebRequest.EscapeURL(key);
                    var escapedValue = UnityWebRequest.EscapeURL(value);
                    yield return $"{escapedKey}={escapedValue}";
                }
            }
        }

        public static UnityWebRequest GetForm<TRequest, TResponse>(TRequest request)
            where TRequest : BaseRequest<TResponse>
            where TResponse : BaseResponse
        {
            var url = Instance.GetApiEndpoint(request.EndPointKey);
            if (request.ApiType == ApiType.GET)
            {
                var query = string.Join("&", GetQueryParams(request));
                if (string.IsNullOrEmpty(query) == false)
                    url = $"{url}?{query}";
            }


            UnityWebRequest unityRequest = new UnityWebRequest(url, request.ApiType.ToString());
            if (request.ApiType == ApiType.PUT || request.ApiType == ApiType.POST)
            {
                string json = JsonHelper.Serialize(request);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                unityRequest.uploadHandler = new UploadHandlerRaw(data);
                unityRequest.SetRequestHeader("Content-Type", "application/json");
            }

            if (request.headers != null)
            {
                // unityRequest.SetRequestHeader("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjY3OTc1MGUxMzA5NDY0Nzc5ZjdlMmU5OSIsImVtYWlsIjoidmFpc2huYXYuZm9yZWV4Y2VsQGdtYWlsLmNvbSIsImlhdCI6MTczNzk3MDIxN30.Vo5ViAeDoRioPJSsCJoWd4Jc0V2E9qX7w7GPUnR7Jes");
                foreach (KeyValuePair<string, string> header in request.headers)
                {
                    unityRequest.SetRequestHeader(header.Key, header.Value);
                }
            }

            unityRequest.downloadHandler = new DownloadHandlerBuffer();
            return unityRequest;
        }

        private static IEnumerator SendCr<TRequest, TResponse>(TRequest request, Action<TRequest, TResponse> onSuccess,
            Action<TRequest> onFail = null)
            where TRequest : BaseRequest<TResponse>
            where TResponse : BaseResponse
        {
            // If the user is offline for more than 2 seconds return error
            float waitTime = 0f;
            while (Application.internetReachability == NetworkReachability.NotReachable)
            {
                waitTime += Time.deltaTime;
                if (waitTime > 2f)
                {
                    request.Result = UnityWebRequest.Result.ConnectionError;
                    request.Error = "You are currently offline.";
                    onFail?.Invoke(request);
                    yield break;
                }
            }


            LastResponseJson = null;
            UnityWebRequest unityRequest = GetForm<TRequest, TResponse>(request);
            Debug.Log($"Sending({unityRequest.url})\n{JsonHelper.Serialize(request)}");


            request.RequestSentTime = DateTime.UtcNow;
            yield return unityRequest.SendWebRequest();

            request.ResponseReceivedTime = DateTime.UtcNow;
            request.Result = unityRequest.result;
            request.Error = unityRequest.error;

            if (unityRequest.result == UnityWebRequest.Result.Success)
            {
                TResponse response;
                try
                {
                    LastResponseJson = unityRequest.downloadHandler.text;
                    response = JsonHelper.Deserialize<TResponse>(LastResponseJson);
                }
                catch (Exception e)
                {
                    request.Result = UnityWebRequest.Result.DataProcessingError;
                    request.Error = e.Message;
                    Debug.LogError($"Issue while loading json: \n{LastResponseJson}");
                    Debug.LogException(e);
                    onFail?.Invoke(request);
                    yield break;
                }

                onSuccess?.Invoke(request, response);
            }
            else
            {
                try
                {
                    var error = JsonHelper.Deserialize<BaseResponse>(unityRequest.downloadHandler.text);
                    request.Error = error.message;
                }
                catch
                {
                    // ignored
                }

                onFail?.Invoke(request);
            }
        }

        public void OnBeforeSerialize()
        {
        }
#if UNITY_EDITOR
        private List<IRequest> _requests = null;

        private static object CreateInstanceWithBestConstructor(Type type)
        {
            // Get all constructors
            ConstructorInfo[] constructors = type.GetConstructors();

            if (constructors.Length == 0)
            {
                Debug.LogError($"No public constructors found for {type.FullName}");
                return null;
            }

            // Try to find a constructor with the least number of parameters
            ConstructorInfo bestConstructor = constructors.OrderBy(c => c.GetParameters().Length).First();
            ParameterInfo[] parameters = bestConstructor.GetParameters();

            // Create default values for parameters (null for reference types, default for value types)
            object[] args = parameters.Select(p => GetDefaultValue(p.ParameterType)).ToArray();

            return bestConstructor.Invoke(args);
        }

        private static object GetDefaultValue(Type type)
        {
            // Return default value for value types, otherwise return null
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
#endif
        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (_requests == null)
            {
                _requests = new List<IRequest>();
                Type baseApiType = typeof(IRequest);
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var assemblyFullName = assembly.FullName.ToLower();
                    if (assemblyFullName.Contains("unity") || assemblyFullName.Contains("system"))
                        continue;

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (baseApiType.IsAssignableFrom(type) == false) continue;
                        if (type.IsAbstract) continue;

                        try
                        {
                            var requestObj = CreateInstanceWithBestConstructor(type);
                            if (requestObj is IRequest request) _requests.Add(request);
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                }
            }

            foreach (IRequest request in _requests)
            {
                if (string.IsNullOrWhiteSpace(request.EndPointKey))
                    Debug.LogError($"No endpoint provided for {request.GetType().Name} has no endpoint key");
                GetApiEndpoint(request.EndPointKey);
            }
#endif
        }
    }
}