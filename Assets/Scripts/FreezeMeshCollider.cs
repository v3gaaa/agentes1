using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeMeshCollider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Find the Rigidbody component on the object
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
