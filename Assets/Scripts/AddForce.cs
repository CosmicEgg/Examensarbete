using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddForce : MonoBehaviour
{
    Rigidbody rb;
    Transform tr;
    HingeJoint hJoint;
    JointMotor motor;
    float targetVelocity = 1000;
    float force = 1000;

    void Start()
    {
        if (TryGetComponent<HingeJoint>(out hJoint))
        {
            motor = hJoint.motor;
        }

        tr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Debug.DrawRay(tr.position, tr.forward);
        //Left joint 
         if (Input.GetKey(KeyCode.X))
        {
            hJoint.useMotor = true;
            motor.targetVelocity = targetVelocity;
            motor.force = force;
            hJoint.motor = motor;
        }

        if (Input.GetKey(KeyCode.C))
        {
            hJoint.useMotor = true;
            motor.targetVelocity = -targetVelocity;
            motor.force = force;
            hJoint.motor = motor;
        }
    }
}
