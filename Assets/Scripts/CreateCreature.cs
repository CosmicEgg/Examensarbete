using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    public List<Node> nodes;
    List<int> nodeOrder;
    Stack<Node> nodeStack;
    Stack<Node> recurssionStack;
    int numOfNodes;
    Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        numOfNodes = Random.Range(1, 11);
        startPosition = Vector3.zero;
        nodes = new List<Node>();
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
                nodes[x].edges.Add(new Edge(nodes[x], nodes[Random.Range(0, numOfNodes)], Random.Range(0, 4), 0));
            }
        }

        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0].id);
        nodes[0].stacked = true;

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


            for (int i = 0; i < currentNode.edges.Count; i++)
            {
                if (!currentNode.edges[i].traversed && !currentNode.edges[i].to.Equals(currentNode))
                {
                    //If not already in stack i.e. edge is not a backwards edge
                    currentNode.edges[i].traversed = true;

                    if (!currentNode.edges[i].to.stacked)
                    {
                        currentNode.edges[i].to.stacked = true;
                        nodeStack.Push(currentNode.edges[i].to);
                        nodeOrder.Add(currentNode.edges[i].to.id);
                    }
                    else //Remove backwards edge
                    {
                        currentNode.edges.RemoveAt(i);
                    }


                    break;
                }
            }

            foreach (Edge edge in currentNode.edges)
            {
                if (!edge.traversed && edge.to.Equals(currentNode))
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
            bool startOver = false;
            Node currentNode = recurssionStack.Peek();

            if (currentNode.edges.Count == 0)
            {
                recurssionStack.Pop();
                continue;
            }

            for (int i = 0; i < currentNode.edges.Count; i++)
            {
                if (currentNode.edges[i].to.Equals(currentNode.edges[i].from)
                    && currentNode.edges[i].numOfTravels < currentNode.edges[i].recursiveLimit)
                {
                    currentNode.edges[i].numOfTravels++;
                    Node newNode = new Node(currentNode);
                    nodes.Add(newNode);
                    currentNode.edges.Add(new Edge(currentNode, newNode, currentNode.edges[i].recursiveLimit, currentNode.edges[i].numOfTravels));
                    recurssionStack.Push(newNode);
                    currentNode.edges.RemoveAt(i);
                    startOver = true;
                    break;
                }
            }

            if (startOver)
            {
                continue;
            }

            recurssionStack.Pop();

        }

        Node rootNode = nodes[0];
        ResetTree(ref rootNode);
        CreateGeometry(rootNode);

        }


    private void ResetTree(ref Node node)
    {
        foreach (Edge e in node.edges)
        {
            if (e.traversed)
            {
                e.traversed = false;
                e.numOfTravels = 0;
                ResetTree(ref e.to);
            }
        }
    }

    private void CreateGeometry(Node root)
    {
        //Root Node def.
        Queue<Node> nodeQueue = new Queue<Node>();
        nodeQueue.Enqueue(root);

        while (nodeQueue.Count > 0)
        {
            Node currentNode = nodeQueue.Peek();
            bool startOver = false;

            if (currentNode.edges.Count == 0)
            {
                nodeQueue.Dequeue();
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
                nodeQueue.Dequeue();
                continue;
            }

            List<Node> tempNodes = new List<Node>();
            foreach (Edge e in currentNode.edges)
            {
                tempNodes.Add(e.to);
            }

            var myhash = new HashSet<Node>();
            var mylist = tempNodes;
            var duplicates = mylist.Where(item => !myhash.Add(item)).Distinct().ToList();

            List<int> occurences = new List<int>();
            for (int i = 0; i < duplicates.Count; i++)
            {
                foreach (Node m in tempNodes)
                {
                    if (duplicates[i].Equals(m))
                    {
                        occurences[i]++;
                    }
                }
            }

            for (int i = 0; i < duplicates.Count; i++)
            {
                for (int j = 0; j < occurences[i]; j++)
                {
                    //Skapa denna så många gånar på något utspritt sett runt om föräldranoden 
                    duplicates[i].
                }
            }






            for (int i = 0; i < currentNode.edges.Count; i++)
            {
                if (!currentNode.edges[i].traversed)
                {
                    currentNode.edges[i].traversed = true;

                    nodeQueue.Enqueue(currentNode.edges[i].to);

                    break;
                }
            }
        }
}



public class Node
{
        //GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bool created = false;
    PrimitiveType Cube;
    float scale;
    Vector3 position;
    Transform Rotation;
    bool symmetry;
    bool terminalOnly;

    public int numOfChildren = Random.Range(0, 5);
    public bool stacked;
    public int id;

    public List<Edge> edges = new List<Edge>();

    public Node(Node other)
    {
        id = other.id;

        foreach(Edge e in other.edges)
        {
            if (!e.to.Equals(e.from))
                edges.Add(new Edge(this, new Node(e.to), e.recursiveLimit, e.numOfTravels));
            if (e.to.Equals(e.from))
                edges.Add(new Edge(this, this, e.recursiveLimit, e.numOfTravels));
        }
    }

    public Node()
    {

    }
}

    public class Edge
    {
        public Node from;
        public Node to;
        public int numOfTravels;
        public int recursiveLimit;

        public Edge(Node from, Node to, int recursiveLimit, int numOfTravels)
        {
            this.from = from;
            this.to = to;
            this.recursiveLimit = recursiveLimit;
            this.numOfTravels = numOfTravels;
        }

        public bool traversed = false;
    }
}
