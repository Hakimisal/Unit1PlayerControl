using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EldenRingMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float acceleration = 10f;
    public float deceleration = 12f;
    public float rotationSpeed = 12f;

    [Header("Jump Settings")]
    public float jumpHeight = 1.6f;
    public float doubleJumpHeight = 1.8f;
    public float gravity = -22f;
    public int maxJumps = 2;

    [Header("Mantle Settings")]
    public bool enableMantling = true;
    public float mantleDistance = 1.1f;
    public float minMantleHeight = 0.5f;
    public float maxMantleHeight = 2f;
    public float mantleSpeed = 0.35f;
    public LayerMask mantleLayers;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;

    private Vector3 moveVelocity;
    private float verticalVelocity;

    private int jumpCount;
    private bool isMantling;
    private bool wasGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        jumpCount = 0;
        verticalVelocity = -2f;
        wasGrounded = controller.isGrounded;
    }

    private void Update()
    {
        if (isMantling)
            return;

        bool grounded = controller.isGrounded;

        // Reset jumps after landing.
        if (grounded && verticalVelocity <= 0f)
        {
            jumpCount = 0;
            verticalVelocity = -2f;
        }

        // Walking off a ledge uses the first jump.
        if (!grounded && wasGrounded && verticalVelocity <= 0f)
        {
            jumpCount = Mathf.Max(jumpCount, 1);
        }

        HandleJump();
        HandleMovement();
        HandleGravity();

        wasGrounded = controller.isGrounded;
    }

    private void HandleJump()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        // Mantle if a climbable ledge is nearby.
        if (enableMantling && TryMantle())
            return;

        // No more jumps available.
        if (jumpCount >= maxJumps)
            return;

        // First jump or double jump.
        float height = jumpCount == 0
            ? jumpHeight
            : doubleJumpHeight;

        verticalVelocity = Mathf.Sqrt(
            height * -2f * gravity
        );

        jumpCount++;

        Debug.Log("Jump: " + jumpCount);
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(x, 0f, z).normalized;

        bool sprinting = Input.GetKey(KeyCode.LeftShift);

        float speed = sprinting
            ? sprintSpeed
            : walkSpeed;

        Vector3 targetVelocity = Vector3.zero;

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 forward;
            Vector3 right;

            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;
            }
            else
            {
                forward = Vector3.forward;
                right = Vector3.right;
            }

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 direction =
                forward * input.z +
                right * input.x;

            direction.Normalize();

            targetVelocity = direction * speed;

            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
            );
        }

        float smoothing = targetVelocity.sqrMagnitude > 0.01f
            ? acceleration
            : deceleration;

        moveVelocity = Vector3.Lerp(
            moveVelocity,
            targetVelocity,
            1f - Mathf.Exp(-smoothing * Time.deltaTime)
        );
    }

    private void HandleGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;

        // Move horizontally and vertically together.
        Vector3 finalMovement =
            moveVelocity +
            Vector3.up * verticalVelocity;

        CollisionFlags collisions = controller.Move(
            finalMovement * Time.deltaTime
        );

        // Stop upward velocity if we hit a ceiling.
        if ((collisions & CollisionFlags.Above) != 0 &&
            verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        // Ground collision.
        if ((collisions & CollisionFlags.Below) != 0 &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
    }

    private bool TryMantle()
    {
        Vector3 forward = transform.forward;

        Vector3 wallOrigin =
            transform.position + Vector3.up * 0.5f;

        // Detect a wall in front of us.
        if (!Physics.Raycast(
            wallOrigin,
            forward,
            out RaycastHit wallHit,
            mantleDistance,
            mantleLayers,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // Check the top of the wall.
        Vector3 topOrigin =
            wallHit.point +
            forward * 0.3f +
            Vector3.up * (maxMantleHeight + 1f);

        if (!Physics.Raycast(
            topOrigin,
            Vector3.down,
            out RaycastHit topHit,
            maxMantleHeight + 2f,
            mantleLayers,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // Surface must be relatively flat.
        if (Vector3.Angle(topHit.normal, Vector3.up) > 35f)
            return false;

        float feetY =
            transform.position.y +
            controller.center.y -
            controller.height * 0.5f;

        float ledgeHeight = topHit.point.y - feetY;

        if (ledgeHeight < minMantleHeight ||
            ledgeHeight > maxMantleHeight)
        {
            return false;
        }

        Vector3 targetPosition =
            topHit.point +
            forward * (controller.radius + 0.2f);

        targetPosition.y =
            topHit.point.y -
            controller.center.y +
            controller.height * 0.5f +
            0.05f;

        // Check that there is space to stand.
        Vector3 center = targetPosition + controller.center;

        float radius = controller.radius * 0.9f;

        float halfHeight = Mathf.Max(
            controller.height * 0.5f,
            radius
        );

        Vector3 bottom =
            center +
            Vector3.up * (-halfHeight + radius + 0.1f);

        Vector3 top =
            center +
            Vector3.up * (halfHeight - radius);

        Collider[] hits = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            mantleLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            // Ignore our own colliders.
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        StartCoroutine(MantleCoroutine(targetPosition));

        return true;
    }

    private IEnumerator MantleCoroutine(Vector3 targetPosition)
    {
        isMantling = true;

        moveVelocity = Vector3.zero;
        verticalVelocity = 0f;

        controller.enabled = false;

        Vector3 startPosition = transform.position;

        float timer = 0f;

        while (timer < mantleSpeed)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / mantleSpeed);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 newPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothT
            );

            // Small upward arc during the mantle.
            newPosition.y += Mathf.Sin(t * Mathf.PI) * 0.25f;

            transform.position = newPosition;

            yield return null;
        }

        transform.position = targetPosition;

        controller.enabled = true;

        verticalVelocity = -2f;

        jumpCount = 0;
        wasGrounded = true;

        isMantling = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 origin =
            transform.position + Vector3.up * 0.5f;

        Gizmos.DrawLine(
            origin,
            origin + transform.forward * mantleDistance
        );
    }
}
