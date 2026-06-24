#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(MPBGeneratorList))]
public class MPBGeneratorListEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MPBGeneratorList script = (MPBGeneratorList)target;
        Renderer renderer = script.GetTargetRenderer();

        if (renderer == null)
        {
            EditorGUILayout.HelpBox("No Renderer found.", MessageType.Error);
            return;
        }

        DrawToolbar(script, renderer);

        EditorGUILayout.Space();

        if (script.Slots == null)
            script.Slots = new List<MPBSlot>();

        for (int i = 0; i < script.Slots.Count; i++)
        {
            DrawSlot(script, renderer, script.Slots[i], i);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(script);
            script.ApplyAll();
        }
    }

    private static void DrawToolbar(MPBGeneratorList script, Renderer renderer)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add MPB Slot"))
            {
                Undo.RecordObject(script, "Add MPB Slot");

                int materialIndex = 0;
                Material material = null;

                if (renderer.sharedMaterials.Length > 0)
                    material = renderer.sharedMaterials[0];

                script.Slots.Add(new MPBSlot
                {
                    Name = "MPB Slot",
                    MaterialIndex = materialIndex,
                    SourceMaterial = material,
                    IncludeHiddenProperties = false
                });

                EditorUtility.SetDirty(script);
            }

            if (GUILayout.Button("Apply All"))
            {
                Undo.RecordObject(script, "Apply All MPB");
                script.ApplyAll();
                EditorUtility.SetDirty(script);
            }

            if (GUILayout.Button("Clear All"))
            {
                Undo.RecordObject(script, "Clear All MPB");
                script.ClearAll();
                EditorUtility.SetDirty(script);
            }
        }
    }

    private static void DrawSlot(
        MPBGeneratorList script,
        Renderer renderer,
        MPBSlot slot,
        int slotListIndex)
    {
        if (slot == null)
            return;

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                slot.Name = EditorGUILayout.TextField(
                    string.IsNullOrWhiteSpace(slot.Name) ? $"MPB Slot {slotListIndex}" : slot.Name
                );

                if (GUILayout.Button("\u25B2", GUILayout.Width(25)) && slotListIndex > 0)
                {
                    SwapSlots(script, slotListIndex, slotListIndex - 1);
                    return;
                }

                if (GUILayout.Button("\u25BC", GUILayout.Width(25)) && slotListIndex < script.Slots.Count - 1)
                {
                    SwapSlots(script, slotListIndex, slotListIndex + 1);
                    return;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    Undo.RecordObject(script, "Remove MPB Slot");
                    script.Slots.RemoveAt(slotListIndex);
                    EditorUtility.SetDirty(script);
                    return;
                }
            }

            DrawMaterialDropdown(renderer, slot);
            AutoAssignShaderGraphAsset(slot, false);

            EditorGUILayout.ObjectField(
                "Shader Graph",
                slot.ShaderGraphAsset,
                typeof(Object),
                false
            );

            slot.IncludeHiddenProperties = EditorGUILayout.Toggle(
                "Include Hidden Properties",
                slot.IncludeHiddenProperties
            );

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh/Generate This Slot"))
                {
                    Undo.RecordObject(script, "Generate MPB Slot");
                    AutoAssignShaderGraphAsset(slot, true);
                    script.GenerateSlot(slot);
                    EditorUtility.SetDirty(script);
                }

                if (GUILayout.Button("Apply This Slot"))
                {
                    Undo.RecordObject(script, "Apply MPB Slot");
                    script.ApplySlot(slot);
                    EditorUtility.SetDirty(script);
                }

                if (GUILayout.Button("Clear This Slot"))
                {
                    Undo.RecordObject(script, "Clear MPB Slot");
                    script.ClearSlot(slot);
                    EditorUtility.SetDirty(script);
                }
            }
            if (GUILayout.Button("Debug Categories"))
            {
                DebugGeneratedCategories(script.Slots[slotListIndex]);
            }

            DrawGeneratedProperties(slot);
        }
    }

    private static void DebugGeneratedCategories(MPBSlot script)
    {
        if (script.Properties == null)
        {
            Debug.Log("[MPBGenerator] Properties list is null.");
            return;
        }

        Debug.Log($"[MPBGenerator] Properties count: {script.Properties.Count}");

        foreach (ShaderPropertyData property in script.Properties)
        {
            if (property == null)
                continue;

            Debug.Log(
                $"[MPBGenerator] Property: {property.Name} | Display: {property.DisplayName} | Category: {property.Category} | Type: {property.Type}"
            );
        }
    }

    private static void DrawMaterialDropdown(
        Renderer renderer,
        MPBSlot slot)
    {
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

        int newIndex = EditorGUILayout.Popup(
            "Material Slot",
            Mathf.Clamp(slot.MaterialIndex, 0, materials.Length - 1),
            options
        );

        if (newIndex != slot.MaterialIndex)
        {
            slot.MaterialIndex = newIndex;
            slot.SourceMaterial = materials[newIndex];
            slot.Properties.Clear();
            AutoAssignShaderGraphAsset(slot, false);
        }

        slot.SourceMaterial = materials[slot.MaterialIndex];
    }

    private static void AutoAssignShaderGraphAsset(
        MPBSlot slot,
        bool verbose)
    {
        if (slot == null || slot.SourceMaterial == null)
            return;

        Shader shader = slot.SourceMaterial.shader;

        if (shader == null)
            return;

        string shaderPath = AssetDatabase.GetAssetPath(shader);

        if (string.IsNullOrEmpty(shaderPath) || !shaderPath.EndsWith(".shadergraph"))
        {
            if (verbose)
                Debug.LogWarning($"[MPBGeneratorList] Shader is not a .shadergraph: {shader.name}");

            return;
        }

        Object shaderGraphAsset = AssetDatabase.LoadMainAssetAtPath(shaderPath);

        if (shaderGraphAsset == null)
        {
            if (verbose)
                Debug.LogWarning($"[MPBGeneratorList] Failed to load Shader Graph: {shaderPath}");

            return;
        }

        slot.ShaderGraphAsset = shaderGraphAsset;
    }

    private static void DrawGeneratedProperties(MPBSlot slot)
    {
        if (slot.Properties == null || slot.Properties.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No properties generated for this slot.",
                MessageType.Info
            );

            return;
        }

        List<string> categories = GetCategories(slot);

        foreach (string category in categories)
            DrawCategory(slot, category);
    }

    private static List<string> GetCategories(MPBSlot slot)
    {
        List<string> categories = new();

        foreach (ShaderPropertyData property in slot.Properties)
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

    private static void DrawCategory(
        MPBSlot slot,
        string category)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(category, EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        foreach (ShaderPropertyData property in slot.Properties)
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

    private static void DrawProperty(
        ShaderPropertyData property)
    {
        string label = string.IsNullOrWhiteSpace(property.DisplayName)
            ? ObjectNames.NicifyVariableName(property.Name.TrimStart('_'))
            : property.DisplayName;

        switch (property.Type)
        {
            case ShaderPropertyType.Color:
                property.ColorValue = EditorGUILayout.ColorField(
                    label,
                    property.ColorValue
                );
                break;

            case ShaderPropertyType.Vector:
                property.VectorValue = EditorGUILayout.Vector4Field(
                    label,
                    property.VectorValue
                );
                break;

            case ShaderPropertyType.Float:
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

            case ShaderPropertyType.Range:
                property.FloatValue = EditorGUILayout.Slider(
                    label,
                    property.FloatValue,
                    property.RangeMin,
                    property.RangeMax
                );
                break;

            case ShaderPropertyType.Texture:
                property.TextureValue = (Texture)EditorGUILayout.ObjectField(
                    label,
                    property.TextureValue,
                    typeof(Texture),
                    false
                );
                break;

            case ShaderPropertyType.Int:
                property.IntValue = EditorGUILayout.IntField(
                    label,
                    property.IntValue
                );
                break;
        }
    }

    private static void SwapSlots(MPBGeneratorList script, int a, int b)
    {
        Undo.RecordObject(script, "Reorder MPB Slots");

        var temp = script.Slots[a];
        script.Slots[a] = script.Slots[b];
        script.Slots[b] = temp;

        EditorUtility.SetDirty(script);
    }
}
#endif