using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace RobinFrouin.MPBGenerator
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class MPBGenerator : MonoBehaviour
    {
        public int MaterialIndex = 0;
        [Header("Source")]
        public Material SourceMaterial;
        public bool IncludeHiddenProperties = false;

        #if UNITY_EDITOR
        [Header("Shader Graph")]
        public UnityEngine.Object ShaderGraphAsset;
        #endif

        [Header("Generated Properties")]
        public List<ShaderPropertyData> Properties = new();

        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;

        private void OnEnable()
        {
            Init();
            ApplyPropertiesToMPB();
        }

        private void OnValidate()
        {
            Init();
            ApplyPropertiesToMPB();
        }

        private void Init()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            _mpb ??= new MaterialPropertyBlock();

            if (SourceMaterial != null && _renderer != null)
                _renderer.sharedMaterial = SourceMaterial;
        }

        [ContextMenu("Generate Properties From Source Material")]
        public void GeneratePropertiesFromSourceMaterial()
        {
            Init();

            if (SourceMaterial == null)
            {
                Debug.LogWarning("[MPBGenerator] No SourceMaterial assigned.", this);
                return;
            }

            Dictionary<string, string> shaderGraphCategories = new();

#if UNITY_EDITOR
            shaderGraphCategories =
                ShaderGraphCategoryParser.GetPropertyCategories(ShaderGraphAsset);
#endif

            Properties.Clear();

            Shader shader = SourceMaterial.shader;
            int propertyCount = shader.GetPropertyCount();

            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = shader.GetPropertyName(i);
                ShaderPropertyType propertyType = shader.GetPropertyType(i);
                ShaderPropertyFlags flags = shader.GetPropertyFlags(i);

                if (!IncludeHiddenProperties && flags.HasFlag(ShaderPropertyFlags.HideInInspector))
                    continue;

                if (!SourceMaterial.HasProperty(propertyName))
                    continue;

                string category = "Other";

                if (shaderGraphCategories.TryGetValue(propertyName, out string parsedCategory))
                    category = parsedCategory;

                ShaderPropertyData data = new ShaderPropertyData
                {
                    Name = propertyName,
                    DisplayName = shader.GetPropertyDescription(i),
                    Category = category,
                    Type = propertyType,
                    Flags = flags,
                    IsToggle = IsToggleProperty(shader, i)
                };

                switch (propertyType)
                {
                    case ShaderPropertyType.Color:
                        data.ColorValue = SourceMaterial.GetColor(propertyName);
                        break;

                    case ShaderPropertyType.Vector:
                        data.VectorValue = SourceMaterial.GetVector(propertyName);
                        break;

                    case ShaderPropertyType.Float:
                        data.FloatValue = SourceMaterial.GetFloat(propertyName);
                        break;

                    case ShaderPropertyType.Range:
                        data.FloatValue = SourceMaterial.GetFloat(propertyName);
                        Vector2 limits = shader.GetPropertyRangeLimits(i);
                        data.RangeMin = limits.x;
                        data.RangeMax = limits.y;
                        break;

                    case ShaderPropertyType.Texture:
                        data.TextureValue = SourceMaterial.GetTexture(propertyName);
                        break;

                    case ShaderPropertyType.Int:
                        data.IntValue = SourceMaterial.GetInt(propertyName);
                        break;
                }

                Properties.Add(data);
            }

            ApplyPropertiesToMPB();
        }

        [ContextMenu("Apply Properties To MPB")]
        public void ApplyPropertiesToMPB()
        {
            Init();

            if (_renderer == null)
                return;

            _renderer.GetPropertyBlock(_mpb);

            foreach (ShaderPropertyData property in Properties)
            {
                if (property == null || string.IsNullOrEmpty(property.Name))
                    continue;

                switch (property.Type)
                {
                    case ShaderPropertyType.Color:
                        _mpb.SetColor(property.Name, property.ColorValue);
                        break;

                    case ShaderPropertyType.Vector:
                        _mpb.SetVector(property.Name, property.VectorValue);
                        break;

                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        _mpb.SetFloat(property.Name, property.FloatValue);
                        break;

                    case ShaderPropertyType.Texture:
                        if (property.TextureValue != null)
                            _mpb.SetTexture(property.Name, property.TextureValue);
                        break;

                    case ShaderPropertyType.Int:
                        _mpb.SetInt(property.Name, property.IntValue);
                        break;
                }
            }

            _renderer.SetPropertyBlock(_mpb);
        }

        [ContextMenu("Clear Property Block")]
        public void ClearPropertyBlock()
        {
            Init();

            if (_renderer != null)
                _renderer.SetPropertyBlock(null);
        }

        private static bool IsToggleProperty(Shader shader, int propertyIndex)
        {
            string propertyName = shader.GetPropertyName(propertyIndex);
            string displayName = shader.GetPropertyDescription(propertyIndex);

            string lowerName = propertyName.ToLowerInvariant();
            string lowerDisplay = displayName.ToLowerInvariant();

            if (lowerName.Contains("toggle") ||
                lowerName.Contains("bool") ||
                lowerDisplay.Contains("toggle") ||
                lowerDisplay.Contains("bool"))
            {
                return true;
            }

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



