using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    // Robot attributes
    public float speed = 5.0f;
    public float jumpForce = 5.0f;
    public float rotationSpeed = 100.0f;
    private bool isGrounded;

    private Rigidbody rb;  // Reference to the Rigidbody component

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
        rb.freezeRotation = true; // Prevent the robot from rotating randomly
    }

    // Update is called once per frame
    void Update()
    {
        // Get input from WASD keys for movement
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Create a movement vector for horizontal and vertical movement
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // Apply movement to the Rigidbody
        Vector3 velocity = movement * speed;
        velocity.y = rb.velocity.y; // Keep the current Y velocity (to respect gravity)
        rb.velocity = velocity; // Apply the velocity to the Rigidbody

        // Jumping (optional)
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // Apply jump force
        }

        // Rotation (optional)
        if (Input.GetKey(KeyCode.Q))
        {
            rb.AddTorque(Vector3.up * -rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            rb.AddTorque(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }

    // Check if the robot is grounded for jumping purposes
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
