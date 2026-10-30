using UnityEngine;

// A whole door object. It opens for the player and guards carrying them.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class LabDoor : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.1f)] private float openDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float closeDistance = 1.8f;
    [SerializeField, Min(1f)] private float framesPerSecond = 16f;

    private SpriteRenderer visual;
    private BoxCollider2D barrier;
    private Transform[] visitors;
    private float frame;
    private bool opening;

    public bool IsOpen => barrier != null && !barrier.enabled;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        barrier = GetComponent<BoxCollider2D>();
        CacheVisitors();
    }

    private void CacheVisitors()
    {
        var movement = FindAnyObjectByType<PlayerMovement>();
        var guards = FindObjectsByType<NpcPatrol>();
        visitors = new Transform[guards.Length + (movement == null ? 0 : 1)];
        int index = 0;
        if (movement != null) visitors[index++] = movement.transform;
        foreach (var guard in guards) visitors[index++] = guard.transform;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || frames == null || frames.Length == 0) return;
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (barrier == null) barrier = GetComponent<BoxCollider2D>();
        if (visitors == null) CacheVisitors();

        bool nearby = false;
        foreach (Transform visitor in visitors)
        {
            if (visitor == null) continue;
            // Distance to the doorway rectangle works for either orientation.
            Vector2 position = transform.InverseTransformPoint(visitor.position);
            Vector2 delta = position - barrier.offset;
            delta.x = Mathf.Max(0f, Mathf.Abs(delta.x) - barrier.size.x * 0.5f);
            delta.y = Mathf.Max(0f, Mathf.Abs(delta.y) - barrier.size.y * 0.5f);
            if (delta.magnitude > (opening ? closeDistance : openDistance)) continue;
            nearby = true;
            break;
        }
        opening = nearby;

        int last = frames.Length - 1;
        frame = Mathf.MoveTowards(frame, opening ? last : 0f, framesPerSecond * Time.deltaTime);
        visual.sprite = frames[Mathf.Clamp(Mathf.FloorToInt(frame + 0.001f), 0, last)];
        barrier.enabled = frame < last;
    }
}
