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

    public enum TypeOfGeneration { random, symmetry, recursion, symplussin };
    public TypeOfGeneration typeOfGeneration;

    int primitiveRand;
    float minScale, maxScale;

    // Start is called before the first frame update
    void Start()
    {
        geometry = new List<GameObject>();
        numOfNodes = Random.Range(1, 5);
        startPosition = Vector3.zero;
        nodes = new List<Node>();
        recurssionStack = new Stack<Node>();
        symmetryStack = new Stack<Node>();
        nodeOrder = new List<int>();
        nodeStack = new Stack<Node>();
        
        minScale = 0.2f;
        maxScale = 2f;

        switch (typeOfGeneration)
        {
            case TypeOfGeneration.random:
                CreateRandomNodes();
                break;
            case TypeOfGeneration.symmetry:
                CreateSymmetryTest();
                break;
            case TypeOfGeneration.recursion:
                CreateRecursionTest();
                break;
            case TypeOfGeneration.symplussin:
                CreateSymmetryPlusSingleTest();
                break;
            default:
                break;
        }


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
                    recurssionStack.Push(edge.to);
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

                    //Om vi har nått denna plats för sista gången se till att ta bort den nya nodens 
                    //koppling till själv nu då det annars inte kommer hända
                    //if (currentNode.edges[i].numOfTravels >= currentNode.edges[i].recursiveLimit)
                    //{
                    //    newNode.edges.RemoveAt(i);
                    //}

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

            if (currentNode.edges.Count == 0 || currentNode.gameObjects.Count == 0)
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
                if (!ReferenceEquals(currentNode, e.to))
                    tempNodes.Add(e.to);
                else
                    Debug.Log("hi");
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
                    else if (!currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.created)
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
        segment.AddComponent<GeoInfo>();

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
            List<GameObject> copyGeoList = new List<GameObject>();

            GameObject parentGeometry = parent.gameObjects[0];
            Collider parentCollider = parentGeometry.GetComponent<Collider>();
            Rigidbody parentRigidBody = parentGeometry.GetComponent<Rigidbody>();

            GameObject currentGeometry;

            int currentGeoIndex = 0;
            bool created = false;
            int tries = 0;

            while (!created)
            {
                tries++;
                if (tries > 100)
                {
                    Debug.Log("To many tries");
                    restart = true;
                    break;
                }
                created = true;

                Vector3 randomPoint = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                      Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

                currentGeometry = GameObject.CreatePrimitive(node.primitiveType);
                currentGeometry.transform.position = randomPoint;
                currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                currentGeometry.transform.localScale = node.scale;
                currentGeometry.AddComponent<Rigidbody>();
                currentGeometry.AddComponent<GeoInfo>();
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
                    currentGeoIndex++;
                    node.posRelativeToParent = currentGeometry.transform.position;
                    node.gameObjects.Add(currentGeometry);
                    currentGeometry.name = node.id.ToString();
                    geometry.Add(currentGeometry);
                }
            }

            if (restart)
            {
                continue;
            }

            currentGeometry = node.gameObjects[currentGeoIndex - 1];

            int random = Random.Range(0, 3);

            for (int i = 1; i < node.occurence; i++)
            {
                Quaternion mirrorRot = currentGeometry.transform.rotation;
                Vector3 axis = new Vector3();

                if (i % 2 == 0)
                {
                    random = Random.Range(0, 3);

                    switch (random)
                    {
                        case 0:
                            axis = parentGeometry.transform.forward;
                            break;
                        case 1:
                            axis = parentGeometry.transform.up;
                            break;
                        case 2:
                            axis = parentGeometry.transform.right;
                            break;
                        default:
                            axis = parentGeometry.transform.forward;
                            break;
                    }
                }

                bool newAxis = true;
                int testAxis = 0;

                while (newAxis)
                {
                    newAxis = false;

                    if (testAxis > 0)
                    {
                        random = Random.Range(0, 3);

                        switch (random)
                        {
                            case 0:
                                axis = parentGeometry.transform.forward;
                                break;
                            case 1:
                                axis = parentGeometry.transform.up;
                                break;
                            case 2:
                                axis = parentGeometry.transform.right;
                                break;
                            default:
                                axis = parentGeometry.transform.forward;
                                break;
                        }
                    }

                    Quaternion objectQuat = currentGeometry.transform.rotation;
                    Quaternion mirrorNormalQuat = new Quaternion(axis.x, axis.y, axis.z, 0);

                    Quaternion reflectedQuat = mirrorNormalQuat * objectQuat;
                    mirrorRot = reflectedQuat;

                    Vector3 mirrorPos = Vector3.Reflect(currentGeometry.transform.position - parentGeometry.transform.position, axis) + parentGeometry.transform.position;

                    GameObject refChild = Instantiate(currentGeometry, mirrorPos, mirrorRot);
                    Collider collider = refChild.GetComponent<Collider>();
                    refChild.AddComponent<GeoInfo>();
                    GeoInfo refGeoInfo = refChild.GetComponent<GeoInfo>();

                    Vector3 directionToMove;
                    float distance = 0;

                    foreach (GameObject g in geometry)
                    {
                        Collider gCollider = g.GetComponent<Collider>();

                        if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                        gCollider, gCollider.transform.position, gCollider.transform.rotation, out directionToMove, out distance) && g != parentGeometry)
                        {
                            Destroy(refChild);
                            newAxis = true;
                            testAxis++;
                            Debug.Log("Test");
                            break;
                        }
                    }

                    if (!newAxis)
                    {
                        node.gameObjects.Add(refChild);
                        geometry.Add(refChild);
                        refChild.name = node.id.ToString();
                        refGeoInfo.RefAxis = axis;
                        refGeoInfo.PosRelParent = parentGeometry.transform.position;
                        restart = true;
                    }

                    if (testAxis > 10 && newAxis)
                    {
                        restart = false;
                        Debug.Log("restarting");
                        foreach (GameObject geo in node.gameObjects)
                        {
                            Destroy(geo);
                        }
                        node.gameObjects.Clear();
                        break;
                    }
                }
            }

            foreach (GameObject copy in node.gameObjects)
            {
                copyGeoList.Add(copy);
            }

            //spegla all befintlig geometri för varje symetri
            for (int i = 0; i < copyGeoList.Count; i++)
            {
                if (!CreateRefGeometry(copyGeoList[i], node, parent))
                {
                    restart = false;
                    break;
                }
            }
        }
    }

    //Detta är en metod för att skapa single edge geometry 
    public void CreateSingleEdgeGeometry(Node node, Node parent)
    {
        if (ReferenceEquals(parent, node) || parent.gameObjects.Count == 0)
        {
            return;
        }
        bool firstGeo = true;
        Vector3 pointOnParent = new Vector3();

        foreach (GameObject pg in parent.gameObjects)
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
                currentGeometry = GameObject.CreatePrimitive(node.primitiveType);

                if (firstGeo)
                {
                    Vector3 randomPoint = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                         Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

                    currentGeometry.transform.position = randomPoint;
                    currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                }

                if (!firstGeo)
                {
                    Vector3 axis = pg.GetComponent<GeoInfo>().RefAxis;
                    Quaternion objectQuat = Quaternion.Euler(node.rotation);
                    Quaternion mirrorNormalQuat = new Quaternion(axis.x, axis.y, axis.z, 0);

                    Quaternion reflectedQuat = mirrorNormalQuat * objectQuat;
                    currentGeometry.transform.rotation = reflectedQuat;

                    currentGeometry.transform.position = Vector3.Reflect(pointOnParent- parentGeometry.GetComponent<GeoInfo>().PosRelParent, axis) + parentGeometry.GetComponent<GeoInfo>().PosRelParent;
                }

                currentGeometry.transform.localScale = node.scale;
                currentGeometry.AddComponent<Rigidbody>();
                currentGeometry.AddComponent<GeoInfo>();
                Rigidbody rb = currentGeometry.GetComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                Collider collider = currentGeometry.GetComponent<Collider>();

                Vector3 directionToMove;
                float distance = 0;


                if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance) && firstGeo)
                {
                    currentGeometry.transform.position += (directionToMove * (distance));
                }

                node.gameObjects.Add(currentGeometry);

                foreach (GameObject g in geometry)
                {
                    Collider gCollider = g.GetComponent<Collider>();

                    if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    gCollider, gCollider.transform.position, gCollider.transform.rotation, out directionToMove, out distance) && g != parentGeometry )
                    {
                        foreach(GameObject geo in node.gameObjects)
                        {
                            Destroy(geo);
                        }

                        node.gameObjects.Clear();
                        node.scale *= 0.9f;
                        created = false;
                        firstGeo = true;
                        break;
                    }
                }

                if (created)
                {
                    geometry.Add(currentGeometry);
                    currentGeometry.name = node.id.ToString();
                    if (firstGeo)
                    {
                        pointOnParent = currentGeometry.transform.position;
                    }

                    firstGeo = false;
                }
            }
        }
    }

    public bool CreateRefGeometry(GameObject currentGeo, Node node, Node parent)
    {
        if (ReferenceEquals(parent, node) || parent.gameObjects.Count == 0)
        {
            return false;
        }
        Vector3 pointOnParent = currentGeo.transform.position;

        for (int i = 1; i < parent.gameObjects.Count; i++)
        {
            GameObject currentGeometry;

            //Förälder Geo information
            GameObject parentGeometry = parent.gameObjects[i];
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
                currentGeometry = GameObject.CreatePrimitive(node.primitiveType);
                currentGeometry.name = node.id.ToString();

                Vector3 axis = parentGeometry.GetComponent<GeoInfo>().RefAxis;
                Quaternion objectQuat = currentGeo.transform.rotation;
                Quaternion mirrorNormalQuat = new Quaternion(axis.x, axis.y, axis.z, 0);

                Quaternion reflectedQuat = mirrorNormalQuat * objectQuat;
                currentGeometry.transform.rotation = reflectedQuat;

                currentGeometry.transform.position = Vector3.Reflect(pointOnParent - parentGeometry.GetComponent<GeoInfo>().PosRelParent, axis) + parentGeometry.GetComponent<GeoInfo>().PosRelParent;

                currentGeometry.transform.localScale = currentGeo.transform.localScale;
                currentGeometry.AddComponent<Rigidbody>();
                currentGeometry.AddComponent<GeoInfo>();
                Rigidbody rb = currentGeometry.GetComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                Collider collider = currentGeometry.GetComponent<Collider>();

                Vector3 directionToMove;
                float distance = 0;

                node.gameObjects.Add(currentGeometry);

                foreach (GameObject g in geometry)
                {
                    Collider gCollider = g.GetComponent<Collider>();

                    if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    gCollider, gCollider.transform.position, gCollider.transform.rotation, out directionToMove, out distance) && g != parentGeometry)
                    {
                        foreach (GameObject geo in node.gameObjects)
                        {
                            Destroy(geo);
                        }
                        node.gameObjects.Clear();
                        node.scale *= 0.9f;
                        created = false;
                        return false;
                    }
                }

                if (created)
                {
                    geometry.Add(currentGeometry);
                    currentGeometry.name = node.id.ToString();
                }
            }
        }
        return true;
    }

