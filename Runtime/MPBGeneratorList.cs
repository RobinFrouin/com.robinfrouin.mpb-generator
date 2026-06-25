using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace RobinFrouin.MPBGenerator
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class MPBGeneratorList : MonoBehaviour
    {
        [Header("Slots")]
        public List<MPBSlot> Slots = new();

        private Renderer _renderer;

        private void OnEnable()
        {
            Init();
            ApplyAll();
        }

        private void OnValidate()
        {
            Init();
            ApplyAll();
        }

        private void Init()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
        }

        public Renderer GetTargetRenderer()
        {
            Init();
            return _renderer;
        }

        public void GenerateSlot(MPBSlot slot)
        {
            Init();

            if (slot == null || slot.SourceMaterial == null)
                return;

            Dictionary<string, string> categories = new();

#if UNITY_EDITOR
            categories = ShaderGraphCategoryParser.GetPropertyCategories(slot.ShaderGraphAsset);
#endif

            slot.Properties.Clear();

            Shader shader = slot.SourceMaterial.shader;
            int propertyCount = shader.GetPropertyCount();

            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = shader.GetPropertyName(i);
                ShaderPropertyType type = shader.GetPropertyType(i);
                ShaderPropertyFlags flags = shader.GetPropertyFlags(i);

                if (!slot.IncludeHiddenProperties &&
                    flags.HasFlag(ShaderPropertyFlags.HideInInspector))
                    continue;

                if (!slot.SourceMaterial.HasProperty(propertyName))
                    continue;

                string category = "Other";

                if (categories.TryGetValue(propertyName, out string parsedCategory))
                    category = parsedCategory;

                ShaderPropertyData data = new ShaderPropertyData
                {
                    Name = propertyName,
                    DisplayName = shader.GetPropertyDescription(i),
                    Category = category,
                    Type = type,
                    Flags = flags,
                    IsToggle = IsToggleProperty(shader, i)
                };

                switch (type)
                {
                    case ShaderPropertyType.Color:
                        data.ColorValue = slot.SourceMaterial.GetColor(propertyName);
                        break;

                    case ShaderPropertyType.Vector:
                        data.VectorValue = slot.SourceMaterial.GetVector(propertyName);
                        break;

                    case ShaderPropertyType.Float:
                        data.FloatValue = slot.SourceMaterial.GetFloat(propertyName);
                        break;

                    case ShaderPropertyType.Range:
                        data.FloatValue = slot.SourceMaterial.GetFloat(propertyName);
                        Vector2 limits = shader.GetPropertyRangeLimits(i);
                        data.RangeMin = limits.x;
                        data.RangeMax = limits.y;
                        break;

                    case ShaderPropertyType.Texture:
                        data.TextureValue = slot.SourceMaterial.GetTexture(propertyName);
                        break;

                    case ShaderPropertyType.Int:
                        data.IntValue = slot.SourceMaterial.GetInt(propertyName);
                        break;
                }

                slot.Properties.Add(data);
            }

            ApplySlot(slot);
        }

        public void ApplySlot(MPBSlot slot)
        {
            Init();

            if (_renderer == null || slot == null)
                return;

            Material[] materials = _renderer.sharedMaterials;

            if (slot.MaterialIndex < 0 || slot.MaterialIndex >= materials.Length)
                return;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();

            foreach (ShaderPropertyData property in slot.Properties)
            {
                if (property == null || string.IsNullOrEmpty(property.Name))
                    continue;

                switch (property.Type)
                {
                    case ShaderPropertyType.Color:
                        mpb.SetColor(property.Name, property.ColorValue);
                        break;

                    case ShaderPropertyType.Vector:
                        mpb.SetVector(property.Name, property.VectorValue);
                        break;

                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        mpb.SetFloat(property.Name, property.FloatValue);
                        break;

                    case ShaderPropertyType.Texture:
                        if (property.TextureValue != null)
                            mpb.SetTexture(property.Name, property.TextureValue);
                        break;

                    case ShaderPropertyType.Int:
                        mpb.SetInt(property.Name, property.IntValue);
                        break;
                }
            }

            _renderer.SetPropertyBlock(mpb, slot.MaterialIndex);
        }

        public void ApplyAll()
        {
            if (Slots == null)
                return;

            foreach (MPBSlot slot in Slots)
                ApplySlot(slot);
        }

        public void ClearSlot(MPBSlot slot)
        {
            Init();

            if (_renderer == null || slot == null)
                return;

            _renderer.SetPropertyBlock(null, slot.MaterialIndex);
        }

        public void ClearAll()
        {
            Init();

            if (_renderer == null)
                return;

            for (int i = 0; i < _renderer.sharedMaterials.Length; i++)
                _renderer.SetPropertyBlock(null, i);
        }

        private static bool IsToggleProperty(Shader shader, int propertyIndex)
        {
            string propertyName = shader.GetPropertyName(propertyIndex).ToLowerInvariant();
            string displayName = shader.GetPropertyDescription(propertyIndex).ToLowerInvariant();

            if (propertyName.Contains("toggle") ||
                propertyName.Contains("bool") ||
                displayName.Contains("toggle") ||
                displayName.Contains("bool"))
                return true;

            string[] attributes = shader.GetPropertyAttributes(propertyIndex);

            foreach (string attribute in attributes)
            {
                if (attribute.Contains("Toggle"))
                    return true;
            }

            return false;
        }
    }
}
