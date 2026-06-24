#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[CustomEditor(typeof(MPBGenerator))]
public class MPBGeneratorEditor : Editor
{
    private SerializedProperty _includeHiddenProperties;
    private SerializedProperty _shaderGraphAsset;

    private void OnEnable()
    {
        _includeHiddenProperties = serializedObject.FindProperty("IncludeHiddenProperties");
        _shaderGraphAsset = serializedObject.FindProperty("ShaderGraphAsset");
    }

    public override void OnInspectorGUI()
    {
        MPBGenerator script = (MPBGenerator)target;
        Renderer renderer = script.GetComponent<Renderer>();

        serializedObject.Update();

        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

        DrawMaterialDropdown(script, renderer);

        EditorGUILayout.PropertyField(_includeHiddenProperties);

        serializedObject.ApplyModifiedProperties();

        AutoAssignShaderGraphAsset(script, false);

        serializedObject.Update();
        EditorGUILayout.PropertyField(_shaderGraphAsset);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh/Generate MPB"))
            {
                Undo.RecordObject(script, "Generate MPB Properties");

                AutoAssignShaderGraphAsset(script, true);
                script.GeneratePropertiesFromSourceMaterial();

                EditorUtility.SetDirty(script);
            }

            if (GUILayout.Button("Apply MPB"))
            {
                Undo.RecordObject(script, "Apply MPB");

                script.ApplyPropertiesToMPB();

                EditorUtility.SetDirty(script);
            }
            if (GUILayout.Button("Clear MPB"))
            {
                Undo.RecordObject(script, "Clear MPB");

                script.ClearPropertyBlock();

                EditorUtility.SetDirty(script);
            }
        }



        if (GUILayout.Button("Debug Categories"))
        {
            DebugGeneratedCategories(script);
        }


        EditorGUILayout.Space();

