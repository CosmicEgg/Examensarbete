using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class muscleTest : MonoBehaviour
{
    public GameObject child1;
    public GameObject child2;


    // Start is called before the first frame update
    void Start()
    {
        Vector3 axis = transform.forward;
        GameObject point1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject point2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        point1.transform.parent = child1.transform;
        point2.transform.parent = child2.transform;

        point1.transform.localScale = new Vector3(0.1f,0.1f, 0.1f);
        point2.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        point1.transform.localPosition = new Vector3(0f, 0f,0.5f);
        point2.transform.localPosition = new Vector3(0f, 0f, 0.5f);

        Vector3 forw = child1.transform.forward;
        Vector3 mirrored = Vector3.Reflect(forw, axis);

        child2.transform.position = Vector3.Reflect(transform.position-child1.transform.position, axis) + transform.position;
        child2.transform.rotation = Quaternion.LookRotation(mirrored, child1.transform.up);
        child2.transform.localScale = new Vector3(child2.transform.localScale.x, child2.transform.localScale.y, child2.transform.localScale.z*-1);
    }

    // Update is called once per frame
    void Update()
    {

    }

}
