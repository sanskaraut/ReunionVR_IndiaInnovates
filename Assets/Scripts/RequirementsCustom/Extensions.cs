using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Extensions
{
    // public static void MarkInteractable(this IEnumerable<Selectable> self, bool isInteractable)
    // {
    //     if (self == null) return;

    //     foreach (Selectable selectable in self)
    //     {
    //         if (selectable != null)
    //             selectable.interactable = isInteractable;
    //     }
    // }

    // public static void MarkInteractable(this IEnumerable<TextAndToggle> self, bool isInteractable)
    // {
    //     foreach (TextAndToggle tat in self)
    //         tat.toggle.interactable = isInteractable;
    // }

    // public static void MarkInteractable(this IEnumerable<TextAndButton> self, bool isInteractable)
    // {
    //     foreach (TextAndButton tab in self)
    //         tab.button.interactable = isInteractable;
    // }

    // public static void SetActive(this IEnumerable<GameObject> self, bool isActive)
    // {
    //     foreach (GameObject gameObject in self)
    //     {
    //         if (gameObject != null)
    //             gameObject.SetActive(isActive);
    //     }
    // }

    // public static void SetActive(this IEnumerable<Component> self, bool isActive)
    // {
    //     foreach (Component component in self)
    //     {
    //         if (component != null)
    //             component.gameObject.SetActive(isActive);
    //     }
    // }

    // public static void SetParent(this IEnumerable<Transform> self, Transform newParent)
    // {
    //     foreach (Transform component in self)
    //     {
    //         if (component != null)
    //             component.SetParent(newParent);
    //     }
    // }

    // public static string CreateMD5Hash(this string input)
    // {
    //     using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
    //     byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
    //     byte[] hashBytes = md5.ComputeHash(inputBytes);
    //     return BitConverter.ToString(hashBytes);
    // }

    public static bool TryGetBounds(this Transform transform, out Bounds bounds)
    {
        var meshRenderers = transform.GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length == 0)
        {
            bounds = default;
            return false;
        }


        var hasBounds = false;
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            if (!hasBounds)
            {
                bounds = meshRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(meshRenderer.bounds);
            }
        }

        return hasBounds;
    }

    // public static IAnnotationData GetDefaultAnnotationData(this AnnotationType type, Vector3 position = default, Quaternion rotation = default)
    // {
    //     return type switch
    //     {
    //         AnnotationType.Line => new LineAnnotationData() { startingPos = position },
    //         AnnotationType.Arrow => new ArrowAnnotationData() { startingPos = position, endingPos = position + rotation * Vector3.up, endRotation = rotation },
    //         AnnotationType.Model => new ModelAnnotationData() { position = position, rotation = rotation, scale = 1f, path = "Circle" },
    //         _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    //     };
    // }

    // public static string FormatVector3(Vector3 value)
    // {
    //     return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
    // }

    // public static string FormatQuternion(Quaternion value)
    // {
    //     return $"{value.x:0.###},{value.y:0.###},{value.z:0.###},{value.w:0.###}";
    // }

    // public static string FormatVector4(Vector4 value)
    // {
    //     return $"{value.x:0.###},{value.y:0.###},{value.z:0.###},{value.w:0.###}";
    // }

    // public static Vector3 ParseVector3(string str)
    // {
    //     string[] values = str.Split(',');
    //     if (values.Length == 3 &&
    //         float.TryParse(values[0], out float x) &&
    //         float.TryParse(values[1], out float y) &&
    //         float.TryParse(values[2], out float z))
    //     {
    //         return new Vector3(x, y, z);
    //     }

    //     throw new Exception("Invalid Vector3 format");
    // }

    // public static Vector4 ParseVector4(string str)
    // {
    //     string[] values = str.Split(',');
    //     if (values.Length == 4 &&
    //         float.TryParse(values[0], out float x) &&
    //         float.TryParse(values[1], out float y) &&
    //         float.TryParse(values[2], out float z) &&
    //         float.TryParse(values[3], out float w))
    //     {
    //         return new Vector4(x, y, z, w);
    //     }

    //     throw new Exception("Invalid Vector3 format");
    // }

    // public static Quaternion ParseQuaternion(string str)
    // {
    //     string[] values = str.Split(',');
    //     if (values.Length == 4 &&
    //         float.TryParse(values[0], out float x) &&
    //         float.TryParse(values[1], out float y) &&
    //         float.TryParse(values[2], out float z) &&
    //         float.TryParse(values[3], out float w))
    //     {
    //         return new Quaternion(x, y, z, w);
    //     }

    //     throw new Exception("Invalid Vector3 format");
    // }
}