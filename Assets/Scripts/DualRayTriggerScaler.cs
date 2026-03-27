using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Project.MRI_Spawning.Scripts
{
    [RequireComponent(typeof(Collider))]
    public class DualRayScaleInteractable : MonoBehaviour
    {
        [Header("XR Controller Ray Names (scene GameObject names)")]
        public string leftControllerName = "Left Controller";
        public string rightControllerName = "Right Controller";

        [Header("Input Actions (Assign in Inspector)")]
        public InputActionReference leftTriggerAction;
        public InputActionReference rightTriggerAction;

        [Header("Scaling Settings")]
        public float scaleFactor = 1f;
        public float minScale = 0.1f;
        public float maxScale = 5f;

        private XRRayInteractor _leftRay;
        private XRRayInteractor _rightRay;
        private bool _leftHover, _rightHover;
        private bool _scaling;
        private Vector3 _initialScale;
        private float _initialDistance;
        private bool _menuWasClosed;

        void Awake()
        {
            var leftControllerObj = GameObject.Find(leftControllerName);
            var rightControllerObj = GameObject.Find(rightControllerName);

            if (leftControllerObj)
                _leftRay = leftControllerObj.GetComponent<XRRayInteractor>();
            if (rightControllerObj)
                _rightRay = rightControllerObj.GetComponent<XRRayInteractor>();
        }

        void OnEnable()
        {
            if (_leftRay)
            {
                _leftRay.hoverEntered.AddListener(OnLeftHoverEnter);
                _leftRay.hoverExited.AddListener(OnLeftHoverExit);
            }
            if (_rightRay)
            {
                _rightRay.hoverEntered.AddListener(OnRightHoverEnter);
                _rightRay.hoverExited.AddListener(OnRightHoverExit);
            }
            leftTriggerAction?.action.Enable();
            rightTriggerAction?.action.Enable();
        }

        void OnDisable()
        {
            if (_leftRay)
            {
                _leftRay.hoverEntered.RemoveListener(OnLeftHoverEnter);
                _leftRay.hoverExited.RemoveListener(OnLeftHoverExit);
            }
            if (_rightRay)
            {
                _rightRay.hoverEntered.RemoveListener(OnRightHoverEnter);
                _rightRay.hoverExited.RemoveListener(OnRightHoverExit);
            }
            leftTriggerAction?.action.Disable();
            rightTriggerAction?.action.Disable();
        }

        void OnLeftHoverEnter(HoverEnterEventArgs args)
        {
            if (args.interactableObject.transform == transform) _leftHover = true;
        }

        void OnLeftHoverExit(HoverExitEventArgs args)
        {
            if (args.interactableObject.transform == transform) _leftHover = false;
        }

        void OnRightHoverEnter(HoverEnterEventArgs args)
        {
            if (args.interactableObject.transform == transform) _rightHover = true;
        }

        void OnRightHoverExit(HoverExitEventArgs args)
        {
            if (args.interactableObject.transform == transform) _rightHover = false;
        }

        void Update()
        {
            if (leftTriggerAction == null || rightTriggerAction == null || _leftRay == null || _rightRay == null)
                return;

            bool leftTrig = leftTriggerAction.action.ReadValue<float>() > 0.5f;
            bool rightTrig = rightTriggerAction.action.ReadValue<float>() > 0.5f;

            if (_leftHover && _rightHover && leftTrig && rightTrig)
            {
                if (!_scaling)
                {
                    _initialScale = transform.localScale;
                    _initialDistance = Vector3.Distance(_leftRay.transform.position, _rightRay.transform.position);
                    _initialDistance = Mathf.Max(_initialDistance, 1e-4f); // Avoid divide-by-zero
                    _scaling = true;

                    // Force-close the menu and block it
                    // if (GlobalMenuController.Instance != null)
                    // {
                    //     GlobalMenuController.Instance.ForceCloseMenu();
                    //     GlobalMenuController.IsMenuEnabledExternally = false;
                    //     _menuWasClosed = true;
                    // }
                }

                float curDist = Vector3.Distance(_leftRay.transform.position, _rightRay.transform.position);
                float ratio = curDist / _initialDistance;
                float newScale = Mathf.Clamp(ratio * scaleFactor, minScale, maxScale);
                Vector3 scaled = _initialScale * newScale;

                if (IsVectorValid(scaled))
                    transform.localScale = scaled;
            }
            else
            {
                if (_scaling && _menuWasClosed)
                {
                    // Restore menu interactivity
                    // if (GlobalMenuController.Instance != null)
                    //     GlobalMenuController.IsMenuEnabledExternally = true;

                    _menuWasClosed = false;
                }
                _scaling = false;
            }
        }

        private bool IsVectorValid(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
                     float.IsNaN(v.y) || float.IsInfinity(v.y) ||
                     float.IsNaN(v.z) || float.IsInfinity(v.z));
        }
    }
}
