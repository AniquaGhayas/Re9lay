using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SpriteMigrationTool
{
    [MenuItem("Tools/Run Sprite Migration")]
    public static void RunMigration()
    {
        try
        {
            Debug.Log("[SpriteMigrationTool] Starting sprite migration...");
            string reportPath = "migration_verification_report.txt";
            using (StreamWriter sw = new StreamWriter(reportPath, false))
            {
                sw.WriteLine("=== SPRITE MIGRATION VERIFICATION REPORT ===");
                sw.WriteLine($"Executed at: {DateTime.Now}");

                // Step 1: Configure Textures & Slices
                sw.WriteLine("\n--- STEP 1: CONFIGURE TEXTURE IMPORTERS & SLICES ---");
                ConfigureSpaceship(sw);
                ConfigureIdleUFO(sw);
                ConfigureExplosionUFO(sw);
                ConfigureFireshot(sw);
                ConfigureSpaceBackground(sw);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Step 2: Create Explosion Animation & Prefab
                sw.WriteLine("\n--- STEP 2: CREATE EXPLOSION ANIMATION & PREFAB ---");
                GameObject explosionPrefab = SetupExplosion(sw);

                // Step 3: Update Player & Enemy Animation Clips
                sw.WriteLine("\n--- STEP 3: UPDATE ANIMATION CLIPS ---");
                UpdatePlayerAnim(sw);
                UpdateEnemyAnim(sw);

                // Step 4: Update Prefabs (player_bullet & Enemy)
                sw.WriteLine("\n--- STEP 4: UPDATE PREFABS ---");
                UpdatePlayerBulletPrefab(sw);
                UpdateEnemyPrefab(sw, explosionPrefab);

                // Step 5: Update Scene (Level1.unity)
                sw.WriteLine("\n--- STEP 5: UPDATE SCENE ASSETS ---");
                UpdateScene(sw);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                sw.WriteLine("\n--- MIGRATION COMPLETED SUCCESSFULLY ---");
            }
            Debug.Log("[SpriteMigrationTool] Sprite migration completed successfully! See migration_verification_report.txt");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpriteMigrationTool] Migration failed: {ex}");
        }
    }

    [MenuItem("Tools/Setup App Icon")]
    public static void SetupAppIcon()
    {
        string iconPath = "Assets/Sprites/icon_logo.png";
        TextureImporter ti = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.isReadable = true;
            ti.alphaIsTransparency = true;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
        }

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon != null)
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { icon });
            AssetDatabase.SaveAssets();
            Debug.Log("[SpriteMigrationTool] Successfully assigned icon_logo.png as Android & Default App Icon!");
        }
        else
        {
            Debug.LogError("[SpriteMigrationTool] Could not load icon at " + iconPath);
        }
    }

    static void ConfigureTextureBasic(TextureImporter ti, SpriteImportMode mode)
    {
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = mode;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.spritePixelsPerUnit = 100f;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
    }

    static void ConfigureSpaceship(StreamWriter sw)
    {
        string path = "Assets/Sprites/spaceship.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ConfigureTextureBasic(ti, SpriteImportMode.Multiple);

        SpriteMetaData[] meta = new SpriteMetaData[4];
        for (int i = 0; i < 4; i++)
        {
            meta[i] = new SpriteMetaData
            {
                name = $"spaceship_{i}",
                rect = new Rect(i * 64, 0, 64, 98),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        ti.spritesheet = meta;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
        sw.WriteLine($"Configured {path}: 4 frames sliced (64x98 each, Point filter, Uncompressed).");
    }

    static void ConfigureIdleUFO(StreamWriter sw)
    {
        string path = "Assets/Sprites/idleufo.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ConfigureTextureBasic(ti, SpriteImportMode.Multiple);

        SpriteMetaData[] meta = new SpriteMetaData[2];
        for (int i = 0; i < 2; i++)
        {
            meta[i] = new SpriteMetaData
            {
                name = $"idleufo_{i}",
                rect = new Rect(i * 64, 18, 64, 48),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        ti.spritesheet = meta;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
        sw.WriteLine($"Configured {path}: 2 frames sliced (64x48 each, Point filter, Uncompressed).");
    }

    static void ConfigureExplosionUFO(StreamWriter sw)
    {
        string path = "Assets/Sprites/explosionufo.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ConfigureTextureBasic(ti, SpriteImportMode.Multiple);

        SpriteMetaData[] meta = new SpriteMetaData[6];
        for (int i = 0; i < 6; i++)
        {
            meta[i] = new SpriteMetaData
            {
                name = $"explosionufo_{i}",
                rect = new Rect(i * 64, 2, 64, 64),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        ti.spritesheet = meta;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
        sw.WriteLine($"Configured {path}: 6 frames sliced (64x64 each, Point filter, Uncompressed).");
    }

    static void ConfigureFireshot(StreamWriter sw)
    {
        string path = "Assets/Sprites/fireshot.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ConfigureTextureBasic(ti, SpriteImportMode.Multiple);

        SpriteMetaData[] meta = new SpriteMetaData[6];
        for (int i = 0; i < 6; i++)
        {
            meta[i] = new SpriteMetaData
            {
                name = $"fireshot_{i}",
                rect = new Rect(i * 32, 0, 32, 34),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        ti.spritesheet = meta;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
        sw.WriteLine($"Configured {path}: 6 frames sliced (32x34 each, Point filter, Uncompressed).");
    }

    static void ConfigureSpaceBackground(StreamWriter sw)
    {
        string path = "Assets/Sprites/space_background.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;

        ConfigureTextureBasic(ti, SpriteImportMode.Single);
        ti.spritePivot = new Vector2(0.5f, 0.5f);
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
        sw.WriteLine($"Configured {path}: Single sprite (Point filter, Uncompressed).");
    }

    static GameObject SetupExplosion(StreamWriter sw)
    {
        string animPath = "Assets/Sprites/Explosion.anim";
        string ctrlPath = "Assets/Sprites/Explosion.controller";
        string prefabPath = "Assets/Prefab/Explosion.prefab";

        Sprite[] explosionSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/explosionufo.png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, animPath);
        }
        clip.frameRate = 12f;
        clip.wrapMode = WrapMode.Once;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[explosionSprites.Length];
        for (int i = 0; i < explosionSprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = explosionSprites[i]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        EditorUtility.SetDirty(clip);

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        }
        if (ctrl.layers.Length > 0 && ctrl.layers[0].stateMachine.states.Length == 0)
        {
            var state = ctrl.layers[0].stateMachine.AddState("Explode");
            state.motion = clip;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            GameObject tempGO = new GameObject("Explosion");
            var sr = tempGO.AddComponent<SpriteRenderer>();
            sr.sprite = explosionSprites.Length > 0 ? explosionSprites[0] : null;
            sr.sortingOrder = 5;

            var anim = tempGO.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            var autoDestroy = tempGO.AddComponent<AutoDestroyExplosion>();
            autoDestroy.lifetime = 0.5f;

            prefab = PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
            GameObject.DestroyImmediate(tempGO);
        }
        else
        {
            var tempGO = PrefabUtility.LoadPrefabContents(prefabPath);
            var sr = tempGO.GetComponent<SpriteRenderer>();
            if (sr != null && explosionSprites.Length > 0) sr.sprite = explosionSprites[0];
            sr.sortingOrder = 5;
            var anim = tempGO.GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = ctrl;
            if (tempGO.GetComponent<AutoDestroyExplosion>() == null)
            {
                var ad = tempGO.AddComponent<AutoDestroyExplosion>();
                ad.lifetime = 0.5f;
            }
            PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
            PrefabUtility.UnloadPrefabContents(tempGO);
        }

        sw.WriteLine($"Explosion setup complete: {animPath}, {ctrlPath}, {prefabPath}");
        return prefab;
    }

    static void UpdatePlayerAnim(StreamWriter sw)
    {
        string path = "Assets/Sprites/Player.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) return;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/spaceship.png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            sw.WriteLine($"Warning: No sprites found in Assets/Sprites/spaceship.png");
            return;
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        float fps = 12f;
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = sprites[i]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        EditorUtility.SetDirty(clip);
        sw.WriteLine($"Updated {path} with {sprites.Length} spaceship frames ({string.Join(", ", sprites.Select(s => s.name))}).");
    }

    static void UpdateEnemyAnim(StreamWriter sw)
    {
        string path = "Assets/Sprites/Enemy.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) return;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/idleufo.png")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            sw.WriteLine($"Warning: No sprites found in Assets/Sprites/idleufo.png");
            return;
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        float fps = 6f;
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = sprites[i]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        EditorUtility.SetDirty(clip);
        sw.WriteLine($"Updated {path} with {sprites.Length} idleufo frames ({string.Join(", ", sprites.Select(s => s.name))}).");
    }

    static void UpdatePlayerBulletPrefab(StreamWriter sw)
    {
        string prefabPath = "Assets/Prefab/player_bullet.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) return;

        Sprite bulletSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/fireshot.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "fireshot_0");

        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null && bulletSprite != null)
        {
            sr.sprite = bulletSprite;
        }

        root.transform.localEulerAngles = Vector3.zero;

        var box = root.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(0.16f, 0.26f);
            box.offset = Vector2.zero;
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        sw.WriteLine($"Updated {prefabPath}: sprite=fireshot_0, rotation=(0,0,0), BoxCollider2D size=(0.16, 0.26).");
    }

    static void UpdateEnemyPrefab(StreamWriter sw, GameObject explosionPrefab)
    {
        string prefabPath = "Assets/Prefab/Enemy.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) return;

        Sprite ufoSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/idleufo.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "idleufo_0");

        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null && ufoSprite != null)
        {
            sr.sprite = ufoSprite;
        }

        root.transform.localEulerAngles = Vector3.zero;

        var box = root.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(0.58f, 0.36f);
            box.offset = Vector2.zero;
        }

        var alien = root.GetComponent<alienController>();
        if (alien != null && explosionPrefab != null)
        {
            alien.explosionPrefab = explosionPrefab;
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        sw.WriteLine($"Updated {prefabPath}: sprite=idleufo_0, rotation=(0,0,0), BoxCollider2D size=(0.58, 0.36), explosionPrefab assigned.");
    }

    static void UpdateScene(StreamWriter sw)
    {
        string scenePath = "Assets/Level1.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Update Player in scene
        GameObject playerGO = GameObject.Find("Player");
        if (playerGO != null)
        {
            var sr = playerGO.GetComponent<SpriteRenderer>();
            Sprite ship0 = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/spaceship.png")
                .OfType<Sprite>()
                .FirstOrDefault(s => s.name == "spaceship_0");
            if (sr != null && ship0 != null)
            {
                sr.sprite = ship0;
            }

            playerGO.transform.localEulerAngles = Vector3.zero;

            var box = playerGO.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(0.50f, 0.86f);
                box.offset = Vector2.zero;
            }
            sw.WriteLine($"Updated Player in {scenePath}: sprite=spaceship_0, rotation=(0,0,0), BoxCollider2D size=(0.50, 0.86).");
        }

        // Update Background in scene
        GameObject bgChild = GameObject.Find("Scenery/Background/1");
        if (bgChild != null)
        {
            var sr = bgChild.GetComponent<SpriteRenderer>();
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/space_background.png");
            if (sr != null && bgSprite != null)
            {
                sr.sprite = bgSprite;
            }
            sw.WriteLine($"Updated Background child in {scenePath}: sprite=space_background.png.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sw.WriteLine($"Saved {scenePath}.");
    }
}
