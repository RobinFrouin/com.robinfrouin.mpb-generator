using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RobinFrouin.MPBGenerator
{
    [Serializable]
    public class ShaderPropertyData
    {
        public string Name;
        public string DisplayName;
        public string Category;

        public ShaderPropertyType Type;
        public ShaderPropertyFlags Flags;

        public bool IsToggle;

        public float FloatValue;
        public float RangeMin;
        public float RangeMax;

        public Color ColorValue;
        public Vector4 VectorValue;
        public Texture TextureValue;
        public int IntValue;
    }
}

