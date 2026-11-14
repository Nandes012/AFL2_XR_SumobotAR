using UnityEngine;

public class BotController : SumoBody
{
    [Header("Controls")]
    public VirtualJoystick driveStick;   // left: forward / reverse
    public VirtualJoystick steerStick;   // right: turn left / right
    public float deadZone = 0.15f;

    protected override void ReadInput(out float throttle, out float steer)
    {
        throttle = driveStick != null ? driveStick.Direction.y : 0f;
        steer = steerStick != null ? steerStick.Direction.x : 0f;

        if (Mathf.Abs(throttle) < deadZone) throttle = 0f;
        if (Mathf.Abs(steer) < deadZone) steer = 0f;
    }
}