using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NpcPatrol), typeof(NpcAnimation), typeof(Rigidbody2D))]
public class GuardCombat : MonoBehaviour
{
    [SerializeField] private PlayerCondition player;
    [SerializeField] private CombatArea combatZone;
    [SerializeField, Min(0.5f)] private float sight = 5f;
    [SerializeField, Min(0.5f)] private float attackRange = 1.7f;
    [SerializeField, Min(0.1f)] private float runSpeed = 8f;
    [SerializeField, Min(1f)] private float loseDistance = 18f;
    [SerializeField, Min(0.1f)] private float chaseOutsideSeconds = 10f;
    [SerializeField, Min(0.1f)] private float repathSeconds = 0.35f;
    [SerializeField, Min(1f)] private float damage = 20f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;

    private Rigidbody2D body;
    private NpcPatrol patrol;
    private NpcAnimation animation;
    private SpriteRenderer sprite;
    private Color normalColor;
    private float stunnedUntil;
    private float nextAttack;
    private float hitAt;
    private bool pendingHit;
    private bool controlling;
    private bool chasing;
    private float lastInZoneTime;
    private float nextPathTime;
    private Vector2 lastTarget;
    private readonly List<Vector2> path = new List<Vector2>();
    private int pathPoint;

    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsChasing => chasing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        patrol = GetComponent<NpcPatrol>();
        animation = GetComponent<NpcAnimation>();
        sprite = GetComponent<SpriteRenderer>();
        normalColor = sprite.color;
        if (player == null) player = FindAnyObjectByType<PlayerCondition>();
    }

    private void FixedUpdate()
    {
        if (player == null || combatZone == null) return;
        if (IsStunned)
        {
            TakeControl();
            Stop();
            return;
        }
        sprite.color = normalColor;

        bool inZone = combatZone.Contains(player.transform.position) &&
                      combatZone.Contains(transform.position);
        float distance = Vector2.Distance(body.position, player.transform.position);
        if (!chasing && inZone && distance <= sight)
        {
            chasing = true;
            nextPathTime = 0f;
        }
        if (chasing && inZone) lastInZoneTime = Time.time;

        // Combat starts by the exit; an ongoing fight can move into nearby rooms.
        if (!chasing || player.IsUnconscious || distance > loseDistance ||
            (!inZone && Time.time - lastInZoneTime > chaseOutsideSeconds))
        {
            ReturnToPatrol();
            return;
        }

        TakeControl();
        if (pendingHit)
        {
            Stop();
            if (Time.time < hitAt) return;
            pendingHit = false;
            if (!player.IsUnconscious && distance <= attackRange + 0.2f)
                player.TakeDamage(damage);
            return;
        }
        if (animation.IsAttacking)
        {
            Stop();
            return;
        }

        Vector2 towardPlayer = (Vector2)player.transform.position - body.position;
        if (distance <= attackRange)
        {
            Stop();
            if (Time.time < nextAttack) return;
            animation.ShowMovement(towardPlayer);
            animation.Attack();
            nextAttack = Time.time + attackCooldown;
            hitAt = Time.time + 0.25f;
            pendingHit = true;
            return;
        }

        Chase();
    }

    private void Chase()
    {
        Vector2 target = player.transform.position;
        if (Time.time >= nextPathTime || pathPoint >= path.Count ||
            Vector2.Distance(target, lastTarget) > 0.75f)
        {
            if (!LabPathfinder.TryFind(body.position, target, path))
            {
                Stop();
                return;
            }
            pathPoint = 0;
            lastTarget = target;
            nextPathTime = Time.time + repathSeconds;
        }
        while (pathPoint < path.Count &&
               Vector2.Distance(body.position, path[pathPoint]) < 0.18f)
            pathPoint++;
        if (pathPoint >= path.Count) { Stop(); return; }

        Vector2 direction = path[pathPoint] - body.position;
        body.linearVelocity = direction.normalized *
            Mathf.Min(runSpeed, direction.magnitude / Time.fixedDeltaTime);
        animation.SetRunning(true);
        animation.ShowMovement(body.linearVelocity);
    }

    public void Stun(float seconds)
    {
        if (!enabled) return;
        chasing = true;
        lastInZoneTime = Time.time;
        nextPathTime = 0f;
        TakeControl();
        pendingHit = false;
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
        animation.CancelAttack();
        sprite.color = new Color(0.55f, 0.82f, 1f);
        Stop();
    }

    private void TakeControl()
    {
        if (controlling) return;
        controlling = true;
        patrol.enabled = false;
    }

    private void ReturnToPatrol()
    {
        chasing = false;
        path.Clear();
        if (!controlling) return;
        pendingHit = false;
        animation.CancelAttack();
        Stop();
        controlling = false;
        patrol.enabled = true;
    }

    private void Stop()
    {
        body.linearVelocity = Vector2.zero;
        animation.SetRunning(false);
        animation.ShowMovement(Vector2.zero);
    }

    private void OnDisable()
    {
        pendingHit = false;
        controlling = false;
        chasing = false;
        path.Clear();
        if (body != null) body.linearVelocity = Vector2.zero;
        if (animation != null) animation.SetRunning(false);
        if (sprite != null) sprite.color = normalColor;
    }
}
