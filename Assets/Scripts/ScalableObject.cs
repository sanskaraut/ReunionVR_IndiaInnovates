using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Project.MRI_Spawning.Scripts
{
    public class ScalableObject : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable _interactable;
        [SerializeField] private Button _menuButton;

        private bool _hasTriggeredMenu;
        private bool _isGrabbed;
        private const float HOVER_DURATION = 4f;

        private void Awake()
        {
            if (_menuButton)
                _menuButton.onClick.AddListener(OpenMenuFromButton);

            _interactable.selectEntered.AddListener(OnGrab);
            _interactable.selectExited.AddListener(OnRelease);
            _interactable.hoverEntered.AddListener(OnHoverEnter);
            _interactable.hoverExited.AddListener(OnHoverExit);
        }

        private void OnDestroy()
        {
            if (_menuButton)
                _menuButton.onClick.RemoveListener(OpenMenuFromButton);

            _interactable.selectEntered.RemoveListener(OnGrab);
            _interactable.selectExited.RemoveListener(OnRelease);
            _interactable.hoverEntered.RemoveListener(OnHoverEnter);
            _interactable.hoverExited.RemoveListener(OnHoverExit);
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            _isGrabbed = true;
            CancelInvoke(nameof(OpenMenuFromButton));
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            _isGrabbed = false;
        }

        private void OnHoverEnter(HoverEnterEventArgs args)
        {
            // Cancel hover menu if:
            // 1. This object is grabbed
            // 2. Any menu is already open for any other object
            // if (_isGrabbed || GlobalMenuController.Instance.IsMenuOpen())
            //     return;

            _hasTriggeredMenu = false;
            Invoke(nameof(OpenMenuFromButton), HOVER_DURATION);
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            _hasTriggeredMenu = false;
            CancelInvoke(nameof(OpenMenuFromButton));
        }

        private void OpenMenuFromButton()
        {
            if (_hasTriggeredMenu || _isGrabbed)
                return;

            // Don't open if menu is already open for a different object
            // var menu = GlobalMenuController.Instance;
            // if (menu.IsMenuOpen() && menu.GetActiveTarget() != this)
            //     return;

            // menu.ShowMenu(this);
            bool isGrabbable = (_interactable.interactionLayers & LayerMask.GetMask("Grabbable")) != 0;
            // menu.SetGrabbableBoolStatus(isGrabbable);
            _hasTriggeredMenu = true;

        }

        public float GetCurrentScale()
        {
            return transform.localScale.x;
        }

        public void ScaleSelf(float scaleDelta)
        {
            transform.localScale += Vector3.one * scaleDelta;
        }

        public void DeleteSelf()
        {
            Destroy(gameObject);
        }

        internal void SetGrabEnabled(bool isOn)
        {
            var grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.enabled = isOn;
            }
        }

    }
}
