using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NpcPatrol), typeof(GuardCombat), typeof(Rigidbody2D))]
public class GuardEscort : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float walkSpeed = 2.2f;

    private Rigidbody2D body;
    private NpcPatrol patrol;
    private GuardCombat combat;
    private NpcAnimation animation;
    private readonly List<Vector2> route = new List<Vector2>();
    private bool reached;

    public bool IsBusy { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        patrol = GetComponent<NpcPatrol>();
        combat = GetComponent<GuardCombat>();
        animation = GetComponent<NpcAnimation>();
    }

    public void Begin(PlayerCondition player, Vector2 ward)
    {
        if (IsBusy || player == null) return;
        IsBusy = true;
        combat.enabled = false;
        patrol.enabled = false;
        animation.CancelAttack();
        body.linearVelocity = Vector2.zero;
        StartCoroutine(Escort(player, ward));
    }

    private IEnumerator Escort(PlayerCondition player, Vector2 ward)
    {
        // Let the complete collapse animation play before approaching.
        yield return new WaitForSeconds(1.1f);
        if (player == null || !player.IsUnconscious) { Finish(); yield break; }

        yield return Travel(player.transform.position, 0.85f);
        if (player == null) { Finish(); yield break; }
        if (!reached)
        {
            player.RecoverAtWard();
            Finish();
            yield break;
        }

        player.BeginCarry(transform);
        yield return Travel(ward, 0.55f);
        if (player != null) player.RecoverAtWard();
        Finish();
    }

    private IEnumerator Travel(Vector2 goal, float stopDistance)
    {
        reached = false;
        float deadline = Time.time + 55f;

        for (int attempt = 0; attempt < 8 && Time.time < deadline; attempt++)
        {
            if (Vector2.Distance(body.position, goal) <= stopDistance) break;
            if (!LabPathfinder.TryFind(body.position, goal, route)) break;

            bool blocked = false;
            foreach (Vector2 point in route)
            {
                float stillTime = 0f;
                float stepDeadline = Time.time + 12f;
                while (Vector2.Distance(body.position, point) > 0.13f &&
                       Vector2.Distance(body.position, goal) > stopDistance &&
                       Time.time < stepDeadline && Time.time < deadline)
                {
                    Vector2 before = body.position;
                    Vector2 direction = point - body.position;
                    body.linearVelocity = direction.normalized * walkSpeed;
                    animation.ShowMovement(body.linearVelocity);
                    yield return new WaitForFixedUpdate();

                    stillTime = Vector2.Distance(before, body.position) < 0.008f
                        ? stillTime + Time.fixedDeltaTime : 0f;
                    if (stillTime <= 1f) continue;
                    blocked = true;
                    break;
                }
                if (blocked || Time.time >= deadline ||
                    Vector2.Distance(body.position, goal) <= stopDistance) break;
            }
            Stop();
        }

        reached = Vector2.Distance(body.position, goal) <= stopDistance;
        Stop();
    }

    private void Finish()
    {
        Stop();
        IsBusy = false;
        patrol.enabled = true;
        combat.enabled = true;
    }

    private void Stop()
    {
        body.linearVelocity = Vector2.zero;
        animation.ShowMovement(Vector2.zero);
    }
}
