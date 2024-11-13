using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform robot;
    public Vector3 offset;

    void Start()
    {

        offset = transform.position - robot.position;
    }
    void LateUpdate()
    {
        // Update the camera's position to follow the robot with the offset
        transform.position = robot.position + robot.TransformDirection(offset);

        // Optionally, you can make the camera look at the robot
        transform.LookAt(robot);
    }
}
