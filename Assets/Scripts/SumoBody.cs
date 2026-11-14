using UnityEngine;
using Vuforia;
using System;

[RequireComponent(typeof(Rigidbody))]
public abstract class SumoBody : MonoBehaviour
{
    [Header("Tracking")]
    public ObserverBehaviour target;     // the Image Target (tracking status)
    public Transform surface;            // ContentRoot (fall height reference)
    public bool allowExtendedTracking = true;

    [Header("Drive")]
    public float moveSpeed = 0.15f;      // m/s forward
    public float reverseMultiplier = 0.8f;
    public float turnSpeed = 180f;       // degrees per second
    public float acceleration = 20f;     // m/s^2 toward the wanted speed
    public bool canMove = true;

    [Header("Push power (when touching the opponent)")]
    public float pushMultiplier = 2.5f;  // speed and acceleration boost while pushing
    public float idleResistance = 0.1f;  // 0 = no resistance, 1 = full, when not driving

    [Header("Physics")]
    public float gravity = 6f;
    public float liftedTraction = 0.1f;  // grip left when not touching the floor
    public float fallLimit = -0.3f;
    public bool respawnOnFall = true;

    [Header("Lift (when an opponent's wedge gets under this bot)")]
    public Transform visual;             // the model child (visual only)
    public float maxLiftTilt = 12f;      // degrees, nose up
    public float maxLiftHeight = 0.008f; // meters
    public float liftDecay = 1.5f;       // lift lost per second when not engaged

    public Action OnFell;

    protected Rigidbody rb;
    Vector3 startLocalPos;
    Quaternion startLocalRot;
    bool fallen;
    float groundedUntil;
    float touchOpponentUntil;

    float lift;                          // 0 = flat, 1 = fully lifted
    Vector3 visualBasePos;
    Quaternion visualBaseRot;

    public float Lift => lift;
    public void AddLift(float amount) { lift = Mathf.Clamp01(lift + amount); }

    protected Transform Surface
    {
        get
        {
            if (surface != null) return surface;
            if (target != null) return target.transform;
            return transform.parent;
        }
    }

    // ContentRoot is kept level by LevelContent, so world up is the right reference
    protected Vector3 Up => Vector3.up;

    protected virtual float SpeedMultiplier => 1f;
    protected abstract void ReadInput(out float throttle, out float steer);

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;   // only the script rotates the bot
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.sleepThreshold = 0f;

        // Default contact offset (1 cm) is huge for a 10 cm bot
        foreach (var c in GetComponentsInChildren<Collider>())
            c.contactOffset = 0.002f;

        startLocalPos = transform.localPosition;

        // Keep only the heading (yaw) so any tilt from the scene is removed
        startLocalRot = Quaternion.Euler(0f, transform.localEulerAngles.y, 0f);
        transform.localRotation = startLocalRot;

        if (visual != null)
        {
            visualBasePos = visual.localPosition;
            visualBaseRot = visual.localRotation;
        }
    }

    bool IsTracked()
    {
        if (target == null) return false;
        var s = target.TargetStatus.Status;
        if (s == Status.TRACKED) return true;
        return allowExtendedTracking && s == Status.EXTENDED_TRACKED;
    }

    public void ResetPose()
    {
        fallen = false;
        groundedUntil = 0f;
        touchOpponentUntil = 0f;
        lift = 0f;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = startLocalPos;
        transform.localRotation = startLocalRot;
    }

    void HandleFall()
    {
        if (respawnOnFall)
        {
            ResetPose();
            return;
        }
        fallen = true;
        canMove = false;
        rb.isKinematic = true;
        OnFell?.Invoke();
    }

    // Contact tracking: floor grip, and whether we are touching the opponent
    void OnCollisionEnter(Collision c) { CheckContact(c); }
    void OnCollisionStay(Collision c) { CheckContact(c); }

    void CheckContact(Collision c)
    {
        if (c.collider.GetComponentInParent<SumoBody>() != null)
        {
            touchOpponentUntil = Time.time + 0.1f;   // touching the other bot
            return;
        }
        groundedUntil = Time.time + 0.1f;            // touching the floor
    }

    // Visual lift: nose up and slightly raised
    void LateUpdate()
    {
        if (visual == null) return;
        visual.localRotation = visualBaseRot * Quaternion.Euler(-maxLiftTilt * lift, 0f, 0f);
        visual.localPosition = visualBasePos + Vector3.up * (maxLiftHeight * lift);
    }

    void FixedUpdate()
    {
        // Lift fades out when no opponent keeps it up
        lift = Mathf.MoveTowards(lift, 0f, liftDecay * Time.fixedDeltaTime);

        if (fallen || !IsTracked())
        {
            rb.isKinematic = true;
            return;
        }
        rb.isKinematic = false;

        Vector3 up = Up;

        // Fell below the floor level
        if (Surface.InverseTransformPoint(transform.position).y < fallLimit)
        {
            HandleFall();
            return;
        }

        float throttle = 0f, steer = 0f;
        if (canMove) ReadInput(out throttle, out steer);

        // A lifted bot loses grip, so the attacker can push it away
        float traction = Time.time < groundedUntil ? 1f : liftedTraction;
        traction = Mathf.Lerp(traction, liftedTraction, lift);

        // Gravity
        rb.AddForce(-up * gravity, ForceMode.Acceleration);

        // Drive direction: along the bot's heading, flattened to the floor
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(transform.up, up);
        fwd.Normalize();

        // Drive: move the horizontal velocity toward the wanted velocity
        float mult = SpeedMultiplier;
        float limit = (throttle >= 0f ? moveSpeed : moveSpeed * reverseMultiplier) * mult;
        float accel = acceleration * Mathf.Max(mult, 1f) * traction;

        // Push power: commit harder when pushing, and give way easily when idle
        if (Time.time < touchOpponentUntil)
        {
            if (Mathf.Abs(throttle) > 0.05f)
            {
                limit *= pushMultiplier;
                accel *= pushMultiplier;
            }
            else
            {
                accel *= idleResistance;
            }
        }

        Vector3 desired = fwd * throttle * limit;

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = Vector3.ProjectOnPlane(velocity, up);
        Vector3 vertical = velocity - horizontal;
        horizontal = Vector3.MoveTowards(horizontal, desired, accel * Time.fixedDeltaTime);
        rb.linearVelocity = horizontal + vertical;

        // Steering + keep level: always rebuild the rotation from the heading only
        Vector3 heading = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        if (heading.sqrMagnitude > 0.0001f)
        {
            float deg = Mathf.Abs(steer) > 0.01f
                ? steer * turnSpeed * Time.fixedDeltaTime
                : 0f;
            Vector3 newHeading = Quaternion.AngleAxis(deg, up) * heading;
            rb.MoveRotation(Quaternion.LookRotation(newHeading, up));
        }
    }
}