using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Plays sound effects and adds a press-down effect to every button.
public class SoundFx : MonoBehaviour
{
    public static SoundFx I;

    [Header("Clips (leave empty to skip)")]
    public AudioClip click;
    public AudioClip beep;     // countdown 3, 2, 1
    public AudioClip go;       // FIGHT!
    public AudioClip lift;     // a wedge lifts a bot
    public AudioClip win;
    public AudioClip lose;

    [Range(0f, 1f)] public float volume = 0.8f;

    AudioSource src;

    void Awake()
    {
        I = this;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;   // 2D sound
    }

    void Start()
    {
        // Every button, including the ones that start inactive
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            b.onClick.AddListener(() => Play(click));
            if (b.GetComponent<PressScale>() == null)
                b.gameObject.AddComponent<PressScale>();
        }
    }

    public static void Play(AudioClip clip)
    {
        if (I == null || clip == null) return;
        I.src.PlayOneShot(clip, I.volume);
    }
}

// Buttons shrink slightly while pressed
public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = 0.93f;
    Vector3 baseScale;

    void Awake() { baseScale = transform.localScale; }
    void OnDisable() { transform.localScale = baseScale; }

    public void OnPointerDown(PointerEventData e) { transform.localScale = baseScale * pressedScale; }
    public void OnPointerUp(PointerEventData e) { transform.localScale = baseScale; }
    public void OnPointerExit(PointerEventData e) { transform.localScale = baseScale; }
}