using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class NpcAnimation : MonoBehaviour
{
    private enum Direction { Down, DownSide, Side, UpSide, Up }

    [SerializeField] private Direction initialFacing = Direction.Down;
    [SerializeField] private bool hasAttack;
    [SerializeField, Min(0.1f)] private float attackDuration = 0.7f;

    private Animator animator;
    private SpriteRenderer sprite;
    private Direction facing;
    private string currentState;
    private float attackEndTime;

    public bool IsAttacking => Time.time < attackEndTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        facing = initialFacing;
    }

    private void OnEnable()
    {
        currentState = null;
    }

    private void Start()
    {
        ShowMovement(Vector2.zero);
    }

    public void ShowMovement(Vector2 movement)
    {
        if (IsAttacking) return;

        bool walking = movement.sqrMagnitude > 0.01f;
        if (walking)
        {
            facing = GetDirection(movement);
            sprite.flipX = movement.x < -0.01f;
        }

        Play((walking ? "Walk " : "Idle ") + facing);
    }

    // Other gameplay scripts can call this when guard combat is added.
    public void Attack()
    {
        if (!hasAttack || IsAttacking) return;

        attackEndTime = Time.time + attackDuration;
        Play("Attack " + facing);
    }

    public void CancelAttack()
    {
        attackEndTime = 0f;
        ShowMovement(Vector2.zero);
    }

    private void Play(string state)
    {
        if (state == currentState || animator == null || !animator.isActiveAndEnabled) return;
        animator.Play(state, 0, 0f);
        currentState = state;
    }

    private static Direction GetDirection(Vector2 movement)
    {
        float x = Mathf.Abs(movement.x);
        float y = Mathf.Abs(movement.y);

        if (x < y * 0.4f)
            return movement.y < 0f ? Direction.Down : Direction.Up;
        if (y < x * 0.4f)
            return Direction.Side;
        return movement.y < 0f ? Direction.DownSide : Direction.UpSide;
    }
}
