#if UNITY_EDITOR
using UnityEngine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class YutScaleCameraSetup
{
    [MenuItem("Tools/Yut Duel/Create Scale Camera And Link")]
    public static void CreateCamera()
    {
        if (Application.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
        var directors = Object.FindObjectsByType<CameraDirector>(FindObjectsSortMode.None);
        if (directors.Length != 1) { Debug.LogError("Keep exactly one active CameraDirector in the open scene."); return; }
        var existing = GameObject.Find("CM_Scale");
        CinemachineCamera cam;
        if (existing != null)
        {
            cam = existing.GetComponent<CinemachineCamera>();
            if (cam == null) { Debug.LogError("CM_Scale exists but has no CinemachineCamera component."); return; }
        }
        else
        {
            var go = new GameObject("CM_Scale");
            Undo.RegisterCreatedObjectUndo(go, "Create scale camera");
            cam = Undo.AddComponent<CinemachineCamera>(go);
            go.transform.position = new Vector3(1.35f, 1.45f, -2.05f);
            go.transform.LookAt(new Vector3(1.35f, 1.35f, 0f));
            var lens = cam.Lens;
            lens.FieldOfView = 36f;
            lens.NearClipPlane = 0.03f;
            lens.FarClipPlane = 100f;
            cam.Lens = lens;
            cam.Priority = 10;
        }
        var serialized = new SerializedObject(directors[0]);
        serialized.FindProperty("scaleCamera").objectReferenceValue = cam;
        serialized.FindProperty("enableDebugKeyboard").boolValue = false;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(directors[0].gameObject.scene);
        Selection.activeGameObject = cam.gameObject;
        Debug.Log("CM_Scale linked. Check its framing in Game view and save the scene.");
    }
}
#endif
