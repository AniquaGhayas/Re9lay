using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;

public class SetupAppIconAndBranding
{
    [MenuItem("Tools/Re9lay Setup Branding and Icons")]
    public static void ExecuteSetup()
    {
        Debug.Log("[Re9lay] Setting up App Icons and Main Menu Logo...");

        // 1. Configure Texture Importer for icon_logo_square and Resources/icon_logo
        ConfigureTexture("Assets/Sprites/icon_logo_square.png");
        ConfigureTexture("Assets/Resources/icon_logo.png");
        ConfigureTexture("Assets/Resources/main_menu_logo.png");
        ConfigureTexture("Assets/Sprites/main_menu_logo.png");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Load Icon Texture
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/icon_logo.png");
        if (icon == null) icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/icon_logo_square.png");

        if (icon != null)
        {
            // Default (Unknown) target group icons
            int[] defaultSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Unknown);
            int defCount = defaultSizes.Length > 0 ? defaultSizes.Length : 1;
            Texture2D[] defaultIcons = new Texture2D[defCount];
            for (int i = 0; i < defCount; i++) defaultIcons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, defaultIcons);

            // Android standard icon sizes (e.g. 192, 144, 96, 72, 48, 36)
            int[] androidSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
            int andCount = androidSizes.Length > 0 ? androidSizes.Length : 1;
            Texture2D[] androidIcons = new Texture2D[andCount];
            for (int i = 0; i < andCount; i++) androidIcons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, androidIcons);

            AssetDatabase.SaveAssets();
            Debug.Log("[Re9lay] Successfully assigned App Icon for Android and Default!");
        }
        else
        {
            Debug.LogError("[Re9lay] Failed to load icon texture!");
        }

        // 3. Wire mainMenuLogo into Level1.unity GUI component
        Texture2D menuLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/main_menu_logo.png");
        if (menuLogo == null) menuLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/main_menu_logo.png");

        if (menuLogo != null)
        {
            string scenePath = "Assets/Level1.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scene.IsValid())
            {
                GUI guiScript = UnityEngine.Object.FindObjectOfType<GUI>();
                if (guiScript != null)
                {
                    guiScript.mainMenuLogo = menuLogo;
                    EditorUtility.SetDirty(guiScript);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[Re9lay] Successfully assigned mainMenuLogo to GUI in Level1.unity and saved scene!");
                }
                else
                {
                    Debug.LogWarning("[Re9lay] GUI script not found in Level1.unity!");
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Re9lay] SetupAppIconAndBranding finished successfully!");
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ti.textureType = TextureImporterType.Default;
        ti.isReadable = true;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
    }
}
