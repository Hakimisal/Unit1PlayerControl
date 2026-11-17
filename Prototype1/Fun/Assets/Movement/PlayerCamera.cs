using UnityEngine;

public class EldenRingCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Camera Position")]
    public float distance = 5f;
    public float height = 1.5f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2.5f;

    [Header("Camera Rotation")]
    public float minPitch = -35f;
    public float maxPitch = 70f;

    [Header("Smoothing")]
    public float positionSmoothness = 12f;
    public float rotationSmoothness = 15f;

    [Header("Camera Collision")]
    public bool enableCollision = true;
    public float collisionRadius = 0.2f;
    public LayerMask collisionLayers = ~0;

    private float yaw;
    private float pitch = 15f;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("Camera player reference is missing!");
            enabled = false;
            return;
        }

        yaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        HandleRotation();
        HandlePosition();
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        Quaternion targetRotation = Quaternion.Euler(
            pitch,
            yaw,
            0f
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSmoothness * Time.deltaTime)
        );
    }

    private void HandlePosition()
    {
        Vector3 targetPoint =
            player.position + Vector3.up * height;

        Vector3 desiredPosition =
            targetPoint - transform.forward * distance;

        if (enableCollision)
        {
            Vector3 direction = desiredPosition - targetPoint;
            float desiredDistance = direction.magnitude;

            if (Physics.SphereCast(
                targetPoint,
                collisionRadius,
                direction.normalized,
                out RaycastHit hit,
                desiredDistance,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                desiredPosition =
                    targetPoint +
                    direction.normalized *
                    Mathf.Max(0f, hit.distance - collisionRadius);
            }
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-positionSmoothness * Time.deltaTime)
        );
    }
}
