using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MPBSlot
{
    public string Name;
    public int MaterialIndex;
    public Material SourceMaterial;

#if UNITY_EDITOR
    public UnityEngine.Object ShaderGraphAsset;
#endif

    public bool IncludeHiddenProperties;
    public List<ShaderPropertyData> Properties = new();
}
