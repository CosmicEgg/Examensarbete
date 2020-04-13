using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    public List<Node> nodes, finalNodes;
    List<int> nodeOrder;
    Stack<Node> nodeStack;
    Stack<Node> recurssionStack;
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
        finalNodes = new List<Node>();
        recurssionStack = new Stack<Node>();
        nodeOrder = new List<int>();
        nodeStack = new Stack<Node>();

        //Spawn Nodes
        for (int i = 0; i < numOfNodes; i++)
        {
            Node node = new Node();
            node.id = i;
            nodes.Add(node);
        }

        //Add Children
        for (int x = 0; x < nodes.Count; x++)
        {
            for (int y = 0; y < nodes[x].numOfChildren; y++)
            {
                nodes[x].edges.Add(new Edge(nodes[x], nodes[Random.Range(0, numOfNodes)]));
            }
        }

        finalNodes = nodes;

        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0].id);
        nodes[0].stacked = true;

        //Start DFS to find all recursive nodes
        while(nodeStack.Count > 0)
        {
            Node currentNode = nodeStack.Peek();
            bool startOver = false;

            if (currentNode.edges.Count == 0)
            {
                nodeStack.Pop();
                continue;
            }

            foreach (Edge edge in currentNode.edges)
            {
                if (!edge.traversed)
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


            //!!Måste se till att detta inte händer varje gång vi startar om loopen!! Gjorde fullösning med en till bool
            //Check all edges for recursive edges and add these to recursionStack
            if (!currentNode.visited)
            {
                currentNode.visited = true;

                foreach (Edge edge in currentNode.edges)
                {
                    if (edge.to == edge.from)
                    {
                        for (int i = 0; i < recursiveLimit; i++)
                        {
                            recurssionStack.Push(currentNode);
                        }
                    }
                }
            }
           
            foreach (Edge edge in currentNode.edges)
            {
                //If not already traversed and not a recursive edge
                if(!edge.traversed && edge.to != currentNode)
                {
                    //If not already in stack i.e. edge is not a backwards edge
                    if (!edge.to.stacked)
                    {
                        edge.to.stacked = true;
                        nodeStack.Push(edge.to);
                        nodeOrder.Add(edge.to.id);
                    }

                    edge.traversed = true;
                    break;
                }
            }
        }

        foreach (int id in nodeOrder)
        {
            Debug.Log(id);
        }

        //Begin adding recursive nodes to graph
        //Tror vi måste göra en DFS från varje recursiv nod för att hitta alla barnen och skapa en helt ny gren nedåt
        while (recurssionStack.Count > 0)
        {
            Node temp = recurssionStack.Pop();
            List<Node> subTree = new List<Node>();
            
            Edge edge = new Edge(nodes[temp.id], temp);
            finalNodes[temp.id].edges.Add(edge);
        }


        //Redo graph traversal on now complete tree creating all geometry
    }
}

public class Node
{
    //GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
    public bool visited = false;
    public int numOfChildren = Random.Range(0, 5);
    public bool stacked;
    public int id;
    //public List<Node> children = new List<Node>();
    public List<Edge> edges = new List<Edge>();
}

public class Edge
{
    public Node from;
    public Node to;
    public int traversions = 0;

    public Edge(Node from, Node to)
    {
        this.from = from;
        this.to = to;
    }

    public bool traversed = false;
}


