using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;

public class EvolutionaryAlgorithm : MonoBehaviour
{
    public UIManager uiManager;
    private bool createFromQueue = false;
    private int maxAllowedTimeToStabilize = 6, currentBatchSize = 0;
    private int timeToStabilize = 6;
    public int batchSize = 6, population = 12;
    float testTime = 10;
    float timer = 0, testsFinished = 0, timeSinceSpawn = 0, testsStarted = 0;
    bool physicsInitiated = false, created = false, createInitialRandomBatch = true;
    List<List<Node>> generationGenomes = new List<List<Node>>();
    CreateCreature creationManager;
    List<Creature> currentCreatures = new List<Creature>();
    Queue<Creature> creaturesToTestQueue = new Queue<Creature>();
    Queue<Creature> creaturesToCreateQueue = new Queue<Creature>();
    List<Test> tests = new List<Test>();
    List<Test> finishedTests = new List<Test>();
    List<Creature> newCreatures = new List<Creature>();
    public GameObject plane;
    public int startSeed;
    Dictionary<Test, float> dictionary = new Dictionary<Test, float>();
    private float currentGeneration = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (startSeed != 0)
        {
            Random.InitState(startSeed);
        }

        uiManager.SetMaxPopulationSize(population);
    }

    // Update is called once per frame
    void Update()
    {
        uiManager.SetPopulationsTested(finishedTests.Count);
        uiManager.SetCurrentGeneration(currentGeneration);
        timeSinceSpawn += Time.deltaTime;

        if (!physicsInitiated)
        {
            return;
        }

        if (finishedTests.Count >= population)
        {       
            currentGeneration++;

            plane.SetActive(false);
            int amountToSelect = population / 2;
            //int amountToSelect = population;
            generationGenomes.Clear();
            List<Creature> bestTests = new List<Creature>();

            for (int i = 0; i < amountToSelect; i++)
            {
                bestTests.Add(GetFittest());
            }

            ///HÄr under är de kaos
            //List<List<Node>> bestGenomes = CreateGenomesFromTests(bestTests);
            //List<List<Node>> bestGenomes2 = CreateGenomesFromTests(bestTests);
            //generationGenomes.AddRange(bestGenomes);
            //generationGenomes.AddRange(bestGenomes2);
            generationGenomes.AddRange(CrossOver(bestTests));
            //Mutate(ref generationGenomes, 0.1f);
            newCreatures = CreatePopulationFromGenomes(generationGenomes);

            //Delete gameobjects
            tests.ForEach(delegate (Test t) { t.PrepareForClear(); });
            tests.Clear();
            finishedTests.ForEach(delegate (Test t) { t.PrepareForClear(); });
            finishedTests.Clear();

            physicsInitiated = false;
            created = true;
            createInitialRandomBatch = false;
            currentBatchSize = creaturesToTestQueue.Count;
            testsFinished = 0;
            timeSinceSpawn = 0;
            plane.SetActive(true);
        }
        //Create creatures queued for creation
        else if (testsFinished >= currentBatchSize && creaturesToCreateQueue.Count > 0)
        {
            createFromQueue = true;
            currentBatchSize = 0;
            testsFinished = 0;
            created = false;
            physicsInitiated = false;
        }
        //Create new random creatures
        else if (testsFinished >= currentBatchSize && creaturesToCreateQueue.Count == 0)
        {
            currentBatchSize = 0;
            testsFinished = 0;
            created = false;
            physicsInitiated = false;
        }   
    }

    //private List<Test> SelectBestTests(int amountToSelect, List<Test> finishedTests)
    //{
    //    List<Test> selection = new List<Test>();

    //    finishedTests.Sort((Test t, Test t2) => t2.fitness.CompareTo(t.fitness));

    //    for (int i = 0; i < amountToSelect; i++)
    //    {
    //        //Kanske borde göra en "new Test" här för kopieringens skull
    //        selection.Add(finishedTests[i]);
    //    }

    //    return selection;
    //}

    //List<List<Node>> CreateGenomesFromTests(List<Test> bestTests)
    //{
    //    List<List<Node>> genomes = new List<List<Node>>();

    //    for (int i = 0; i < bestTests.Count; i++)
    //    {
    //        List<Node> nodes = creationManager.CreateNodesFromSeed(bestTests[i].creature.nodes.Count, bestTests[i].creature.nodes[0].seed);
    //        genomes.Add(nodes);
    //    }

    //    return genomes;
    //}

    //List<List<Node>> CreateGenomesFromTests(List<Test> bestTests)
    //{
    //    List<List<Node>> genomes = new List<List<Node>>();
    //    List<Node> nodes = new List<Node>();

    //    for (int i = 0; i < bestTests.Count; i++)
    //    {
    //        nodes = new List<Node>();
    //        Node newRoot = new Node();
    //        creationManager.CopyNodeTree(bestTests[i].creature.nodes[0], out newRoot);
    //        nodes = creationManager.GetExpandedNodesList(newRoot);

    //        genomes.Add(nodes);

    //        //foreach (Node n in bestTests[i].creature.nodes)
    //        //{
    //        //    Node newNode = new Node(n.primitiveType, n.scale, n.rotation, n.id, n.referenceNode, n.recursionJointType, n.scaleFactor, n.seed);
    //        //    nodes.Add(newNode);
    //        //}

    //        //foreach (Node n in bestTests[i].creature.nodes)
    //        //{
    //        //    foreach (Edge e in n.edges)
    //        //    {

    //        //    }
    //        //}

    //        //for (int j = 0; j < nodes.Count; j++)
    //        //{
    //        //    if (nodes[j].edges.Count > 0)
    //        //    {
    //        //        foreach (Edge e in nodes[j].edges)
    //        //        {
    //        //            Node node = nodes.Find(x => x == e.to);

    //        //            nodes[j].edges.Add(new Edge(nodes[j], e.to, e.recursiveLimit, e.numOfTravels));
    //        //        }
    //        //    }
    //        //}

    //        //nodes = bestTests[i].creature.nodes;
    //        //List<Node> nodes = creationManager.CreateNodesFromSeed(bestTests[i].creature.nodes.Count, bestTests[i].creature.nodes[0].seed);
    //    }

    //    //   genomes.Add(nodes);

    //    return genomes;
    //}

    //List<List<Node>> CrossOver(List<Test> bestTests)
    //{
    //    List<List<Node>> genomes = new List<List<Node>>();
    //    //List<Node> nodes = new List<Node>();

    //    for (int i = 0; i < bestTests.Count; i++)
    //    {
    //        List<Node> expandedNodeList = creationManager.GetExpandedNodesList(bestTests[i].creature.nodes[0]);
    //        genomes.Add(expandedNodeList);

    //        //nodes = new List<Node>();
    //        //Node newRoot = new Node();
    //        //creationManager.CopyNodeTree(bestTests[i].creature.nodes[0], out newRoot);
    //        //nodes = creationManager.GetExpandedNodesList(newRoot);
    //        //genomes.Add(nodes);
    //    }

    //    int count = genomes.Count;

    //    for (int i = 0; i < genomes.Count; i++)
    //    {
    //        int random = Random.Range(i, count);
    //        var temp = genomes[i];
    //        genomes[i] = genomes[random];
    //        genomes[random] = temp;
    //    }

    //    List<List<Node>> newGenomes = new List<List<Node>>();
    //    List<Node> crossOverNodeBranch1 = new List<Node>();
    //    List<Node> crossOverNodeBranch2 = new List<Node>();

    //    for (int i = 0; i < genomes.Count - 1; i += 2)
    //    {
    //        //Select random node from nodeList
    //        int index1 = Random.Range(1, genomes[i].Count);
    //        Node originalCrossOverNode1 = genomes[i][index1];
    //        Node crossOverNode1 = new Node();
    //        //Retreive all nodes branching from this node
    //        creationManager.CopyNodeTree(originalCrossOverNode1, out crossOverNode1);

    //        //Select random node from nodeList
    //        int index2 = Random.Range(1, genomes[i + 1].Count);
    //        Node originalCrossOverNode2 = genomes[i + 1][index2];
    //        Node crossOverNode2 = new Node();
    //        //Retreive all nodes branching from this node
    //        creationManager.CopyNodeTree(originalCrossOverNode2, out crossOverNode2);

    //        if (originalCrossOverNode1.parent != null)
    //        {
    //            Edge toAdd = new Edge();
    //            List<Edge> edgesToRemove = new List<Edge>();
    //            int counter = 0;
    //            foreach (Edge e in originalCrossOverNode1.parent.edges)
    //            {
    //                if (e.to.Equals(originalCrossOverNode1))
    //                {
    //                    counter++;
    //                    toAdd = new Edge(originalCrossOverNode1.parent, crossOverNode2, e.recursiveLimit, e.numOfTravels);
    //                    toAdd.recursiveNumb = e.recursiveNumb;
    //                    edgesToRemove.Add(e);
    //                }
    //            }

    //            foreach (Edge e in edgesToRemove)
    //            {
    //                originalCrossOverNode1.parent.edges.Remove(e);
    //            }

    //            originalCrossOverNode1.parent.edges.Add(toAdd);
    //        }
    //        else if (originalCrossOverNode1.parent == null)
    //        {

    //        }

    //        if (originalCrossOverNode2.parent != null)
    //        {

    //            Edge toAdd = new Edge();
    //            List<Edge> edgesToRemove = new List<Edge>();
    //            int counter = 0;

    //            foreach (Edge e in originalCrossOverNode2.parent.edges)
    //            {
    //                if (e.to.Equals(originalCrossOverNode2))
    //                {
    //                    counter++;
    //                    toAdd = new Edge(originalCrossOverNode2.parent, crossOverNode1, e.recursiveLimit, e.numOfTravels);
    //                    toAdd.recursiveNumb = e.recursiveNumb;

    //                    edgesToRemove.Add(e);
    //                }
    //            }

    //            foreach (Edge e in edgesToRemove)
    //            {
    //                originalCrossOverNode2.parent.edges.Remove(e);
    //            }

    //            originalCrossOverNode2.parent.edges.Add(toAdd);
    //        }
    //        else if (originalCrossOverNode2.parent == null)
    //        {

    //        }

    //        //crossOverNode1.parent = originalCrossOverNode2.parent;
    //        //crossOverNode2.parent = originalCrossOverNode1.parent;

    //        Node temp = new Node();
    //        temp = crossOverNode1.parent;
    //        crossOverNode1.parent = originalCrossOverNode2.parent;
    //        crossOverNode2.parent = temp;

    //        List<Node> newNodes1 = new List<Node>();
    //        List<Node> newNodes2 = new List<Node>();
    //        newNodes1 = creationManager.GetExpandedNodesList(genomes[i][0]);
    //        newNodes2 = creationManager.GetExpandedNodesList(genomes[i + 1][0]);

    //        newGenomes.Add(newNodes1);
    //        newGenomes.Add(newNodes2);
    //    }


    //    return newGenomes;
    //}

    List<List<Node>> CrossOver(List<Creature> bestTests)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);
        List<List<Node>> genomes = new List<List<Node>>();
        List<List<Node>> newGenomes = new List<List<Node>>();

        int fatherNumbOfNodes;
        int motherNumbOfNodes;

        int cutOffFather;
        int cutOffMother;

        Node childOneRoot;
        Node childTwoRoot;

        Node cutOffNodeOne;
        Node cutOffNodeTwo;

        foreach (Creature t in bestTests)
        {
            genomes.Add(t.nodes);
        }

        for (int i = 0; i < genomes.Count; i = i + 2)
        {
            fatherNumbOfNodes = CountNodesInThree(genomes[i][0]);
            motherNumbOfNodes = CountNodesInThree(genomes[i + 1][0]);

            cutOffFather = Random.Range(2, fatherNumbOfNodes);
            cutOffMother = Random.Range(2, motherNumbOfNodes);

            creationManager.CopyNodeTree(genomes[i][0], out childOneRoot);
            creationManager.CopyNodeTree(genomes[i + 1][0], out childTwoRoot);

            cutOffNodeOne = FindNodeInPosition(childOneRoot, cutOffFather);
            cutOffNodeTwo = FindNodeInPosition(childTwoRoot, cutOffMother);

            if (cutOffNodeOne.parent != null)
            {
                foreach (Edge e in cutOffNodeOne.parent.edges)
                {
                    if (e.to == cutOffNodeOne)
                        e.to = cutOffNodeTwo;
                }
            }
            else
                print("Father null");


            if (cutOffNodeOne.parent != null)
            {
                foreach (Edge e in cutOffNodeTwo.parent.edges)
                {
                    if (e.to == cutOffNodeTwo)
                        e.to = cutOffNodeOne;
                }
            }
            else
                print("Mother null");

            List<Node> childOne = new List<Node>();
            List<Node> childTwo = new List<Node>();

            childOne.Add(childOneRoot);
            childTwo.Add(childTwoRoot);

            newGenomes.Add(childOne);
            newGenomes.Add(childTwo);
        }

        foreach (List<Node> nL in genomes)
        {
            newGenomes.Add(nL);
        }

        return newGenomes;
    }

    int CountNodesInThree(Node root)
    {
        Queue<Node> nodeQueue = new Queue<Node>();
        nodeQueue.Enqueue(root);
        int countNode = 0;

        while (nodeQueue.Count > 0)
        {
            Node currentNode = nodeQueue.Dequeue();
            countNode++;

            foreach (Edge e in currentNode.edges)
            {
                if (e.to == e.from)
                    continue;
                else
                {
                    nodeQueue.Enqueue(e.to);
                } 
                    
            }
        }
        return countNode;
    }

    Node FindNodeInPosition(Node root, int position)
    {
        int nodeCounter = 0;
        Queue<Node> nodeQueue = new Queue<Node>();
        nodeQueue.Enqueue(root);

        Node currentNode = new Node();

        while (nodeCounter != position)
        {
            currentNode = nodeQueue.Dequeue();
            nodeCounter++;

            foreach (Edge e in currentNode.edges)
            {
                if (e.to == e.from)
                    continue;
                else
                {
                    nodeQueue.Enqueue(e.to);
                }
            }
        }

        return currentNode;
    }

    void Mutate(ref List<List<Node>> genomes, float mutationRate = 0.001f)
    {
        for (int i = 0; i < genomes.Count; i++)
        {
            for (int j = 0; j < genomes[i].Count; j++)
            {
                float mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int seed = Random.Range(1, 10000);
                    Random.InitState(seed);

                    Node mutatedNode = genomes[i][j];

                    int primitiveRand = Random.Range(0, 3);
                    Vector3 scale;
                    PrimitiveType primitiveType;
                    float minScale = creationManager.minScale, maxScale = creationManager.maxScale;
                    float randUniScale;

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

                    Vector3 rotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
                    mutatedNode.primitiveType = primitiveType;
                    mutatedNode.rotation = rotation;
                    mutatedNode.scale = scale;
                    
                    mutatedNode.seed = seed;

                    mutatedNode.numbOfMuscles = Random.Range(1, 5);
                    mutatedNode.muscleSeeds = new int[mutatedNode.numbOfMuscles, 3];

                    for (int l = 0; l < mutatedNode.numbOfMuscles; l++)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            mutatedNode.muscleSeeds[l, k] = Random.Range(0, 1000);
                        }
                    }

                    Color color = new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f));

                    mutatedNode.color = color;

                    genomes[i][j] = mutatedNode;
                }

                mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int rnd = Random.Range(0, 2);

                    if (rnd == 0)
                        genomes[i][j].numbOfMuscles++;
                    else
                        genomes[i][j].numbOfMuscles--;

                    if (genomes[i][j].numbOfMuscles < 0)
                        genomes[i][j].numbOfMuscles = 0;
                    else if(genomes[i][j].numbOfMuscles < 4)
                        genomes[i][j].numbOfMuscles = 4;
                }

                mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int rnd = Random.Range(0, 1000);

                    int muscle = Random.Range(0, genomes[i][j].numbOfMuscles+1);

                    if (genomes[i][j].numbOfMuscles != 0)
                        genomes[i][j].muscleSeeds[muscle, 0] = rnd;                   
                }

                mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int rnd = Random.Range(0, 1000);

                    int muscle = Random.Range(0, genomes[i][j].numbOfMuscles + 1);

                    if (genomes[i][j].numbOfMuscles != 0)
                        genomes[i][j].muscleSeeds[muscle, 1] = rnd;
                }

                mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int rnd = Random.Range(0, 1000);

                    int muscle = Random.Range(0, genomes[i][j].numbOfMuscles + 1);

                    if (genomes[i][j].numbOfMuscles != 0)
                        genomes[i][j].muscleSeeds[muscle, 2] = rnd;
                }
            }
        }
    }

    private List<Creature> CreatePopulationFromGenomes(List<List<Node>> genomes)
    {
        currentCreatures.Clear();
        creaturesToTestQueue.Clear();
        creaturesToCreateQueue.Clear();

        int placementFactor = 0;
        for (int i = 0; i < genomes.Count; i++) 
        {
            placementFactor++;
            if (placementFactor == batchSize)
            {
                placementFactor = 0;
            }

            Creature newCreature = creationManager.CreateCreatureFromNodes(genomes[i][0]);
            newCreature.handle.transform.position = new Vector3(20 * placementFactor, 5, 0);
            newCreature.handle.layer = LayerMask.NameToLayer(placementFactor.ToString());

            foreach (Transform child in newCreature.handle.transform)
            {
                if (null == child)
                {
                    continue;
                }
                child.gameObject.layer = LayerMask.NameToLayer(placementFactor.ToString());
            }

            if (i < batchSize)
            {
                currentBatchSize++;
                currentCreatures.Add(newCreature);
                creaturesToTestQueue.Enqueue(newCreature);
            }
            else
            {
                newCreature.handle.SetActive(false);
                creaturesToCreateQueue.Enqueue(newCreature);
            }
        }

        return currentCreatures;
    }

    bool ReadyToStart(Creature creature)
    {
        //foreach (GameObject g in creature.geometry)
        for (int i = 0; i < creature.handle.transform.childCount; i++)
        {
            Transform child = creature.handle.transform.GetChild(i);

            if (child != null)
            {
                if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    if (rb.velocity.magnitude > 1)
                    {
                        return false;
                    }
                    //else if (rb.angularVelocity.magnitude > 2)
                    //{
                    //    return false;
                    //}
                }
            }
        }

        return true;

        //foreach (GameObject g in creature.handle.transform.)
        //{
        //    if (g != null)
        //    {
        //        if (g.TryGetComponent<Rigidbody>(out Rigidbody rb))
        //        {
        //            if (rb.velocity.magnitude > 0.5)
        //            {
        //                return false;
        //            }
        //            else if (rb.angularVelocity.magnitude > 0.5)
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //}
        
        //return true;
    }

    void FixedUpdate()
    {
        CheckForStartTestOrDestroyCreature();

        UpdateTests();

        if (!physicsInitiated && created)
        {
            InitiatePhysicsOnCreatures();
            physicsInitiated = true;
        }

        if (createFromQueue && !created)
        {
            CreateFromQueue();
            created = true;
        }

        if (!physicsInitiated && !created && createInitialRandomBatch)
        {
            CreateInitialRandomBatch();
            created = true;
        }
    }

    private void CreateFromQueue()
    {
        if (creaturesToCreateQueue.Count == 0)
        {
            return;
        }

        for (int i = 0; i < batchSize; i++)
        {
            Creature newCreature = creaturesToCreateQueue.Dequeue();
            if (!newCreature.handle.activeSelf) newCreature.handle.SetActive(true);
            newCreature.handle.transform.position = new Vector3(20 * i, 5, 0);

            newCreature.handle.layer = LayerMask.NameToLayer(i.ToString());

            foreach (Transform child in newCreature.handle.transform)
            {
                if (null == child)
                {
                    continue;
                }
                child.gameObject.layer = LayerMask.NameToLayer(i.ToString());
            }

            currentBatchSize++;
            currentCreatures.Add(newCreature);
            creaturesToTestQueue.Enqueue(newCreature);
        }

        timeSinceSpawn = 0;
    }

    private void CheckForStartTestOrDestroyCreature()
    {
        if (creaturesToTestQueue.Count != 0 && timeSinceSpawn > timeToStabilize)
        {
            if (ReadyToStart(creaturesToTestQueue.Peek()))
            {
                Evaluate();
            }
            else if (!ReadyToStart(creaturesToTestQueue.Peek()) && timeSinceSpawn > maxAllowedTimeToStabilize)
            {
                if (createInitialRandomBatch && !createFromQueue)
                {
                    Creature toDestroy = creaturesToTestQueue.Dequeue();
                    currentBatchSize--;
                    currentCreatures.Remove(toDestroy);
                    Destroy(toDestroy.handle);
                }
                else if (createFromQueue)
                {
                    Creature toDestroy = creaturesToTestQueue.Dequeue();
                    Test test = new Test(toDestroy, 0);
                    test.finished = true;
                    test.fitness = 0;
                    tests.Add(test);
                    currentCreatures.Remove(toDestroy);
                    Destroy(toDestroy.handle);
                }
            }
        }
    }

    private void CreateInitialRandomBatch()
    {
        plane.SetActive(false);

        if (TryGetComponent<CreateCreature>(out creationManager)) { }
        else creationManager = gameObject.AddComponent<CreateCreature>();

        for (int i = 0; i < batchSize; i++)
        {
            currentBatchSize++;
            Creature creature = creationManager.Create();
            creature.handle.transform.position = new Vector3(20 * i, 5, 0);

            creature.handle.layer = LayerMask.NameToLayer(i.ToString());


            foreach (Transform child in creature.handle.transform)
            {
                if (null == child)
                {
                    continue;
                }
                child.gameObject.layer = LayerMask.NameToLayer(i.ToString());
            }

            currentCreatures.Add(creature);
            creaturesToTestQueue.Enqueue(creature);
        }

        timeSinceSpawn = 0;
        plane.SetActive(true);
    }

    private void InitiatePhysicsOnCreatures()
    {
        foreach (Creature c in currentCreatures)
        {
            foreach (GameObject g in c.geometry)
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
        }
    }

    private void UpdateTests()
    {
        if (tests.Count > 0)
        {
            for (int i = 0; i < tests.Count; i++)
            {
                Test t = tests[i];
                if (!t.finished)
                {
                    t.Update();
                }
                else
                {
                    finishedTests.Add(t);
                    testsFinished++;
                    tests[i].PrepareForClear();
                    tests.RemoveAt(i);
                }
            }
        }
    }

    Creature GetFittest()
    {
        float maxFitness = float.MinValue;
        int index = 0;
        for (int i = 0; i < finishedTests.Count; i++)
        {
            if (finishedTests[i].creature.fitness > maxFitness)
            {
                maxFitness = finishedTests[i].creature.fitness;
                index = i;
            }
        }
        Creature fittest = finishedTests[index].creature;
        finishedTests.Remove(finishedTests[index]);
        return fittest;
    }

    void Evaluate()
    {
        if (creaturesToTestQueue.Count == 0)
        {
            return;
        }

        Test test = new Test(creaturesToTestQueue.Dequeue(), testTime);
        testsStarted++;
        tests.Add(test);
    }

    public class Test
    {
        public bool finished = false;
        public Creature creature;
        public float fitness;
        float testTime;
        float timer = 0;
        Vector3 initialCenterOfMass, endCenterOfMass;

        public Test(Creature creature, float testTime)
        {
            this.testTime = testTime;
            this.creature = creature;
            initialCenterOfMass = CalculateMeanCenterOfMass();
        }

        public void Update()
        {
            if (!finished)
            {
                timer += Time.deltaTime;
                creature.Update();

                if (timer > testTime)
                {
                    endCenterOfMass = CalculateMeanCenterOfMass();
                    float distanceTravelled = Vector2.Distance(new Vector2(initialCenterOfMass.x, initialCenterOfMass.z), new Vector2(endCenterOfMass.x, endCenterOfMass.z));
                    fitness = distanceTravelled;
                    finished = true;
                    Destroy(creature.handle);
                }
            }
        }


        public void PrepareForClear()
        {
            Destroy(creature.handle);
        }

        Vector3 CalculateMeanCenterOfMass()
        {
            Vector3 center = new Vector3();
            for (int i = 0; i < creature.geometry.Count; i++)
            {
                if (creature.geometry[i] == null)
                {
                    continue;
                }

                if (creature.geometry[i].TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    center += rb.worldCenterOfMass;
                }
            }

            center = center / creature.geometry.Count;

            return center;
        }        
    }
}
