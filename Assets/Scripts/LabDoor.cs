using UnityEngine;

// A whole door object. It opens near the player and closes after they move away.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class LabDoor : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.1f)] private float openDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float closeDistance = 1.8f;
    [SerializeField, Min(1f)] private float framesPerSecond = 16f;

    private SpriteRenderer visual;
    private BoxCollider2D barrier;
    private Collider2D player;
    private float frame;
    private bool opening;

    public bool IsOpen => barrier != null && !barrier.enabled;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        barrier = GetComponent<BoxCollider2D>();
        var movement = FindAnyObjectByType<PlayerMovement>();
        if (movement != null) player = movement.GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || player == null || frames == null || frames.Length == 0) return;

        // Distance to the doorway rectangle works for either orientation.
        Vector2 position = transform.InverseTransformPoint(player.bounds.center);
        Vector2 delta = position - barrier.offset;
        delta.x = Mathf.Max(0f, Mathf.Abs(delta.x) - barrier.size.x * 0.5f);
        delta.y = Mathf.Max(0f, Mathf.Abs(delta.y) - barrier.size.y * 0.5f);
        opening = delta.magnitude <= (opening ? closeDistance : openDistance);

        int last = frames.Length - 1;
        frame = Mathf.MoveTowards(frame, opening ? last : 0f, framesPerSecond * Time.deltaTime);
        visual.sprite = frames[Mathf.Clamp(Mathf.FloorToInt(frame + 0.001f), 0, last)];
        barrier.enabled = frame < last;
    }
}
