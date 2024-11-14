using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

public class RobotAgent : MonoBehaviour
{
    public float speed = 5f; // Robot movement speed
    public Transform stackLocation; // Stack location to drop objects
    private NavMeshAgent navAgent; // The NavMeshAgent for pathfinding
    private bool isYielding = false; // Flag to check if robot is yielding
    private float backupTime = 2f; // Duration for the robot to back up
    private Animator robotAnimator; // Animator component
    private bool carryingObject = false; // Is the robot carrying an object?
    private GameObject objectInHand = null; // The object the robot is carrying
    private int objectCount = 0; // Current count of objects in the stack

    // Start is called before the first frame update
    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        robotAnimator = GetComponent<Animator>();
        navAgent.speed = speed;
        StartCoroutine(GetNextAction());
    }

    // Update is called once per frame
    void Update()
    {
        // Handle any continuous updates if needed
    }

    // Get the next action from the Python server
    IEnumerator GetNextAction()
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get("http://localhost:5000/next_action"))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string action = www.downloadHandler.text.Trim().Trim('"'); // Remove quotes and trim whitespace
                    Debug.Log("Received action: " + action);
                    PerformAction(action);
                }
                else
                {
                    Debug.LogError("Error getting next action: " + www.error);
                }
            }

            yield return new WaitForSeconds(1f); // Wait for a short duration before getting the next action
        }
    }

    // Perform an action based on the server's response
    void PerformAction(string action)
    {
        Debug.Log("Performing action: " + action);
        switch (action)
        {
            case "move_forward":
                MoveForward();
                break;
            case "rotate":
                RotateRobotTowardsTarget();
                break;
            case "stop":
                navAgent.isStopped = true;
                break;
            case "resume":
                navAgent.isStopped = false;
                break;
            default:
                Debug.LogWarning("Unknown action: " + action);
                break;
        }
    }

    // Move robot forward
    void MoveForward()
    {
        Debug.Log("Moving forward");
        navAgent.Move(transform.forward * speed * Time.deltaTime);
    }

    // Rotate robot to face the target object
    void RotateRobotTowardsTarget()
    {
        Debug.Log("Rotating towards target");
        // Example rotation logic (you can customize this)
        Vector3 direction = stackLocation.position - transform.position;
        direction.y = 0; // Keep rotation on the y-axis only
        Quaternion toRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, speed * Time.deltaTime);
        robotAnimator.SetTrigger("Turn"); // Set the Turn trigger
    }

    // Pick up an object
    void PickUpObject(GameObject obj)
    {
        if (!carryingObject)
        {
            Debug.Log("Picking up object: " + obj.name);
            objectInHand = obj;
            carryingObject = true;
            obj.transform.SetParent(transform);
            obj.transform.localPosition = new Vector3(0, 1, 0);
            robotAnimator.SetTrigger("PickUp");
            StartCoroutine(SendDataToServer("PickUp"));
        }
    }

    // Drop the object at a stack location
    void DropObject()
    {
        if (carryingObject && objectInHand != null)
        {
            Debug.Log("Dropping object: " + objectInHand.name);
            objectInHand.transform.SetParent(null);
            objectInHand.transform.position = stackLocation.position;
            carryingObject = false;
            objectInHand = null;
            objectCount++;
            robotAnimator.SetTrigger("Drop");
            StartCoroutine(SendDataToServer("Drop"));
        }
    }

    // Send data to the Python server
    IEnumerator SendDataToServer(string action)
    {
        WWWForm form = new WWWForm();
        form.AddField("robot_action", action);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:5000/robot_action", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data sent successfully: " + action);
            }
            else
            {
                Debug.LogError("Error sending data: " + www.error);
            }
        }
    }

    // Handle collision with another robot (simplified version)
    void HandleCollision(GameObject otherRobot)
    {
        Debug.Log("Collision detected with: " + otherRobot.name);
        if (!isYielding)
        {
            isYielding = true;
            navAgent.isStopped = true; // Stop robot movement
            StartCoroutine(BackUp(otherRobot)); // Backup for a short duration
        }
    }

    // Back up the robot when collision is detected
    IEnumerator BackUp(GameObject otherRobot)
    {
        Debug.Log("Backing up");
        transform.Translate(Vector3.back * Time.deltaTime * speed);
        yield return new WaitForSeconds(backupTime); // Backup for a set duration
        isYielding = false;
        navAgent.isStopped = false; // Resume robot movement
    }

    // Handle collision with the target
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            Debug.Log("Collided with target: " + collision.gameObject.name);
        }
    }
}