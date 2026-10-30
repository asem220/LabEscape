using UnityEngine;

[RequireComponent(typeof(NpcPatrol), typeof(NpcAnimation), typeof(Rigidbody2D))]
public class GuardCombat : MonoBehaviour
{
    [SerializeField] private PlayerCondition player;
    [SerializeField] private CombatArea combatZone;
    [SerializeField, Min(0.5f)] private float sight = 5f;
    [SerializeField, Min(0.5f)] private float attackRange = 1.25f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 2.4f;
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

    public bool IsStunned => Time.time < stunnedUntil;

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
        if (player.IsUnconscious || !inZone || distance > sight)
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
            if (!player.IsUnconscious && distance <= attackRange + 0.2f &&
                combatZone.Contains(player.transform.position))
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

        body.linearVelocity = towardPlayer.normalized * chaseSpeed;
        animation.ShowMovement(body.linearVelocity);
    }

    public void Stun(float seconds)
    {
        if (!enabled) return;
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
        animation.ShowMovement(Vector2.zero);
    }

    private void OnDisable()
    {
        pendingHit = false;
        controlling = false;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (sprite != null) sprite.color = normalColor;
    }
}
