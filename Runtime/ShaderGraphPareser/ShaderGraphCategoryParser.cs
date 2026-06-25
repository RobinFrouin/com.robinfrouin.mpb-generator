#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RobinFrouin.MPBGenerator
{
    public static class ShaderGraphCategoryParser
    {
        [Serializable]
        private class BaseData
        {
            public string m_Type;
        }

        [Serializable]
        private class IdRef
        {
            public string m_Id;
        }

        [Serializable]
        private class CategoryData
        {
            public string m_Type;
            public string m_ObjectId;
            public string m_Name;
            public List<IdRef> m_ChildObjectList;
        }

        [Serializable]
        private class PropertyData
        {
            public string m_Type;
            public string m_ObjectId;
            public string m_Name;
            public string m_DefaultReferenceName;
            public string m_OverrideReferenceName;
        }

        public static Dictionary<string, string> GetPropertyCategories(UnityEngine.Object shaderGraphAsset)
        {
            Dictionary<string, string> result = new();

            if (shaderGraphAsset == null)
                return result;

            string path = AssetDatabase.GetAssetPath(shaderGraphAsset);

            if (string.IsNullOrEmpty(path) || !path.EndsWith(".shadergraph"))
                return result;

            string text = File.ReadAllText(path);
            List<string> jsonObjects = SplitJsonObjects(text);

            Dictionary<string, string> objectIdToPropertyReference = new();
            List<CategoryData> categories = new();

            foreach (string json in jsonObjects)
            {
                BaseData baseData = JsonUtility.FromJson<BaseData>(json);

                if (baseData == null || string.IsNullOrEmpty(baseData.m_Type))
                    continue;

                if (baseData.m_Type == "UnityEditor.ShaderGraph.CategoryData")
                {
                    CategoryData category = JsonUtility.FromJson<CategoryData>(json);

                    if (category != null)
                        categories.Add(category);
                }
                else if (baseData.m_Type.Contains("ShaderProperty"))
                {
                    PropertyData property = JsonUtility.FromJson<PropertyData>(json);

                    if (property == null)
                        continue;

                    string referenceName = GetReferenceName(property);

                    if (!string.IsNullOrEmpty(property.m_ObjectId) &&
                        !string.IsNullOrEmpty(referenceName))
                    {
                        objectIdToPropertyReference[property.m_ObjectId] = referenceName;
                    }
                }
            }

            foreach (CategoryData category in categories)
            {
                if (category == null || category.m_ChildObjectList == null)
                    continue;

                string categoryName = string.IsNullOrWhiteSpace(category.m_Name)
                    ? "Other"
                    : category.m_Name;

                foreach (IdRef child in category.m_ChildObjectList)
                {
                    if (child == null || string.IsNullOrEmpty(child.m_Id))
                        continue;

                    if (objectIdToPropertyReference.TryGetValue(child.m_Id, out string propertyReference))
                        result[propertyReference] = categoryName;
                }
            }

            return result;
        }

        private static string GetReferenceName(PropertyData property)
        {
            if (!string.IsNullOrEmpty(property.m_OverrideReferenceName))
                return property.m_OverrideReferenceName;

            return property.m_DefaultReferenceName;
        }

        private static List<string> SplitJsonObjects(string text)
        {
            List<string> objects = new();

            int depth = 0;
            int startIndex = -1;
            bool inString = false;
            bool escaping = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inString)
                {
                    if (escaping)
                        escaping = false;
                    else if (c == '\\')
                        escaping = true;
                    else if (c == '"')
                        inString = false;

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    continue;
                }

                if (c == '{')
                {
                    if (depth == 0)
                        startIndex = i;

                    depth++;
                }
                else if (c == '}')
                {
                    depth--;

                    if (depth == 0 && startIndex >= 0)
                    {
                        objects.Add(text.Substring(startIndex, i - startIndex + 1));
                        startIndex = -1;
                    }
                }
            }

            return objects;
        }
    }
}

#endif