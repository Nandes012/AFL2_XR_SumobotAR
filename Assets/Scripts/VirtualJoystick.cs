using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum AxisMode { Both, VerticalOnly, HorizontalOnly }

    [SerializeField] RectTransform background;
    [SerializeField] RectTransform handle;
    public AxisMode axis = AxisMode.Both;

    public Vector2 Direction { get; private set; }

    public void OnPointerDown(PointerEventData e) => OnDrag(e);

    public void OnDrag(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, e.position, e.pressEventCamera, out Vector2 pos))
            return;

        Vector2 radius = background.rect.size * 0.5f;
        Vector2 v = new Vector2(pos.x / radius.x, pos.y / radius.y);

        if (axis == AxisMode.VerticalOnly) v.x = 0f;
        if (axis == AxisMode.HorizontalOnly) v.y = 0f;

        Direction = Vector2.ClampMagnitude(v, 1f);
        handle.anchoredPosition = new Vector2(Direction.x * radius.x,
                                              Direction.y * radius.y);
    }

    public void OnPointerUp(PointerEventData e) => ResetStick();

    void OnDisable() => ResetStick();

    public void ResetStick()
    {
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}