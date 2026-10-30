using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private float speed = 4f;

    private Rigidbody2D body;
    private Animator animator;
    private Vector2 movement;

    private string facing = "Down";
    private string currentAnimation;

    public string Facing => facing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.action.Enable();

        currentAnimation = null;
    }

    private void Update()
    {
        movement = moveAction != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        movement = Vector2.ClampMagnitude(movement, 1f);

        bool isMoving = movement.sqrMagnitude > 0.01f;

        if (isMoving)
            UpdateFacing();

        string nextAnimation =
            (isMoving ? "Player_Walk_" : "Player_Idle_") + facing;

        if (nextAnimation != currentAnimation)
        {
            animator.Play(nextAnimation, 0, 0f);
            currentAnimation = nextAnimation;
        }
    }

    private void FixedUpdate()
    {
        body.linearVelocity = movement * speed;
    }

    private void UpdateFacing()
{
    if (movement.y > 0.1f)
    {
        if (movement.x > 0.1f)
            facing = "Left_Up";
        else if (movement.x < -0.1f)
            facing = "Right_Up";
        else
            facing = "Up";
    }
    else if (movement.y < -0.1f)
    {
        if (movement.x > 0.1f)
            facing = "Left_Down";
        else if (movement.x < -0.1f)
            facing = "Right_Down";
        else
            facing = "Down";
    }
    else
    {
        facing = movement.x > 0f
            ? "Left_Down"
            : "Right_Down";
    }
}

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();

        movement = Vector2.zero;

        if (body != null)
            body.linearVelocity = Vector2.zero;
    }
}
