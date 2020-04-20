using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    public GameObject prefab;
    List<GameObject> geometry;
    public List<Node> nodes;
    List<int> nodeOrder;
    Stack<Node> nodeStack;
    Stack<Node> recurssionStack;
    int numOfNodes;
    Vector3 startPosition;

    // Start is called before the first frame update
    void Start()
    {
        geometry = new List<GameObject>();
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
        InterpretTree(rootNode);

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

    private void InterpretTree(Node root)
    {
        //Root Node def.
        Queue<Node> nodeQueue = new Queue<Node>();
        nodeQueue.Enqueue(root);

        CreateRootGeometry(root);

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

            foreach (Edge e in currentNode.edges)
            {
                foreach (Node n in duplicates)
                {
                    if (e.to == n)
                    {
                        e.traversed = true;
                    }
                }
            }

            //Region for multiple edges to one node
            //Occurences contain the number of times a duplicate exists stored 
            //as an integer in indexing corresponding to duplicates indexing
            List<int> occurences = new List<int>(duplicates.Count);
            for (int i = 0; i < duplicates.Count; i++)
            {
                occurences.Add(0);
                foreach (Node m in tempNodes)
                {
                    if (duplicates[i].Equals(m))
                    {
                        occurences[i]++;
                    }
                }
            }

            //Handle duplicates and mark them as traversed
            for (int i = 0; i < duplicates.Count; i++)
            {
                for (int j = 0; j < occurences[i]; j++)
                {

                    //Jämn spegling runt ett plan
                    if (occurences[i] % 2 == 0)
                    {
                        //duplicates[i].
                    }
                    else //Ojämnt vilket betyder att vi tar en och riktar den i ett annat plan och speglar de tidigare jämna som vanligt 
                    {

                    }
                }

                nodeQueue.Enqueue(duplicates[i]);
            }

            //Creating all not already traversed normal children/nodes
            for (int i = 0; i < currentNode.edges.Count; i++)
            {
                if (!currentNode.edges[i].traversed)
                {
                    currentNode.edges[i].traversed = true;

                    CreateSingleEdgeGeometry(currentNode.edges[i].to, currentNode);

                    nodeQueue.Enqueue(currentNode.edges[i].to);
                }
            }
        }
    }


    public void CreateRootGeometry(Node node)
    {
        GameObject segment = Instantiate(prefab, new Vector3(0, 10, 0), Quaternion.identity);
        GeometryInfo info = segment.GetComponent<GeometryInfo>();
        info.SetID(node.id);
        segment.transform.localScale = new Vector3(1, 1, 1);
        node.created = true;
        node.gameObjects.Add(segment);
        geometry.Add(segment);
    }

    //Detta är en metod för att skapa single edge geometry 
    public void CreateSingleEdgeGeometry(Node node, Node parent)
    {
        if (parent.gameObjects.Count == 0)
        {
            return;
        }

        //Förälder Geo information
        GameObject parentGeometry = parent.gameObjects[0];
        BoxCollider parentBoxCollider = parentGeometry.GetComponent<BoxCollider>();
        Rigidbody parentRigidBody = parentGeometry.GetComponent<Rigidbody>();

        //Random punkt på förälder
        Vector3 randomPoint = new Vector3(Random.Range(parentBoxCollider.bounds.min.x, parentBoxCollider.bounds.max.x),
            Random.Range(parentBoxCollider.bounds.min.y, parentBoxCollider.bounds.max.y), Random.Range(parentBoxCollider.bounds.min.z, parentBoxCollider.bounds.max.z));

        Vector3 randomPlacementDirection = (randomPoint - parentGeometry.transform.position).normalized;
        randomPoint = randomPoint + (randomPlacementDirection * 5f);

        Vector3 parentBoundPoint = parentBoxCollider.ClosestPoint(randomPoint);

        GameObject currentGeometry = Instantiate(prefab, parentBoundPoint, Quaternion.identity);  
        GeometryInfo info = currentGeometry.GetComponent<GeometryInfo>();
        info.SetID(node.id);
        info.SetParentID(parent.id);
        currentGeometry.transform.localScale = node.scale;
        Rigidbody rigidBody = currentGeometry.GetComponent<Rigidbody>();
        BoxCollider boxCollider = currentGeometry.GetComponent<BoxCollider>();
        boxCollider.transform.localScale = currentGeometry.transform.localScale;

        //Max Distance från förälder
        Vector3 parentPlacementDirection = (parentBoundPoint - parentGeometry.transform.position).normalized;

        float largetAxis = Mathf.Max(node.scale.x, node.scale.y, node.scale.z);
        Vector3 maxDistanceToParent = parentBoundPoint + (parentPlacementDirection * largetAxis);

        currentGeometry.transform.position = maxDistanceToParent;

        //Närmaste punkten på nya geometrin från förälder punkten
        Vector3 closestPointOnCurrent = rigidBody.ClosestPointOnBounds(parentBoundPoint);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = closestPointOnCurrent;
        sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        //GameObject sphereParent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphereParent.transform.position = parentBoundPoint;
        //sphereParent.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        //Flytta Current i den rikting den distansen
        Vector3 distPointToPoint = parentBoundPoint - closestPointOnCurrent;

        //currentGeometry.transform.position += distPointToPoint;
    }
}




public class Node
{
    public bool created = false;
    PrimitiveType Cube;
    bool symmetry;
    bool terminalOnly;
    public Vector3 scale = Vector3.one * 3f/* new Vector3(Random.Range(0.1f, 2f), Random.Range(0.1f, 2f), Random.Range(0.1f, 2f))*/;
    public Quaternion rotation;
    public List<GameObject> gameObjects = new List<GameObject>();

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

    public Node(){}

    public Transform GetTransform(GameObject gameObject)
    {
        Transform transform = gameObject.transform;
        //transform.position = this.position;
        transform.localScale = this.scale;
        transform.rotation = this.rotation;

        return transform;
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

