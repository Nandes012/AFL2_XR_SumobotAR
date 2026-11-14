using UnityEngine;

public class EnemyBot : SumoBody
{
    [Header("AI")]
    public Transform player;           // the SumoBot
    public float startDelay = 1.0f;    // seconds before the enemy starts moving
    public bool active;

    [Header("Aggression")]
    public float steerSharpness = 15f; // degrees of error for full steering (lower = sharper)
    public float minThrottle = 0.6f;   // keeps moving while turning
    public float chargeRange = 0.20f;  // charges at full power inside this range
    public float strafeJitter = 0.15f; // small random weave so it isn't predictable

    [Header("Dash")]
    public float dashSpeed = 0.30f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 1.8f;
    public float dashRange = 0.22f;
    public float dashAngle = 40f;      // dash only when roughly facing the player

    float startTime;
    float dashEnd;
    float nextDash;
    float jitterSeed;

    protected override void Awake()
    {
        base.Awake();
        respawnOnFall = false;          // falling always means losing
        allowExtendedTracking = false;  // Battle needs the marker in view
        jitterSeed = Random.value * 100f;
    }

    // Called by GameManager when the match starts
    public void Begin()
    {
        ResetPose();
        canMove = true;
        active = true;
        startTime = Time.time + startDelay;
        nextDash = startTime + dashCooldown * 0.5f;
        dashEnd = 0f;
    }

    // Called by GameManager when the match ends
    public void Stop()
    {
        active = false;
    }

    protected override float SpeedMultiplier
    {
        get { return Time.time < dashEnd ? dashSpeed / Mathf.Max(moveSpeed, 0.01f) : 1f; }
    }

    protected override void ReadInput(out float throttle, out float steer)
    {
        throttle = 0f;
        steer = 0f;
        if (!active || player == null || Time.time < startTime) return;

        Vector3 up = Up;
        Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - transform.position, up);
        float dist = toPlayer.magnitude;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up);
        float angle = Vector3.SignedAngle(fwd, toPlayer, up);

        // Add a small weave so the enemy isn't perfectly predictable
        float weave = (Mathf.PerlinNoise(jitterSeed, Time.time * 0.8f) - 0.5f) * 2f * strafeJitter;
        steer = Mathf.Clamp(angle / steerSharpness + weave, -1f, 1f);

        // Keep driving while turning. Only slow down when the player is behind it.
        float absAngle = Mathf.Abs(angle);
        throttle = absAngle > 120f ? 0.3f : 1f;
        throttle = Mathf.Max(minThrottle, throttle);

        // Dash when close, roughly facing the player, and off cooldown
        if (Time.time >= nextDash && dist < dashRange && absAngle < dashAngle)
        {
            dashEnd = Time.time + dashDuration;
            nextDash = Time.time + dashCooldown;
        }
    }
}