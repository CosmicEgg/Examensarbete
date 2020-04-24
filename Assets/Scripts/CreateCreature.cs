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
    Stack<Node> symmetryStack;
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
        symmetryStack = new Stack<Node>();
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
                //If not yet traveled and not looking at ourselves
                if (!currentNode.edges[i].traversed && !ReferenceEquals(currentNode, currentNode.edges[i].to))
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
                        Debug.Log("Removing edge from " + currentNode.id + " to " + currentNode.edges[i].to.id);
                        currentNode.edges.RemoveAt(i);
                    }

                    break;
                }
            }

            foreach (Edge edge in currentNode.edges)
            {
                if (!edge.traversed && ReferenceEquals(currentNode, edge.to))
                {
                    //If not already in stack i.e. edge is not a backwards edge
                    //recurssionStack.Push(edge.to);
                    nodeOrder.Add(edge.to.id);

                    edge.traversed = true;
                    break;
                }
            }
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
                if (ReferenceEquals(currentNode.edges[i].to,currentNode.edges[i].from)
                    && currentNode.edges[i].numOfTravels < currentNode.edges[i].recursiveLimit)
                {
                    currentNode.edges[i].numOfTravels++;
                    Node newNode = new Node(currentNode);
                    if (ReferenceEquals(newNode, currentNode))
                    {

                    }

                    //Om vi har nått denna plats för sista gången se till att ta bort den nya nodens 
                    //koppling till själv nu då det annars inte kommer hända
                    if (currentNode.edges[i].numOfTravels >= currentNode.edges[i].recursiveLimit)
                    {
                        newNode.edges.RemoveAt(i);
                    }

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

        //Interpret tree in a BFS-like fashion
        while (nodeQueue.Count > 0 && nodeQueue.Count < 1000)
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
                //If there is any node net yet created we still have work to do and continue
                //if (!edge.to.created)
                //{
                //    startOver = false;
                //    break;
                //}
                //else
                //    startOver = true;

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

            //Make deepCopy of currentNodes to prevent deletion of nodes in original list
            List<Node> tempNodes = new List<Node>();
            foreach (Edge e in currentNode.edges)
            {
                tempNodes.Add(e.to);
            }

            var myhash = new HashSet<Node>();
            var mylist = tempNodes;
            var duplicates = mylist.Where(item => !myhash.Add(item)).Distinct().ToList();

            //Region for multiple edges to one node
            //Occurences contain the number of times a duplicate exists stored 
            //as an integer in indexing corresponding to duplicates indexing
            for (int i = 0; i < duplicates.Count; i++)
            {
                foreach (Node n in tempNodes)
                {
                    if (duplicates[i].Equals(n))
                    {
                        n.symmetry = true;
                        n.occurence++;
                    }
                }
            }

            //Creating all not already traversed normal children/nodes
            for (int i = 0; i < currentNode.edges.Count; i++)
            {
                if (!currentNode.edges[i].traversed /*&& !currentNode.edges[i].to.created*/)
                {
                    currentNode.edges[i].traversed = true;

                    if (currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.created)
                    {
                        CreateSymmetricalGeometry(currentNode.edges[i].to, currentNode);
                    }
                    else
                    {
                        CreateSingleEdgeGeometry(currentNode.edges[i].to, currentNode);
                    }

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

    public void CreateSymmetricalGeometry(Node node, Node parent)
    {
        if (ReferenceEquals(parent, node) || parent.gameObjects.Count == 0)
        {
            return;
        }

        bool restart = false;

        while (!restart)
        {

            //foreach (GameObject pg in parent.gameObjects)
            //{
                GameObject parentGeometry = parent.gameObjects[0];
                Collider parentCollider = parentGeometry.GetComponent<Collider>();
                Rigidbody parentRigidBody = parentGeometry.GetComponent<Rigidbody>();

                Vector3 randomPoint = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                          Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

                GameObject currentGeometry = new GameObject();

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

                    currentGeometry = GameObject.CreatePrimitive(node.primitiveType);
                    currentGeometry.transform.position = randomPoint;
                    currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                    currentGeometry.transform.position = randomPoint;     
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
                        node.posRelativeToParent = currentGeometry.transform.position;
                        node.gameObjects.Add(currentGeometry);
                        geometry.Add(currentGeometry);
                    }
                }

            for (int i = 1; i < node.occurence; i++)
            {
                Quaternion mirrorRot = currentGeometry.transform.rotation;
                if (i == 1)
                {
                    //Quaternion mirrorRot = new Quaternion(currentGeometry.transform.localRotation.x * -1.0f,
                    //                        currentGeometry.transform.localRotation.y,
                    //                        currentGeometry.transform.localRotation.z,
                    //                        currentGeometry.transform.localRotation.w * - 1.0f);

                    Vector3.Reflect(currentGeometry.transform.InverseTransformPoint, 
                    node.gameObjects.Add(reflection);
                    geometry.Add(reflection);
                }

                //else if (i == 2)
                //{
                //    //Quaternion mirrorRot = new Quaternion(currentGeometry.transform.localRotation.x *-1.0f,
                //    //                       currentGeometry.transform.localRotation.y,
                //    //                       currentGeometry.transform.localRotation.z,
                //    //                       currentGeometry.transform.localRotation.w * -1.0f);

                //    Vector3 mirrorPos = Vector3.Reflect(node.posRelativeToParent, parentGeometry.transform.up);
                //    GameObject reflection = Instantiate(currentGeometry, mirrorPos, mirrorRot);
                //    node.gameObjects.Add(reflection);
                //    geometry.Add(reflection);
                //}
                //else if (i == 3)
                //{
                //    //Quaternion mirrorRot = new Quaternion(currentGeometry.transform.localRotation.x *-1.0f,
                //    //                       currentGeometry.transform.localRotation.y,
                //    //                       currentGeometry.transform.localRotation.z,
                //    //                       currentGeometry.transform.localRotation.w * -1.0f);

                //    Vector3 mirrorPos = Vector3.Reflect(node.posRelativeToParent, -parentGeometry.transform.forward);
                //    GameObject reflection = Instantiate(currentGeometry, mirrorPos, mirrorRot);
                //    node.gameObjects.Add(reflection);
                //    geometry.Add(reflection);
                //}
            }

            restart = true;
        }
    }


    //Detta är en metod för att skapa single edge geometry 
    public void CreateSingleEdgeGeometry(Node node, Node parent)
    {
        if (ReferenceEquals(parent, node)|| parent.gameObjects.Count == 0)
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
    public Vector3 posRelativeToParent;
    public bool created = false;
    public bool symmetry = false;
    public int occurence = 1;
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

        //tror shallow copying uppstår här
        scale = other.scale;

        //och här
        rotation = other.rotation;
        primitiveType = other.primitiveType;

        foreach(Edge e in other.edges)
        {
            if (!ReferenceEquals(e.to, e.from))
                edges.Add(new Edge(this, new Node(e.to), e.recursiveLimit, e.numOfTravels));
            if (ReferenceEquals(e.to, e.from))
                edges.Add(new Edge(this, new Node(this), e.recursiveLimit, e.numOfTravels));
        }
    }

    public Node(){}

    public Node(int primitiveRand, float minScale, float maxScale, Vector3 rotation, int id)
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

