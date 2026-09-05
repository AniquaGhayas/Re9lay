/*
2D Space Shooter - Dynamic Screen, Device & Background Controller
Automatically adapts Camera, Physics Colliders, Backgrounds, and Bounds to any device resolution/aspect ratio.
*/

using UnityEngine;

[ExecuteAlways]
public class DynamicScreenBoundaries : MonoBehaviour
{
    public static DynamicScreenBoundaries Instance { get; private set; }

    [Header("Calculated World Bounds (Read Only)")]
    public float minWorldX;
    public float maxWorldX;
    public float minWorldY;
    public float maxWorldY;

    private Camera cam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        cam = Camera.main;
        UpdateScreenBoundaries();
    }

    void Update()
    {
        UpdateScreenBoundaries();
    }

    public void UpdateScreenBoundaries()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        minWorldX = bottomLeft.x;
        maxWorldX = topRight.x;
        minWorldY = bottomLeft.y;
        maxWorldY = topRight.y;

        float screenWidth = maxWorldX - minWorldX;
        float screenHeight = maxWorldY - minWorldY;

        Transform sceneryTransform = null;
        GameObject sceneryObj = GameObject.Find("Scenery");
        if (sceneryObj != null) sceneryTransform = sceneryObj.transform;

        // Position 4 physical BoxCollider2D walls matching exact screen edges under Scenery parent
        PositionWall("Floor", new Vector3(0, minWorldY - 0.25f, 0), new Vector3(screenWidth + 2f, 0.5f, 1f), sceneryTransform);
        PositionWall("Ceiling", new Vector3(0, maxWorldY + 0.25f, 0), new Vector3(screenWidth + 2f, 0.5f, 1f), sceneryTransform);
        PositionWall("LeftWall", new Vector3(minWorldX - 0.25f, 0, 0), new Vector3(0.5f, screenHeight + 2f, 1f), sceneryTransform);
        PositionWall("RightWall", new Vector3(maxWorldX + 0.25f, 0, 0), new Vector3(0.5f, screenHeight + 2f, 1f), sceneryTransform);

        // Dynamically scale Background container to cover full screen width on any device
        ScaleBackgroundContainer("Background", screenWidth);
    }

    private void ScaleBackgroundContainer(string containerName, float targetWidth)
    {
        GameObject bgObj = GameObject.Find(containerName);
        if (bgObj != null)
        {
            float scaleX = Mathf.Max(1.0f, targetWidth / 4.0f);
            bgObj.transform.localScale = new Vector3(scaleX, bgObj.transform.localScale.y, bgObj.transform.localScale.z);
        }
    }

    private void PositionWall(string wallName, Vector3 targetPos, Vector3 targetScale, Transform parent)
    {
        GameObject wall = GameObject.Find(wallName);
        if (wall == null)
        {
            wall = new GameObject(wallName);
            wall.AddComponent<BoxCollider2D>();
        }

        if (parent != null && wall.transform.parent != parent)
        {
            wall.transform.SetParent(parent);
        }

        wall.transform.position = targetPos;
        wall.transform.localScale = targetScale;
    }
}
