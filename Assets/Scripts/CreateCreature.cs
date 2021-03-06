﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CreateCreature : MonoBehaviour
{
    List<GameObject> geometry = new List<GameObject>();
    List<Muscle> muscles;
    public List<Node> nodes;
    List<Node> nodeOrder;
    Stack<Node> nodeStack;
    Stack<Node> recurssionStack;
    Stack<Node> symmetryStack;
    List<Muscle> leftRecurssionMuscle;
    List<Muscle> oriRecurssionMuscle;
    List<Muscle> frontRecurssionMuscle;
    List<Muscle> backRecurssionMuscle;
    List<GameObject> copyGameObject;
    int numOfNodes;
    Vector3 startPosition;
    Node root;
    bool generated = false;
    //public int seed;
    public float minLimbSpacing = 0, maxLimbSpacing = 0.3f;
    float limbSpacing;
    Color color;

    GameObject rootGameObject;
    public bool PhysicsOn = true;
    public enum TypeOfGeneration { random, symmetry, recursion, symplussin };
    public TypeOfGeneration typeOfGeneration;

    int primitiveRand;
    public float minScale, maxScale;

    // Start is called before the first frame update
    void Start()
    {
        //Create();        
    }

    public Creature Create(int newSeed = 0)
    {
        //DestroyCurrent();
        //generated = false;

        //seed = newSeed;
        //if (seed == 0)
        //{
        //    seed = Random.Range(1, 10000);
        //}
        //Random.InitState(seed);

        oriRecurssionMuscle = new List<Muscle>();
        leftRecurssionMuscle = new List<Muscle>();
        frontRecurssionMuscle = new List<Muscle>();
        backRecurssionMuscle = new List<Muscle>();
        copyGameObject = new List<GameObject>();
        PhysicsOn = true;
        limbSpacing = Random.Range(minLimbSpacing, maxLimbSpacing);
        muscles = new List<Muscle>();
        geometry = new List<GameObject>();
        numOfNodes = Random.Range(1, 5);
        startPosition = Vector3.zero;
        nodes = new List<Node>();
        recurssionStack = new Stack<Node>();
        symmetryStack = new Stack<Node>();
        nodeOrder = new List<Node>();
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
                    currentNode.edges[i].to.parent = currentNode;

                    if (!currentNode.edges[i].to.stacked)
                    {
                        currentNode.edges[i].to.stacked = true;
                        nodeStack.Push(currentNode.edges[i].to);
                        nodeOrder.Add(currentNode.edges[i].to);
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

                    foreach (Node n in recurssionStack)
                    {
                        if (ReferenceEquals(n, edge.to))
                            addNode = false;
                    }

                    if (addNode)
                    {
                        recurssionStack.Push(edge.to);
                        edge.to.startOfRecurssion = true;
                        nodeOrder.Add(edge.to);
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

            while (currentNodeRecurssion.Count > 0)
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

                currentNode.numOfRecursiveChildren = selfEdges;

                int counter = -1;

                for (int i = 0; i < currentNode.edges.Count; i++)
                {
                    if (ReferenceEquals(currentNode.edges[i].to, currentNode.edges[i].from))
                    {
                        CopyNodeTree(currentNode, out Node newNode);
                        newNode.parent = currentNode;
                        counter++;
                        //-1 för att vi skapat en kopia redan och borde egenltigen redan höjt numOfTravels
                        if (currentNode.edges[i].numOfTravels < currentNode.edges[i].recursiveLimit - 1)
                        {
                            for (int j = 0; j < selfEdges; j++)
                            {
                                newNode.edges.Add(new Edge(newNode, newNode, currentNode.edges[i].recursiveLimit, currentNode.edges[i].numOfTravels + 1, j));
                            }
                        }

                        currentNodeRecurssion.Enqueue(newNode);

                        currentNode.edges.RemoveAt(i);
                        edgesToAdd.Add(new Edge(currentNode, newNode, 4, 4, counter));
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

        InterpretTree(root);
        int geoCounter = 0;
        int fixedJointCounter = 0;
        foreach (GameObject g in geometry)
        {
            if (g != null)
            {
                if (g.TryGetComponent<FixedJoint>(out FixedJoint joint))
                {
                    fixedJointCounter++;
                }
                
                geoCounter++;
            }
        }

        if (fixedJointCounter == geoCounter - 1)
        {
            DestroyCurrent();
            return Create();
        }

        if (geoCounter < 2)
        {
            DestroyCurrent();

            return Create();
        }
        else if (geoCounter > 100)
        {
            DestroyCurrent();
            return Create();
        }

        GameObject handle = new GameObject();
        foreach (GameObject g in geometry)
        {
            g.transform.parent = handle.transform;
        }
        return new Creature(nodes, geometry, handle);
    }

    //Returnerar för få noder
    //public List<Node> CreateNodesFromSeed(int numberOfNodes, int rootSeed)
    //{
    //    DestroyCurrent();
    //    //generated = false;

    //    oriRecurssionMuscle = new List<Muscle>();
    //    leftRecurssionMuscle = new List<Muscle>();
    //    frontRecurssionMuscle = new List<Muscle>();
    //    backRecurssionMuscle = new List<Muscle>();
    //    copyGameObject = new List<GameObject>();
    //    PhysicsOn = true;
    //    limbSpacing = Random.Range(minLimbSpacing, maxLimbSpacing);
    //    muscles = new List<Muscle>();
    //    geometry = new List<GameObject>();
    //    numOfNodes = numberOfNodes;
    //    startPosition = Vector3.zero;
    //    nodes = new List<Node>();
    //    recurssionStack = new Stack<Node>();
    //    symmetryStack = new Stack<Node>();
    //    nodeOrder = new List<Node>();
    //    nodeStack = new Stack<Node>();

    //    minScale = 1f;
    //    maxScale = 2f;

    //    switch (typeOfGeneration)
    //    {
    //        case TypeOfGeneration.random:
    //            CreateNewNodesFromSeed(numOfNodes, rootSeed);
    //            break;
    //        case TypeOfGeneration.symmetry:
    //            CreateSymmetryTest();
    //            break;
    //        case TypeOfGeneration.recursion:
    //            CreateRecursionTest();
    //            break;
    //        case TypeOfGeneration.symplussin:
    //            CreateSymmetryPlusSingleTest();
    //            break;
    //        default:
    //            break;
    //    }

    //    //Start DFS to find all recursive nodes
    //    while (nodeStack.Count > 0)
    //    {
    //        Node currentNode = nodeStack.Peek();
    //        bool startOver = false;

    //        if (currentNode.edges.Count == 0)
    //        {
    //            nodeStack.Pop();
    //            currentNode.stacked = false;
    //            continue;
    //        }

    //        foreach (Edge edge in currentNode.edges)
    //        {
    //            //If all are traversed we are finished with this node
    //            if (!edge.traversed)
    //            {
    //                startOver = false;
    //                break;
    //            }
    //            else
    //                startOver = true;
    //        }

    //        //Pop and start over
    //        if (startOver)
    //        {
    //            nodeStack.Pop();
    //            currentNode.stacked = false;
    //            continue;
    //        }


    //        for (int i = 0; i < currentNode.edges.Count; i++)
    //        {
    //            //If not yet traveled and not looking at ourselves
    //            if (!currentNode.edges[i].traversed && !ReferenceEquals(currentNode, currentNode.edges[i].to))
    //            {
    //                //If not already in stack i.e. edge is not a backwards edge
    //                currentNode.edges[i].traversed = true;
    //                currentNode.edges[i].to.parent = currentNode;

    //                if (!currentNode.edges[i].to.stacked)
    //                {
    //                    currentNode.edges[i].to.stacked = true;
    //                    nodeStack.Push(currentNode.edges[i].to);
    //                    nodeOrder.Add(currentNode.edges[i].to);
    //                }
    //                else //Remove backwards edge
    //                {
    //                    Debug.Log("Removing edge from " + currentNode.id + " to " + currentNode.edges[i].to.id);
    //                    currentNode.edges.RemoveAt(i);
    //                }

    //                break;
    //            }
    //        }

    //        foreach (Edge edge in currentNode.edges)
    //        {
    //            if (!edge.traversed && ReferenceEquals(currentNode, edge.to))
    //            {
    //                //If not already in stack i.e. edge is not a backwards edge
    //                bool addNode = true;

    //                foreach (Node n in recurssionStack)
    //                {
    //                    if (ReferenceEquals(n, edge.to))
    //                        addNode = false;
    //                }

    //                if (addNode)
    //                {
    //                    recurssionStack.Push(edge.to);
    //                    edge.to.startOfRecurssion = true;
    //                    nodeOrder.Add(edge.to);
    //                }
    //                edge.traversed = true;
    //            }
    //        }
    //    }

    //    root = nodes[0];
    //    ResetTree(ref root);

    //    Queue<Node> recurssionQueue = new Queue<Node>();

    //    while (recurssionStack.Count > 0)
    //    {
    //        Node queueNode = recurssionStack.Pop();

    //        recurssionQueue.Enqueue(queueNode);
    //    }

    //    while (recurssionQueue.Count > 0)
    //    {
    //        Node originalRecursiveNode = recurssionQueue.Peek();

    //        Queue<Node> currentNodeRecurssion = new Queue<Node>();
    //        currentNodeRecurssion.Enqueue(originalRecursiveNode);

    //        while (currentNodeRecurssion.Count > 0)
    //        {
    //            Node currentNode = currentNodeRecurssion.Peek();
    //            //List<Node> newAddedNodes = new List<Node>();
    //            List<Edge> edgesToAdd = new List<Edge>();
    //            int selfEdges = 0;
    //            foreach (Edge e in currentNode.edges)
    //            {
    //                if (ReferenceEquals(e.to, e.from))
    //                {
    //                    selfEdges++;
    //                }
    //            }

    //            currentNode.numOfRecursiveChildren = selfEdges;

    //            int counter = -1;

    //            for (int i = 0; i < currentNode.edges.Count; i++)
    //            {
    //                if (ReferenceEquals(currentNode.edges[i].to, currentNode.edges[i].from))
    //                {
    //                    CopyNodeTree(currentNode, out Node newNode);
    //                    newNode.parent = currentNode;
    //                    counter++;
    //                    //-1 för att vi skapat en kopia redan och borde egenltigen redan höjt numOfTravels
    //                    if (currentNode.edges[i].numOfTravels < currentNode.edges[i].recursiveLimit - 1)
    //                    {
    //                        for (int j = 0; j < selfEdges; j++)
    //                        {
    //                            newNode.edges.Add(new Edge(newNode, newNode, currentNode.edges[i].recursiveLimit, currentNode.edges[i].numOfTravels + 1, j, new Vector3(1, 0, 0)));
    //                        }
    //                    }

    //                    currentNodeRecurssion.Enqueue(newNode);

    //                    currentNode.edges.RemoveAt(i);
    //                    edgesToAdd.Add(new Edge(currentNode, newNode, 4, 4, counter, new Vector3(0, 0, 1)));
    //                    i = -1;
    //                }
    //            }

    //            currentNode.edges.AddRange(edgesToAdd);
    //            currentNodeRecurssion.Dequeue();
    //        }
    //        recurssionQueue.Dequeue();
    //    }
    //    root = nodes[0];
    //    ResetTree(ref root);

    //    InterpretTreeWithoutCreatingGeometry(root);

    //    return nodes;
    //}

    private void ResetTree(ref Node node, bool clearGameObjects = false)
    {
        foreach (Edge e in node.edges)
        {
            if (e.traversed)
            {
                if (clearGameObjects)
                {
                    e.to.gameObjects.Clear();
                }
                e.to.created = false;
                e.traversed = false;
                e.numOfTravels = 0;
                e.to.createdGeo = false;
                ResetTree(ref e.to);
            }
        }
    }

    private void ResetNodes(ref Node root)
    {
        Queue<Node> nodeQueue = new Queue<Node>();
        //nodes.Remove(root);
        nodeQueue.Enqueue(root);

        while (nodeQueue.Count > 0)
        {
            Node currentNode = nodeQueue.Dequeue();

            foreach (Edge e in currentNode.edges)
            {
                //nodes.Remove(e.to);
                if (e.to == null)
                {

                }
                e.to.gameObjects.Clear();
                nodeQueue.Enqueue(e.to);
            }
        }
    }

    //public void InterpretTreeWithoutCreatingGeometry(Node root)
    //{
    //    //Root Node def.
    //    Queue<Node> nodeQueue = new Queue<Node>();
    //    CreateRootWithoutGeomtery(ref root);

    //    nodeQueue.Enqueue(root);
    //    int numbofgeo = 1;
    //    Node startOfRecurssionNode = new Node();

    //    //Interpret tree in a BFS-like fashion
    //    while (nodeQueue.Count > 0 && nodeQueue.Count < 1000)
    //    {
    //        Node currentNode = nodeQueue.Peek();
    //        currentNode.partOfGraph = true;
    //        bool startOver = false;

    //        if (currentNode.startOfRecurssion)
    //        {
    //            startOfRecurssionNode = currentNode;
    //            startOfRecurssionNode.scale.z = startOfRecurssionNode.scale.x;

    //        }

    //        if (currentNode.edges.Count == 0/* || currentNode.gameObjects.Count == 0*/)
    //        {
    //            nodeQueue.Dequeue();
    //            continue;
    //        }


    //        foreach (Edge edge in currentNode.edges)
    //        {
    //            if (!edge.traversed)
    //            {
    //                startOver = false;
    //                break;
    //            }
    //            else
    //                startOver = true;
    //        }

    //        //Pop and start over
    //        if (startOver)
    //        {
    //            nodeQueue.Dequeue();
    //            continue;
    //        }

    //        //Make deepCopy of currentNodes to prevent deletion of nodes in original list
    //        List<Node> tempNodes = new List<Node>();
    //        foreach (Edge e in currentNode.edges)
    //        {
    //            tempNodes.Add(e.to);
    //        }

    //        var myhash = new HashSet<Node>();
    //        var mylist = tempNodes;
    //        var duplicates = mylist.Where(item => !myhash.Add(item)).Distinct().ToList();

    //        //Region for multiple edges to one node
    //        //Occurences contain the number of times a duplicate exists stored 
    //        //as an integer in indexing corresponding to duplicates indexing
    //        for (int i = 0; i < duplicates.Count; i++)
    //        {
    //            foreach (Node n in tempNodes)
    //            {
    //                if (duplicates[i].Equals(n))
    //                {
    //                    n.symmetry = true;
    //                    n.occurence++;
    //                }
    //            }
    //        }

    //        //Creating all not already traversed normal children/nodes
    //        for (int i = 0; i < currentNode.edges.Count; i++)
    //        {
    //            if (!currentNode.edges[i].traversed /*&& !currentNode.edges[i].to.created*/)
    //            {
    //                currentNode.edges[i].traversed = true;

    //                if (!currentNode.edges[i].to.createdGeo)
    //                {
    //                    nodeQueue.Enqueue(currentNode.edges[i].to);
    //                    currentNode.edges[i].to.createdGeo = true;
    //                }
    //            }
    //        }
    //    }

    //    CleanupNodes();
    //}

    //private void CreateRootWithoutGeomtery(ref Node node)
    //{
    //    //rootGameObject = GameObject.CreatePrimitive(node.primitiveType);
    //    //rootGameObject.transform.position = new Vector3(0, 0, 0);
    //    node.rotation = Vector3.zero;
    //    //rootGameObject.transform.rotation = Quaternion.Euler(node.rotation);
    //    //rootGameObject.transform.localScale = node.scale;
    //    //Rigidbody rb = rootGameObject.AddComponent<Rigidbody>();
    //    //rootGameObject.AddComponent<GeoInfo>();
    //    //rb.isKinematic = true;
    //    //rb.useGravity = false;
    //    if (root.startOfRecurssion)
    //        root.scale.x = root.scale.z;
    //    node.parent = null;
    //    node.created = true;
    //    node.gameObjects.Clear();
    //    //node.gameObjects.Add(rootGameObject);
    //    //geometry.Add(rootGameObject);
    //}

    public void InterpretTree(Node root)
    {
        //Root Node def.
        Queue<Node> nodeQueue = new Queue<Node>();
        CreateRootGeometry(root);
        nodeQueue.Enqueue(root);
        int numbofgeo = 1;
        Node startOfRecurssionNode = new Node();

        //Interpret tree in a BFS-like fashion
        while (nodeQueue.Count > 0 && nodeQueue.Count < 1000)
        {
            Node currentNode = nodeQueue.Peek();
            currentNode.partOfGraph = true;
            bool startOver = false;

            if (currentNode.startOfRecurssion)
            {
                startOfRecurssionNode = currentNode;
                startOfRecurssionNode.scale.z = startOfRecurssionNode.scale.x;

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
                    Random.InitState(currentNode.edges[i].to.seed);

                    if (currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo)
                    {
                        CreateSymmetricalGeometry(currentNode.edges[i].to, currentNode);
                    }
                    else if (!currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo && currentNode.edges[i].recursiveNumb == -1)
                    {
                        CreateSingleEdgeGeometry(currentNode.edges[i].to, currentNode, currentNode.edges[i], startOfRecurssionNode);
                        numbofgeo++;
                        Debug.Log(numbofgeo);
                    }
                    else if (!currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo && currentNode.edges[i].recursiveNumb > -1)
                    {
                        CreateRecurssionGeometry(currentNode.edges[i].to, currentNode, currentNode.edges[i], startOfRecurssionNode);
                    }

                    if (!currentNode.edges[i].to.createdGeo)
                    {
                        nodeQueue.Enqueue(currentNode.edges[i].to);
                        currentNode.edges[i].to.createdGeo = true;
                    }
                }
            }        
        }

        CleanupNodes();
    }

    public Creature CreateCreatureFromNodes(Node root, int counter = 0)
    {
        int currentCounter = counter;
        if (root == null)
        {
            print("Root is null");
        }
        //Root Node def.
        ResetTree(ref root);
        ResetNodes(ref root);
        geometry = new List<GameObject>();
        oriRecurssionMuscle = new List<Muscle>();
        leftRecurssionMuscle = new List<Muscle>();
        frontRecurssionMuscle = new List<Muscle>();
        backRecurssionMuscle = new List<Muscle>();
        copyGameObject = new List<GameObject>();
        PhysicsOn = true;
        //limbSpacing = Random.Range(minLimbSpacing, maxLimbSpacing);
        muscles = new List<Muscle>();
        startPosition = Vector3.zero;
        //nodes = oldNodes;

        nodes = GetExpandedNodesList(root);

        //recurssionStack = new Stack<Node>();
        //symmetryStack = new Stack<Node>();
        //nodeOrder = new List<Node>();
        //nodeStack = new Stack<Node>();

        Queue<Node> nodeQueue = new Queue<Node>();
        CreateRootGeometry(root);
        nodeQueue.Enqueue(root);
        int numbofgeo = 1;
        Node startOfRecurssionNode = new Node();

        //Interpret tree in a BFS-like fashion
        while (nodeQueue.Count > 0 && nodeQueue.Count < 1000)
        {
            Node currentNode = nodeQueue.Peek();
            //seed = currentNode.seed;
            //Random.InitState(seed);
            nodeQueue.Peek().partOfGraph = true;
            bool startOver = false;

            if (currentNode.startOfRecurssion)
            {
                startOfRecurssionNode = currentNode;
                startOfRecurssionNode.scale.z = startOfRecurssionNode.scale.x;
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
                    Random.InitState(currentNode.edges[i].to.seed);

                    if (currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo)
                    {
                        CreateSymmetricalGeometry(currentNode.edges[i].to, currentNode);
                    }
                    //Nu borde allting tolkas som single edge geometry och inget bör gå in i createrecursiongeometry
                    else if (!currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo && currentNode.edges[i].recursiveNumb >= -1)
                    {
                        CreateSingleEdgeGeometry(currentNode.edges[i].to, currentNode, currentNode.edges[i], startOfRecurssionNode);
                    }
                    else if (!currentNode.edges[i].to.symmetry && !currentNode.edges[i].to.createdGeo && currentNode.edges[i].recursiveNumb > -1)
                    {
                        CreateRecurssionGeometry(currentNode.edges[i].to, currentNode, currentNode.edges[i], startOfRecurssionNode);
                    }

                    if (!currentNode.edges[i].to.createdGeo)
                    {
                        nodeQueue.Enqueue(currentNode.edges[i].to);
                        currentNode.edges[i].to.createdGeo = true;
                    }
                }
            }
        }

        int geoCounter = 0;
        foreach (GameObject g in geometry)
        {
            if (g != null)
            {

                geoCounter++;
            }
        }

        if (geoCounter < 2)
        {
            DestroyCurrent();

            return CreateCreatureFromNodes(root, ++currentCounter);
        }
        else if (geoCounter > 100)
        {
            DestroyCurrent();
            if (currentCounter > 10)
            {
                print("CreateCreature from nodes - too much geometry");
                return Create();
            }
            return CreateCreatureFromNodes(root, ++currentCounter);
        }


        CleanupNodes();
        GameObject handle = new GameObject();
        foreach (GameObject g in geometry)
        {
            g.transform.parent = handle.transform;
        }
        return new Creature(nodes, geometry, handle);
    }

    //Remove nodes not part of graph and edges leading to them in remaining nodes
    private void CleanupNodes()
    {
        //List<Node> nodesToDelete = new List<Node>();
        //List<Edge> edgesToDelete = new List<Edge>();

        //foreach (Node n in nodes)
        //{
        //    if (!n.partOfGraph)
        //    {
        //        nodesToDelete.Add(n);
        //    }
        //}

        //foreach (Node node in nodesToDelete)
        //{
        //    foreach (Node n in nodes)
        //    {
        //        foreach (Edge e in n.edges)
        //        {
        //            if (e.to == node)
        //            {
        //                edgesToDelete.Add(e);
        //            }
        //        }

        //        foreach (Edge e in edgesToDelete)
        //        {
        //            n.edges.Remove(e);
        //        }
        //    }
        //}

        //foreach (Node n in nodesToDelete)
        //{
        //    nodes.Remove(n);
        //}
    }

    private void CreateRecurssionGeometry(Node node, Node parent, Edge currentEdge, Node recurssionNode)
    {
        if (ReferenceEquals(parent, node) || parent.gameObjects.Count == 0 || node.scaleFactor < 0.2)
        {
            return;
        }
        Vector3 pointOnParent = new Vector3();
        bool firstGeo = true;

        bool startOver = true;
        int maxNumbOfTries = 0;

        while (startOver)
        {
            firstGeo = true;
            startOver = false;
            if (maxNumbOfTries > 100)
            {
                Debug.Log("nope");
                break;
            }
            foreach(GameObject pg in parent.gameObjects)
            {
                if(firstGeo)
                {
                    GameObject currentGeometry;

                    //Förälder Geo information
                    GameObject parentGeometry = parent.gameObjects[0];
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
                        currentGeometry.transform.position = parentGeometry.transform.position + (parentGeometry.transform.up * parentCollider.bounds.extents.y);
                        currentGeometry.transform.rotation = parentGeometry.transform.rotation;


                        if (parent.numOfRecursiveChildren > 1)
                        {
                            int multiplier = 1;
                            if (currentEdge.recursiveNumb % 2 == 0)
                            {
                                multiplier = -1;
                            }

                            switch (currentEdge.recursiveNumb)
                            {
                                case 0:
                                    currentGeometry.transform.RotateAround(parentGeometry.transform.position, parentGeometry.transform.forward, multiplier * 45);
                                    break;
                                case 1:
                                    currentGeometry.transform.RotateAround(parentGeometry.transform.position, parentGeometry.transform.forward, multiplier * 45);
                                    break;
                                case 2:
                                    currentGeometry.transform.RotateAround(parentGeometry.transform.position, parentGeometry.transform.right, multiplier * 45);
                                    break;
                                case 3:
                                    currentGeometry.transform.RotateAround(parentGeometry.transform.position, parentGeometry.transform.right, multiplier * 45);
                                    break;
                                default:
                                    break;
                            }
                        }

                        currentGeometry.transform.localScale = recurssionNode.scale * node.scaleFactor;
                        node.scale *= node.scaleFactor;
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
                            currentGeometry.transform.position += (directionToMove * (distance + limbSpacing));
                        }


                        if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                            parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance)
                            && parent.referenceNode == null)
                        {
                            currentGeometry.transform.position += (directionToMove * (distance + limbSpacing));
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

                                Debug.Log("Destroying Recurssion symmetry");
                                node.gameObjects.Clear();
                                created = false;
                                break;
                            }
                        }

                        if (!created)
                        {
                            break;
                        }

                        if (created)
                        {
                            JointManager joint;

                            if (currentGeometry.TryGetComponent<JointManager>(out joint))
                            {
                                joint.AddRecursionJoint(parentGeometry, node.recursionJointType);
                            }
                            else
                            {
                                joint = currentGeometry.AddComponent<JointManager>();
                                joint.AddRecursionJoint(parentGeometry, node.recursionJointType);
                            }

                            if (parent == recurssionNode)
                            {
                                MuscleManager muscles;
                                if (currentEdge.recursiveNumb == 0)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {
                                        muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                            node.muscleConnectedAnchorSeed, node.seed);

                                        oriRecurssionMuscle = muscles.muscles;
                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();
                                        muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                            node.muscleConnectedAnchorSeed, node.seed);

                                        oriRecurssionMuscle = muscles.muscles;
                                    }
                                }
                                else if (currentEdge.recursiveNumb == 1)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {
                                        if (oriRecurssionMuscle.Count == 0)
                                        {
                                            GameObject temp = new GameObject();
                                            MuscleManager muscleManager = temp.AddComponent<MuscleManager>();

                                            muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                                node.muscleConnectedAnchorSeed, node.seed);

                                            muscleManager.CreateRefMuscles(parentGeometry, temp, muscles.muscles, parentGeometry.transform.right);
                                            oriRecurssionMuscle = muscleManager.muscles;
                                            leftRecurssionMuscle = muscles.muscles;
                                            Destroy(temp);
                                        }
                                        else
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, parentGeometry.transform.right);
                                            leftRecurssionMuscle = muscles.muscles;
                                        }
                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();

                                        if (oriRecurssionMuscle.Count == 0)
                                        {
                                            GameObject temp = new GameObject();
                                            MuscleManager muscleManager = temp.AddComponent<MuscleManager>();

                                            muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                                node.muscleConnectedAnchorSeed, node.seed);

                                            muscleManager.CreateRefMuscles(parentGeometry, temp, muscles.muscles, parentGeometry.transform.right);
                                            oriRecurssionMuscle = muscleManager.muscles;
                                            leftRecurssionMuscle = muscles.muscles;
                                            Destroy(temp);
                                        }
                                        else
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, parentGeometry.transform.right);
                                            leftRecurssionMuscle = muscles.muscles;
                                        }
                                    }
                                }
                                else if (currentEdge.recursiveNumb == 2)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {
                                        if (oriRecurssionMuscle.Count == 0)
                                        {
                                            GameObject temp = new GameObject();
                                            MuscleManager muscleManager = temp.AddComponent<MuscleManager>();

                                            muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                                node.muscleConnectedAnchorSeed, node.seed);

                                            muscleManager.CreateRefMuscles(parentGeometry, temp, muscles.muscles, (parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            oriRecurssionMuscle = muscleManager.muscles;
                                            frontRecurssionMuscle = muscles.muscles;
                                            Destroy(temp);
                                        }
                                        else
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            frontRecurssionMuscle = muscles.muscles;
                                        }

                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();

                                        if (oriRecurssionMuscle.Count == 0)
                                        {
                                            GameObject temp = new GameObject();
                                            MuscleManager muscleManager = temp.AddComponent<MuscleManager>();

                                            muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                                node.muscleConnectedAnchorSeed, node.seed);

                                            muscleManager.CreateRefMuscles(parentGeometry, temp, muscles.muscles, (parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            oriRecurssionMuscle = muscleManager.muscles;
                                            frontRecurssionMuscle = muscles.muscles;
                                            Destroy(temp);
                                        }
                                        else
                                        {
                                            foreach (Edge e in parent.edges)
                                            {
                                                if (e.recursiveNumb == 0)
                                                {
                                                    muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                                    frontRecurssionMuscle = muscles.muscles;
                                                    break;
                                                }
                                            }

                                        }
                                    }
                                }
                                else if (currentEdge.recursiveNumb == 3)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {

                                        if (oriRecurssionMuscle.Count == 0)
                                        {
                                            GameObject temp = new GameObject();
                                            MuscleManager muscleManager = temp.AddComponent<MuscleManager>();

                                            muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                                node.muscleConnectedAnchorSeed, node.seed);

                                            muscleManager.CreateRefMuscles(parentGeometry, temp, muscles.muscles, (-parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            oriRecurssionMuscle = muscleManager.muscles;
                                            backRecurssionMuscle = muscles.muscles;
                                            Destroy(temp);
                                        }
                                        else
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (-parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            backRecurssionMuscle = muscles.muscles;
                                        }
                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();

                                        if (oriRecurssionMuscle.Count == 0)
                                        {
                                            GameObject temp = new GameObject();
                                            MuscleManager muscleManager = temp.AddComponent<MuscleManager>();

                                            muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                                node.muscleConnectedAnchorSeed, node.seed);

                                            muscleManager.CreateRefMuscles(parentGeometry, temp, muscles.muscles, (-parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            oriRecurssionMuscle = muscleManager.muscles;
                                            backRecurssionMuscle = muscles.muscles;
                                            Destroy(temp);
                                        }
                                        else
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (-parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            backRecurssionMuscle = muscles.muscles;
                                        }
                                    }
                                }
                            }
                            else if (parent != recurssionNode)
                            {
                                MuscleManager muscles;
                                if (currentEdge.recursiveNumb == 0)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {
                                        muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle);
                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();
                                        muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle);
                                    }

                                    oriRecurssionMuscle = muscles.muscles;
                                }
                                else if (currentEdge.recursiveNumb == 1)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {
                                        if (leftRecurssionMuscle.Count == 0)
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, parentGeometry.transform.right);
                                            leftRecurssionMuscle = muscles.muscles;
                                        }
                                        else
                                        {
                                            muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, leftRecurssionMuscle);
                                        }
                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();
                                        if (leftRecurssionMuscle.Count == 0)
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, parentGeometry.transform.right);
                                            leftRecurssionMuscle = muscles.muscles;
                                        }
                                        else
                                        {
                                            muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, leftRecurssionMuscle);
                                        }
                                    }
                                }
                                else if (currentEdge.recursiveNumb == 2)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {
                                        if (frontRecurssionMuscle.Count == 0)
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            frontRecurssionMuscle = muscles.muscles;
                                        }
                                        else
                                        {
                                            muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, frontRecurssionMuscle);
                                        }

                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();
                                        if (frontRecurssionMuscle.Count == 0)
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            frontRecurssionMuscle = muscles.muscles;
                                        }
                                        else
                                        {
                                            muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, frontRecurssionMuscle);
                                        }
                                    }
                                }
                                else if (currentEdge.recursiveNumb == 3)
                                {
                                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                                    {
                                        if (backRecurssionMuscle.Count == 0)
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (-parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            backRecurssionMuscle = muscles.muscles;
                                        }
                                        else
                                        {
                                            muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, backRecurssionMuscle);
                                        }
                                    }
                                    else
                                    {
                                        muscles = currentGeometry.AddComponent<MuscleManager>();
                                        if (backRecurssionMuscle.Count == 0)
                                        {
                                            muscles.CreateRefMuscles(parentGeometry, currentGeometry, oriRecurssionMuscle, (-parentGeometry.transform.forward + parentGeometry.transform.right).normalized);
                                            backRecurssionMuscle = muscles.muscles;
                                        }
                                        else
                                        {
                                            muscles.CreateRecurssionMuscles(parentGeometry, currentGeometry, backRecurssionMuscle);
                                        }
                                    }
                                }

                            }
                            copyGameObject.Add(currentGeometry);
                            geometry.Add(currentGeometry);
                            currentGeometry.GetComponent<GeoInfo>().recursiveNumb = currentEdge.recursiveNumb;
                            currentGeometry.name = node.id.ToString();

                            if (node.color != null)
                            {
                                currentGeometry.GetComponent<Renderer>().material.color = node.color;
                            }

                            firstGeo = false;
                        }
                    }
                }
                else if (!firstGeo)
                {
                    CreateRefRecGeometry(copyGameObject[0],pg, node, parent);
                }
            }
            copyGameObject.Clear();
        }
    }

    public void FixedUpdate()
    {
            if (PhysicsOn)
            {
            foreach (GameObject g in geometry)
            {
                if (g != null)
                {
                    if (g.TryGetComponent<Rigidbody>(out Rigidbody rb))
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.interpolation = RigidbodyInterpolation.Extrapolate;

                    }
                }
            }

            PhysicsOn = false;
        }

        if (!generated)
        {
            generated = true;
            Create(/*seed*/);
            //InterpretTree(root);

        }

        //Spawn new Creature
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DestroyCurrent();
            Create();
            PhysicsOn = true;
        }
    }

    private void DestroyCurrent()
    {
        for (int i = 0; i < geometry.Count; i++)
        {
            Destroy(geometry[i]);
        }
    }

    public void CreateRootGeometry(Node node)
    {
        rootGameObject = GameObject.CreatePrimitive(node.primitiveType);
        rootGameObject.transform.position = new Vector3(0, 0, 0);
        node.rotation = Vector3.zero;
        rootGameObject.transform.rotation = Quaternion.Euler(node.rotation);
        rootGameObject.transform.localScale = node.scale;
        Rigidbody rb = rootGameObject.AddComponent<Rigidbody>();
        rootGameObject.AddComponent<GeoInfo>();
        rb.isKinematic = true;
        rb.useGravity = false;
        if (root.startOfRecurssion)
            root.scale.x = root.scale.z;
        node.parent = null;
        node.created = true;
        node.gameObjects.Clear();
        node.gameObjects.Add(rootGameObject);
        geometry.Add(rootGameObject);

        if(node.color != null)
        {
            rootGameObject.GetComponent<Renderer>().material.color = node.color;
        }
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
                if (tries > 20)
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
                    currentGeometry.transform.position += (directionToMove * (distance + limbSpacing));
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
                    JointManager joint;

                    if (currentGeometry.TryGetComponent<JointManager>(out joint))
                    {
                        joint.AddRandomJoint(parentGeometry, node, parent);
                    }
                    else
                    {
                        joint = currentGeometry.AddComponent<JointManager>();
                        joint.AddRandomJoint(parentGeometry, node, parent);
                    }

                    MuscleManager muscles;
                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                    {
                        muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                            node.muscleConnectedAnchorSeed, node.seed);
                    }
                    else
                    {
                        muscles = currentGeometry.AddComponent<MuscleManager>();
                        muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                            node.muscleConnectedAnchorSeed, node.seed);
                    }
                    currentGeoIndex++;
                    node.gameObjects.Add(currentGeometry);
                    currentGeometry.name = node.id.ToString();
                    geometry.Add(currentGeometry);
                    currentGeometry.GetComponent<GeoInfo>().ParentToChildVector = currentGeometry.transform.position - parentGeometry.transform.position;

                    if (node.color != null)
                    {
                        currentGeometry.GetComponent<Renderer>().material.color = node.color;
                    }
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
                Vector3 axis = parentGeometry.transform.right;

                if (i % 2 == 0)
                {
                    if (parent.referenceNode != null)
                    {
                        if (node.referenceNode.axisList.Count > 0 && i - 1 < node.referenceNode.axisList.Count)
                        {
                            if (node.referenceNode.axisList.ElementAt(i - 1) != null )
                            {
                                axis = node.referenceNode.axisList.ElementAt(i - 1);
                            }

                            //!!!
                            //axis = node.referenceNode.axisList.Dequeue();
                            //!!!

                        }
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
                    Quaternion reflectedQuat = mirrorNormalQuat * objectQuat * mirrorNormalQuat;
                    mirrorRot = reflectedQuat;

                    Vector3 mirrorPos = Vector3.Reflect(currentGeometry.transform.position - parentGeometry.transform.position, axis) + parentGeometry.transform.position;

                    GameObject refChild = GameObject.CreatePrimitive(node.primitiveType);
                    refChild.transform.position = mirrorPos;
                    refChild.transform.rotation = mirrorRot;
                    refChild.transform.localScale = node.scale;
                    Rigidbody rb = refChild.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;

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
                            //Debug.Log("Test");
                            break;
                        }
                    }

                    if (!newAxis)
                    {
                        JointManager joint;

                        if (refChild.TryGetComponent<JointManager>(out joint))
                        {
                            joint.AddRandomJoint(parentGeometry, node, parent);
                        }
                        else
                        {
                            joint = refChild.AddComponent<JointManager>();
                            joint.AddRandomJoint(parentGeometry, node, parent);
                        }

                        MuscleManager muscles;
                        if (refChild.TryGetComponent<MuscleManager>(out muscles))
                        {
                            muscles.CreateRefMuscles(parentGeometry, refChild, currentGeometry.GetComponent<MuscleManager>().muscles, axis);
                        }
                        else
                        {
                            muscles = refChild.AddComponent<MuscleManager>();
                            muscles.CreateRefMuscles(parentGeometry, refChild, currentGeometry.GetComponent<MuscleManager>().muscles, axis);
                        }

                        node.gameObjects.Add(refChild);
                        geometry.Add(refChild);
                        refChild.name = node.id.ToString();
                        //!!!!!!!!
                        //currentGeometry.GetComponent<GeoInfo>().RefAxis = axis;
                        currentGeometry.GetComponent<GeoInfo>().RefAxis = parentGeometry.GetComponent<GeoInfo>().RefAxis;
                        //!!!!!
                        refGeoInfo.RefAxis = axis;
                        refGeoInfo.ParentToChildVector = refChild.transform.position - parentGeometry.transform.position;
                        node.axisList.Add(axis);
                        refGeoInfo.PosRelParent = parentGeometry.transform.position;

                        if (node.color != null)
                        {
                            refChild.GetComponent<Renderer>().material.color = node.color;
                        }

                        restart = true;
                    }

                    if (testAxis > 10 && newAxis)
                    {
                        restart = false;
                        Debug.Log("restarting");
                        foreach (GameObject geo in node.gameObjects)
                        {
                            Destroy(geo);
                            geometry.Remove(geo);
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

                    if (firstGeo)
                    {
                        if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                            parentCollider, parentCollider.transform.position, parentCollider.transform.rotation, out directionToMove, out distance))
                        {
                            currentGeometry.transform.position += (directionToMove * (distance + limbSpacing));
                        }
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
                        JointManager joint;

                        if (currentGeometry.TryGetComponent<JointManager>(out joint))
                        {
                            joint.AddRandomJoint(parentGeometry, node, parent);
                        }
                        else
                        {
                            joint = currentGeometry.AddComponent<JointManager>();
                            joint.AddRandomJoint(parentGeometry, node, parent);
                        }

                        if (firstGeo)
                        {
                            MuscleManager muscles;
                            if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                            {
                                muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                    node.muscleConnectedAnchorSeed, node.seed);
                            }
                            else
                            {
                                muscles = currentGeometry.AddComponent<MuscleManager>();
                                muscles.CreateNewMuscles(parentGeometry, currentGeometry, node.numbOfMuscleSeed, node.muscleStrengthSeed, node.muscleAnchorSeed,
                                    node.muscleConnectedAnchorSeed, node.seed);
                            }
                        }
                        else
                        {
                            MuscleManager muscles;
                            if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                            {
                                muscles.CreateRefMuscles(parentGeometry, currentGeometry, node.gameObjects[0].GetComponent<MuscleManager>().muscles, pg.GetComponent<GeoInfo>().RefAxis);
                            }
                            else
                            {
                                muscles = currentGeometry.AddComponent<MuscleManager>();
                                muscles.CreateRefMuscles(parentGeometry, currentGeometry, node.gameObjects[0].GetComponent<MuscleManager>().muscles, pg.GetComponent<GeoInfo>().RefAxis);
                            }
                        }


                        geometry.Add(currentGeometry);
                        currentGeometry.GetComponent<GeoInfo>().recursiveNumb = currentEdge.recursiveNumb;
                        currentGeometry.name = node.id.ToString();

                        if (node.color != null)
                        {
                            currentGeometry.GetComponent<Renderer>().material.color = node.color;
                        }

                        currentGeometry.GetComponent<GeoInfo>().RefAxis = pg.GetComponent<GeoInfo>().RefAxis;

                        if (firstGeo)
                        {
                            pointOnParent = currentGeometry.transform.position;
                            currentGeometry.GetComponent<GeoInfo>().ParentToChildVector = currentGeometry.transform.position - parentGeometry.transform.position;
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
                        Destroy(currentGeometry);
                        node.gameObjects.Clear();
                        created = false;
                        return false;
                    }
                }

                if (created)
                {
                    JointManager joint;

                    if (currentGeometry.TryGetComponent<JointManager>(out joint))
                    {
                        joint.AddRandomJoint(parentGeometry, node, parent);
                    }
                    else
                    {
                        joint = currentGeometry.AddComponent<JointManager>();
                        joint.AddRandomJoint(parentGeometry, node, parent);
                    }

                    MuscleManager muscles;
                    if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                    {
                        muscles.CreateRefMuscles(parentGeometry, currentGeometry, currentGeo.GetComponent<MuscleManager>().muscles, axis);
                    }
                    else
                    {
                        muscles = currentGeometry.AddComponent<MuscleManager>();
                        muscles.CreateRefMuscles(parentGeometry, currentGeometry, currentGeo.GetComponent<MuscleManager>().muscles, axis);
                    }

                    currentGeometry.GetComponent<GeoInfo>().RefAxis = axis;
                    geometry.Add(currentGeometry);
                    node.gameObjects.Add(currentGeometry);
                    currentGeometry.name = node.id.ToString();

                    if (node.color != null)
                    {
                        currentGeometry.GetComponent<Renderer>().material.color = node.color;
                    }
                }
            }
        }
        return true;
    }

    public bool CreateRefRecGeometry(GameObject currentGeo, GameObject parentGeo, Node node, Node parent)
    {
        if (ReferenceEquals(parent, node) || parent.gameObjects.Count == 0)
        {
            return false;
        }
        Vector3 pointOnParent = currentGeo.transform.position;

        GameObject currentGeometry;

        //Förälder Geo information
        GameObject parentGeometry = parentGeo;
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
                    Destroy(currentGeometry);
                    node.gameObjects.Clear();
                    created = false;
                    return false;
                }
            }

            if (created)
            {
                JointManager joint;

                if (currentGeometry.TryGetComponent<JointManager>(out joint))
                {
                    joint.AddRandomJoint(parentGeometry, node, parent);
                }
                else
                {
                    joint = currentGeometry.AddComponent<JointManager>();
                    joint.AddRandomJoint(parentGeometry, node, parent);
                }

                MuscleManager muscles;
                if (currentGeometry.TryGetComponent<MuscleManager>(out muscles))
                {
                    muscles.CreateRefMuscles(parentGeometry, currentGeometry, currentGeo.GetComponent<MuscleManager>().muscles, axis);
                }
                else
                {
                    muscles = currentGeometry.AddComponent<MuscleManager>();
                    muscles.CreateRefMuscles(parentGeometry, currentGeometry, currentGeo.GetComponent<MuscleManager>().muscles, axis);
                }

                geometry.Add(currentGeometry);
                node.gameObjects.Add(currentGeometry);
                currentGeometry.name = node.id.ToString();

                if (node.color != null)
                {
                    currentGeometry.GetComponent<Renderer>().material.color = node.color;
                }
            }
        }
        return true;
    }

    //Needs to be called after all recursive nodes have been flushed out as single nodes to prevent inifinite loop
    public List<Node> GetExpandedNodesList(Node root)
    {
        if (root == null)
        {
            Debug.Log("Root is null, GetExpandedNodeList");
            return null;
        }

        List<Node> expandedNodes = new List<Node>();
        Queue<Node> nodeQueue = new Queue<Node>();
        //List<Tuple<Node, Node>> pairs = new List<Tuple<Node, Node>>();
        //Node newRoot = new Node(root.primitiveType, root.scale, root.rotation, root.id, root.referenceNode, root.recursionJointType, root.scaleFactor, root.seed);
        expandedNodes.Add(root);
        nodeQueue.Enqueue(root);

        while (nodeQueue.Count > 0)
        {
            Node currentNode = nodeQueue.Dequeue();

            if (currentNode == null)
            {
                print("currentNode is null, GetExpandedNodesList");
                continue;
            }

            foreach (Edge e in currentNode.edges)
            {
                nodeQueue.Enqueue(e.to);
                //Node newNode = new Node(e.to.primitiveType, e.to.scale, e.to.rotation, e.to.id, e.to.referenceNode, e.to.recursionJointType, e.to.scaleFactor, e.to.seed);
                //expandedNodes.Add(newNode);
                //pairs.Add(new Tuple<Node, Node>(e.to, newNode));
                expandedNodes.Add(e.to);
            }
        }

        //nodeQueue.Clear();
        //nodeQueue.Enqueue(root);
        //while (nodeQueue.Count > 0)
        //{
        //    Node currentNode = nodeQueue.Dequeue();

        //    foreach (Edge e in currentNode.edges)
        //    {

        //        if (e.to.Equals(n))
        //        {
        //            n.edges.Add(new Edge());
        //        }
        //        nodeQueue.Enqueue(e.to);
        //        expandedNodes.Add(new Node(e.to.primitiveType, e.to.scale, e.to.rotation, e.to.id, e.to.referenceNode, e.to.recursionJointType, e.to.scaleFactor, e.to.seed));
        //        //expandedNodes.Add(e.to);
        //    }
        //}



        return expandedNodes;
    }

    public void CopyNodeTree(Node oriNode, out Node outNode)
    {
        if (oriNode == null)
        {
            print("oriNode is null, CopyNodeTree");
            outNode = null;
            return;
        }
        List<Node> toReset = new List<Node>();
        Stack<Node> copyStack = new Stack<Node>();
        copyStack.Push(oriNode);
        Dictionary<Node, Node> copyNodeEdge = new Dictionary<Node, Node>();
        bool nextNode = false;
        Node newOriNode = new Node(oriNode.primitiveType, oriNode.scale, oriNode.rotation, oriNode.id, oriNode, oriNode.recursionJointType, oriNode.scaleFactor, oriNode.seed);
        newOriNode.numbOfMuscleSeed = oriNode.numbOfMuscleSeed;
        newOriNode.muscleStrengthSeed = oriNode.muscleStrengthSeed;
        newOriNode.muscleAnchorSeed = oriNode.muscleAnchorSeed;
        newOriNode.muscleConnectedAnchorSeed = oriNode.muscleConnectedAnchorSeed;
        newOriNode.numOfRecursiveChildren = oriNode.numOfRecursiveChildren;
        newOriNode.color = oriNode.color;

        newOriNode.created = true;
        toReset.Add(newOriNode);
        copyNodeEdge.Add(oriNode, newOriNode);

        while (copyStack.Count > 0)
        {
            nextNode = false;

            Node currentNode = copyStack.Peek();

            foreach (Edge e in currentNode.edges)
            {
                if (e.to == null)
                {
                    print("currentNode.e.to is null, CopyNodeTree");
                }

                if (!e.to.created && !ReferenceEquals(e.to, e.from))
                {
                    Node newNode = new Node(e.to.primitiveType, e.to.scale, e.to.rotation, e.to.id, e.to, e.to.recursionJointType, e.to.scaleFactor, e.to.seed);
                    newNode.color = e.to.color;
                    //newNode.scale = e.to.scale;
                    //newNode.scaleFactor = e.to.scaleFactor;
                    copyStack.Push(e.to);
                    copyNodeEdge.Add(e.to, newNode);
                    e.to.created = true;
                    e.to.parent = currentNode;
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
            //Foreach outgoing edge in original
            foreach (Edge e in pair.Key.edges)
            {
                //If the edge is non-recursive
                if(!ReferenceEquals(e.to, e.from) && e.to != null)
                {
                    //Add an outgoing edge to copy of original between the copy and the copy of e.to
                    if(copyNodeEdge.TryGetValue(e.to, out Node temp))
                    {
                        Edge newEdge = new Edge(pair.Value, temp, e.recursiveLimit, e.numOfTravels/*, e.recursiveNumb, e.axis*/);
                        newEdge.recursiveNumb = e.recursiveNumb;
                        pair.Value.edges.Add(newEdge);
                    
                    }
                }
            }
        }
        outNode = newOriNode;
    }

    #region CreationType

    private void CreateRandomNodes()
    {
        //Spawn Nodes
        for (int i = 0; i < numOfNodes; i++)
        {
            int seed = Random.Range(1, 10000);
            Random.InitState(seed);

            primitiveRand = Random.Range(0, 3);
            Vector3 rotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            Node node = new Node(primitiveRand, minScale, maxScale, rotation, i);
            node.seed = seed;
            node.numbOfMuscleSeed = Random.Range(1, 10000);
            node.muscleStrengthSeed = Random.Range(1, 10000);
            node.muscleAnchorSeed = Random.Range(1, 10000);
            node.muscleConnectedAnchorSeed = Random.Range(1, 10000);

            if (i == 0)
            {
                color = new Color(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f));
            }
            node.color = color;
            nodes.Add(node);
        }

        //Add Children
        for (int x = 0; x < nodes.Count; x++)
        {
            for (int y = 0; y < nodes[x].numOfChildren; y++)
            {
                Random.InitState(nodes[x].seed);
                nodes[x].edges.Add(new Edge(nodes[x], nodes[Random.Range(0, numOfNodes)], Random.Range(0, 4), 0));
            }
        }

        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0]);
        nodes[0].stacked = true;
    }

    //private void CreateNewNodesFromSeed(int numberOfNodes, int rootSeed)
    //{      
    //    //Spawn Nodes
    //    for (int i = 0; i < numberOfNodes; i++)
    //    {
    //        if (i == 0)
    //        {
    //            Random.InitState(rootSeed);
    //            seed = rootSeed;
    //        }
    //        else
    //        {
    //            seed = Random.Range(1, 10000);
    //            Random.InitState(seed);
    //        }

    //        primitiveRand = Random.Range(0, 3);
    //        Vector3 rotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
    //        Node node = new Node(primitiveRand, minScale, maxScale, rotation, i);
    //        node.seed = seed;

    //        if (i == 0)
    //        {
    //            color = new Color(
    //            Random.Range(0f, 1f),
    //            Random.Range(0f, 1f),
    //            Random.Range(0f, 1f));
    //        }
    //        node.color = color;
    //        nodes.Add(node);
    //    }

    //    //Add Children
    //    for (int x = 0; x < nodes.Count; x++)
    //    {
    //        for (int y = 0; y < nodes[x].numOfChildren; y++)
    //        {
    //            Random.InitState(nodes[x].seed);
    //            nodes[x].edges.Add(new Edge(nodes[x], nodes[Random.Range(0, numOfNodes)], Random.Range(0, 4), 0));
    //        }
    //    }

    ////Root Node def.
    //nodeStack.Push(nodes[0]);
    //nodeOrder.Add(nodes[0]);
    //nodes[0].stacked = true;
    //}

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
        //nodes[1].edges.Add(new Edge(nodes[1], nodes[2], Random.Range(0, 4), 0));
        //nodes[2].edges.Add(new Edge(nodes[2], nodes[3], Random.Range(0, 4), 0));


        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0]);
        nodes[0].stacked = true;
    }

    private void CreateSymmetryPlusSingleTest()
    {
        //Spawn Nodes
        for (int i = 0; i < 5; i++)
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
        //nodes[1].edges.Add(new Edge(nodes[1], nodes[2], Random.Range(0, 4), 0));
        nodes[2].edges.Add(new Edge(nodes[2], nodes[3], Random.Range(0, 4), 0));
        nodes[3].edges.Add(new Edge(nodes[3], nodes[4], Random.Range(0, 4), 0));

        //Root Node def.
        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0]);
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

        nodes[0].edges.Add(new Edge(nodes[0], nodes[1], 3, 0));
        nodes[0].edges.Add(new Edge(nodes[0], nodes[1], 3, 0));
        nodes[0].edges.Add(new Edge(nodes[0], nodes[1], 3, 0));
        nodes[1].edges.Add(new Edge(nodes[1], nodes[1], 1, 0));

        //nodes[2].edges.Add(new Edge(nodes[2], nodes[3], 4, 0));
        //nodes[3].edges.Add(new Edge(nodes[3], nodes[4], 3, 0));
        //nodes[4].edges.Add(new Edge(nodes[4], nodes[5], 3, 0));

        nodeStack.Push(nodes[0]);
        nodeOrder.Add(nodes[0]);
        nodes[0].stacked = true;
    }
    #endregion
}


