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

        for (int x = 0; x < nodes.Count; x++)
        {
            for (int y = 0; y < nodes[x].numOfChildren; y++)
            {
                nodes[x].children.Add(nodes[Random.Range(0, numOfNodes + 1)]);
            }
        }

        for (int i = 0; i < ; i++)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class Node
{
    //GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
    public int numOfChildren = Random.Range(0, 11);
    public List<Node> children = new List<Node>();
}

public class Edge
{

}

