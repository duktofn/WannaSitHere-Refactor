using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class HyperCasualFXMaterialConverter
    {
        private static readonly string[] MaterialPaths = new[]
        {
            "Assets/Lana Studio/Hyper Casual FX/Materials/Circles_AB.mat",
            "Assets/Lana Studio/Hyper Casual FX/Materials/Circles_Additive.mat",
            "Assets/Lana Studio/Hyper Casual FX/Materials/Drop_AB.mat",
            "Assets/Lana Studio/Hyper Casual FX/Materials/Square.mat"
        };

        static HyperCasualFXMaterialConverter()
        {
            EditorApplication.delayCall += ConvertAll;
        }

        [MenuItem("Tools/VFX/Convert Hyper Casual FX Materials to URP")]
        public static void ConvertAll()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpShader == null)
            {
                Debug.LogError("[HyperCasualFX] Cannot find shader 'Universal Render Pipeline/Particles/Unlit'!");
                return;
            }

            int convertedCount = 0;
            foreach (var path in MaterialPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    Debug.LogWarning($"[HyperCasualFX] Material not found at {path}");
                    continue;
                }

                if (mat.shader == urpShader)
                {
                    // Check if properties are already properly configured
                    if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                    {
                        continue;
                    }
                }

                // 1. Snapshot source properties
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : (mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null);
                Vector2 mainScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : (mat.HasProperty("_BaseMap") ? mat.GetTextureScale("_BaseMap") : Vector2.one);
                Vector2 mainOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : (mat.HasProperty("_BaseMap") ? mat.GetTextureOffset("_BaseMap") : Vector2.zero);
                bool isAdditive = path.Contains("Additive") || (mat.shader != null && mat.shader.name.Contains("Additive"));

                // 2. Assign URP Shader
                mat.shader = urpShader;

                // 3. Restore BaseMap
                if (mainTex != null && mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", mainTex);
                    mat.SetTextureScale("_BaseMap", mainScale);
                    mat.SetTextureOffset("_BaseMap", mainOffset);
                }

                // 4. Set BaseColor
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", Color.white);
                }

                // 5. Surface Type: Transparent
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);

                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;

                // 6. Blending Options
                if (isAdditive)
                {
                    // Additive Blending (SrcAlpha, One)
                    if (mat.HasProperty("_Blend"))
                        mat.SetFloat("_Blend", 2f); // BlendMode.Additive

                    mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)BlendMode.One);
                    mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    mat.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
                    mat.SetFloat("_ZWrite", 0f);

                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.DisableKeyword("_ALPHAMODULATE_ON");
                }
                else
                {
                    // Alpha Blending (SrcAlpha, OneMinusSrcAlpha)
                    if (mat.HasProperty("_Blend"))
                        mat.SetFloat("_Blend", 0f); // BlendMode.Alpha

                    mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0f);

                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.DisableKeyword("_ALPHAMODULATE_ON");
                }

                EditorUtility.SetDirty(mat);
                convertedCount++;
                Debug.Log($"[HyperCasualFX] Converted {mat.name} to URP Particles/Unlit (Additive: {isAdditive})");
            }

            if (convertedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[HyperCasualFX] Successfully converted {convertedCount} materials to URP!");
            }
        }
    }
}

