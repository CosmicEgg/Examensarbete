using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuscleManager : MonoBehaviour
{
    public List<Muscle> muscles = new List<Muscle>();
    public float numbOfMuscles;
    float time, cycle;
    float cycleSpeed = 3f;

    public void CreateNewMuscles(GameObject parent, GameObject child)
    {
        numbOfMuscles = Random.Range(1, 3);

        for (int i = 0; i < numbOfMuscles; i++)
        {
            Muscle m = new Muscle(parent, child);
            m.CreateNewMuscle();
            muscles.Add(m);
        }
    }
    public void CreateRefMuscles(GameObject parent, GameObject child, List<Muscle> refMuscle, Vector3 refAxis)
    {
        foreach (Muscle muscle in refMuscle)
        {
            Muscle m = new Muscle(parent, child);
            m.CreateRefMuscle(muscle, refAxis);
            muscles.Add(m);
        }
    }
    public void CreateRecurssionMuscles(GameObject parent, GameObject child, List<Muscle> refMuscle)
    {
        foreach (Muscle muscle in refMuscle)
        {
            Muscle m = new Muscle(parent, child);
            m.CreateRecurssionMuscle(muscle);
            muscles.Add(m);
        }
    }

    void Update()
    {
        time += Time.deltaTime;
        cycle = Mathf.Sin(time * cycleSpeed);

        if (cycle > 0)
        {
            foreach (Muscle m in muscles)
            {
                //m.Contraction();
            }
        }
        else
        {
            foreach (Muscle m in muscles)
            {
                //m.Relaxation();
            }
        }

        foreach (Muscle m in muscles)
        {
            m.DrawMuscle();
        }
    }
}
public class Muscle
{
    public Vector3 relaxationDistance, connectedAnchor, anchor;
    ConfigurableJoint distanceJoint;
    public JointDrive jointDrive;
    public GameObject emptyParent, emptyChild;
    GameObject child, parent;
    public Vector3 connectedAnchorInLocal;

    public Muscle(GameObject parent, GameObject child)
    {
        jointDrive = new JointDrive();
        this.parent = parent;
        this.child = child;
    }