public class Node
{
    public int numOfRecursiveChildren = 0;
    public float randUniScale;
    public bool startOfRecurssion = false;
    public Node referenceNode = null;
    public bool createdGeo = false; 
    public bool created = false;
    public bool symmetry = false;
    public int occurence = 0;
    public float scaleFactor = 1;
    public int recursionJointType = Random.Range(0,3);
    public PrimitiveType primitiveType;
    public Vector3 scale;
    public Vector3 rotation;
    public Vector3 parentToChildDir;
    public List<GameObject> gameObjects = new List<GameObject>();
    public List<Vector3> axisList = new List<Vector3>();
    public int numOfChildren = Random.Range(0, 5);
    public bool stacked;
    public int id;
    public bool partOfGraph = false;
    public Node parent;
    public Color color;
    public int seed;
    public int muscleStrengthSeed, muscleAnchorSeed, muscleConnectedAnchorSeed, numbOfMuscleSeed;

    public List<Node> children = new List<Node>();
    public List<Edge> edges = new List<Edge>();

    public Node(PrimitiveType primitiveType, Vector3 scale, Vector3 rotation, int id, Node referenceNode, int recursionJointType, float scaleFactor, int seed)
    {
        this.primitiveType = primitiveType;
        this.scale = scale;
        this.scaleFactor = scaleFactor * 0.7f;
        this.rotation = rotation;
        this.id = id;
        this.referenceNode = referenceNode;
        partOfGraph = true;
        this.recursionJointType = recursionJointType;
        this.seed = seed;

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
    //public Vector3 axis;

    public Edge(Node from, Node to, int recursiveLimit, int numOfTravels)
    {
        this.from = from;
        this.to = to;
        this.recursiveLimit = recursiveLimit;
        this.numOfTravels = numOfTravels;
    }

    public Edge() { }

    public Edge(Node from, Node to, int recursiveLimit, int numOfTravels, int recursiveNumb/*, Vector3 axis*/)
    {
        this.from = from;
        this.to = to;
        this.recursiveLimit = recursiveLimit;
        this.numOfTravels = numOfTravels;
        this.recursiveNumb = recursiveNumb;
        //this.axis = axis;
    }
}


public class Creature
{
    public List<Node> nodes = new List<Node>();
    public List<GameObject> geometry = new List<GameObject>();
    public GameObject handle;
    public float timer = 0, timeToGetReady = 0;
    public bool active = false;
    public bool readyToStart = false;
    public bool finished = false;
    public float fitness;
    public Vector3 initialCenterOfMass, finalCenterOfMass;

