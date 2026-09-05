using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class AssetInspector
{
    [MenuItem("Tools/Inspect Assets")]
    public static void Inspect()
    {
        SpriteMigrationTool.RunMigration();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== ASSET INSPECTION REPORT ===");

        // 1. Inspect Prefabs
        string[] prefabs = new string[] { "Assets/Prefab/Enemy.prefab", "Assets/Prefab/player_bullet.prefab" };
        foreach (var p in prefabs)
        {
            sb.AppendLine($"\n[Prefab: {p}]");
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null)
            {
                DumpGameObject(go, "  ", sb);
            }
        }

        // 2. Inspect Animations
        string[] animClips = new string[] { "Assets/Sprites/Player.anim", "Assets/Sprites/Enemy.anim" };
        foreach (var a in animClips)
        {
            sb.AppendLine($"\n[AnimationClip: {a}]");
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(a);
            if (clip != null)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                foreach (var b in bindings)
                {
                    sb.AppendLine($"  Binding: {b.path} / {b.propertyName} (type: {b.type.Name})");
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, b);
                    for (int i = 0; i < keyframes.Length; i++)
                    {
                        var val = keyframes[i].value;
                        string valName = val != null ? val.name : "null";
                        string valPath = val != null ? AssetDatabase.GetAssetPath(val) : "none";
                        sb.AppendLine($"    time={keyframes[i].time:F2}s -> {valName} ({valPath})");
                    }
                }
            }
        }

        // 3. Inspect Controllers
        string[] controllers = new string[] {
            "Assets/Sprites/Background.controller",
            "Assets/Sprites/Spritesheet_64x29_0.controller",
            "Assets/Sprites/Spritesheet_64x29_1.controller",
            "Assets/Sprites/Spritesheet_64x29_2.controller",
            "Assets/Sprites/eSpritesheet_40x30_0.controller"
        };
        foreach (var c in controllers)
        {
            sb.AppendLine($"\n[AnimatorController: {c}]");
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(c);
            if (ctrl != null)
            {
                foreach (var layer in ctrl.layers)
                {
                    sb.AppendLine($"  Layer: {layer.name}");
                    foreach (var state in layer.stateMachine.states)
                    {
                        string motionName = state.state.motion != null ? state.state.motion.name : "none";
                        sb.AppendLine($"    State: {state.state.name} (Motion: {motionName})");
                    }
                }
            }
        }

        // 4. Inspect Sprites in Assets/Sprites
        sb.AppendLine("\n[Sprite Assets in Assets/Sprites]");
        string[] spriteFiles = Directory.GetFiles("Assets/Sprites", "*.*", SearchOption.TopDirectoryOnly);
        foreach (var sf in spriteFiles)
        {
            if (sf.EndsWith(".meta")) continue;
            string unityPath = sf.Replace("\\", "/");
            TextureImporter ti = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (ti != null)
            {
                sb.AppendLine($"  {unityPath}: spriteImportMode={ti.spriteImportMode}, pixelsPerUnit={ti.spritePixelsPerUnit}, filterMode={ti.filterMode}");
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(unityPath);
                List<string> subSprites = new List<string>();
                foreach (var sa in subAssets)
                {
                    if (sa is Sprite s)
                    {
                        subSprites.Add($"{s.name} (rect: {s.rect})");
                    }
                }
                sb.AppendLine($"    Sub-sprites ({subSprites.Count}): {string.Join(", ", subSprites)}");
            }
        }

        // 5. Inspect Level1.unity scene objects
        sb.AppendLine("\n[Scene: Assets/Level1.unity]");
        bool sceneAlreadyOpen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == "Assets/Level1.unity";
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!sceneAlreadyOpen)
        {
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Level1.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        }
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            DumpGameObject(root, "", sb);
        }

        File.WriteAllText("asset_inspection_report.txt", sb.ToString());
        Debug.Log("Asset inspection report written to asset_inspection_report.txt");
    }

    static void DumpGameObject(GameObject go, string indent, StringBuilder sb)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        var anim = go.GetComponent<Animator>();
        string details = "";
        if (sr != null && sr.sprite != null)
        {
            details += $" [SR: {sr.sprite.name} from {AssetDatabase.GetAssetPath(sr.sprite)}]";
        }
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            details += $" [AnimCtrl: {anim.runtimeAnimatorController.name}]";
        }
        var player = go.GetComponent<playerController>();
        if (player != null)
        {
            string bulletRef = player.playerBullet != null ? player.playerBullet.name : "null";
            details += $" [playerController: bullet={bulletRef}]";
        }
        var enemyGen = go.GetComponent<enemyGenerator>();
        if (enemyGen != null)
        {
            string alienRef = enemyGen.alien != null ? enemyGen.alien.name : "null";
            details += $" [enemyGenerator: alien={alienRef}]";
        }
        var scroll = go.GetComponent<ScrollingScript>();
        if (scroll != null)
        {
            details += $" [ScrollingScript: speed={scroll.speed}, dir={scroll.direction}, looping={scroll.isLooping}, linked={scroll.isLinkedToCamera}]";
        }

        sb.AppendLine($"{indent}{go.name}{details} (pos={go.transform.position}, rot={go.transform.eulerAngles}, scale={go.transform.localScale})");
        var colliders = go.GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            sb.AppendLine($"{indent}  Collider: {col.GetType().Name}, bounds={col.bounds.size}, isTrigger={col.isTrigger}");
            if (col is BoxCollider2D box)
            {
                sb.AppendLine($"{indent}    BoxCollider2D: size={box.size}, offset={box.offset}");
            }
            else if (col is CircleCollider2D circle)
            {
                sb.AppendLine($"{indent}    CircleCollider2D: radius={circle.radius}, offset={circle.offset}");
            }
        }
        for (int i = 0; i < go.transform.childCount; i++)
        {
            DumpGameObject(go.transform.GetChild(i).gameObject, indent + "  ", sb);
        }
    }
}
