using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.MRI_Spawning.Scripts
{
    public class ThreeDeeModel : MonoBehaviour
    {
        private class CacheHolder
        {
            public GameObject model;
            public bool hasBounds;
            public Bounds bounds;
        }
    
        private static Dictionary<string, CacheHolder> _caches = new Dictionary<string, CacheHolder>();

        public string Link { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsContentValid { get; private set; }
        private byte[] _data;
        [SerializeField] private BoxCollider _collider;

        public IEnumerator Init(string link)
        {
            Link = link;
            IsLoading = true;
            IsContentValid = false;
            
            if (_caches.TryGetValue(link, out CacheHolder cached))
            {
                IsContentValid = true;
                foreach (object item in LoadFromCache(cached))
                    yield return item;
                IsLoading = false;
                yield break;
            }
            yield return StartCoroutine(DownloadContent(link));
            if (_data == null)
            {
                gameObject.SetActive(false);
                IsLoading = false;
                IsContentValid = false;
                yield break;
            }

            IsContentValid = true;
            foreach (object item in PrepareCache(_data))
                yield return item;

            if (_caches.TryGetValue(link, out  cached))
            {
                IsContentValid = true;
                foreach (object item in LoadFromCache(cached))
                    yield return item;
                IsLoading = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator DownloadContent(string url)
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = ApiHandling.ApiHandler.Instance.BaseUrl.TrimEnd('/', '\\');
                var relativeUrl = url.TrimStart('/', '\\');
                url = $"{baseUrl}/{relativeUrl}";
            }

            using UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Download failed: \n    url: {url}\n    error: {request.error}");
                _data = null;
                yield break;
            }

            _data = request.downloadHandler.data;
        }

        private IEnumerable PrepareCache(byte[] bytes)
        {
            GltfImport gltf = new GltfImport();
            Task<bool> task = gltf.LoadGltfBinary(bytes);
            while (!task.IsCompleted)
                yield return null;

            if (!task.Result)
            {
                IsContentValid = false;
                yield break;
            }

            Transform parent = new GameObject($"[Parent]").transform;
            parent.gameObject.SetActive(false);
            task = gltf.InstantiateMainSceneAsync(parent);
            while (!task.IsCompleted)
                yield return null;

            if (!task.Result)
            {
                IsContentValid = false;
                yield break;
            }
        
            parent.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            parent.localScale = Vector3.one;
            bool hasBounds = parent.TryGetBounds(out var bounds);
            var cache = new CacheHolder()
            {
                model = parent.gameObject,
                hasBounds = hasBounds,
                bounds = bounds
            };

            _caches.Add(Link, cache);
            IsContentValid = true;
        }

        private IEnumerable LoadFromCache(CacheHolder cache)
        {
            var instance = Instantiate(cache.model, transform, false).transform;
            instance.gameObject.SetActive(true);

            if (!cache.hasBounds) yield break;

            var bounds = cache.bounds;
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDimension == 0) yield break;

            float scaleFactor = 1.0f /* / maxDimension*/;
            instance.localScale = Vector3.one * scaleFactor;
            if (_collider)
            {
                _collider.size = bounds.size * scaleFactor;
                _collider.center += bounds.center * scaleFactor;
            }
        }
    }
}