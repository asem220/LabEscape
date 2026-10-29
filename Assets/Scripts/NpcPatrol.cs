using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(NpcAnimation))]
public class NpcPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [SerializeField, Min(0)] private int firstPoint;
    [SerializeField, Min(0.1f)] private float speed = 1.5f;
    [SerializeField, Min(0f)] private float waitSeconds = 0.2f;

    private Rigidbody2D body;
    private NpcAnimation animation;
    private int targetPoint;
    private float waitLeft;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animation = GetComponent<NpcAnimation>();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetComponent<CapsuleCollider2D>().isTrigger = false;

        if (points != null && points.Length > 0)
            targetPoint = Mathf.Clamp(firstPoint, 0, points.Length - 1);
    }

    private void FixedUpdate()
    {
        if (points == null || points.Length == 0 || animation.IsAttacking)
        {
            Stop();
            return;
        }

        if (waitLeft > 0f)
        {
            waitLeft -= Time.fixedDeltaTime;
            Stop();
            return;
        }

        Vector2 toTarget = (Vector2)points[targetPoint].position - body.position;
        if (toTarget.sqrMagnitude < 0.01f)
        {
            targetPoint = (targetPoint + 1) % points.Length;
            waitLeft = waitSeconds;
            Stop();
            return;
        }

        // The Rigidbody2D moves through physics, so walls and characters block it.
        Vector2 velocity = toTarget.normalized * Mathf.Min(speed, toTarget.magnitude / Time.fixedDeltaTime);
        body.linearVelocity = velocity;
        animation.ShowMovement(velocity);
    }

    private void Stop()
    {
        if (body != null) body.linearVelocity = Vector2.zero;
        if (animation != null) animation.ShowMovement(Vector2.zero);
    }

    private void OnDisable()
    {
        Stop();
    }
}
