using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class JointManager : MonoBehaviour
{
    Joint joint;
    GameObject parent;
    Transform transform, parentTransform;

    public void AddRandomJoint(GameObject parent)
    {
        this.parent = parent;
        transform = gameObject.transform;
        parentTransform = parent.transform;
        int random = Random.Range(0, 3);
        random = 2;
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
                //Spherical joint I.e ball and socket
                joint = gameObject.AddComponent<CharacterJoint>();
                SetupCharacterJoint();
                break;

            case 3:
                //prismatic joint = no rotation and movement only along one axis (distance joint)
                joint = gameObject.AddComponent<ConfigurableJoint>();
                SetupPrismaticJoint();
                break;

            case 4:
                //cylindrical joint = prismatic + revolute joint
                joint = gameObject.AddComponent<ConfigurableJoint>();
                SetupCylindricalJoint();
                break;

            default:
                break;
        }
    }

    private void SetupPrismaticJoint()
    {
        joint.connectedBody = parent.GetComponent<Rigidbody>();
        joint.enableCollision = true;
    }

    private void SetupCharacterJoint()
    {
        Collider parentCollider = parent.GetComponent<Collider>();
        Collider childCollider = GetComponent<Collider>();

        GameObject placementOnParent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnParent.GetComponent<Collider>().isTrigger = true;
        placementOnParent.transform.position = parentCollider.ClosestPoint(transform.position);

        GameObject emptyParent = new GameObject();
        emptyParent.transform.position = placementOnParent.transform.position;
        emptyParent.transform.parent = parent.transform;
        placementOnParent.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        placementOnParent.transform.parent = emptyParent.transform;


        GameObject placementOnChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnChild.GetComponent<Collider>().isTrigger = true;
        placementOnChild.transform.position = childCollider.ClosestPoint(placementOnParent.transform.position);

        GameObject emptyChild = new GameObject();
        emptyChild.transform.position = placementOnChild.transform.position;
        emptyChild.transform.parent = transform;
        placementOnChild.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        placementOnChild.transform.parent = emptyChild.transform;


        GameObject middle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        middle.GetComponent<Collider>().isTrigger = true;
        middle.transform.position = placementOnChild.transform.position + ((placementOnParent.transform.position - placementOnChild.transform.position) / 2);
        middle.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        middle.transform.parent = transform;

        joint.connectedBody = parent.GetComponent<Rigidbody>();
        joint.anchor = middle.transform.localPosition;
        joint.enableCollision = true;
    }

    private void SetupFixedJoint()
    {
        joint.connectedBody = parent.GetComponent<Rigidbody>();
        joint.enableCollision = true;
    }

    private void SetupHingeJoint()
    {
        //  joint.autoConfigureConnectedAnchor = true;
        Collider parentCollider = parent.GetComponent<Collider>();
        Collider childCollider = GetComponent<Collider>();

        GameObject placementOnParent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnParent.GetComponent<Collider>().isTrigger = true;
        placementOnParent.transform.position = parentCollider.ClosestPoint(transform.position);

        GameObject emptyParent = new GameObject();
        emptyParent.transform.position = placementOnParent.transform.position;
        emptyParent.transform.parent = parent.transform;
        placementOnParent.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        placementOnParent.transform.parent = emptyParent.transform;


        GameObject placementOnChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnChild.GetComponent<Collider>().isTrigger = true;
        placementOnChild.transform.position = childCollider.ClosestPoint(placementOnParent.transform.position);

        GameObject emptyChild = new GameObject();
        emptyChild.transform.position = placementOnChild.transform.position;
        emptyChild.transform.parent = transform;
        placementOnChild.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        placementOnChild.transform.parent = emptyChild.transform;


        GameObject middle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        middle.GetComponent<Collider>().isTrigger = true;
        middle.transform.position = placementOnChild.transform.position + ((placementOnParent.transform.position - placementOnChild.transform.position) / 2);
        middle.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        middle.transform.parent = transform;

        joint.connectedBody = parent.GetComponent<Rigidbody>();
        joint.anchor = middle.transform.localPosition;
        joint.enableCollision = true;
    }

    private void SetupCylindricalJoint()
    {
        joint.connectedBody = parent.GetComponent<Rigidbody>();
        joint.enableCollision = true;
    }
}
