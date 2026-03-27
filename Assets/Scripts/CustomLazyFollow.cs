using UnityEngine;

namespace Project.MRI_Spawning.Scripts
{
    public class CustomLazyFollow : MonoBehaviour
    {
        [Header("Maximum horizontal distance to keep from Main Camera (in meters)")]
        public float followDistance = 2f;

        [Header("Smoothness (higher is slower, lower is snappier)")]
        public float smoothTime = 0.3f;

        private Transform _mainCamera;
        private Vector3 _velocity = Vector3.zero;

        void Start()
        {
            // Auto-detect main camera
            if (Camera.main != null)
            {
                _mainCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogError("CustomLazyFollow: No Main Camera found! Please tag your camera as 'MainCamera'.");
            }
        }

        void LateUpdate()
        {
            if (_mainCamera == null)
                return;

            // Camera's position on XZ plane (object keeps its own Y)
            Vector3 cameraXZ = new Vector3(_mainCamera.position.x, transform.position.y, _mainCamera.position.z);

            // Flat direction from camera to this object
            Vector3 direction = (transform.position - cameraXZ);
            direction.y = 0f;
            float currentDistance = direction.magnitude;

            // Only follow if object is farther than followDistance
            if (currentDistance <= followDistance)
                return;

            // If overlapped, push outwards (use camera's backward)
            if (direction.sqrMagnitude < 0.001f)
                direction = -_mainCamera.forward;

            direction.Normalize();

            // Target position = camera position + (direction * followDistance), on XZ plane, keep Y
            Vector3 targetPos = cameraXZ + direction * followDistance;

            // Smoothly move
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
        }
    }
}