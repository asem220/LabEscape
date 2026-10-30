using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerEnergy), typeof(PlayerMovement), typeof(Animator))]
public class PlayerCondition : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float collapseFood = 10f;
    [SerializeField, Min(0f)] private float collapseMagic = 10f;
    [SerializeField] private CombatArea combatZone;
    [SerializeField] private Transform wardReturn;

    private PlayerEnergy energy;
    private PlayerMovement movement;
    private Rigidbody2D body;
    private Animator animator;
    private InputAction stunAction;
    private float nextStunTime;
    private Vector2 startingPosition;
    private Transform carrier;
    private Rigidbody2D carrierBody;
    private Vector2 carryOffset = Vector2.down * 0.65f;

    public float Health { get; private set; }
    public float MaxHealth => maxHealth;
    public float HealthFraction => Mathf.Clamp01(Health / maxHealth);
    public bool IsUnconscious { get; private set; }
    public bool InCombatZone => combatZone != null && combatZone.Contains(transform.position);

    private void Awake()
    {
        energy = GetComponent<PlayerEnergy>();
        movement = GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Health = maxHealth;
        startingPosition = transform.position;
        stunAction = new InputAction("Stun", InputActionType.Button, "<Keyboard>/f");
        stunAction.performed += _ => StunNearbyGuard();
    }

    private void OnEnable() => stunAction.Enable();
    private void OnDisable() => stunAction.Disable();
    private void OnDestroy() => stunAction.Dispose();

    private void Update()
    {
        if (!IsUnconscious &&
            (energy.Food <= collapseFood || energy.Magic <= collapseMagic))
            Collapse();
    }

    private void LateUpdate()
    {
        if (carrier == null || !IsUnconscious) return;
        if (carrierBody != null && carrierBody.linearVelocity.sqrMagnitude > 0.01f)
            carryOffset = -carrierBody.linearVelocity.normalized * 0.65f;
        Vector2 position = (Vector2)carrier.position + carryOffset;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    public void TakeDamage(float amount)
    {
        if (IsUnconscious || amount <= 0f) return;
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f) Collapse();
    }

    private void StunNearbyGuard()
    {
        if (IsUnconscious || !InCombatZone || Time.time < nextStunTime) return;
        GuardCombat nearest = null;
        float distance = 2f;
        foreach (GuardCombat guard in FindObjectsByType<GuardCombat>())
        {
            if (!combatZone.Contains(guard.transform.position) || !guard.enabled) continue;
            float current = Vector2.Distance(transform.position, guard.transform.position);
            if (current >= distance) continue;
            nearest = guard;
            distance = current;
        }
        if (nearest == null || !energy.UseTelekinesis(1.5f)) return;
        nearest.Stun(2f);
        nextStunTime = Time.time + 0.5f;
    }

    private void Collapse()
    {
        if (IsUnconscious) return;
        IsUnconscious = true;
        string facing = movement.Facing;
        movement.enabled = false;
        body.linearVelocity = Vector2.zero;
        animator.Play("Player_Fall_" + facing, 0, 0f);

        GuardEscort nearest = null;
        float shortest = float.PositiveInfinity;
        var path = new List<Vector2>();
        foreach (GuardEscort guard in FindObjectsByType<GuardEscort>())
        {
            if (guard.IsBusy || !LabPathfinder.TryFind(guard.transform.position, transform.position, path)) continue;
            float length = 0f;
            Vector2 previous = guard.transform.position;
            foreach (Vector2 point in path)
            {
                length += Vector2.Distance(previous, point);
                previous = point;
            }
            if (length >= shortest) continue;
            shortest = length;
            nearest = guard;
        }
        if (nearest != null)
            nearest.Begin(this, wardReturn != null ? (Vector2)wardReturn.position : startingPosition);
        else
            StartCoroutine(RecoverWithoutGuard());
    }

    private IEnumerator RecoverWithoutGuard()
    {
        yield return new WaitForSeconds(2f);
        RecoverAtWard();
    }

    public void BeginCarry(Transform guard)
    {
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        carrier = guard;
        carrierBody = guard.GetComponent<Rigidbody2D>();
    }

    public void RecoverAtWard()
    {
        carrier = null;
        carrierBody = null;
        Vector2 position = wardReturn != null ? (Vector2)wardReturn.position : startingPosition;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        body.position = position;
        body.simulated = true;
        body.linearVelocity = Vector2.zero;
        Health = Mathf.Min(maxHealth, 60f);
        energy.RestoreMinimum(40f, 40f);
        IsUnconscious = false;
        movement.enabled = true;
    }
}
