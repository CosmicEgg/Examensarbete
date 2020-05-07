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
        
        minScale = 1f;
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
                    bool addNode = true;

                    foreach  (Node n in recurssionStack)
                    {
                        if (ReferenceEquals(n, edge.to))
                            addNode = false;
                    }

                    if (addNode)
                    {
                        recurssionStack.Push(edge.to);
                        edge.to.startOfRecurssion = true;
                        nodeOrder.Add(edge.to.id);
                    }
                    edge.traversed = true;
                }
            }
        }

        root = nodes[0];
        ResetTree(ref root);

        Queue<Node> recurssionQueue = new Queue<Node>();

        while (recurssionStack.Count > 0)
        {
            Node queueNode = recurssionStack.Pop();

            recurssionQueue.Enqueue(queueNode);
        }

        while (recurssionQueue.Count > 0)
        {
            Node originalRecursiveNode = recurssionQueue.Peek();

            Queue<Node> currentNodeRecurssion = new Queue<Node>();
            currentNodeRecurssion.Enqueue(originalRecursiveNode);                   

            while(currentNodeRecurssion.Count > 0)
            {
                Node currentNode = currentNodeRecurssion.Peek();
                //List<Node> newAddedNodes = new List<Node>();
                List<Edge> edgesToAdd = new List<Edge>();
                int selfEdges = 0;
                foreach (Edge e in currentNode.edges)
                {
                    if (ReferenceEquals(e.to, e.from))
                    {
                        selfEdges++;
                    }
                }

                int counter = -1;

                for (int i = 0; i < currentNode.edges.Count; i++)
                {
                    if (ReferenceEquals(currentNode.edges[i].to, currentNode.edges[i].from))
                    {
                        CopyNodeTree(currentNode, out Node temp);
                        counter++;
                        //-1 för att vi skapat en kopia redan och borde egenltigen redan höjt numOfTravels
                        if (currentNode.edges[i].numOfTravels < currentNode.edges[i].recursiveLimit - 1)
                        {
                            for (int j = 0; j < selfEdges; j++)
                            {
                                temp.edges.Add(new Edge(temp, temp, currentNode.edges[i].recursiveLimit, currentNode.edges[i].numOfTravels + 1, j, new Vector3(1, 0, 0)));
                            }
                        }

                        currentNodeRecurssion.Enqueue(temp);

                        currentNode.edges.RemoveAt(i);
                        edgesToAdd.Add(new Edge(currentNode, temp, 4, 4, counter, new Vector3(0,0,1)));
                        i = -1;
                    }
                }

                currentNode.edges.AddRange(edgesToAdd);
                currentNodeRecurssion.Dequeue();
            }
            recurssionQueue.Dequeue();
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
                e.to.created = false;
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
        int numbofgeo = 1;
        Node startOfRecurssionNode = new Node();

        //Interpret tree in a BFS-like fashion
        while (nodeQueue.Count > 0 && nodeQueue.Count < 1000)
        {
            Node currentNode = nodeQueue.Peek();
            bool startOver = false;

            if (currentNode.startOfRecurssion)
            {
                startOfRecurssionNode = currentNode;
            }

            if (currentNode.edges.Count == 0 || currentNode.gameObjects.Count == 0)
            {
                nodeQueue.Dequeue();
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

                    if (currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo)
                    {
                        CreateSymmetricalGeometry(currentNode.edges[i].to, currentNode);
                    }
                    else if (!currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo /*&& currentNode.edges[i].recursiveNumb == -1*/)
                    {
                        CreateSingleEdgeGeometry(currentNode.edges[i].to, currentNode, currentNode.edges[i], startOfRecurssionNode);
                        numbofgeo++;
                        Debug.Log(numbofgeo);
                    }
                    //else if (!currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo && currentNode.edges[i].recursiveNumb > -1)
                    //{
                    //    CreateRecurssionGeometry();
                    //}

                    if (!currentNode.edges[i].to.createdGeo)
                    {
                        nodeQueue.Enqueue(currentNode.edges[i].to);
                        currentNode.edges[i].to.createdGeo = true;
                    }
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
                currentGeometry = GameObject.CreatePrimitive(node.primitiveType);

                //if (parent.referenceNode == null)
                //{
                currentGeometry.transform.position = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                    Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));
                //}
                //else if(parent.referenceNode != null)
                //{
                //    currentGeometry.transform.position = parentGeometry.transform.position - node.referenceNode.parentToChildDir;
                //}

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
                    node.gameObjects.Add(currentGeometry);
                    currentGeometry.name = node.id.ToString();
                    geometry.Add(currentGeometry);
                    currentGeometry.GetComponent<GeoInfo>().ParentToChildDir = currentGeometry.transform.position - parentGeometry.transform.position;
                }
            }

            if (restart)
            {
                continue;
            }

            currentGeometry = node.gameObjects[currentGeoIndex - 1];
            

            int random;

            for (int i = 1; i < node.occurence; i++)
            {
                Quaternion mirrorRot = currentGeometry.transform.rotation;
                Vector3 axis = new Vector3();

                if (i % 2 == 0)
                {
                    if (parent.referenceNode != null)
                    {
                        axis = node.referenceNode.axisList.Dequeue();
                    }
                    else
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
                }

                bool newAxis = true;
                int testAxis = 0;

                while (newAxis)
                {
                    newAxis = false;

                    if (testAxis > 0/* && parent.referenceNode == null*/)
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
                        refGeoInfo.ParentToChildDir = refChild.transform.position - parentGeometry.transform.position;
                        node.axisList.Enqueue(axis);
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

    public void CreateSingleEdgeGeometry(Node node, Node parent, Edge currentEdge, Node recurssionNode)
    {
        if (ReferenceEquals(parent, node) || parent.gameObjects.Count == 0)
        {
            return;
        }
        bool firstGeo = true;
        Vector3 pointOnParent = new Vector3();

        bool startOver = true;
        int maxNumbOfTries = 0;

        while (startOver)
        {
            startOver = false;
            if(maxNumbOfTries > 100)
            {
                Debug.Log("nope");
                break;
            }


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

                    if (firstGeo && parent.startOfRecurssion)
                    {
                        Vector3 randomPoint = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                             Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

                        currentGeometry.transform.position = randomPoint;
                        currentGeometry.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
                    }
                    else if (firstGeo && currentEdge.recursiveNumb == -1)
                    {
                        Vector3 randomPoint = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                             Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

                        currentGeometry.transform.position = randomPoint;
                        currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                    }
                    else if(firstGeo && !parent.startOfRecurssion && currentEdge.recursiveNumb > -1)
                    {
                        foreach(Edge e in recurssionNode.edges)
                        {
                            if(e.recursiveNumb == currentEdge.recursiveNumb)
                            {
                                foreach(GameObject g in e.to.gameObjects)
                                {
                                    if (g.GetComponent<GeoInfo>().recursiveNumb == currentEdge.recursiveNumb)
                                    {
                                        Vector3 matchDirection = g.GetComponent<GeoInfo>().ParentToChildDir;
                                        currentGeometry.transform.position = parentGeometry.transform.position + matchDirection;
                                    }
                                }
                            }
                        }
                        currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                    }
                    //else if (firstGeo && !parent.startOfRecurssion && currentEdge.recursiveNumb == 0)
                    //{
                    //    currentGeometry.transform.position = parentGeometry.transform.position + node.referenceNode.parentToChildDir;
                    //    currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                    //}
                    //else if (firstGeo && parent.referenceNode != null && currentEdge.recursiveNumb > 0)
                    //{
                    //    Vector3 axis = currentEdge.axis;
                    //    Quaternion objectQuat = Quaternion.Euler(node.rotation);
                    //    Quaternion mirrorNormalQuat = new Quaternion(axis.x, axis.y, axis.z, 0);

                    //    Quaternion reflectedQuat = mirrorNormalQuat * objectQuat;
                    //    currentGeometry.transform.rotation = reflectedQuat;

                    //    currentGeometry.transform.position = Vector3.Reflect(parent.gameObjects[0].transform.position - parentGeometry.GetComponent<GeoInfo>().PosRelParent, axis) + parentGeometry.GetComponent<GeoInfo>().PosRelParent;
                    //}
                    //else if (firstGeo && parent.referenceNode == null && currentEdge.recursiveNumb > 0)
                    //{
                    //    Vector3 axis = currentEdge.axis;
                    //    Quaternion objectQuat = Quaternion.Euler(node.rotation);
                    //    Quaternion mirrorNormalQuat = new Quaternion(axis.x, axis.y, axis.z, 0);

                    //    Quaternion reflectedQuat = mirrorNormalQuat * objectQuat;
                    //    currentGeometry.transform.rotation = reflectedQuat;

                    //    currentGeometry.transform.position = Vector3.Reflect(parent.gameObjects[0].transform.position - parentGeometry.GetComponent<GeoInfo>().PosRelParent, axis) + parentGeometry.GetComponent<GeoInfo>().PosRelParent;
                    //}

                    if (!firstGeo)
                    {
                        Vector3 axis = pg.GetComponent<GeoInfo>().RefAxis;
                        Quaternion objectQuat = Quaternion.Euler(node.rotation);
                        Quaternion mirrorNormalQuat = new Quaternion(axis.x, axis.y, axis.z, 0);

                        Quaternion reflectedQuat = mirrorNormalQuat * objectQuat;
                        currentGeometry.transform.rotation = reflectedQuat;
                      
                        currentGeometry.transform.position = Vector3.Reflect(pointOnParent - parentGeometry.GetComponent<GeoInfo>().PosRelParent, axis) + parentGeometry.GetComponent<GeoInfo>().PosRelParent;
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

                    if (firstGeo/* && parent.referenceNode != null*/)
                    {
                        //int counter = 0;

                        //while (!Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                        //parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance))
                        //{
                        //    if (currentEdge.recursiveNumb <= 0)
                        //        currentGeometry.transform.position -= 0.05f * node.referenceNode.parentToChildDir.normalized;
                        //    else if (currentEdge.recursiveNumb > 0)
                        //        currentGeometry.transform.position += 0.05f * (parentGeometry.transform.position - currentGeometry.transform.position).normalized;

                        //    counter++;
                        //    if (counter > 100)
                        //        break;
                        //}

                        if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                            parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance))
                        {
                            currentGeometry.transform.position += (directionToMove * (distance));
                        }
                    }

                    if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                        parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance) && firstGeo
                        && parent.referenceNode == null)
                    {
                        currentGeometry.transform.position += (directionToMove * (distance));
                    }

                    node.gameObjects.Add(currentGeometry);

                    //foreach (GameObject g in geometry)
                    //{
                    //    Collider gCollider = g.GetComponent<Collider>();

                    //    if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    //    gCollider, gCollider.transform.position, gCollider.transform.rotation, out directionToMove, out distance) && g != parentGeometry)
                    //    {
                    //        foreach (GameObject geo in node.gameObjects)
                    //        {
                    //            Destroy(geo);
                    //        }

                    //        node.gameObjects.Clear();
                    //        created = false;
                    //        firstGeo = true;
                    //        startOver = true;
                    //        break;
                    //    }
                    //}

                    if (startOver)
                        break;

                    if (created)
                    {
                        geometry.Add(currentGeometry);
                        currentGeometry.GetComponent<GeoInfo>().recursiveNumb = currentEdge.recursiveNumb;
                        currentGeometry.name = node.id.ToString();
                        if (firstGeo)
                        {
                            pointOnParent = currentGeometry.transform.position;
                            currentGeometry.GetComponent<GeoInfo>().ParentToChildDir = currentGeometry.transform.position - parentGeometry.transform.position;
                        }

                        firstGeo = false;
                    }
                }
                if (startOver)
                    break;
            }
            if (startOver)
            {
                maxNumbOfTries++;
                continue;
            }
        }
    }

    public void CreateRecurssionGeometry(Node node, Node parent, Edge currentEdge)
    {
        if (ReferenceEquals(parent, node) || parent.gameObjects.Count == 0)
        {
            return;
        }
        bool firstGeo = true;
        Vector3 pointOnParent = new Vector3();

        bool startOver = true;
        int maxNumbOfTries = 0;

        while (startOver)
        {
            startOver = false;
            if (maxNumbOfTries > 100)
            {
                Debug.Log("nope");
                break;
            }


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

                    if (firstGeo && parent.startOfRecurssion)
                    {
                        Vector3 randomPoint = new Vector3(Random.Range(parentCollider.bounds.min.x, parentCollider.bounds.max.x),
                             Random.Range(parentCollider.bounds.min.y, parentCollider.bounds.max.y), Random.Range(parentCollider.bounds.min.z, parentCollider.bounds.max.z));

                        currentGeometry.transform.position = randomPoint;
                        currentGeometry.transform.rotation = Quaternion.Euler(node.rotation);
                    }
                    else if (!firstGeo && parent.startOfRecurssion)
                    {
                        Vector3 axis = pg.GetComponent<GeoInfo>().RefAxis;
                        Quaternion objectQuat = Quaternion.Euler(node.rotation);
                        Quaternion mirrorNormalQuat = new Quaternion(axis.x, axis.y, axis.z, 0);

                        Quaternion reflectedQuat = mirrorNormalQuat * objectQuat;
                        currentGeometry.transform.rotation = reflectedQuat;

                        currentGeometry.transform.position = Vector3.Reflect(pointOnParent - parentGeometry.GetComponent<GeoInfo>().PosRelParent, axis) + parentGeometry.GetComponent<GeoInfo>().PosRelParent;
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
                        parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance) && firstGeo
                        && parent.startOfRecurssion)
                    {
                        currentGeometry.transform.position += (directionToMove * (distance));
                    }

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
                            created = false;
                            firstGeo = true;
                            startOver = true;
                            break;
                        }
                    }

                    if (startOver)
                        break;

                    if (created)
                    {
                        geometry.Add(currentGeometry);
                        currentGeometry.name = node.id.ToString();
                        if (firstGeo)
                        {
                            pointOnParent = currentGeometry.transform.position;
                            node.parentToChildDir = currentGeometry.transform.position - parentGeometry.transform.position;
                        }

                        firstGeo = false;
                    }
                }
                if (startOver)
                    break;
            }
            if (startOver)
            {
                maxNumbOfTries++;
                continue;
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

    public void CopyNodeTree(Node oriNode, out Node outNode)
    {
        List<Node> toReset = new List<Node>();
        Stack<Node> copyStack = new Stack<Node>();
        copyStack.Push(oriNode);
        Dictionary<Node, Node> copyNodeEdge = new Dictionary<Node, Node>();
        bool nextNode = false;
        Node newOriNode = new Node(oriNode.primitiveType, oriNode.scale, oriNode.rotation, oriNode.id, oriNode);
        //newOriNode.created = true;
        //toReset.Add(newOriNode);
        copyNodeEdge.Add(oriNode, newOriNode);

        while (copyStack.Count > 0)
        {
            nextNode = false;

            Node currentNode = copyStack.Peek();

            foreach (Edge e in currentNode.edges)
            {
                if (!e.to.created && !ReferenceEquals(e.to, e.from))
                {
                    Node newNode = new Node(e.to.primitiveType, e.to.scale, e.to.rotation, e.to.id, e.to);
                    copyStack.Push(e.to);
                    copyNodeEdge.Add(e.to, newNode);
                    e.to.created = true;
                    toReset.Add(e.to);
                    nextNode = true;
                    break;
                }
            }

            if (nextNode)
            {
                continue;
            }


            copyStack.Pop();
        }
        foreach (Node n in toReset)
        {
            n.created = false;
        }


        foreach (KeyValuePair<Node, Node> pair in copyNodeEdge)
        {
            foreach (Edge e in pair.Key.edges)
            {
                if(!ReferenceEquals(e.to, e.from))
                {
                    if(copyNodeEdge.TryGetValue(e.to, out Node temp))
                        pair.Value.edges.Add(new Edge(pair.Value, temp, e.recursiveLimit, e.numOfTravels));
                }
            }
        }
        outNode = newOriNode;
    }

    public Vector3 RayCastDistance(GameObject parentGeometry, GameObject currentGeometry)
    {
        RaycastHit hitParent;
        RaycastHit hitChild;
        Vector3 hitChildOnBound = new Vector3();
        Vector3 hitParentOnBound = new Vector3();

        parentGeometry.layer = 2;

        if (Physics.Raycast(parentGeometry.transform.position, (currentGeometry.transform.position - parentGeometry.transform.position).normalized, out hitChild))
        {
            hitChildOnBound = hitChild.point;
        }

        parentGeometry.layer = 0;
        currentGeometry.layer = 2;

        if (Physics.Raycast(hitChildOnBound, (parentGeometry.transform.position - currentGeometry.transform.position).normalized, out hitParent))
        {
            hitParentOnBound = hitParent.point;
        }

        currentGeometry.layer = 0;

        Vector3 distance = hitParentOnBound - hitChildOnBound;

        return distance;
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
        //nodes[1].edges.Add(new Edge(nodes[1], nodes[2], Random.Range(0, 4), 0));
        //nodes[2].edges.Add(new Edge(nodes[2], nodes[3], Random.Range(0, 4), 0));


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

        primitiveRand = Random.Range(0, 3);
        Vector3 sinRotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        Node sinNode = new Node(primitiveRand, minScale, maxScale, sinRotation, 1);

        //primitiveRand = Random.Range(0, 3);
        //Vector3 rotation2 = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        //Node node2 = new Node(primitiveRand, minScale, maxScale, rotation2, 2);

        //primitiveRand = Random.Range(0, 3);
        //Vector3 rotation3 = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        //Node node3 = new Node(primitiveRand, minScale, maxScale, rotation3, 3);

        //primitiveRand = Random.Range(0, 3);
        //Vector3 rotation4= new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        //Node node4= new Node(primitiveRand, minScale, maxScale, rotation4, 4);

        //primitiveRand = Random.Range(0, 3);
        //Vector3 rotation5 = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        //Node node5 = new Node(primitiveRand, minScale, maxScale, rotation5, 5);

        nodes.Add(node);
        nodes.Add(sinNode);
        //nodes.Add(node2);
        //nodes.Add(node3);
        //nodes.Add(node4);
        //nodes.Add(node5);

        nodes[0].edges.Add(new Edge(nodes[0], nodes[0], 3, 0));
        nodes[0].edges.Add(new Edge(nodes[0], nodes[0], 3, 0));
        //nodes[0].edges.Add(new Edge(nodes[0], nodes[0], 3, 0));
        //nodes[2].edges.Add(new Edge(nodes[2], nodes[3], 4, 0));
        //nodes[3].edges.Add(new Edge(nodes[3], nodes[4], 3, 0));
        //nodes[4].edges.Add(new Edge(nodes[4], nodes[5], 3, 0));

        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0].id);
        nodes[0].stacked = true;
    }
    #endregion
}

