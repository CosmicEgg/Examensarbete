using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuscleManager : MonoBehaviour
{
    public List<Muscle> muscles = new List<Muscle>();
    public float numbOfMuscles;

    public void CreateNewMuscles(GameObject parent, GameObject child, Node node)
    {
        for (int i = 0; i < node.numbOfMuscles; i++)
        {
            Muscle m = new Muscle(parent, child, node.offSetCycle);
            m.CreateNewMuscle(node, node.muscleSeeds[i,0], node.muscleSeeds[i, 1], node.muscleSeeds[i, 2]);
            muscles.Add(m);
        }
    }
    public void CreateRefMuscles(GameObject parent, GameObject child, List<Muscle> refMuscle, Vector3 refAxis, Node node)
    {
        float offSetCycle = 0;

        if (refMuscle != null)
        {
            if(refMuscle.Count != 0)
            {
                if (refMuscle[0] != null)
                    offSetCycle = refMuscle[0].offSetCycle;
            }
        }

        if (offSetCycle == 0)
            offSetCycle = Mathf.PI;
        else
            offSetCycle = 0;

        foreach (Muscle muscle in refMuscle)
        {
            Muscle m = new Muscle(parent, child, offSetCycle);
            m.CreateRefMuscle(muscle, refAxis);
            muscles.Add(m);
        }
    }
    public void CreateRecurssionMuscles(GameObject parent, GameObject child, List<Muscle> refMuscle, Node node)
    {
        float offSetCycle = 0;

        if (refMuscle != null)
        {
            if (refMuscle.Count != 0)
            {
                if (refMuscle[0] != null)
                    offSetCycle = refMuscle[0].offSetCycle;
            }
        }

        if (offSetCycle == 0)
            offSetCycle = Mathf.PI;
        else
            offSetCycle = 0;

        foreach (Muscle muscle in refMuscle)
        {
            Muscle m = new Muscle(parent, child, offSetCycle);
            m.CreateRecurssionMuscle(muscle);
            muscles.Add(m);
        }
    }

    public void UpdateMuscles()
    {
        foreach (Muscle m in muscles)
        {
            m.UpdateMuscle();
            m.DrawMuscle();
        }
    }

    public void Relax()
    {
        foreach (Muscle m in muscles)
        {
            m.Relaxation();
        }
    }

    private void OnDestroy()
    {
        foreach (Muscle m in muscles)
        {
            Destroy(m.emptyChild);
            Destroy(m.emptyParent);
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
    public float strength;
    float time = 0;
    float cycle;
    float cycleSpeed;
    public float offSetCycle;

    public int strenghtSeed, anchorSeed, connectedAnchorSeed;

    public Muscle(GameObject parent, GameObject child, float offSetCycle)
    {
        jointDrive = new JointDrive();
        this.parent = parent;
        this.child = child;
        this.offSetCycle = offSetCycle;
        cycleSpeed = Mathf.PI;
    }

    public void UpdateMuscle()
    {
        time += Time.deltaTime;
        cycle = Mathf.Sin(time * cycleSpeed + offSetCycle);

        if (cycle > 0)
        {
           Contraction();      
        }
        else
        {
           Relaxation();
        }
    }

    public void CreateNewMuscle(Node node, int strenghtSeed, int anchorSeed,int connectedAnchorSeed)
    {
        Random.InitState(strenghtSeed);
        strength = Random.Range(0.0f, 1.0f);

        jointDrive.positionSpring = 0f;
        jointDrive.maximumForce = 3.402823e+38f * strength;
        jointDrive.positionDamper = 0;

        Collider parentCollider = parent.GetComponent<Collider>();
        Collider childCollider = child.GetComponent<Collider>();

        Random.InitState(connectedAnchorSeed);
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

        Random.InitState(anchorSeed);
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

        Random.InitState(node.seed);
    }

    public void CreateRefMuscle(Muscle muscle, Vector3 refAxis)
    {
        jointDrive = muscle.jointDrive;
        strength = muscle.strength;
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
        connectedAnchorInLocal = emptyParent.transform.position - parent.transform.position;
    }

    public void CreateRecurssionMuscle(Muscle muscle)
    {
        jointDrive = muscle.jointDrive;
        strength = muscle.strength;

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
        connectedAnchorInLocal = emptyParent.transform.position - parent.transform.position;
    }

    public void Contraction()
    {
        jointDrive.positionSpring = 400f * strength;
        jointDrive.maximumForce = 400f * strength;
        jointDrive.positionDamper = 1;
        distanceJoint.xDrive = jointDrive;
        distanceJoint.yDrive = jointDrive;
        distanceJoint.zDrive = jointDrive;
        distanceJoint.targetPosition = relaxationDistance / 2;
    }
    public void Relaxation()
    {
        jointDrive.positionSpring = 0;
        jointDrive.maximumForce = 0;
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

