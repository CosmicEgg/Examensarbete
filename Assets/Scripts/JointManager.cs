using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class JointManager : MonoBehaviour
{
    Joint joint;
    GameObject child;
    Transform transform, childTransform;

    public void AddRandomJoint(GameObject child)
    {
        this.child = child;
        transform = gameObject.transform;
        childTransform = child.transform;
        int random = Random.Range(0, 2);
        //random = 0;
        switch (random)
        {
            case 0:
                //Simply connects two rigidbodies. Similiar to parenting but relying on physics instead
                joint = gameObject.AddComponent<FixedJoint>();
                SetupFixedJoint();
                break;

            case 1:
                //Revolute Joint
                joint = gameObject.AddComponent<HingeJoint>();
                SetupHingeJoint();
                break;

            case 2:
                //cylindrical joint = prismatic + revolute joint
                joint = gameObject.AddComponent<ConfigurableJoint>();
                SetupCylindricalJoint();
                break;

            case 3:
                //prismatic joint = no rotation and movement in only along one axisx
                joint = gameObject.AddComponent<ConfigurableJoint>();
                SetupPrismaticJoint();
                break;

            case 4:
                //Spherical joint I.e ball and socket
                joint = gameObject.AddComponent<CharacterJoint>();
                break;
            default:
                break;
        }

        joint.autoConfigureConnectedAnchor = false;
    }

    private void SetupPrismaticJoint()
    {
        joint.connectedBody = child.GetComponent<Rigidbody>();
        joint.enableCollision = true;
    }

    private void SetupFixedJoint()
    {
        joint.connectedBody = child.GetComponent<Rigidbody>();
        joint.enableCollision = true;
    }

    private void SetupHingeJoint()
    {
        joint.connectedBody = child.GetComponent<Rigidbody>();
        joint.enableCollision = true;
    }

    private void SetupCylindricalJoint()
    {
        joint.connectedBody = child.GetComponent<Rigidbody>();
        joint.enableCollision = true;
    }
}