#region CreationType

    private void CreateRandomNodes()
    {
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
    }

    //For testing symmetry
    private void CreateSymmetryTest()
    {
        //Spawn Nodes
        for (int i = 0; i < 4; i++)
        {
            primitiveRand = Random.Range(0, 3);
            Vector3 rotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            Node node = new Node(primitiveRand, minScale, maxScale, rotation, i);
            nodes.Add(node);
        }

        //symmetry + symmetry + single
        nodes[0].edges.Add(new Edge(nodes[0], nodes[1], Random.Range(0, 4), 0));
        nodes[0].edges.Add(new Edge(nodes[0], nodes[1], Random.Range(0, 4), 0));
        nodes[1].edges.Add(new Edge(nodes[1], nodes[2], Random.Range(0, 4), 0));
        nodes[1].edges.Add(new Edge(nodes[1], nodes[2], Random.Range(0, 4), 0));
        nodes[2].edges.Add(new Edge(nodes[2], nodes[3], Random.Range(0, 4), 0));


        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0].id);
        nodes[0].stacked = true;
    }

    private void CreateSymmetryPlusSingleTest()
    {
        //Spawn Nodes
        for (int i = 0; i < 4; i++)
        {
            primitiveRand = Random.Range(0, 3);
            Vector3 rotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            Node node = new Node(primitiveRand, minScale, maxScale, rotation, i);
            nodes.Add(node);
        }

        //symmetry + single + symmetry 
        nodes[0].edges.Add(new Edge(nodes[0], nodes[1], Random.Range(0, 4), 0));
        nodes[0].edges.Add(new Edge(nodes[0], nodes[1], Random.Range(0, 4), 0));
        nodes[1].edges.Add(new Edge(nodes[1], nodes[2], Random.Range(0, 4), 0));
        //nodes[2].edges.Add(new Edge(nodes[2], nodes[3], Random.Range(0, 4), 0));
        //nodes[2].edges.Add(new Edge(nodes[2], nodes[3], Random.Range(0, 4), 0));

        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0].id);
        nodes[0].stacked = true;
    }

    private void CreateRecursionTest()
    {

        primitiveRand = Random.Range(0, 3);
        Vector3 rotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        Node node = new Node(primitiveRand, minScale, maxScale, rotation, 0);
        nodes.Add(node);
            
        nodes[0].edges.Add(new Edge(nodes[0], nodes[0], 4, 0));

        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0].id);
        nodes[0].stacked = true;
    }
    #endregion
}

public class Node
{
    float randUniScale;
    public Vector3 posRelativeToParent;
    public bool created = false;
    public bool symmetry = false;
    public int occurence = 0;
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
        scale = other.scale *0.75f;

        //och här
        rotation = other.rotation;
        primitiveType = other.primitiveType;

        foreach(Edge e in other.edges)
        {
            if (!ReferenceEquals(e.to, e.from))
                edges.Add(new Edge(this, new Node(e.to), e.recursiveLimit, e.numOfTravels));
            if (ReferenceEquals(e.to, e.from) && e.recursiveLimit > e.numOfTravels)
                edges.Add(new Edge(this, this, e.recursiveLimit, e.numOfTravels));
        }
    }

    public Node(){}

    public Node(int primitiveRand, float minScale, float maxScale, Vector3 rotation, int id)
    {
        primitiveRand = 0;
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

