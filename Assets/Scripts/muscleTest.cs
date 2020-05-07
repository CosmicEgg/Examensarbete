using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class muscleTest : MonoBehaviour
{
    public GameObject parent;



    private int numbOfMuscles;

    private float cycle;
    private float time;
    private float cycleSpeed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        cycle = Mathf.Sin(time * cycleSpeed);

    }

    void AttachMuscle()
    {

    }
}