    public Creature(List<Node> nodes, List<GameObject> geometry, GameObject handle)
    {
        this.handle = handle;
        this.nodes = nodes;
        this.geometry = geometry;
    }

    public void Update()
    {
        timer += Time.deltaTime;

        foreach (GameObject g in geometry)
        {
            if (g != null)
            {
                if (g.TryGetComponent<MuscleManager>(out MuscleManager muscleManager))
                {
                    muscleManager.UpdateMuscles();
                }
            }
        }
    }
}

//public class TreeContainer : MonoBehaviour, ISerializationCallbackReceiver
//{
//    [Serializable]
//    public struct SerializableNode
//    {
//        public int numOfRecursiveChildren;
//        public float randUniScale;
//        public bool startOfRecurssion;
//        public Node referenceNode;
//        public bool createdGeo;
//        public bool created;
//        public bool symmetry;
//        public int occurence;
//        public float scaleFactor;
//        public int recursionJointType;
//        public PrimitiveType primitiveType;
//        public Vector3 scale;
//        public Vector3 rotation;
//        public Vector3 parentToChildDir;
//        public List<GameObject> gameObjects;
//        public List<Vector3> axisList;
//        public int numOfChildren;
//        public bool stacked;
//        public int id;
//        public bool partOfGraph;
//        public Node parent;
//        public Color color;
//        public int seed;

//        public List<Edge> edges;

//        public int childCount;
//        public int indexOfFirstChild;
//    }

//    Node root = new Node();
//    public List<SerializableNode> serializedNodes;

//    public void OnBeforeSerialize()
//    {
//        if (serializedNodes == null)
//        {
//            serializedNodes = new List<SerializableNode>();
//        }

//        if (root == null)
//        {
//            root = new Node();
//        }

//        serializedNodes.Clear();
//        AddNodeToSerializedNodes(root);
//    }

//    void AddNodeToSerializedNodes(Node n)
//    {
//        var serializedNode = new SerializableNode()
//        {
//            numOfRecursiveChildren = n.numOfRecursiveChildren,
//            randUniScale = n.randUniScale,
//            startOfRecurssion = n.startOfRecurssion,
//            createdGeo = n.createdGeo,
//            created = n.created,
//            symmetry = n.symmetry,
//            occurence = n.occurence,
//            scaleFactor = n.scaleFactor,
//            recursionJointType = n.recursionJointType,
//            primitiveType = n.primitiveType,
//            scale = n.scale,
//            rotation = n.rotation,
//            parentToChildDir = n.parentToChildDir,
//            numOfChildren = n.numOfChildren,
//            stacked = n.stacked,
//            id = n.id,
//            partOfGraph = n.partOfGraph,
//            seed = n.seed,

//            //Datatyper som är oklara hur de serialize:as
//            axisList = n.axisList,
//            color = n.color,
//            gameObjects = n.gameObjects,

//            referenceNode = n.referenceNode,
//            edges = n.edges,
//            parent = n.parent, //cykliskt?//

//            childCount = n.edges.Count,
//            indexOfFirstChild = serializedNodes.Count + 1
//        };

//        serializedNodes.Add(serializedNode);

//        //Kanske borde ta hänsyn till rekursiva edges och ifall e.to är null
//        foreach (Edge e in n.edges)
//        {
//            AddNodeToSerializedNodes(e.to);
//        }
//    }

//    public void OnAfterDeserialize()
//    {
//        if (serializedNodes.Count > 0)
//        {
//            ReadNodeFromSerializedNodes(0, out root);
//        }
//        else
//            root = new Node();
//    }

//    int ReadNodeFromSerializedNodes(int index, out Node node)
//    {
//        var serializedNode = serializedNodes[index];

//        Node newNode = new Node()
//        {
//            numOfRecursiveChildren = serializedNode.numOfRecursiveChildren,
//            randUniScale = serializedNode.randUniScale,
//            startOfRecurssion = serializedNode.startOfRecurssion,
//            createdGeo = serializedNode.createdGeo,
//            created = serializedNode.created,
//            symmetry = serializedNode.symmetry,
//            occurence = serializedNode.occurence,
//            scaleFactor = serializedNode.scaleFactor,
//            recursionJointType = serializedNode.recursionJointType,
//            primitiveType = serializedNode.primitiveType,
//            scale = serializedNode.scale,
//            rotation = serializedNode.rotation,
//            parentToChildDir = serializedNode.parentToChildDir,
//            numOfChildren = serializedNode.numOfChildren,
//            stacked = serializedNode.stacked,
//            id = serializedNode.id,
//            partOfGraph = serializedNode.partOfGraph,

//            //oklart hur dessa läses av
//            //referenceNode = serializedNode.referenceNode,
//            axisList = serializedNode.axisList,
//            color = serializedNode.color,
//            seed = serializedNode.seed,
//            gameObjects = serializedNode.gameObjects,
//            //edges = serializedNode.edges,
//            //parent = serializedNode.parent,

//            referenceNode = new Node(),
//            parent = new Node(),
//            edges = new List<Edge>(),
//            children = new List<Node>()
//        };

//        // The tree needs to be read in depth-first, since that's how we wrote it out.
//        for (int i = 0; i != serializedNode.childCount; i++)
//        {
//            Node childNode;
//            index = ReadNodeFromSerializedNodes(++index, out childNode);
//            //Edge edge = new Edge(newNode, childNode, )
//            newNode.children.Add(childNode);
//        }

//        node = newNode;
//        return index;
//    }
//}

//public class BehaviourWithTree : MonoBehaviour, ISerializationCallbackReceiver
//    {
//        // Node class that is used at runtime.
//        // This is internal to the BehaviourWithTree class and is not serialized.
//        public class Node
//        {
//            public string interestingValue = "value";
//            public List<Node> children = new List<Node>();
//        }
//        // Node class that we will use for serialization.
//        [Serializable]
//        public struct SerializableNode
//        {
//            public string interestingValue;
//            public int childCount;
//            public int indexOfFirstChild;
//        }
//        // The root node used for runtime tree representation. Not serialized.
//        Node root = new Node();
//        // This is the field we give Unity to serialize.
//        public List<SerializableNode> serializedNodes;
//        public void OnBeforeSerialize()
//        {
//            // Unity is about to read the serializedNodes field's contents.
//            // The correct data must now be written into that field "just in time".
//            if (serializedNodes == null) serializedNodes = new List<SerializableNode>();
//            if (root == null) root = new Node();
//            serializedNodes.Clear();
//            AddNodeToSerializedNodes(root);
//            // Now Unity is free to serialize this field, and we should get back the expected 
//            // data when it is deserialized later.
//        }
//        void AddNodeToSerializedNodes(Node n)
//        {
//            var serializedNode = new SerializableNode()
//            {
//                interestingValue = n.interestingValue,
//                childCount = n.children.Count,
//                indexOfFirstChild = serializedNodes.Count + 1
//            }
//            ;
//            serializedNodes.Add(serializedNode);
//            foreach (var child in n.children)
//                AddNodeToSerializedNodes(child);
//        }
//        public void OnAfterDeserialize()
//        {
//            //Unity has just written new data into the serializedNodes field.
//            //let's populate our actual runtime data with those new values.
//            if (serializedNodes.Count > 0)
//            {
//                ReadNodeFromSerializedNodes(0, out root);
//            }
//            else
//                root = new Node();
//        }
//        int ReadNodeFromSerializedNodes(int index, out Node node)
//        {
//            var serializedNode = serializedNodes[index];
//            // Transfer the deserialized data into the internal Node class
//            Node newNode = new Node()
//            {
//                interestingValue = serializedNode.interestingValue,
//                children = new List<Node>()
//            }
//            ;
//            // The tree needs to be read in depth-first, since that's how we wrote it out.
//            for (int i = 0; i != serializedNode.childCount; i++)
//            {
//                Node childNode;
//                index = ReadNodeFromSerializedNodes(++index, out childNode);
//                newNode.children.Add(childNode);
//            }
//            node = newNode;
//            return index;
//        }
//    }


