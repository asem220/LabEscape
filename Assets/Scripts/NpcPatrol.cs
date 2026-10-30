using System.Collections.Generic;
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
    private readonly List<Vector2> detour = new List<Vector2>();
    private int detourPoint;
    private Vector2 lastPosition;
    private float lastProgressCheck;
    private bool rejoinOnEnable;

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
        ResetProgress();
    }

    private void OnEnable()
    {
        if (!rejoinOnEnable || body == null) return;
        rejoinOnEnable = false;
        RejoinRoute();
    }

    private void FixedUpdate()
    {
        if (points == null || points.Length == 0 || animation.IsAttacking)
        {
            Stop();
            ResetProgress();
            return;
        }

        if (waitLeft > 0f)
        {
            waitLeft -= Time.fixedDeltaTime;
            Stop();
            ResetProgress();
            return;
        }

        while (detourPoint < detour.Count &&
               Vector2.Distance(body.position, detour[detourPoint]) < 0.18f)
            detourPoint++;

        if (detourPoint >= detour.Count) detour.Clear();
        Vector2 destination = detour.Count > 0
            ? detour[detourPoint]
            : (Vector2)points[targetPoint].position;
        Vector2 toTarget = destination - body.position;
        if (detour.Count == 0 && toTarget.sqrMagnitude < 0.02f)
        {
            targetPoint = (targetPoint + 1) % points.Length;
            waitLeft = waitSeconds;
            Stop();
            ResetProgress();
            return;
        }

        // The Rigidbody2D moves through physics, so walls and characters block it.
        Vector2 velocity = toTarget.normalized * Mathf.Min(speed, toTarget.magnitude / Time.fixedDeltaTime);
        body.linearVelocity = velocity;
        animation.ShowMovement(velocity);
        CheckForStall();
    }

    // Called after combat or after carrying the player back to the ward.
    public void RejoinRoute()
    {
        if (body == null || points == null || points.Length == 0) return;

        float shortest = float.PositiveInfinity;
        var candidate = new List<Vector2>();
        var best = new List<Vector2>();
        for (int i = 0; i < points.Length; i++)
        {
            if (!LabPathfinder.TryFind(body.position, points[i].position, candidate)) continue;
            float length = 0f;
            Vector2 previous = body.position;
            foreach (Vector2 waypoint in candidate)
            {
                length += Vector2.Distance(previous, waypoint);
                previous = waypoint;
            }
            if (length >= shortest) continue;
            shortest = length;
            targetPoint = i;
            best.Clear();
            best.AddRange(candidate);
        }
        detour.Clear();
        detour.AddRange(best);
        detourPoint = 0;
        ResetProgress();
    }

    private void CheckForStall()
    {
        if (Time.time - lastProgressCheck < 0.8f) return;
        if (Vector2.Distance(body.position, lastPosition) < 0.1f)
        {
            detour.Clear();
            if (LabPathfinder.TryFind(body.position, points[targetPoint].position, detour))
                detourPoint = 0;
        }
        ResetProgress();
    }

    private void ResetProgress()
    {
        if (body == null) return;
        lastPosition = body.position;
        lastProgressCheck = Time.time;
    }

    private void Stop()
    {
        if (body != null) body.linearVelocity = Vector2.zero;
        if (animation != null) animation.ShowMovement(Vector2.zero);
    }

    private void OnDisable()
    {
        rejoinOnEnable = true;
        detour.Clear();
        detourPoint = 0;
        Stop();
    }
}
