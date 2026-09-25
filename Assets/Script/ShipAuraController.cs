using UnityEngine;

/*
Re9lay - Real-time Ship Relaxation & Biofeedback Aura
Procedurally renders a biofeedback ring around the player ship:
- Green / Cyan: Muscle relaxed below baseline, weapon armed & ready.
- Red: Active firing burst on muscle contraction.
- Pulsing Amber: Fired, waiting for conscious muscle relaxation reset.
*/

public class ShipAuraController : MonoBehaviour
{
    private playerController player;
    private LineRenderer line;
    private float burstTimer = 0f;
    private const int SegmentCount = 36;
    private const float AuraRadius = 0.55f;

    void Awake()
    {
        player = GetComponent<playerController>();
        SetupAuraRenderer();
    }

    void SetupAuraRenderer()
    {
        GameObject auraChild = new GameObject("ShipAuraRing");
        auraChild.transform.SetParent(transform);
        auraChild.transform.localPosition = Vector3.zero;
        auraChild.transform.localRotation = Quaternion.identity;
        auraChild.transform.localScale = Vector3.one;

        line = auraChild.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = SegmentCount;
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader != null)
        {
            line.material = new Material(shader);
        }

        line.sortingOrder = -1; // Render just under the ship sprite

        // Generate circular ring points
        Vector3[] points = new Vector3[SegmentCount];
        for (int i = 0; i < SegmentCount; i++)
        {
            float rad = (i / (float)SegmentCount) * Mathf.PI * 2f;
            points[i] = new Vector3(Mathf.Cos(rad) * AuraRadius, Mathf.Sin(rad) * AuraRadius, 0f);
        }
        line.SetPositions(points);
    }

    void Update()
    {
        if (line == null) return;

        if (player == null)
        {
            player = GetComponent<playerController>();
            if (player == null) return;
        }

        // Hide aura if game over or player disabled
        if (player.isGameOver || !line.gameObject.activeInHierarchy)
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;

        if (player.IsFiringBurst)
        {
            burstTimer = 0.2f; // Red flare duration
        }
        if (burstTimer > 0f)
        {
            burstTimer -= Time.deltaTime;
        }

        Color targetColor;
        float targetWidth = 0.08f;

        if (burstTimer > 0f)
        {
            // Firing burst
            targetColor = new Color(1f, 0.15f, 0.15f, 0.95f);
            targetWidth = 0.12f;
        }
        else if (player.IsActivating && !player.IsTriggerArmed)
        {
            // Waiting for patient to relax forearm
            float pulse = Mathf.PingPong(Time.time * 5f, 1f);
            targetColor = new Color(1f, 0.65f, 0.1f, 0.45f + 0.45f * pulse);
            targetWidth = 0.09f;
        }
        else
        {
            // Armed & Ready (Muscle relaxed below baseline)
            float breathe = Mathf.Sin(Time.time * 3f) * 0.1f;
            targetColor = new Color(0.15f, 1f, 0.7f, 0.75f + breathe);
            targetWidth = 0.07f;
        }

        line.startColor = targetColor;
        line.endColor = targetColor;
        line.startWidth = targetWidth;
        line.endWidth = targetWidth;
    }
}