public class Node
{
    float randUniScale;
    public bool startOfRecurssion = false;
    public Node referenceNode = null;
    public bool createdGeo = false; 
    public bool created = false;
    public bool symmetry = false;
    public int occurence = 0;
    public PrimitiveType primitiveType;
    public Vector3 scale;
    public Vector3 rotation;
    public Vector3 parentToChildDir;
    public List<GameObject> gameObjects = new List<GameObject>();
    public Queue<Vector3> axisList = new Queue<Vector3>();
    public int numOfChildren = Random.Range(0, 5);
    public bool stacked;
    public int id;

    public List<Edge> edges = new List<Edge>();

    public Node(PrimitiveType primitiveType, Vector3 scale, Vector3 rotation, int id, Node referenceNode)
    {
        this.primitiveType = primitiveType;
        this.scale = scale;
        this.rotation = rotation;
        this.id = id;
        this.referenceNode = referenceNode;
    }

    public Node(){}

    public Node(int primitiveRand, float minScale, float maxScale, Vector3 rotation, int id)
    {
        //primitiveRand = 0;
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
    public bool traversed = false;
    public int recursiveNumb = -1;
    public Vector3 axis;

    public Edge(Node from, Node to, int recursiveLimit, int numOfTravels)
    {
        this.from = from;
        this.to = to;
        this.recursiveLimit = recursiveLimit;
        this.numOfTravels = numOfTravels;
    }
    public Edge(Node from, Node to, int recursiveLimit, int numOfTravels, int recursiveNumb, Vector3 axis)
    {
        this.from = from;
        this.to = to;
        this.recursiveLimit = recursiveLimit;
        this.numOfTravels = numOfTravels;
        this.recursiveNumb = recursiveNumb;
        this.axis = axis;
    }
}

