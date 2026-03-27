using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.MRI_Spawning.Scripts
{
    public class DynamicObjectSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _transformParentObject;    // Parent for spawned objects
        [SerializeField] private GameObject _transformPlaceholder;     // Placeholder for spawn location

        [Header("Prefabs & Buttons")]
        [SerializeField] private List<GameObject> _prefabs = new List<GameObject>();
        [SerializeField] private List<Button> _buttons = new List<Button>();

        private void Awake()
        {
            // Link each button's click to spawn the corresponding prefab
            for (int i = 0; i < _buttons.Count; i++)
            {
                int index = i; // Capture variable for closure
                if (_buttons[index] != null)
                {
                    _buttons[index].onClick.AddListener(() => InstantiateAtIndex(index));
                }
            }
        }

        private void InstantiateAtIndex(int index)
        {
            if (index < 0 || index >= _prefabs.Count)
            {
                Debug.LogWarning("Prefab index out of range!");
                return;
            }
            if (!_prefabs[index] || !_transformPlaceholder || !_transformParentObject)
            {
                Debug.LogWarning("Missing reference for spawning object at index " + index);
                return;
            }

            // Instantiate at placeholder's position/rotation, but keep original prefab scale
            GameObject obj = Instantiate(
                _prefabs[index],
                _transformPlaceholder.transform.position,
                _transformPlaceholder.transform.rotation,
                _transformParentObject.transform
            );
            // DO NOT SET obj.transform.localScale -- keeps prefab's default

            // === AUTO-ATTACH SCALABLE OBJECT SCRIPT ===
            // if (obj.GetComponent<ScalableObject>() == null)
                // obj.AddComponent<ScalableObject>();
        }
    }
}