    public void CreateNewMuscle()
    {
        jointDrive.positionSpring = 0f;
        jointDrive.maximumForce = 3.402823e+38f;
        jointDrive.positionDamper = 0;

        Collider parentCollider = parent.GetComponent<Collider>();
        Collider childCollider = child.GetComponent<Collider>();

        connectedAnchor = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

        GameObject placementOnParent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnParent.transform.position = connectedAnchor;

        Collider collider = placementOnParent.GetComponent<Collider>();

        Vector3 directionToMove;
        float distance;

        if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
            parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance))
        {
            placementOnParent.transform.position += (directionToMove * (distance));
        }

        placementOnParent.transform.position = parentCollider.ClosestPoint(placementOnParent.transform.position);

        emptyParent = new GameObject();
        emptyParent.transform.position = placementOnParent.transform.position;
        emptyParent.transform.parent = parent.transform;

        placementOnParent.transform.parent = emptyParent.transform;
        placementOnParent.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        anchor = new Vector3(Random.Range(childCollider.bounds.min.x, childCollider.bounds.max.x),
                Random.Range(childCollider.bounds.min.y, childCollider.bounds.max.y), Random.Range(childCollider.bounds.min.z, childCollider.bounds.max.z));

        GameObject placementOnChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnChild.transform.position = anchor;

        collider = placementOnChild.GetComponent<Collider>();

        if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
            childCollider, childCollider.transform.position, childCollider.transform.rotation, out directionToMove, out distance))
        {
            placementOnChild.transform.position += (directionToMove * (distance));
        }

        placementOnChild.transform.position = childCollider.ClosestPoint(placementOnChild.transform.position);
        emptyChild = new GameObject();
        emptyChild.transform.position = placementOnChild.transform.position;
        emptyChild.transform.parent = child.transform;

        placementOnChild.transform.parent = emptyChild.transform;
        placementOnChild.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        connectedAnchor = emptyParent.transform.localPosition;
        anchor = emptyChild.transform.localPosition;

        distanceJoint = child.AddComponent<ConfigurableJoint>();
        distanceJoint.autoConfigureConnectedAnchor = false;
        distanceJoint.connectedBody = parent.GetComponent<Rigidbody>();
        distanceJoint.anchor = anchor;
        distanceJoint.connectedAnchor = connectedAnchor;
        distanceJoint.xDrive = jointDrive;
        distanceJoint.yDrive = jointDrive;
        distanceJoint.zDrive = jointDrive;
        distanceJoint.enableCollision = true;

        relaxationDistance = placementOnParent.transform.position - placementOnChild.transform.position;
        distanceJoint.targetPosition = relaxationDistance;
        connectedAnchorInLocal = emptyParent.transform.position - parent.transform.position;
    }
    public void CreateRefMuscle(Muscle muscle, Vector3 refAxis)
    {
        jointDrive = muscle.jointDrive;

        connectedAnchor = Vector3.Reflect(muscle.connectedAnchorInLocal, refAxis) + parent.transform.position;

        GameObject placementOnParent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnParent.transform.position = connectedAnchor;

        emptyParent = new GameObject();
        emptyParent.transform.position = placementOnParent.transform.position;
        emptyParent.transform.parent = parent.transform;

        placementOnParent.transform.parent = emptyParent.transform;
        placementOnParent.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        GameObject placementOnChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnChild.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        emptyChild = new GameObject();
        placementOnChild.transform.parent = emptyChild.transform;
        placementOnChild.transform.localPosition = Vector3.zero;

        emptyChild.transform.position = emptyParent.transform.position - Vector3.Reflect(muscle.relaxationDistance, refAxis);
        emptyChild.transform.parent = child.transform;

        connectedAnchor = emptyParent.transform.localPosition;
        anchor = emptyChild.transform.localPosition;

        distanceJoint = child.AddComponent<ConfigurableJoint>();
        distanceJoint.autoConfigureConnectedAnchor = false;
        distanceJoint.connectedBody = parent.GetComponent<Rigidbody>();
        distanceJoint.anchor = anchor;
        distanceJoint.connectedAnchor = connectedAnchor;
        distanceJoint.xDrive = jointDrive;
        distanceJoint.yDrive = jointDrive;
        distanceJoint.zDrive = jointDrive;
        distanceJoint.enableCollision = true;

        relaxationDistance = placementOnParent.transform.position - placementOnChild.transform.position;
        distanceJoint.targetPosition = relaxationDistance;
    }

    public void CreateRecurssionMuscle(Muscle muscle)
    {
        jointDrive = muscle.jointDrive;

        GameObject placementOnParent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnParent.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        emptyParent = new GameObject();
        placementOnParent.transform.parent = emptyParent.transform;
        placementOnParent.transform.localPosition = Vector3.zero;

        emptyParent.transform.parent = parent.transform;
        emptyParent.transform.localPosition = muscle.emptyParent.transform.localPosition;


        GameObject placementOnChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        placementOnChild.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        emptyChild = new GameObject();
        placementOnChild.transform.parent = emptyChild.transform;
        placementOnChild.transform.localPosition = Vector3.zero;

        emptyChild.transform.parent = child.transform;
        emptyChild.transform.localPosition = muscle.emptyChild.transform.localPosition;

        connectedAnchor = emptyParent.transform.localPosition;
        anchor = emptyChild.transform.localPosition;

        distanceJoint = child.AddComponent<ConfigurableJoint>();
        distanceJoint.autoConfigureConnectedAnchor = false;
        distanceJoint.connectedBody = parent.GetComponent<Rigidbody>();
        distanceJoint.anchor = anchor;
        distanceJoint.connectedAnchor = connectedAnchor;
        distanceJoint.xDrive = jointDrive;
        distanceJoint.yDrive = jointDrive;
        distanceJoint.zDrive = jointDrive;
        distanceJoint.enableCollision = true;

        relaxationDistance = placementOnParent.transform.position - placementOnChild.transform.position;
        distanceJoint.targetPosition = relaxationDistance;
    }
    public void Contraction()
    {
        jointDrive.positionSpring = 100f;
        jointDrive.maximumForce = 3.402823e+38f;
        jointDrive.positionDamper = 0;
        distanceJoint.xDrive = jointDrive;
        distanceJoint.yDrive = jointDrive;
        distanceJoint.zDrive = jointDrive;
        distanceJoint.targetPosition = Vector3.zero;
    }
    public void Relaxation()
    {
        jointDrive.positionSpring = 1f;
        jointDrive.maximumForce = 3.402823e+38f;
        jointDrive.positionDamper = 0;
        distanceJoint.xDrive = jointDrive;
        distanceJoint.yDrive = jointDrive;
        distanceJoint.zDrive = jointDrive;
        distanceJoint.targetPosition = relaxationDistance;
    }
    public void DrawMuscle()
    {
        Debug.DrawLine(emptyChild.transform.position, emptyParent.transform.position, Color.red);
    }

}

