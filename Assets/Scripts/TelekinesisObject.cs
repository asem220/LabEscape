using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TelekinesisObject : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference throwAction;

    [Header("Telekinesis")]
    [SerializeField] private float pickupRange = 4f;
    [SerializeField] private float holdDistance = 1.8f;
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float minThrowImpulse = 4f;
    [SerializeField] private float maxThrowImpulse = 18f;

    private PlayerEnergy energy;
    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private SpriteRenderer visual;
    private Color originalColor;
    private InputAction pointInput;
    private InputAction interactInput;
    private InputAction throwInput;

    private Vector2 aim = Vector2.down;
    private bool held;
    private bool charging;
    private float charge;

    private void Awake()
    {
        energy = player != null ? player.GetComponent<PlayerEnergy>() : null;
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        originalColor = visual.color;
        // Each object owns its input; hiding one box cannot disable another box.
        pointInput = pointAction != null ? pointAction.action.Clone() : null;
        interactInput = interactAction != null ? interactAction.action.Clone() : null;
        throwInput = throwAction != null ? throwAction.action.Clone() : null;

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.linearDamping = 2f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        pointInput?.Enable();
        interactInput?.Enable();
        throwInput?.Enable();
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (held && energy != null && !energy.UseTelekinesis(Time.deltaTime)) Drop();
        if (player == null || worldCamera == null ||
            pointInput == null || interactInput == null ||
            throwInput == null)
            return;

        Vector2 screen =
            pointInput.ReadValue<Vector2>();

        Vector3 cursor = worldCamera.ScreenToWorldPoint(
            new Vector3(screen.x, screen.y,
                transform.position.z - worldCamera.transform.position.z)
        );

        Vector2 direction =
            (Vector2)cursor - (Vector2)player.position;

        if (direction.sqrMagnitude > 0.01f)
            aim = direction.normalized;

        if (interactInput.WasPressedThisFrame())
        {
            if (held)
            {
                Drop();
                return;
            }

            bool inRange = Vector2.Distance(
                player.position, body.position
            ) <= pickupRange;

            if (inRange && boxCollider.OverlapPoint(cursor) && (energy == null || energy.Magic > 0f))
            {
                held = true;
                charging = false;
                charge = 0f;
                body.linearVelocity = Vector2.zero;
            }
        }

        if (!held)
            return;

        if (throwInput.WasPressedThisFrame())
        {
            charging = true;
            charge = 0f;
        }

        if (charging && throwInput.IsPressed())
        {
            charge = Mathf.Clamp01(
                charge + Time.deltaTime /
                Mathf.Max(0.01f, chargeDuration)
            );
        }

        visual.color =
            Color.Lerp(Color.cyan, Color.blue, charge);

        if (charging && throwInput.WasReleasedThisFrame())
        {
            float impulse = Mathf.Lerp(
                minThrowImpulse, maxThrowImpulse, charge
            );

            Drop();
            body.AddForce(aim * impulse, ForceMode2D.Impulse);
        }
    }

    private void FixedUpdate()
    {
        if (!held || player == null)
            return;

        Vector2 destination =
            (Vector2)player.position + aim * holdDistance;

        body.linearVelocity = Vector2.ClampMagnitude(
            (destination - body.position) * 10f, 15f
        );
    }

    private void Drop()
    {
        held = false;
        charging = false;
        charge = 0f;

        if (body != null)
            body.linearVelocity = Vector2.zero;

        if (visual != null)
            visual.color = originalColor;
    }

    private void OnDisable()
    {
        Drop();

        pointInput?.Disable();
        interactInput?.Disable();
        throwInput?.Disable();
    }

    private void OnDestroy()
    {
        pointInput?.Dispose();
        interactInput?.Dispose();
        throwInput?.Dispose();
    }
}
