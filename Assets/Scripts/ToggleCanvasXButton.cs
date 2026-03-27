using UnityEngine;
using UnityEngine.InputSystem;

// Required for new Input System

namespace Project.MRI_Spawning.Scripts
{
    public class ToggleCanvasOnXButton : MonoBehaviour
    {
        [Header("Assign your UI Canvas here (the parent GameObject)")]
        public GameObject targetCanvas;

        [Header("Input Action Reference for 'X' Button (Left Controller)")]
        public InputActionReference xButtonAction; // Assign in Inspector

        private bool _isCanvasActive = false;

        private void OnEnable()
        {
            if (xButtonAction != null)
            {
                xButtonAction.action.performed += OnXButtonPressed;
                xButtonAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (xButtonAction != null)
                xButtonAction.action.performed -= OnXButtonPressed;
        }

        private void OnXButtonPressed(InputAction.CallbackContext context)
        {
            if (targetCanvas == null) return;

            _isCanvasActive = !_isCanvasActive;
            targetCanvas.SetActive(_isCanvasActive);
        }
    }
}