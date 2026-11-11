using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Редакторський скрипт: одним кліком налаштовує ЛР3.
/// Меню: Lab 3 -> Налаштувати сцену і префаб
/// 1) На префаб піци додає BoxCollider (під розмір моделі) і FoodObject.
/// 2) На XR Origin додає скрипти жестів і призначає посилання в інспекторі.
/// 3) Прибирає зайві копії ARSessionStateDisplay на StateText.
/// </summary>
public static class Lab3Setup
{
    private const string PrefabPath = "Assets/Prefabs/F_Europe_Pizza_Pepperoni.prefab";

    private static readonly string[] TexturePaths =
    {
        "Assets/Free Hyper Casual Street Food Pack/Textures/streetFood_BC.png",
        "Assets/Textures/Lab3/streetFood_BC_Burnt.png",
        "Assets/Textures/Lab3/streetFood_BC_Frozen.png",
        "Assets/Textures/Lab3/streetFood_BC_Neon.png",
    };

    [MenuItem("Lab 3/Налаштувати сцену і префаб")]
    public static void Setup()
    {
        string prefabLog = SetupPrefab();
        string sceneLog = SetupScene();
        AssetDatabase.SaveAssets();

        string msg = prefabLog + "\n" + sceneLog;
        Debug.Log("[Lab3Setup]\n" + msg);
        EditorUtility.DisplayDialog("ЛР3 налаштовано", msg, "OK");
    }

    private static string SetupPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // --- BoxCollider під розмір усіх мешів (у локальних координатах кореня)
            if (!root.TryGetComponent(out BoxCollider box))
                box = root.AddComponent<BoxCollider>();

            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds bounds = default;

            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                Vector3 c = mb.center, e = mb.extents;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = c + Vector3.Scale(e, new Vector3(
                        (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!hasBounds) { bounds = new Bounds(p, Vector3.zero); hasBounds = true; }
                    else bounds.Encapsulate(p);
                }
            }

            if (hasBounds)
            {
                box.center = bounds.center;
                // трохи товщий по висоті, щоб по пласкій піці легше влучити пальцем
                box.size = new Vector3(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.x * 0.25f), bounds.size.z);
            }

            // --- FoodObject (маркер для жестів + анімація)
            if (!root.TryGetComponent(out FoodObject _))
                root.AddComponent<FoodObject>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return $"Префаб: BoxCollider (size {box.size}) + FoodObject додано.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string SetupScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var log = new List<string>();

        ARRaycastManager raycastManager = Object.FindAnyObjectByType<ARRaycastManager>();
        if (raycastManager == null)
            return "ПОМИЛКА: на сцені не знайдено ARRaycastManager (XR Origin). Відкрий SampleScene.";

        GameObject origin = raycastManager.gameObject;
        Camera cam = origin.GetComponentInChildren<Camera>(true);

        // --- PlaceOrMoveObject: призначаємо камеру
        PlaceOrMoveObject placer = GetOrAdd<PlaceOrMoveObject>(origin);
        var so = new SerializedObject(placer);
        so.FindProperty("arCamera").objectReferenceValue = cam;
        so.ApplyModifiedPropertiesWithoutUndo();

        // --- Переміщення
        var drag = GetOrAdd<ObjectDragController>(origin);
        drag.arCamera = cam;
        drag.raycastManager = raycastManager;

        // --- Масштабування
        var pinch = GetOrAdd<PinchScaleController>(origin);
        pinch.placer = placer;

        // --- Обертання
        var rotate = GetOrAdd<RotateGestureController>(origin);
        rotate.placer = placer;
        rotate.arCamera = cam;

        // --- Тапи: текстура / анімація
        var tap = GetOrAdd<ObjectTapController>(origin);
        tap.arCamera = cam;
        tap.textures = TexturePaths
            .Select(p => AssetDatabase.LoadAssetAtPath<Texture>(p))
            .Where(t => t != null)
            .ToArray();

        foreach (var c in new Object[] { placer, drag, pinch, rotate, tap })
            EditorUtility.SetDirty(c);

        log.Add($"XR Origin: додано Drag/Pinch/Rotate/Tap контролери, камера = {(cam ? cam.name : "НЕ ЗНАЙДЕНО")}, текстур = {tap.textures.Length}.");

        // --- Прибираємо дублікати ARSessionStateDisplay
        int removed = 0;
        foreach (var group in Object.FindObjectsByType<ARSessionStateDisplay>(FindObjectsInactive.Include)
                     .GroupBy(d => d.gameObject))
        {
            foreach (var extra in group.Skip(1))
            {
                Object.DestroyImmediate(extra);
                removed++;
            }
        }
        log.Add($"Зайвих ARSessionStateDisplay видалено: {removed}.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Add("Сцену збережено.");

        return string.Join("\n", log);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        return go.TryGetComponent(out T c) ? c : go.AddComponent<T>();
    }
}