        DrawGeneratedProperties(script);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(script);
            script.ApplyPropertiesToMPB();
        }
    }

    private static void DrawMaterialDropdown(MPBGenerator script, Renderer renderer)
    {
        if (renderer == null)
        {
            EditorGUILayout.HelpBox("No Renderer found on this GameObject.", MessageType.Error);
            return;
        }

        Material[] materials = renderer.sharedMaterials;

        if (materials == null || materials.Length == 0)
        {
            EditorGUILayout.HelpBox("Renderer has no materials.", MessageType.Warning);
            return;
        }

        string[] options = new string[materials.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            string materialName = materials[i] != null ? materials[i].name : "None";
            options[i] = $"Element {i} - {materialName}";
        }

        int clampedIndex = Mathf.Clamp(script.MaterialIndex, 0, materials.Length - 1);

        if (clampedIndex != script.MaterialIndex)
        {
            Undo.RecordObject(script, "Clamp Material Index");

            script.MaterialIndex = clampedIndex;
            script.SourceMaterial = materials[clampedIndex];
            script.Properties.Clear();

            EditorUtility.SetDirty(script);
        }

        int newIndex = EditorGUILayout.Popup(
            "Material Slot",
            script.MaterialIndex,
            options
        );

        if (newIndex != script.MaterialIndex)
        {
            Undo.RecordObject(script, "Change Material Slot");

            script.MaterialIndex = newIndex;
            script.SourceMaterial = materials[newIndex];
            script.Properties.Clear();

            AutoAssignShaderGraphAsset(script, false);

            EditorUtility.SetDirty(script);
        }

        if (script.SourceMaterial != materials[script.MaterialIndex])
        {
            Undo.RecordObject(script, "Sync Source Material");

            script.SourceMaterial = materials[script.MaterialIndex];
            script.Properties.Clear();

            AutoAssignShaderGraphAsset(script, false);

            EditorUtility.SetDirty(script);
        }

        EditorGUI.BeginDisabledGroup(true);

        EditorGUILayout.ObjectField(
            "Source Material",
            script.SourceMaterial,
            typeof(Material),
            false
        );

        EditorGUI.EndDisabledGroup();
    }

    private static void AutoAssignShaderGraphAsset(MPBGenerator script, bool verbose)
    {
        if (script == null)
        {
            if (verbose)
                Debug.LogWarning("[MPBGenerator] Script is null.");

            return;
        }

        if (script.SourceMaterial == null)
        {
            if (verbose)
                Debug.LogWarning("[MPBGenerator] SourceMaterial is null.");

            return;
        }

        Shader shader = script.SourceMaterial.shader;

        if (shader == null)
        {
            if (verbose)
                Debug.LogWarning($"[MPBGenerator] Material '{script.SourceMaterial.name}' has no shader.");

            return;
        }

        if (verbose)
            Debug.Log($"[MPBGenerator] Shader = {shader.name}");

        string shaderPath = AssetDatabase.GetAssetPath(shader);

        if (string.IsNullOrEmpty(shaderPath))
        {
            if (verbose)
                Debug.LogWarning($"[MPBGenerator] No AssetDatabase path found for shader '{shader.name}'.");

            return;
        }

        if (verbose)
            Debug.Log($"[MPBGenerator] Shader Path = {shaderPath}");

        if (!shaderPath.EndsWith(".shadergraph"))
        {
            if (verbose)
                Debug.LogWarning($"[MPBGenerator] Shader path is not a .shadergraph file: {shaderPath}");

            return;
        }

        Object shaderGraphAsset = AssetDatabase.LoadMainAssetAtPath(shaderPath);

        if (shaderGraphAsset == null)
        {
            if (verbose)
                Debug.LogWarning($"[MPBGenerator] Failed to load shadergraph asset from '{shaderPath}'.");

            return;
        }

        if (verbose)
            Debug.Log($"[MPBGenerator] ShaderGraph found: {shaderGraphAsset.name}");

        if (script.ShaderGraphAsset == shaderGraphAsset)
        {
            if (verbose)
                Debug.Log($"[MPBGenerator] ShaderGraph already assigned: {shaderGraphAsset.name}");

            return;
        }

        Undo.RecordObject(script, "Auto Assign Shader Graph Asset");

        script.ShaderGraphAsset = shaderGraphAsset;

        EditorUtility.SetDirty(script);

        if (verbose)
            Debug.Log($"[MPBGenerator] Assigned ShaderGraphAsset: {shaderGraphAsset.name}");
    }

    private static void DrawGeneratedProperties(MPBGenerator script)
    {
        if (script.Properties == null || script.Properties.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No generated properties. Select a material slot, then click Refresh/Generate From Material.",
                MessageType.Info
            );

            return;
        }

        List<string> categories = GetCategories(script);

        foreach (string category in categories)
        {
            DrawCategory(category, script);
        }
    }

    private static List<string> GetCategories(MPBGenerator script)
    {
        List<string> categories = new List<string>();

        foreach (MPBGenerator.ShaderPropertyData property in script.Properties)
        {
            if (property == null)
                continue;

            string category = string.IsNullOrWhiteSpace(property.Category)
                ? "Other"
                : property.Category;

            if (!categories.Contains(category))
                categories.Add(category);
        }

        return categories;
    }

    private static void DrawCategory(string category, MPBGenerator script)
    {
        bool hasProperty = false;

        foreach (MPBGenerator.ShaderPropertyData property in script.Properties)
        {
            if (property == null)
                continue;

            string propertyCategory = string.IsNullOrWhiteSpace(property.Category)
                ? "Other"
                : property.Category;

            if (propertyCategory == category)
            {
                hasProperty = true;
                break;
            }
        }

        if (!hasProperty)
            return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(category, EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        foreach (MPBGenerator.ShaderPropertyData property in script.Properties)
        {
            if (property == null)
                continue;

            string propertyCategory = string.IsNullOrWhiteSpace(property.Category)
                ? "Other"
                : property.Category;

            if (propertyCategory != category)
                continue;

            DrawProperty(property);
        }

        EditorGUI.indentLevel--;
    }

    private static void DrawProperty(MPBGenerator.ShaderPropertyData property)
    {
        string label = string.IsNullOrWhiteSpace(property.DisplayName)
            ? ObjectNames.NicifyVariableName(property.Name.TrimStart('_'))
            : property.DisplayName;

        switch (property.Type)
        {
            case ShaderPropertyType.Color:
                {
                    bool hdr = property.Flags.HasFlag(ShaderPropertyFlags.HDR);

                    property.ColorValue = EditorGUILayout.ColorField(
                        new GUIContent(label),
                        property.ColorValue,
                        true,
                        true,
                        hdr
                    );

                    break;
                }

            case ShaderPropertyType.Vector:
                {
                    property.VectorValue = EditorGUILayout.Vector4Field(
                        label,
                        property.VectorValue
                    );

                    break;
                }

            case ShaderPropertyType.Float:
                {
                    if (property.IsToggle)
                    {
                        bool value = property.FloatValue > 0.5f;
                        value = EditorGUILayout.Toggle(label, value);
                        property.FloatValue = value ? 1f : 0f;
                    }
                    else
                    {
                        property.FloatValue = EditorGUILayout.FloatField(
                            label,
                            property.FloatValue
                        );
                    }

                    break;
                }

            case ShaderPropertyType.Range:
                {
                    property.FloatValue = EditorGUILayout.Slider(
                        label,
                        property.FloatValue,
                        property.RangeMin,
                        property.RangeMax
                    );

                    break;
                }

            case ShaderPropertyType.Texture:
                {
                    property.TextureValue = (Texture)EditorGUILayout.ObjectField(
                        label,
                        property.TextureValue,
                        typeof(Texture),
                        false
                    );

                    break;
                }

            case ShaderPropertyType.Int:
                {
                    property.IntValue = EditorGUILayout.IntField(
                        label,
                        property.IntValue
                    );

                    break;
                }
        }
    }

    private static void DebugGeneratedCategories(MPBGenerator script)
    {
        if (script.Properties == null)
        {
            Debug.Log("[MPBGenerator] Properties list is null.");
            return;
        }

        Debug.Log($"[MPBGenerator] Properties count: {script.Properties.Count}");

        foreach (MPBGenerator.ShaderPropertyData property in script.Properties)
        {
            if (property == null)
                continue;

            Debug.Log(
                $"[MPBGenerator] Property: {property.Name} | Display: {property.DisplayName} | Category: {property.Category} | Type: {property.Type}"
            );
        }
    }
}
#endif