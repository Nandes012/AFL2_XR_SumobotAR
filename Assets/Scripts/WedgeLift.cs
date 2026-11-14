using UnityEngine;

// Put on each bot. When this bot's front blade is under the opponent
// and this bot is pushing harder, the opponent gets lifted.
public class WedgeLift : MonoBehaviour
{
    public SumoBody self;
    public SumoBody opponent;

    [Header("Wedge zone (local to this bot, meters)")]
    public float reachMin = 0.03f;       // closest distance to the opponent's center
    public float reachMax = 0.12f;       // farthest distance
    public float halfWidth = 0.05f;      // left/right tolerance

    [Header("Lift")]
    public float liftRate = 4f;          // lift gained per second while engaged
    public float minClosingSpeed = 0.02f;

    Rigidbody selfRb, oppRb;

    void Awake()
    {
        if (self == null) self = GetComponent<SumoBody>();
    }

    void Start()
    {
        selfRb = self.GetComponent<Rigidbody>();
        if (opponent != null) oppRb = opponent.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (self == null || opponent == null || oppRb == null) return;
        if (!opponent.gameObject.activeInHierarchy) return;

        // Where is the opponent relative to this bot's front?
        Vector3 local = self.transform.InverseTransformPoint(opponent.transform.position);
        bool inZone = local.z > reachMin && local.z < reachMax
                      && Mathf.Abs(local.x) < halfWidth;
        if (!inZone) return;

        // Whoever is pushing harder wins the lift (head-on pushes don't lift both)
        Vector3 toOpp = opponent.transform.position - self.transform.position;
        toOpp.y = 0f;
        if (toOpp.sqrMagnitude < 0.0001f) return;
        toOpp.Normalize();

        float attack = Vector3.Dot(selfRb.linearVelocity, toOpp);
        float defend = Vector3.Dot(oppRb.linearVelocity, -toOpp);

        if (attack > minClosingSpeed && attack > defend)
            opponent.AddLift(liftRate * Time.fixedDeltaTime);
    }
}