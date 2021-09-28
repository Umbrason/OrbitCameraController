using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OrbitCameraController))]
public class OrbitCameraControllerEditor : Editor
{
    const float subCategorySpacing = 5f;
    #region MenuItems
    [MenuItem("CONTEXT/Camera/Create Orbit Camera", false, 51)]
    public static void ConvertToOrbitController()
    {
        GameObject CameraGO = Selection.activeGameObject;
        if (CameraGO.transform.parent != null)
        {
            Debug.LogWarning("Camera gameObject must have no parent transform!");
            return;
        }        

        GameObject OrbitCenterGO = new GameObject("Orbit Camera", typeof(OrbitCameraController));

        Undo.RegisterCreatedObjectUndo(OrbitCenterGO, "orbit camera creation");
        Undo.RegisterCompleteObjectUndo(CameraGO.transform, "orbit camera creation");
        Undo.SetTransformParent(CameraGO.transform, OrbitCenterGO.transform, "orbit camera creation");

        OrbitCenterGO.transform.position = CameraGO.transform.position;
        CameraGO.transform.localPosition = Vector3.forward * -10f;
        CameraGO.transform.localRotation = Quaternion.identity;
        OrbitCenterGO.transform.rotation = Quaternion.Euler(30f, 0, 0);
    }

    [MenuItem("GameObject/Orbit Camera", false, 49)]
    public static void CreateOrbitCamera()
    {        
        GameObject OrbitCenterGO = new GameObject("Orbit Camera", typeof(OrbitCameraController));
        Undo.RegisterCreatedObjectUndo(OrbitCenterGO, "orbit camera creation");
        GameObject CameraGO = new GameObject("Camera", typeof(Camera));
        CameraGO.transform.SetParent(OrbitCenterGO.transform);
        CameraGO.transform.localPosition = Vector3.forward * -10f;
        OrbitCenterGO.transform.rotation = Quaternion.Euler(30f, 0, 0);
    }
    #endregion

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty movementProperty = serializedObject.FindProperty("movementSettings");
        GUILayout.Label("Movement", EditorStyles.boldLabel);

        BeginIndent(10f);
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("movementSpeed"));
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("sprintSpeedMultiplier"));
        GUILayout.Space(subCategorySpacing);
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("surfaceFollowType"));
        BeginConditionIndent(10f, movementProperty.FindPropertyRelative("surfaceFollowType").enumValueIndex != (int)MovementSettings.SurfaceFollowType.None);
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("collisionDetection"));
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("smoothness"));
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("allowFlight"));
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("surfaceCheckRange"));
        EditorGUILayout.PropertyField(movementProperty.FindPropertyRelative("groundMask"));
        EndConditionIndent();

        EndIndent();
        GUILayout.Box("", GUILayout.Height(1f), GUILayout.ExpandWidth(true));
        GUILayout.Space(5f);


        GUILayout.Label("Rotation", EditorStyles.boldLabel);
        BeginIndent(10f);
        SerializedProperty rotationProperty = serializedObject.FindProperty("rotationSettings");

        EditorGUILayout.PropertyField(rotationProperty.FindPropertyRelative("rotationButton"));
        EditorGUILayout.PropertyField(rotationProperty.FindPropertyRelative("rotationSensitivity"));
        EditorGUILayout.PropertyField(rotationProperty.FindPropertyRelative("easingBehaviour"));
        EditorGUILayout.PropertyField(rotationProperty.FindPropertyRelative("smoothness"));
        GUILayout.Space(subCategorySpacing);
        EditorGUILayout.PropertyField(rotationProperty.FindPropertyRelative("constrainX"));
        BeginConditionIndent(10f, rotationProperty.FindPropertyRelative("constrainX").boolValue);
        DrawMinMax(rotationProperty.FindPropertyRelative("rotationConstraintsX"), -90, 90);
        EndConditionIndent();
        GUILayout.Space(subCategorySpacing);
        EditorGUILayout.PropertyField(rotationProperty.FindPropertyRelative("constrainY"));
        BeginConditionIndent(10f, rotationProperty.FindPropertyRelative("constrainY").boolValue);
        DrawMinMax(rotationProperty.FindPropertyRelative("rotationConstraintsY"), 0, 360);
        EndConditionIndent();


        EndIndent();
        GUILayout.Box("", GUILayout.Height(1f), GUILayout.ExpandWidth(true));
        GUILayout.Space(5f);

        GUILayout.Label("Zoom", EditorStyles.boldLabel);
        BeginIndent(10f);
        SerializedProperty zoomProperty = serializedObject.FindProperty("zoomSettings");
        EditorGUILayout.PropertyField(zoomProperty.FindPropertyRelative("zoomSensitivity"));
        DrawMinMax(zoomProperty.FindPropertyRelative("zoomRange"), 1f, 50f);
        GUILayout.Space(subCategorySpacing);
        EditorGUILayout.PropertyField(zoomProperty.FindPropertyRelative("collisionDetection"));
        BeginConditionIndent(10f, zoomProperty.FindPropertyRelative("collisionDetection").enumValueIndex != (int)ZoomSettings.CollisionDetectionMethod.None);
        EditorGUILayout.PropertyField(zoomProperty.FindPropertyRelative("collisionLayerMask"));
        EndConditionIndent();
        EndIndent();
        GUILayout.Box("", GUILayout.Height(1f), GUILayout.ExpandWidth(true));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMinMax(SerializedProperty property, float min, float max)
    {
        Vector2 sliderValue = property.vector2Value;

        GUILayout.BeginHorizontal();
        GUILayout.Label(property.displayName);
        GUILayout.Space(5f);
        sliderValue.x = EditorGUILayout.IntField(Mathf.RoundToInt(sliderValue.x), GUILayout.Width(35));
        EditorGUILayout.MinMaxSlider(ref sliderValue.x, ref sliderValue.y, min, max);
        sliderValue.y = EditorGUILayout.IntField(Mathf.RoundToInt(sliderValue.y), GUILayout.Width(35));
        property.vector2Value = Vector2Int.RoundToInt(sliderValue);
        GUILayout.EndHorizontal();
    }

    #region GUI functions
    private void BeginIndent(float indent)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(indent);
        GUILayout.BeginVertical();
    }

    private void EndIndent()
    {
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void BeginConditionIndent(float indent, bool enabled)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(indent);
        GUILayout.BeginVertical();
        GUI.enabled = enabled;
    }

    private void EndConditionIndent()
    {
        GUI.enabled = true;
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    #endregion
}
