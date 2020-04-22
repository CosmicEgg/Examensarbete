using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    List<GameObject> geometry;
    public List<Node> nodes;
    List<int> nodeOrder;
    Stack<Node> nodeStack;
    Stack<Node> recurssionStack;
    int numOfNodes;
    Vector3 startPosition;
    Node root;
    bool generated = false;

    int primitiveRand;
    float minScale, maxScale;

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

        minScale = 0.2f;
        maxScale = 2f;

        //Spawn Nodes
        for (int i = 0; i < numOfNodes; i++)
        {
            primitiveRand = Random.Range(0, 3);
            Vector3 rotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            Node node = new Node(primitiveRand, minScale, maxScale, rotation, i);
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

        root = nodes[0];
        ResetTree(ref root);
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

        while (nodeQueue.Count > 0 || nodeQueue.Count < 1000)
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
                if (!edge.to.created)
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
                if(currentNode != e.to)
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
                foreach (Node n in tempNodes)
                {
                    if (duplicates[i].Equals(n))
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
            }

            //Creating all not already traversed normal children/nodes
            for (int i = 0; i < currentNode.edges.Count; i++)
            {
                if (!currentNode.edges[i].traversed && !currentNode.edges[i].to.created)
                {
                    currentNode.edges[i].traversed = true;

                    CreateSingleEdgeGeometry(currentNode.edges[i].to, currentNode);

                    currentNode.edges[i].to.created = true;
                    nodeQueue.Enqueue(currentNode.edges[i].to);
                }
            }
        }
    }

    public void FixedUpdate()
    {
        if (!generated)
        {
            generated = true;
            InterpretTree(root);
        }
    }

    public void CreateRootGeometry(Node node)
    {
        GameObject segment = GameObject.CreatePrimitive(node.primitiveType);
        segment.transform.position = new Vector3(0, 10, 0);
        segment.transform.rotation = Quaternion.Euler(node.rotation);
        segment.transform.localScale = node.scale;
        segment.AddComponent<Rigidbody>();
        Rigidbody rb = segment.GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        node.created = true;
        node.gameObjects.Add(segment);
        geometry.Add(segment);
    }

    //Detta är en metod för att skapa single edge geometry 
    public void CreateSingleEdgeGeometry(Node node, Node parent)
    {
        if (parent == node || parent.gameObjects.Count == 0)
        {
            return;
        }

        foreach(GameObject pg in parent.gameObjects)
        {
            GameObject currentGeometry;

            //Förälder Geo information
            GameObject parentGeometry = pg;
            Collider parentCollider = parentGeometry.GetComponent<Collider>();
            Rigidbody parentRigidBody = parentGeometry.GetComponent<Rigidbody>();

            bool created = false;
            int tries = 0;
            while (!created)
            {
                tries++;
                if (tries > 100)
                {
                    Debug.Log("To many tries");
                    break;
                }
                created = true;
                //Random punkt på förälder
                Vector3 randomPoint = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                    Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

                currentGeometry = GameObject.CreatePrimitive(node.primitiveType);
                currentGeometry.transform.position = randomPoint;
                currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                currentGeometry.transform.localScale = node.scale;
                currentGeometry.AddComponent<Rigidbody>();
                Rigidbody rb = currentGeometry.GetComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                Collider collider = currentGeometry.GetComponent<Collider>();

                Vector3 directionToMove;
                float distance = 0;

                if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance))
                {
                    currentGeometry.transform.position += (directionToMove * (distance));
                }

                foreach (GameObject g in geometry)
                {
                    Collider gCollider = g.GetComponent<Collider>();

                    if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    gCollider, gCollider.transform.position, gCollider.transform.rotation, out directionToMove, out distance) && g != parentGeometry)
                    {
                        Destroy(currentGeometry);
                        created = false;
                        break;
                    }
                }

                if (created)
                {
                    node.gameObjects.Add(currentGeometry);
                    geometry.Add(currentGeometry);
                }
            }
        }
    }
}

public class Node
{
    float randUniScale;

    public bool created = false;
    public PrimitiveType primitiveType;
    public Vector3 scale;
    public Vector3 rotation;
    public List<GameObject> gameObjects = new List<GameObject>();
    public int numOfChildren = Random.Range(0, 5);
    public bool stacked;
    public int id;

    public List<Edge> edges = new List<Edge>();

    public Node(Node other)
    {
        id = other.id;
        scale = other.scale;
        rotation = other.rotation;
        primitiveType = other.primitiveType;

        foreach(Edge e in other.edges)
        {
            if (!e.to.Equals(e.from))
                edges.Add(new Edge(this, new Node(e.to), e.recursiveLimit, e.numOfTravels));
            if (e.to.Equals(e.from))
                edges.Add(new Edge(this, this, e.recursiveLimit, e.numOfTravels));
        }
    }

    public Node(){}

    public Node(int primitiveRand,float minScale, float maxScale, Vector3 rotation, int id)
    {
        switch (primitiveRand)
        {
            case 0:
                primitiveType = PrimitiveType.Cube;
                scale = new Vector3(Random.Range(minScale, maxScale), Random.Range(minScale, maxScale), Random.Range(minScale, maxScale));
                break;
            case 1:
                primitiveType = PrimitiveType.Capsule;
                randUniScale = Random.Range(minScale, maxScale);
                scale = new Vector3(randUniScale, randUniScale, randUniScale);
                break;
            case 2:
                primitiveType = PrimitiveType.Sphere;
                randUniScale = Random.Range(minScale, maxScale);
                scale = new Vector3(randUniScale, randUniScale, randUniScale);
                break;
            default:
                primitiveType = PrimitiveType.Cube;
                scale = new Vector3(Random.Range(minScale, maxScale), Random.Range(minScale, maxScale), Random.Range(minScale, maxScale));
                break;
        }

        this.rotation = rotation;
        this.id = id;
    
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

