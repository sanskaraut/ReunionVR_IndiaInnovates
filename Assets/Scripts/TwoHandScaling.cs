using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Project.MRI_Spawning.Scripts
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class TwoHandScaling : MonoBehaviour
    {
        [Header("Scaling Settings")]
        public float scaleFactor = 1f;
        public float minScale = 0.1f;
        public float maxScale = 5f;
        public bool smoothScaling = true;
        public float smoothSpeed = 5f;

        [Header("Visual Feedback")]
        public bool showDebugLines = true;
        public Color debugLineColor = Color.yellow;

        private XRGrabInteractable _grabInteractable;
        private Transform _leftController;
        private Transform _rightController;
        private Vector3 _initialScale;
        private float _initialDistance;
        private float _targetScale = 1f;

        private Vector3 _lockedPosition;
        private Quaternion _lockedRotation;

        // State tracking
        private bool _isTwoHandedScaling;
        private bool _wasTwoHandedScaling;

        void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            // Always set _initialScale to a sane default!
            _initialScale = transform.localScale;
            if (_initialScale == Vector3.zero) _initialScale = Vector3.one;
        }

        void Update()
        {
            // Detect transition into/out of two-handed scaling mode
            int numHands = _grabInteractable.interactorsSelecting.Count;

            _wasTwoHandedScaling = _isTwoHandedScaling;
            _isTwoHandedScaling = (numHands == 2);

            if (_isTwoHandedScaling)
            {
                // Assign only when entering scaling mode
                if (!_wasTwoHandedScaling)
                {
                    _leftController = _grabInteractable.interactorsSelecting[0]?.transform;
                    _rightController = _grabInteractable.interactorsSelecting[1]?.transform;

                    if (_leftController == null || _rightController == null)
                        return;

                    _initialDistance = Vector3.Distance(_leftController.position, _rightController.position);
                    // Defensive: Prevent divide-by-zero
                    if (Mathf.Abs(_initialDistance) < 1e-4f) _initialDistance = 1e-4f;

                    _initialScale = transform.localScale;
                    if (_initialScale == Vector3.zero) _initialScale = Vector3.one;

                    _lockedPosition = transform.position;
                    _lockedRotation = transform.rotation;
                }

                // Lock object in place
                transform.position = _lockedPosition;
                transform.rotation = _lockedRotation;

                // Perform scaling
                HandleTwoHandedScaling();
            }
            else
            {
                _leftController = null;
                _rightController = null;
            }

            // Smooth scaling (defensive: avoid NaN/Inf)
            if (smoothScaling)
            {
                float denom = (_initialScale.x == 0f) ? 1f : _initialScale.x;
                float currentScale = transform.localScale.x / denom;
                if (float.IsNaN(currentScale) || float.IsInfinity(currentScale)) currentScale = 1f;

                float smoothedScale = Mathf.Lerp(currentScale, _targetScale, Time.deltaTime * smoothSpeed);
                if (float.IsNaN(smoothedScale) || float.IsInfinity(smoothedScale)) smoothedScale = 1f;

                Vector3 newLocalScale = _initialScale * smoothedScale;
                if (!IsVectorValid(newLocalScale)) newLocalScale = Vector3.one;
                transform.localScale = newLocalScale;
            }
        }

        void HandleTwoHandedScaling()
        {
            if (_leftController == null || _rightController == null) return;

            float currentDistance = Vector3.Distance(_leftController.position, _rightController.position);
            if (Mathf.Abs(_initialDistance) < 1e-4f) return; // skip, can't scale

            float scaleRatio = currentDistance / _initialDistance;
            if (float.IsNaN(scaleRatio) || float.IsInfinity(scaleRatio)) return;

            float newScale = Mathf.Clamp(scaleRatio * scaleFactor, minScale, maxScale);
            if (float.IsNaN(newScale) || float.IsInfinity(newScale)) newScale = 1f;

            if (smoothScaling)
            {
                _targetScale = newScale;
            }
            else
            {
                Vector3 newLocalScale = _initialScale * newScale;
                if (!IsVectorValid(newLocalScale)) newLocalScale = Vector3.one;
                transform.localScale = newLocalScale;
            }

            if (showDebugLines)
            {
                Debug.DrawLine(_leftController.position, _rightController.position, debugLineColor);
            }
        }

        // Utility: Check for valid scale
        private bool IsVectorValid(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
                     float.IsNaN(v.y) || float.IsInfinity(v.y) ||
                     float.IsNaN(v.z) || float.IsInfinity(v.z));
        }

        void OnDrawGizmosSelected()
        {
            if (_isTwoHandedScaling && _leftController != null && _rightController != null)
            {
                Gizmos.color = debugLineColor;
                Gizmos.DrawLine(_leftController.position, _rightController.position);
                Gizmos.DrawWireSphere(_leftController.position, 0.02f);
                Gizmos.DrawWireSphere(_rightController.position, 0.02f);
            }
        }
    }
}
