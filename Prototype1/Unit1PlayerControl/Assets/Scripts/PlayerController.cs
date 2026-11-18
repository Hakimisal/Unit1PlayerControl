using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Movement Tuning(Editable in the inspector)
    public float Speed = 5.0f;
    public float turnSpeed;

    // Input System action exposed in inspector for binding(WASD/ArrowKeys)
    public InputAction moveAction;
   
    // Current input value( x = left/right, y = forward/back, kept pricate for internal use)
    private Vector2 moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Enable the moveAction so it starts reading input
        moveAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        // Read the 2D vector from the MoveAction(x: horizontal, y: vertical)
        moveInput = moveAction.ReadValue<Vector2>();

        // move forward/back along Z using the y component
        transform.Translate(Vector3.forward * Time.deltaTime * Speed * moveInput.y);
       
        // Rotate around local Y (yaw) using the x component
        transform.Rotate(Vector3.up, Time.deltaTime * turnSpeed * moveInput.x);
        
    }
}
