using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

public class RobotAgent : MonoBehaviour
{
    public float speed = 5f; // Robot movement speed
    public Transform stackLocation; // Stack location to drop objects
    public int maxObjectsInStack = 5; // Max objects per stack
    private bool carryingObject = false; // Is the robot carrying an object?
    private GameObject objectInHand = null; // The object the robot is carrying
    private NavMeshAgent navAgent; // The NavMeshAgent for pathfinding
    private int objectCount = 0; // Current count of objects in the stack
    private float rotationSpeed = 100f; // Speed of robot rotation
    private bool isYielding = false; // Flag to check if robot is yielding
    private Transform targetObject; // The target object the robot is moving towards
    private float backupTime = 2f; // Duration for the robot to back up
    private float detectionRange = 10f; // Range within which the robot can detect objects

    // Start is called before the first frame update
    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = speed;
        // Example: Set a target (for demonstration purposes, you should have your own target logic)
        GameObject target = GameObject.FindGameObjectWithTag("Target");
        if (target != null)
        {
            targetObject = target.transform;
            navAgent.SetDestination(targetObject.position);
        }
        else
        {
            Debug.LogWarning("Target object not found. Please ensure there is a GameObject with the tag 'Target' in the scene.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (targetObject != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(targetObject.position);
        }
    }

    // Detect nearby objects, robots, and walls
    void DetectSurroundings()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, detectionRange))
        {
            if (hit.collider.CompareTag("Object"))
            {
                PickUpObject(hit.collider.gameObject);
            }
            else if (hit.collider.CompareTag("Robot"))
            {
                HandleCollision(hit.collider.gameObject);
            }
            else if (hit.collider.CompareTag("Wall"))
            {
                AvoidWall();
            }
        }
    }

    // Move robot forward
    void MoveForward()
    {
        if (!carryingObject)
        {
            navAgent.Move(transform.forward * speed * Time.deltaTime);
        }
    }

    // Rotate robot to face the target object
    void RotateRobotTowardsTarget()
    {
        if (objectInHand != null)
        {
            Vector3 direction = objectInHand.transform.position - transform.position;
            direction.y = 0; // Keep rotation on the y-axis only
            Quaternion toRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
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
        transform.Translate(Vector3.back * Time.deltaTime * speed);
        yield return new WaitForSeconds(backupTime); // Backup for a set duration

        // After backup, resume movement
        isYielding = false;
        navAgent.isStopped = false;
    }

    // Avoid wall by changing direction or stopping
    void AvoidWall()
    {
        Debug.Log("Wall detected. Adjusting path.");
        navAgent.isStopped = true;
        // Implement additional logic here to change direction or find a new path around obstacles
    }

    // Pick up an object (only one object at a time)
    void PickUpObject(GameObject obj)
    {
        if (!carryingObject)
        {
            Debug.Log("Picking up object: " + obj.name);
            objectInHand = obj;
            carryingObject = true;
            obj.transform.SetParent(transform); // Attach the object to the robot
            obj.transform.localPosition = new Vector3(0, 1, 0); // Position it in front of the robot
        }
    }

    // Drop the object at a stack location
    void DropObject()
    {
        if (carryingObject && objectInHand != null)
        {
            Debug.Log("Dropping object: " + objectInHand.name);
            objectInHand.transform.SetParent(null); // Detach object from robot
            objectInHand.transform.position = stackLocation.position; // Place at stack location
            carryingObject = false;
            objectInHand = null;
            objectCount++;

            if (objectCount >= maxObjectsInStack)
            {
                Debug.Log("Stack limit reached! Sending robot to a new task.");
            }
        }
    }

    // Communicate robot actions to the Python server
    IEnumerator SendDataToServer(string robotAction)
    {
        WWWForm form = new WWWForm();
        form.AddField("robot_action", robotAction);  // Example data (you can send more details)

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:5000/step", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data sent successfully: " + robotAction);
            }
            else
            {
                Debug.LogError("Error sending data: " + www.error);
            }
        }
    }

    // Process the server response and act accordingly
    void ProcessServerResponse(string response)
    {
        // Implement logic to move or drop objects based on response from the server
        if (response.Contains("move"))
        {
            Vector3 newPos = new Vector3(5, 0, 5);  // Example position, could be returned from Python
            MoveToPosition(newPos);
        }

        if (response.Contains("drop"))
        {
            DropObject();
        }
    }

    public void MoveToPosition(Vector3 position)
    {
        navAgent.SetDestination(position);
    }

    // Perform an action (e.g., pick up or drop an object)
    public void PerformAction(string action)
    {
        switch (action)
        {
            case "pickup":
                break;
            case "drop":
                DropObject();
                break;
            case "move":
                break;
        }
    }
}