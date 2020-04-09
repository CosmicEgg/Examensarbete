using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    public List<Node> nodes;
    List<int> nodeOrder;
    Stack<Node> nodeStack;
    int recursiveLimit;
    int numOfNodes;
    Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        numOfNodes = Random.Range(0, 11);
        recursiveLimit = 3;
        startPosition = Vector3.zero;
        nodes = new List<Node>();
        nodeOrder = new List<int>();
        nodeStack = new Stack<Node>();

        //Spawn Nodes
        for (int i = 0; i < numOfNodes; i++)
        {
            Node newNode = new Node();
            newNode.id = i;
            nodes.Add(newNode);
        }

        //Add Children
        for (int x = 0; x < nodes.Count; x++)
        {
            for (int y = 0; y < nodes[x].numOfChildren; y++)
            {
                nodes[x].children.Add(nodes[Random.Range(0, numOfNodes)]);
            }
        }

        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0].id);
        nodes[0].stacked = true;

        //Start DFS
        while(nodeStack.Count > 0)
        {
            Node currentNode = nodeStack.Peek();
            bool startOver = false;

            if (currentNode.children.Count == 0)
            {
                nodeStack.Pop();
                continue;
            }

            foreach (Node child in currentNode.children)
            {
                if (!child.visited)
                {
                    startOver = false;
                    break;
                }
                else
                    startOver = true;
            }

            if (startOver)
            {
                nodeStack.Pop();
                continue;
            }

            foreach (Node child in currentNode.children)
            {
                if(!child.visited)
                {
                    nodeStack.Push(child);
                    nodeOrder.Add(child.id);
                    child.visited = true;
                    break;
                }
            }
        }

        foreach (int id in nodeOrder)
        {
            Debug.Log(id);
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
    public int numOfChildren = Random.Range(0, 5);
    public bool stacked;
    public int id;
    public List<Node> children = new List<Node>();
}

public class Edge
{
    public Node from;
    public Node to;
    public bool traversed = false;
}


