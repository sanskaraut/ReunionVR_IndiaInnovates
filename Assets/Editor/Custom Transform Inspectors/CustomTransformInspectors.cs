using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Transform))]
public class CustomTransformInspector : Editor
{
    private GUIContent _resetContent;
    private GUIContent _pasteContent;

    // Static variables to store copied transform data
    private static Vector3 _copiedPosition;
    private static Vector3 _copiedRotation;
    private static Vector3 _copiedScale;
    private static bool _hasClipboard = false;

    public override void OnInspectorGUI()
    {
        if (_resetContent == null)
        {
            var resetTexture = Resources.Load<Texture2D>("reset-icon");
            _resetContent = new GUIContent(resetTexture, "Reset");
        }

        if (_pasteContent == null)
        {
            var pasteTexture = Resources.Load<Texture2D>("paste-icon");
            _pasteContent = new GUIContent(pasteTexture, "Paste");
        }

        Transform transform = (Transform)target;
        bool useWorldSpace = Tools.pivotRotation == PivotRotation.Global;

        EditorGUI.BeginChangeCheck();
        DrawPosRotScale(transform, useWorldSpace);
        EditorGUILayout.Space(8);
        DrawButtons(transform, useWorldSpace);
        EditorGUI.EndChangeCheck();
    }

    private void DrawButtons(Transform transform, bool useWorldSpace)
    {
        EditorGUILayout.BeginHorizontal();

        GUIContent spaceToggleContent = new GUIContent(useWorldSpace ? "World" : "Local", "Toggle between World and Local space coordinates");

        var width = GUILayout.Width((EditorGUIUtility.currentViewWidth / 3f) - 10f);
        
        
        if (GUILayout.Button(spaceToggleContent, EditorStyles.toolbarButton, width))
        {
            Tools.pivotRotation = useWorldSpace ? PivotRotation.Local : PivotRotation.Global;
            Repaint();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Copy Transform", EditorStyles.toolbarButton, width))
        {
            CopyTransform(transform, useWorldSpace);
        }
        
        GUILayout.Space(10);

        GUI.enabled = _hasClipboard;
        if (GUILayout.Button("Paste Transform", EditorStyles.toolbarButton, width))
        {
            PasteTransform(transform, useWorldSpace);
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawPropertyWithResetAndPaste(string label, Vector3 value, System.Action<Vector3> onValueChanged, System.Action onReset, System.Action onPaste)
    {
        EditorGUILayout.BeginHorizontal();

        // Draw the Vector3 field
        EditorGUI.BeginChangeCheck();
        Vector3 newValue = EditorGUILayout.Vector3Field(label, value);
        if (EditorGUI.EndChangeCheck())
            onValueChanged(newValue);
        
        // Reset button
        if (GUILayout.Button(_resetContent, EditorStyles.toolbarButton, GUILayout.Width(EditorGUIUtility.singleLineHeight * 1.8f)))
            onReset();

        // Paste button (only enabled if we have clipboard data)
        GUI.enabled = _hasClipboard;
        if (GUILayout.Button(_pasteContent, EditorStyles.toolbarButton, GUILayout.Width(EditorGUIUtility.singleLineHeight * 1.8f)))
            onPaste();
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPosRotScale(Transform transform, bool useWorldSpace)
    {
        // Position with inline reset and paste buttons
        DrawPropertyWithResetAndPaste("Position",
            useWorldSpace ? transform.position : transform.localPosition,
            (newValue) =>
            {
                Undo.RecordObject(transform, "Change Position");
                if (useWorldSpace)
                    transform.position = newValue;
                else
                    transform.localPosition = newValue;
                EditorUtility.SetDirty(transform);
            },
            () =>
            {
                Undo.RecordObject(transform, "Reset Position");
                if (useWorldSpace)
                    transform.position = Vector3.zero;
                else
                    transform.localPosition = Vector3.zero;
                EditorUtility.SetDirty(transform);
            },
            () =>
            {
                Undo.RecordObject(transform, "Paste Position");
                if (useWorldSpace)
                    transform.position = _copiedPosition;
                else
                    transform.localPosition = _copiedPosition;
                EditorUtility.SetDirty(transform);
            });

        // Rotation with inline reset and paste buttons
        DrawPropertyWithResetAndPaste("Rotation",
            useWorldSpace ? transform.eulerAngles : transform.localEulerAngles,
            (newValue) =>
            {
                Undo.RecordObject(transform, "Change Rotation");
                if (useWorldSpace)
                    transform.eulerAngles = newValue;
                else
                    transform.localEulerAngles = newValue;
                EditorUtility.SetDirty(transform);
            },
            () =>
            {
                Undo.RecordObject(transform, "Reset Rotation");
                if (useWorldSpace)
                    transform.rotation = Quaternion.identity;
                else
                    transform.localRotation = Quaternion.identity;
                EditorUtility.SetDirty(transform);
            },
            () =>
            {
                Undo.RecordObject(transform, "Paste Rotation");
                if (useWorldSpace)
                    transform.eulerAngles = _copiedRotation;
                else
                    transform.localEulerAngles = _copiedRotation;
                EditorUtility.SetDirty(transform);
            });

        // Scale with inline reset and paste buttons (always local)
        DrawPropertyWithResetAndPaste("Scale",
            transform.localScale,
            (newValue) =>
            {
                Undo.RecordObject(transform, "Change Scale");
                transform.localScale = newValue;
                EditorUtility.SetDirty(transform);
            },
            () =>
            {
                Undo.RecordObject(transform, "Reset Scale");
                transform.localScale = Vector3.one;
                EditorUtility.SetDirty(transform);
            },
            () =>
            {
                Undo.RecordObject(transform, "Paste Scale");
                transform.localScale = _copiedScale;
                EditorUtility.SetDirty(transform);
            });
    }
    
    private void CopyTransform(Transform transform, bool useWorldSpace)
    {
        if (useWorldSpace)
        {
            _copiedPosition = transform.position;
            _copiedRotation = transform.eulerAngles;
        }
        else
        {
            _copiedPosition = transform.localPosition;
            _copiedRotation = transform.localEulerAngles;
        }

        _copiedScale = transform.localScale; // Scale is always local
        _hasClipboard = true;
    }

    private void PasteTransform(Transform transform, bool useWorldSpace)
    {
        if (!_hasClipboard) return;

        Undo.RecordObject(transform, "Paste Transform");

        if (useWorldSpace)
        {
            transform.position = _copiedPosition;
            transform.eulerAngles = _copiedRotation;
        }
        else
        {
            transform.localPosition = _copiedPosition;
            transform.localEulerAngles = _copiedRotation;
        }

        transform.localScale = _copiedScale;

        EditorUtility.SetDirty(transform);
    }
}