using UnityEngine;
using Vuforia;

public class DebugStatus : MonoBehaviour
{
    public ObserverBehaviour target;
    public Rigidbody bot;
    public Transform content;      // ContentRoot
    public GameObject modePanel;
    public GameObject ground;

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28 };
        style.normal.textColor = Color.yellow;

        string status = target != null ? target.TargetStatus.Status.ToString() : "no target";
        string y = (bot != null && content != null)
            ? content.InverseTransformPoint(bot.position).y.ToString("F3") : "-";
        string kin = bot != null ? bot.isKinematic.ToString() : "-";
        string cols = bot != null ? CountColliders(bot.transform) : "-";
        string gr = ground != null ? ground.activeSelf.ToString() : "-";

        GUI.Label(new Rect(20, 20, 1000, 300),
            "Status: " + status +
            "\nBot Y (local): " + y +
            "\nBot kinematic: " + kin +
            "\nBot colliders on: " + cols +
            "\nMode panel active: " + (modePanel != null && modePanel.activeSelf) +
            "\nGround active: " + gr, style);
    }

    string CountColliders(Transform t)
    {
        int on = 0, total = 0;
        foreach (var c in t.GetComponentsInChildren<Collider>(true))
        {
            total++;
            if (c.enabled && c.gameObject.activeInHierarchy) on++;
        }
        return on + " / " + total;
    }
}