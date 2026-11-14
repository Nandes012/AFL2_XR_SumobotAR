using UnityEngine;

// Keeps the content level with the world: follows the marker's position
// and heading (yaw) but ignores its tilt.
[DefaultExecutionOrder(1000)]
public class LevelContent : MonoBehaviour
{
    public bool level = true;          // untick to compare with the old behaviour
    public float smoothing = 15f;      // higher = snappier, lower = smoother

    Transform marker;

    void Awake()
    {
        marker = transform.parent;
    }

    void LateUpdate()
    {
        if (!level || marker == null) return;

        Vector3 f = Vector3.ProjectOnPlane(marker.forward, Vector3.up);
        if (f.sqrMagnitude < 0.0001f)
            f = Vector3.ProjectOnPlane(marker.right, Vector3.up);
        if (f.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(f.normalized, Vector3.up);
        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
    }
}