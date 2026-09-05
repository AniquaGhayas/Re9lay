using UnityEngine;

/// <summary>
/// Attach to your Background SpriteRenderer GameObject.
/// Automatically:
///   - Resets position to camera centre
///   - Scales sprite to fill camera height (no stretching)
///   - Clones itself and loops both panels seamlessly downward
/// Does NOT require Tiled draw mode or Full Rect mesh type.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class InfiniteBackground : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Downward scroll speed in world units per second.")]
    public float scrollSpeed = 2f;

    private Transform _clone;
    private float _panelHeight;

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("[InfiniteBackground] Missing SpriteRenderer or Sprite on " + gameObject.name);
            return;
        }

        // -- 1. Make sure Draw Mode is Simple (works with any sprite import settings)
        sr.drawMode = SpriteDrawMode.Simple;

        // -- 2. Reset scale to (1,1,1) first so bounds are accurate
        transform.localScale = Vector3.one;

        // -- 3. Scale uniformly to fill the FULL camera height
        if (Camera.main != null)
        {
            float camH       = Camera.main.orthographicSize * 2f;
            float camW       = camH * Camera.main.aspect;
            float spriteH    = sr.sprite.bounds.size.y;
            float spriteW    = sr.sprite.bounds.size.x;

            // Scale by whichever axis needs more coverage
            float scaleByH   = camH / spriteH;
            float scaleByW   = camW / spriteW;
            float finalScale = Mathf.Max(scaleByH, scaleByW);

            transform.localScale = new Vector3(finalScale, finalScale, 1f);

            // -- 4. Snap to camera centre
            Vector3 camPos = Camera.main.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }

        // -- 5. Record panel height after scaling
        _panelHeight = sr.bounds.size.y;

        // -- 6. Clone and place directly above
        GameObject cloneObj = Instantiate(gameObject, transform.parent);
        cloneObj.name = gameObject.name + "_Clone";
        Destroy(cloneObj.GetComponent<InfiniteBackground>());

        _clone = cloneObj.transform;
        _clone.position = new Vector3(
            transform.position.x,
            transform.position.y + _panelHeight,
            transform.position.z
        );

        Debug.Log($"[InfiniteBackground] Ready. Panel height = {_panelHeight:F2}");
    }

    void Update()
    {
        if (_clone == null) return;

        float delta = scrollSpeed * Time.deltaTime;

        transform.Translate(Vector3.down * delta, Space.World);
        _clone.Translate(Vector3.down * delta, Space.World);

        // Recycle whichever panel scrolls off the bottom
        if (transform.position.y <= _clone.position.y - _panelHeight)
        {
            transform.position = new Vector3(
                transform.position.x,
                _clone.position.y + _panelHeight,
                transform.position.z
            );
        }

        if (_clone.position.y <= transform.position.y - _panelHeight)
        {
            _clone.position = new Vector3(
                _clone.position.x,
                transform.position.y + _panelHeight,
                _clone.position.z
            );
        }
    }
}
