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
                currentNode.stacked = false;
                continue;
            }

            foreach (Edge edge in currentNode.edges)
            {
                //If all are traversed we are finished with this node
                if (!edge.traversed)
                {
                    startOver = false;
                    break;
                }
                else
                    startOver = true;
            }

            //Pop and start over
            if (startOver)
            {
                nodeStack.Pop();
                currentNode.stacked = false;
                continue;
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

            foreach (Edge edge in currentNode.edges)
            {
                if (!edge.traversed && edge.to == currentNode)
                {
                    //If not already in stack i.e. edge is not a backwards edge
                    recurssionStack.Push(edge.to);
                    nodeOrder.Add(edge.to.id);
                    
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
            Node currentNode = recurssionStack.Peek();
            Node newNode = new Node();
            newNode.visited = true;
            newNode.id = currentNode.id;
            nodes.Add(newNode);


            foreach (Edge e in currentNode.edges)
            {
                if (e.to != currentNode && !e.to.visited)
                {
                    Node child = new Node
                    Edge temp  = new Edge(newNode, new Node(e.to));

                }
            }

            Edge edge = new Edge(nodes[currentNode.id], newNode);
            currentNode.edges.Add(edge);


            List<Node> subTree = new List<Node>();



            //finalNodes[temp.id].edges.Add(edge);
        }


        //Redo graph traversal on now complete tree creating all geometry
    }

    public void SubTreeDFS(Node root)
    {
        Stack<Node> nodeStack = new Stack<Node>();
        List<int> nodeOrder = new List<int>();

        nodeStack.Push(root);
        nodeOrder.Add(root.id);
        root.stacked = true;

        //Start DFS to find all recursive nodes
        while (nodeStack.Count > 0)
        {
            Node currentNode = nodeStack.Peek();
            bool startOver = false;

            if (currentNode.edges.Count == 0)
            {
                nodeStack.Pop();
                currentNode.stacked = false;
                continue;
            }

            foreach (Edge edge in currentNode.edges)
            {
                //If all are traversed we are finished with this node
                if (!edge.traversed)
                {
                    startOver = false;
                    break;
                }
                else
                    startOver = true;
            }

            //Pop and start over
            if (startOver)
            {
                nodeStack.Pop();
                currentNode.stacked = false;
                continue;
            } 

            foreach (Edge edge in currentNode.edges)
            {
                //If not already traversed and not a recursive edge
                if (!edge.traversed && edge.to != currentNode)
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

            foreach (Edge edge in currentNode.edges)
            {
                if (!edge.traversed && edge.to == currentNode)
                {
                    //If not already in stack i.e. edge is not a backwards edge
                    recurssionStack.Push(edge.to);
                    nodeOrder.Add(edge.to.id);

                    edge.traversed = true;
                    break;
                }
            }
        }
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

    public Node()
    {

    }

    public Node(Node other)
    {

    }

    public Node DeepCopy()
    {
        Node other = (Node) this.MemberwiseClone();
        other.edges = new List<Edge>(edges);
        return other;
    }
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


