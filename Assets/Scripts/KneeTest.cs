using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KneeTest : MonoBehaviour
{
    SpringJoint j1, j2;

    // Start is called before the first frame update
    void Start()
    {
        SpringJoint[] joints = GetComponents<SpringJoint>();
        
        j1 = joints[0];
        j2 = joints[1];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        j1.spring = 0;
        j2.spring = 0;


        if (Input.GetKeyDown(KeyCode.X))
        {
            //Lower limit of the distance range over which the spring will not apply any force.
            j1.minDistance = 20;
            //Upper limit of the distance range over which the spring will not apply any force.
            j1.maxDistance = 20;
            //Spring is the force keeping to objects together
            j1.spring = 100;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            //Lower limit of the distance range over which the spring will not apply any force.
            j2.minDistance = 20;
            //Upper limit of the distance range over which the spring will not apply any force.
            j2.maxDistance = 20;
            //Spring is the force keeping to objects together
            j2.spring = 100;
        }
    }
}
