using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MinimapRendererFix
{
    const string PCRPAssetPath = "Assets/Settings/PC_RPAsset.asset";
    const string SourceRendererPath = "Assets/Settings/PC_Renderer.asset";
    const string MinimapRendererPath = "Assets/Settings/Minimap_Renderer.asset";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string MinimapTag = "MinimapCamera";

    [MenuItem("Tools/Undead Courier/Fix Minimap Renderer")]
    public static void Fix()
    {
        int index = EnsureDecalFreeRenderer();
        if (index < 0)
        {
            Debug.LogError("[MinimapRendererFix] failed to register decal-free renderer");
            return;
        }

        AssignRendererToMinimapCamera(index);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MinimapRendererFix] done. minimapRendererIndex={index}");
    }

    static int EnsureDecalFreeRenderer()
    {
        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PCRPAssetPath);
        if (rp == null)
        {
            Debug.LogError("[MinimapRendererFix] rp asset not found: " + PCRPAssetPath);
            return -1;
        }

        var minimapRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MinimapRendererPath);
        if (minimapRenderer == null)
        {
            minimapRenderer = BuildDecalFreeRenderer();
            if (minimapRenderer == null) return -1;
        }

        var so = new SerializedObject(rp);
        var listProp = so.FindProperty("m_RendererDataList");

        for (int i = 0; i < listProp.arraySize; i++)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == minimapRenderer)
                return i;
        }

        int newIndex = listProp.arraySize;
        listProp.arraySize += 1;
        listProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = minimapRenderer;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(rp);
        return newIndex;
    }

    static ScriptableRendererData BuildDecalFreeRenderer()
    {
        if (!AssetDatabase.CopyAsset(SourceRendererPath, MinimapRendererPath))
        {
            Debug.LogError("[MinimapRendererFix] could not copy source renderer");
            return null;
        }
        AssetDatabase.ImportAsset(MinimapRendererPath, ImportAssetOptions.ForceUpdate);

        var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MinimapRendererPath);
        if (data == null) return null;

        var toRemove = new List<ScriptableRendererFeature>();
        foreach (var f in data.rendererFeatures)
        {
            if (f is DecalRendererFeature) toRemove.Add(f);
        }

        var so = new SerializedObject(data);
        var featuresProp = so.FindProperty("m_RendererFeatures");
        var mapProp = so.FindProperty("m_RendererFeatureMap");

        for (int i = featuresProp.arraySize - 1; i >= 0; i--)
        {
            var obj = featuresProp.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
            if (obj is DecalRendererFeature)
            {
                featuresProp.DeleteArrayElementAtIndex(i);
                if (i < mapProp.arraySize) mapProp.DeleteArrayElementAtIndex(i);
            }
        }
        so.ApplyModifiedProperties();

        foreach (var f in toRemove)
        {
            if (f != null) Object.DestroyImmediate(f, true);
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(MinimapRendererPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MinimapRendererPath);
    }

    static void AssignRendererToMinimapCamera(int index)
    {
        Scene active = SceneManager.GetActiveScene();
        bool opened = false;
        if (!active.IsValid() || active.path != ScenePath)
        {
            active = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            opened = true;
        }

        GameObject[] tagged = GameObject.FindGameObjectsWithTag(MinimapTag);
        int applied = 0;
        foreach (var go in tagged)
        {
            var cam = go.GetComponent<Camera>();
            if (cam == null) continue;
            var data = cam.GetUniversalAdditionalCameraData();
            data.SetRenderer(index);
            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(data);
            applied++;
        }

        if (applied == 0)
        {
            Debug.LogError("[MinimapRendererFix] no MinimapCamera found in scene");
            return;
        }

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);
        if (opened) { }
        Debug.Log($"[MinimapRendererFix] assigned renderer index {index} to {applied} minimap camera(s)");
    }
}
