using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    public List<Node> nodes;
    public List<Edge> edges;
    int numOfNodes;
    Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        numOfNodes = Random.Range(0, 11);
        startPosition = Vector3.zero;
        nodes = new List<Node>();
        edges = new List<Edge>();

        for (int i = 0; i < numOfNodes; i++)
        {
            nodes.Add(new Node());
        }



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class Node
{
    GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
}

public class Edge
{

}

