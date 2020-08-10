using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;

public class EvolutionaryAlgorithm : MonoBehaviour
{
    List<int> testIds = new List<int>();
    public float fitnessScoreToBeat = 0;
    public UIManager uiManager;
    public Test.FitnessType fitnessType;
    public bool createFromFile = false;
    private bool createFromQueue = false;
    private bool createdFromFileDone = false;
    private bool checkedForTooHighStartingVelocity = false;
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

    public float currentExperimentMaxNormalizedFitness;
    SerializationTest serializationTest;
    private bool proceedToNextFitnessTest = false, saveFittestCreature = false;
    

    // Start is called before the first frame update
    void Start()
    {
        if (createFromFile)
        {
            createInitialRandomBatch = false;
        }

        if (startSeed != 0)
        {
            Random.InitState(startSeed);
        }

        serializationTest = new SerializationTest();
        uiManager.SetMaxPopulationSize(population);
    }

    // Update is called once per frame
    void Update()
    {
        uiManager.SetPopulationsTested(finishedTests.Count);
        uiManager.SetCurrentGeneration(currentGeneration);
        timeSinceSpawn += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.P))
        {
            proceedToNextFitnessTest = true;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            saveFittestCreature = true;
        }

        if (!physicsInitiated)
        {
            return;
        }

        if (finishedTests.Count >= population)
        {
            currentGeneration++;

            //SavePopulation(finishedTests);
            //List<Node> test1 = creationManager.GetExpandedNodesList(finishedTests[0].creature.nodes[0]);

            //List<Node> deserializedPopulation = serializationTest.DeserializePopulation();
            //List<Node> deserializedRoot = creationManager.GetExpandedNodesList(serializationTest.DeserializeTree());

            //AssertEqual(finishedTests, deserializedPopulation);

            //using (StreamWriter file = new StreamWriter(@"C:\Users\adria\Desktop\seedTest.txt", true))
            //{
                //foreach (Test t in finishedTests)
                //{
                    //file.WriteLine("Generation: " + currentGeneration + ", fitness: " + t.fitness + ", node count: " + t.creature.nodes.Count);

                //}
            //}



            plane.SetActive(false);
            int amountToSelect = population / 2;
            //int amountToSelect = population;
            generationGenomes.Clear();

            switch (fitnessType)
            {
                case Test.FitnessType.StaticAABBTop:
                    SetNormalizedFitness(0);
                    break;
                case Test.FitnessType.StaticHighestAABBTop:
                    SetNormalizedFitness(0);
                    SetNormalizedFitness(1);
                    break;
                case Test.FitnessType.HighestAABBTopHighestAABBBottom:
                    SetNormalizedFitness(0);
                    SetNormalizedFitness(1);
                    break;
                case Test.FitnessType.HighestAABBBottomDistance:
                    SetNormalizedFitness(0);
                    SetNormalizedFitness(1);
                    break;
                case Test.FitnessType.Distance:
                    SetNormalizedFitness(0);
                    break;
                default:
                    break;
            }

            List<Node> champions = CheckIfFullFitness();
            List<Node> bestTests = new List<Node>();

            if (champions != null)
            {
                if (fitnessType != Test.FitnessType.Distance)
                {
                    fitnessType++;
                }
                bestTests = champions;
            }
            else
            {
                for (int i = 0; i < amountToSelect; i++)
                {
                    bestTests.Add(GetFittest());
                }
            }
            
          
            



            ///HÄr under är de kaos
            //adList<List<Node>> bestGenomes = new List<List<Node>>();
            //List<List<Node>> bestGenomes2 = CreateGenomesFromTests(bestTests);
            //generationGenomes.AddRange(bestGenomes);
            //generationGenomes.AddRange(bestGenomes2);

            generationGenomes.AddRange(CrossOver(ref bestTests));

            //foreach (Creature t in bestTests)
            //{
            //    bestGenomes.Add(t.nodes);
            //}
            //generationGenomes.AddRange(bestGenomes);

            Mutate(ref generationGenomes, 0.1f);

            if (proceedToNextFitnessTest)
            {
                fitnessType++;
                proceedToNextFitnessTest = false;
            }

            newCreatures = CreatePopulationFromGenomes(generationGenomes);

            //Delete gameobjects
            tests.ForEach(delegate (Test t) { t.PrepareForClear(); });
            tests.Clear();
            finishedTests.ForEach(delegate (Test t) { t.PrepareForClear(); });
            finishedTests.Clear();

            checkedForTooHighStartingVelocity = false;
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

    private List<Node> CheckIfFullFitness()
    {
        List<Node> champions = new List<Node>();
        List<Creature> bestCreatures = new List<Creature>();
        int percentToSelect = 5;

        float fitnessToCheck = 0;

        foreach (Test t in finishedTests)
        {
            switch (fitnessType)
            {
                case Test.FitnessType.StaticAABBTop:
                    fitnessScoreToBeat = 3.5f;
                    fitnessToCheck = t.creature.NonNormalizedFitnessScores[0];
                break;
                case Test.FitnessType.StaticHighestAABBTop:
                    fitnessScoreToBeat = 5;
                    fitnessToCheck = t.creature.NonNormalizedFitnessScores[0] + t.creature.NonNormalizedFitnessScores[1];
                    //fitnessToCheck = t.creature.normalizedFitness;
                    break;
                case Test.FitnessType.HighestAABBTopHighestAABBBottom:
                    fitnessScoreToBeat = 5;
                    fitnessToCheck = t.creature.NonNormalizedFitnessScores[0] + t.creature.NonNormalizedFitnessScores[1];
                    //fitnessToCheck = t.creature.normalizedFitness;
                    break;
                case Test.FitnessType.HighestAABBBottomDistance:
                    fitnessScoreToBeat = 5;

                    fitnessToCheck = t.creature.NonNormalizedFitnessScores[0] + t.creature.NonNormalizedFitnessScores[1];
                    //fitnessToCheck = t.creature.normalizedFitness;
                    break;
                case Test.FitnessType.Distance:
                    fitnessScoreToBeat = 10000;
                    fitnessToCheck = t.creature.NonNormalizedFitnessScores[0];
                    break;
                default:
                    break;
            }

            if (fitnessToCheck > fitnessScoreToBeat)
            {
                bestCreatures.Add(t.creature);

                if (bestCreatures.Count >= percentToSelect)
                {
                    for (int i = 0; i < percentToSelect; i++)
                    {
                        for (int j = 0; j < population/2/percentToSelect; j++)
                        {
                            Node newNode;
                            creationManager.CopyNodeTree(bestCreatures[i].nodes[0], out newNode);
                            champions.Add(newNode);
                        }
                    }
                    return champions;
                }
            }
        }

       return null;
    }

    private void SetNormalizedFitness(int i)
    {
        float totalFitness = 0;

        foreach (Test t in finishedTests)
        {
            totalFitness += t.creature.NonNormalizedFitnessScores[i];
        }

        foreach (Test t in finishedTests)
        {
            t.creature.normalizedFitness += (t.creature.NonNormalizedFitnessScores[i] / totalFitness) * 100;
        }
    }

    public void SaveCreature(Creature creature, int test)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(creature.handle, "Assets/Prefabs/Test " + test + "/" + creature.normalizedFitness + ".prefab");
    }

    public void NewSaveFolder(int test)
    {
        string guid = AssetDatabase.CreateFolder("Assets/Prefabs", "Test " + test);
        string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);
    }

    void OnDrawGizmosSelected(Bounds myBounds)
    {
        // this function shows the bounding box as a white wire cube in the scene view
        // when the object containing this bounds code is selected
        var center = myBounds.center;
        var size = myBounds.size;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, size.z));
    }

    void AssignOffSetToMuscleBFS(ref Creature creature)
    {
        Node check = creature.nodes[0];
        int numberOffNodeInCreature = CountNodesInThree(ref check);

        float offSetCyclePerNode;

        if (numberOffNodeInCreature - 1 <= 0)
            offSetCyclePerNode = 0;
        else
            offSetCyclePerNode = Mathf.PI / numberOffNodeInCreature - 1;

        Queue<Node> nodeQueue = new Queue<Node>();
        List<Node> visted = new List<Node>();
        nodeQueue.Enqueue(check);
        visted.Add(check);
        int countNode = -1;

        while (nodeQueue.Count > 0)
        {
            Node currentNode = nodeQueue.Dequeue();
            countNode++;
            foreach (GameObject g in currentNode.gameObjects)
            {
                if (g != null)
                {
                    MuscleManager mm;
                    g.TryGetComponent<MuscleManager>(out mm);
                    if (mm != null)
                    {
                        mm.offSetCycle = offSetCyclePerNode * countNode;
                    }
                }
            }

            foreach (Edge e in currentNode.edges)
            {
                if (e.to == e.from || CheckIfContains(ref visted, ref e.to))
                    continue;
                else
                {
                    nodeQueue.Enqueue(e.to);
                    visted.Add(e.to);
                }
            }
        }
    }

    private void DestroyCreature(ref Creature toDestroy)
    {
        Destroy(toDestroy.handle);
    }

    static void ShowMessage([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
    {
        print(" Line " + lineNumber + " (" + caller + ")");
    }

    private bool AssertEqual(List<Test> finishedTests, List<Node> deserializedRoots)
    {
        for (int i = 0; i < finishedTests.Count; i++)
        {
            List<Node> testNodes = creationManager.GetExpandedNodesList(finishedTests[i].creature.nodes[0]);
            List<Node> deserializedNodes = creationManager.GetExpandedNodesList(deserializedRoots[i]);

            if (testNodes.Count != deserializedNodes.Count)
            {
                return false;
            }

            for (int j = 0; j < testNodes.Count; j++)
            {
                if (testNodes[j].color != deserializedNodes[j].color)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].rotation != deserializedNodes[j].rotation)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].scale != deserializedNodes[j].scale)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].primitiveType != deserializedNodes[j].primitiveType)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].edges.Count != deserializedNodes[j].edges.Count)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].scaleFactor != deserializedNodes[j].scaleFactor)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].recursionJointType != deserializedNodes[j].recursionJointType)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].numOfRecursiveChildren != deserializedNodes[j].numOfRecursiveChildren)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].seed != deserializedNodes[j].seed)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }
                if (testNodes[j].uniqueId != deserializedNodes[j].uniqueId)
                {
                    ShowMessage();
                    Debug.Break();
                    return false;
                }

            }
        }

        return true;
    }

    private void SavePopulation(List<Test> tests)
    {
        List<Node> roots = new List<Node>();

        foreach (Test t in tests)
        {
            roots.Add(t.creature.nodes[0]);
        }
        serializationTest.SerializePopulation(roots);
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

    List<List<Node>> CrossOver(ref List<Node> bestTests)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        List<List<Node>> genomes = new List<List<Node>>();
        List<List<Node>> newGenomes = new List<List<Node>>();
        
        int fatherNumbOfNodes;
        int motherNumbOfNodes;

        int cutOffFather;
        int cutOffMother;

        Node fatherRoot = new Node();
        Node motherRoot = new Node();

        Node childOneRoot = new Node();
        Node childTwoRoot = new Node();

        Node cutOffNodeOne = new Node();
        Node cutOffNodeTwo = new Node();

        foreach (Node n in bestTests)
        {
            List<Node> temp = new List<Node>();
            temp.Add(n);
            genomes.Add(temp);
        }

        for (int i = 0; i < bestTests.Count; i = i + 2)
        {
            int fatherPosInList = Random.Range(0, genomes.Count);
            fatherRoot = genomes[fatherPosInList][0];
            genomes.Remove(genomes[fatherPosInList]);

            int motherPosInList = Random.Range(0, genomes.Count);
            motherRoot = genomes[motherPosInList][0];
            genomes.Remove(genomes[motherPosInList]);

            fatherNumbOfNodes = CountNodesInThree(ref fatherRoot);
            motherNumbOfNodes = CountNodesInThree(ref motherRoot);

            cutOffFather = Random.Range(2, fatherNumbOfNodes);
            cutOffMother = Random.Range(2, motherNumbOfNodes);

            if (fatherNumbOfNodes < 3 && motherNumbOfNodes < 3)
            {
                Node copy1;
                Node copy2;

                creationManager.CopyNodeTree(childOneRoot, out copy1);
                creationManager.CopyNodeTree(childTwoRoot, out copy2);

                cutOffNodeOne = FindNodeInPosition(ref childOneRoot, fatherNumbOfNodes);
                cutOffNodeTwo = FindNodeInPosition(ref childTwoRoot, motherNumbOfNodes);

                int rand = Random.Range(1, 3);

                for (int k = 0; k < rand; k++)
                {
                    cutOffNodeOne.edges.Add(new Edge(cutOffNodeOne, copy2, Random.Range(0, 4), 0));
                    cutOffNodeTwo.edges.Add(new Edge(cutOffNodeTwo, copy1, Random.Range(0, 4), 0));
                }

                copy1.parent = cutOffNodeTwo;
                copy2.parent = cutOffNodeOne;
            }
            else
            {
                creationManager.CopyNodeTree(fatherRoot, out childOneRoot);
                creationManager.CopyNodeTree(motherRoot, out childTwoRoot);

                cutOffNodeOne = FindNodeInPosition(ref childOneRoot, cutOffFather);
                cutOffNodeTwo = FindNodeInPosition(ref childTwoRoot, cutOffMother);

                if (cutOffNodeOne.parent == cutOffNodeTwo.parent)
                {

                }

                if (cutOffNodeOne.parent != null)
                {
                    foreach (Edge e in cutOffNodeOne.parent.edges)
                    {
                        if (ReferenceEquals(cutOffNodeOne, e.to))
                            e.to = cutOffNodeTwo;
                    }
                }
                else
                    print("Father null");


                if (cutOffNodeTwo.parent != null)
                {
                    foreach (Edge e in cutOffNodeTwo.parent.edges)
                    {
                        if (ReferenceEquals(e.to, cutOffNodeTwo))
                            e.to = cutOffNodeOne;
                    }
                }
                else
                    print("Mother null");

                Node temp = new Node();
                temp = cutOffNodeOne.parent;
                cutOffNodeOne.parent = cutOffNodeTwo.parent;
                cutOffNodeTwo.parent = temp;
            }

            List<Node> childOne = new List<Node>();
            List<Node> childTwo = new List<Node>();

            childOne.Add(childOneRoot);
            childTwo.Add(childTwoRoot);

            newGenomes.Add(childOne);
            newGenomes.Add(childTwo);
        }

        foreach (Node n in bestTests)
        {
            List<Node> temp = new List<Node>();
            temp.Add(n);
            newGenomes.Add(temp);
        }

        return newGenomes;
    }

    int CountNodesInThree(ref Node root)
    {
        Queue<Node> nodeQueue = new Queue<Node>();
        List<Node> visted = new List<Node>();
        nodeQueue.Enqueue(root);
        visted.Add(root);
        int countNode = 0;

        while (nodeQueue.Count > 0)
        {
            Node currentNode = nodeQueue.Dequeue();
            countNode++;

            foreach (Edge e in currentNode.edges)
            {
                if (e.to == e.from || CheckIfContains(ref visted, ref e.to))
                    continue;
                else
                {
                    nodeQueue.Enqueue(e.to);
                    visted.Add(e.to);
                }
            }
        }
        return countNode;
    }

    Node FindNodeInPosition(ref Node root, int position)
    {
        int nodeCounter = 0;
        Queue<Node> nodeQueue = new Queue<Node>();
        List<Node> visted = new List<Node>();
        nodeQueue.Enqueue(root);
        visted.Add(root);

        Node currentNode = new Node();

        while (nodeCounter != position)
        {
            //Queue empty bug
            currentNode = nodeQueue.Dequeue();
            nodeCounter++;

            foreach (Edge e in currentNode.edges)
            {
                if (e.to == e.from || CheckIfContains(ref visted, ref e.to))
                    continue;
                else
                {
                    nodeQueue.Enqueue(e.to);
                    visted.Add(e.to);
                }
            }
        }

        return currentNode;
    }

    bool CheckIfContains(ref List<Node> listOfNodes, ref Node node)
    {
        bool contains = false;

        foreach (Node n in listOfNodes)
        {
            if (n == node)
                contains = true;
        }

        return contains;
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
                    //Random.InitState(seed);

                    Node root = genomes[i][0];

                    int numbOfNodes = CountNodesInThree(ref root);
                    int randomIndex = Random.Range(1, numbOfNodes + 1);
                    Node mutatedNode = FindNodeInPosition(ref root, randomIndex);          

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
                    {
                        genomes[i][j].numbOfMuscles++;

                        if (genomes[i][j].numbOfMuscles > 4)
                            genomes[i][j].numbOfMuscles = 4;

                        int[,] newMuscleSeeds = new int[genomes[i][j].numbOfMuscles, 3];

                        for (int p = 0; p < genomes[i][j].numbOfMuscles; p++)
                        {
                            if (p == genomes[i][j].numbOfMuscles - 1)
                            {
                                for (int s = 0; s < 3; s++)
                                {
                                    newMuscleSeeds[p, s] = Random.Range(0, 1000);
                                }
                            }
                            else
                            {
                                for (int s = 0; s < 3; s++)
                                {
                                    newMuscleSeeds[p, s] = genomes[i][j].muscleSeeds[p, s];
                                }
                            }
                        }

                        genomes[i][j].muscleSeeds = newMuscleSeeds;
                    }
                    else
                    {
                        genomes[i][j].numbOfMuscles--;

                        if (genomes[i][j].numbOfMuscles < 0)
                            genomes[i][j].numbOfMuscles = 0;

                        int[,] newMuscleSeeds = new int[genomes[i][j].numbOfMuscles, 3];

                        for (int p = 0; p < genomes[i][j].numbOfMuscles; p++)
                        {
                            for (int s = 0; s < 3; s++)
                            {
                                newMuscleSeeds[p, s] = genomes[i][j].muscleSeeds[p, s];
                            }
                        }

                        genomes[i][j].muscleSeeds = newMuscleSeeds;
                    }
                }

                mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int rnd = Random.Range(0, 1000);

                    int muscle = Random.Range(0, genomes[i][j].numbOfMuscles);

                    if (genomes[i][j].numbOfMuscles != 0)
                        genomes[i][j].muscleSeeds[muscle, 0] = rnd;
                }

                mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int rnd = Random.Range(0, 1000);

                    int muscle = Random.Range(0, genomes[i][j].numbOfMuscles);

                    if (genomes[i][j].numbOfMuscles != 0)
                        genomes[i][j].muscleSeeds[muscle, 1] = rnd;
                }

                mutationChance = Random.Range(0.0f, 1.0f);

                if (mutationChance <= mutationRate)
                {
                    int rnd = Random.Range(0, 1000);

                    int muscle = Random.Range(0, genomes[i][j].numbOfMuscles);

                    if (genomes[i][j].numbOfMuscles != 0)
                        genomes[i][j].muscleSeeds[muscle, 2] = rnd;
                }
            }
        }
    }

    private List<Creature> CreatePopulationFromGenomes(List<List<Node>> genomes)
    {
        foreach (Creature c in currentCreatures)
        {
            Creature toDestroy = c;
            DestroyCreature(ref toDestroy);
        }
        currentCreatures.Clear();

        for (int i = 0; i < creaturesToTestQueue.Count; i++)
        {
            Creature toDestroy = creaturesToTestQueue.Dequeue();
            DestroyCreature(ref toDestroy);
        }
        creaturesToTestQueue.Clear();
        for (int i = 0; i < creaturesToCreateQueue.Count; i++)
        {
            Creature toDestroy = creaturesToCreateQueue.Dequeue();
            DestroyCreature(ref toDestroy);
        }
        creaturesToCreateQueue.Clear();

        int placementFactor = 0;
        for (int i = 0; i < genomes.Count; i++)
        {
            if (placementFactor == batchSize)
            {
                placementFactor = 0;
            }

            Node newCreatureNode = genomes[i][0];
            Creature newCreature = creationManager.CreateCreatureFromNodes(ref newCreatureNode);

            if (saveFittestCreature)
            {
                if (i == population / 2)
                {
                    NewSaveFolder(0);
                    newCreature.normalizedFitness = currentExperimentMaxNormalizedFitness;
                    SaveCreature(newCreature, 0);

                    //Återställ fitness till noll
                    newCreature.SetFitness(0, 0);
                    newCreature.SetFitness(1, 0);
                    saveFittestCreature = false;
                }
            }

            newCreature.handle.transform.position = new Vector3(20 * placementFactor, 5, 0);
            newCreature.handle.name = placementFactor.ToString();
            newCreature.handle.layer = LayerMask.NameToLayer(placementFactor.ToString());

            foreach (Transform child in newCreature.handle.transform)
            {
                if (null == child)
                {
                    continue;
                }
                child.gameObject.layer = LayerMask.NameToLayer(placementFactor.ToString());
            }

            placementFactor++;


            if (i < batchSize)
            {
                currentBatchSize++;
                //InitiatePhysicsOnCreature(newCreature);
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


    bool CheckForTooHighSpawnVelocity(Creature creature)
    {
        for (int i = 0; i < creature.handle.transform.childCount; i++)
        {
            Transform child = creature.handle.transform.GetChild(i);

            if (child != null)
            {
                if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    if (rb.velocity.magnitude > 7)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }


    bool ReadyToStart(Creature creature)
    {
        //if (creature.handle == null)
        //{
        //    return false;
        //}
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

        if (createFromFile && !created && !createdFromFileDone)
        {
            //CreatePopulationFromFile();
            //CreateChampionFromFile();
            createdFromFileDone = true;
            createFromFile = false;
            created = true;
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

    //private List<Creature> CreatePopulationFromFile()
    //{
    //    plane.SetActive(false);

    //    if (creationManager == null)
    //    {
    //        if (TryGetComponent<CreateCreature>(out creationManager)) { }
    //        else creationManager = gameObject.AddComponent<CreateCreature>();
    //    }

    //    List<Node> genomes = new List<Node>();
    //    genomes = serializationTest.DeserializePopulation();

    //    currentCreatures.Clear();
    //    creaturesToTestQueue.Clear();
    //    creaturesToCreateQueue.Clear();

    //    int placementFactor = 0;
    //    for (int i = 0; i < genomes.Count; i++)
    //    {
    //        if (placementFactor == batchSize)
    //        {
    //            placementFactor = 0;
    //        }

    //        Creature newCreature = creationManager.CreateCreatureFromNodes(genomes[i]);
    //        newCreature.handle.transform.position = new Vector3(20 * placementFactor, 5, 0);
    //        newCreature.handle.SetActive(false);
    //        newCreature.handle.name = placementFactor.ToString();
    //        newCreature.handle.layer = LayerMask.NameToLayer(placementFactor.ToString());

    //        foreach (Transform child in newCreature.handle.transform)
    //        {
    //            if (null == child)
    //            {
    //                continue;
    //            }

    //            string layer = placementFactor.ToString();

    //            child.gameObject.layer = LayerMask.NameToLayer(placementFactor.ToString());
    //        }

    //        placementFactor++;


    //        if (i < batchSize)
    //        {
    //            currentBatchSize++;
    //            //InitiatePhysicsOnCreature(newCreature);
    //            currentCreatures.Add(newCreature);
    //            creaturesToTestQueue.Enqueue(newCreature);
    //        }
    //        else
    //        {
    //            newCreature.handle.SetActive(false);
    //            creaturesToCreateQueue.Enqueue(newCreature);
    //        }
    //    }

    //    foreach (Creature c in currentCreatures)
    //    {
    //        c.handle.SetActive(true);
    //    }
    //    plane.SetActive(true);
    //    return currentCreatures;
    //}

    //private void CreateChampionFromFile()
    //{
    //    plane.SetActive(false);
    //    batchSize = 1;
    //    if (creationManager == null)
    //    {
    //        if (TryGetComponent<CreateCreature>(out creationManager)) { }
    //        else creationManager = gameObject.AddComponent<CreateCreature>();
    //    }
    //    Creature newCreature = creationManager.CreateCreatureFromNodes(serializationTest.DeserializeTree());
    //    if (!newCreature.handle.activeSelf) newCreature.handle.SetActive(true);
    //    newCreature.handle.transform.position = new Vector3(0, 5, 0);
    //    newCreature.handle.SetActive(false);
    //    creaturesToCreateQueue.Enqueue(newCreature);
    //    plane.SetActive(true);
    //    timeSinceSpawn = 0;
    //}

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
            //InitiatePhysicsOnCreature(newCreature);
            currentCreatures.Add(newCreature);
            creaturesToTestQueue.Enqueue(newCreature);
        }

        checkedForTooHighStartingVelocity = false;
        timeSinceSpawn = 0;
    }

    private void CheckForStartTestOrDestroyCreature()
    {
        //if (!checkedForTooHighStartingVelocity && timeSinceSpawn > 1)
        //{
        //    checkedForTooHighStartingVelocity = true;
        //    for (int i = 0; i < currentCreatures.Count; i++)
        //    {
        //        Creature c = currentCreatures[i];
        //        if (CheckForTooHighSpawnVelocity(c))
        //        {
        //            if (createInitialRandomBatch && !createFromQueue)
        //            {
        //                print("Removing from initial randombatch");
        //                Creature toDestroy = c;
        //                currentBatchSize--;
        //                currentCreatures.RemoveAt(i--);
        //                Destroy(toDestroy.handle);
        //            }
        //            else if (currentGeneration > 0)
        //            {
        //                print("REMOVED");
        //                Creature toDestroy = c;
        //                toDestroy.SetFitness(0, 0);
        //                toDestroy.SetFitness(1, 0);
        //                Test test = new Test(fitnessType, toDestroy, 0);
        //                test.finished = true;
        //                tests.Add(test);
        //                currentCreatures.RemoveAt(i--);
        //                Destroy(toDestroy.handle);
        //            }
        //            else if (createdFromFileDone)
        //            {
        //                Creature toDestroy = c;
        //                toDestroy.SetFitness(0, 0);
        //                toDestroy.SetFitness(1, 0);
        //                Test test = new Test(fitnessType, toDestroy, 0);
        //                test.finished = true;
        //                tests.Add(test);
        //                currentCreatures.RemoveAt(i--);
        //                Destroy(toDestroy.handle);
        //            }
        //        }
        //    }
        //}
        


        if (creaturesToTestQueue.Count != 0 && timeSinceSpawn > timeToStabilize)
        {
            if (ReadyToStart(creaturesToTestQueue.Peek()))
            {
                Evaluate();
            }
            else if (!ReadyToStart(creaturesToTestQueue.Peek()) && timeSinceSpawn > maxAllowedTimeToStabilize)
            {
                //if (creaturesToTestQueue.Peek().handle == null)
                //{
                //    creaturesToTestQueue.Dequeue();
                //}
                //else
                //{
                    if (createInitialRandomBatch && !createFromQueue)
                    {
                        print("Removing from initial randombatch");
                        Creature toDestroy = creaturesToTestQueue.Dequeue();
                        currentBatchSize--;
                        currentCreatures.Remove(toDestroy);
                        Destroy(toDestroy.handle);
                    }
                    else if (currentGeneration > 0)
                    {
                        print("REMOVED");
                        Creature toDestroy = creaturesToTestQueue.Dequeue();
                        toDestroy.SetFitness(0, 0);
                        toDestroy.SetFitness(1, 0);
                        Test test = new Test(fitnessType, toDestroy, 0);
                        test.finished = true;
                        //test.fitness = 0;
                        tests.Add(test);
                        currentCreatures.Remove(toDestroy);
                        Destroy(toDestroy.handle);
                    }
                    else if (createdFromFileDone)
                    {
                        Creature toDestroy = creaturesToTestQueue.Dequeue();
                        toDestroy.SetFitness(0, 0);
                        toDestroy.SetFitness(1, 0);
                        Test test = new Test(fitnessType, toDestroy, 0);
                        test.finished = true;
                        //test.fitness = 0;
                        tests.Add(test);
                        currentCreatures.Remove(toDestroy);
                        Destroy(toDestroy.handle);

                    }
                //}
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
            //InitiatePhysicsOnCreature(creature);
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

    private void InitiatePhysicsOnCreature(Creature creature)
    {
        foreach (GameObject g in creature.geometry)
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

    private void UpdateTests()
    {
        if (tests.Count > 0)
        {
            bool cleanup = false;

            for (int i = 0; i < tests.Count; i++)
            {
                Test t = tests[i];
                if (!tests[i].finished)
                {
                    tests[i].Update();
                }
                else
                {
                    for (int j = 0; j < testIds.Count; j++)
                    {
                        if (tests[i].id == testIds[i])
                        {
                            print("duplicate test");
                        }
                    }

                    testIds.Add(tests[i].id);

                    if (finishedTests.Count < population)
                    {
                        finishedTests.Add(t);
                        testsFinished++;
                    }

                    if (finishedTests.Count == population)
                    {

                        cleanup = true;
                        break;
                    }

                    tests[i].PrepareForClear();
                    tests.RemoveAt(i--);
                }
            }

            if (cleanup)
            {
                tests.ForEach(delegate (Test t) { t.PrepareForClear(); });
                tests.Clear();
                creaturesToTestQueue.Clear();
            }
        }
    }

    Node GetFittest()
    {
        float maxFitness = float.MinValue;
        int index = 0;
        for (int i = 0; i < finishedTests.Count; i++)
        {
            if (finishedTests[i].creature.normalizedFitness > maxFitness)
            {
                maxFitness = finishedTests[i].creature.normalizedFitness;
                currentExperimentMaxNormalizedFitness = maxFitness;
                index = i;
            }
        }
        Node fittest = finishedTests[index].creature.nodes[0];
        finishedTests.Remove(finishedTests[index]);
        return fittest;
    }

    void Evaluate()
    {
        if (creaturesToTestQueue.Count == 0)
        {
            return;
        }
        else if(creaturesToTestQueue.Peek().handle == null)
        {
            return;
        }

        Test test = new Test(fitnessType, creaturesToTestQueue.Dequeue(), testTime);
        testsStarted++;
        tests.Add(test);
    }

    public class Test
    {
        public enum FitnessType
        {
            StaticAABBTop,
            StaticHighestAABBTop,
            HighestAABBTopHighestAABBBottom,
            HighestAABBBottomDistance,
            Distance
        };
        static int counter;
        public int id;
        public FitnessType fitnessType;
        public bool finished = false;
        public Creature creature;
        //public float fitness;
        float testTime;
        float jumpTime = 1;
        float timer = 0;
        Vector3 initialCenterOfMass, endCenterOfMass;
        private bool startSizeCalculated = false;
        private float AABBTopMaxHeight = 0, AABBBottomMaxHeight = 0;
        int generalTestTimeLimit = 5;

        public Test(FitnessType fitnessType, Creature creature, float testTime)
        {
            counter++;
            id = counter;
            this.testTime = testTime;
            this.creature = creature;
            this.fitnessType = fitnessType;
            initialCenterOfMass = CalculateMeanCenterOfMass();
        }

        public void Update()
        {
            if (!finished)
            {
                timer += Time.deltaTime;

                switch (fitnessType)
                {
                    case FitnessType.StaticAABBTop:
                        StaticAABBTopTest();
                        break;
                    case FitnessType.StaticHighestAABBTop:
                        StaticHighestAABBTopTest();
                        break;
                    case FitnessType.HighestAABBTopHighestAABBBottom:
                        HighestAABBTopHighestAABBBottomTest();
                        break;
                    case FitnessType.HighestAABBBottomDistance:
                        HighestAABBBottomDistanceTest();
                        break;
                    case FitnessType.Distance:
                        HorizontalDistanceTraveledTest();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Destroy(creature.handle);
            }
        }

        private void HighestAABBBottomDistanceTest()
        {
            Bounds AABB = new Bounds();

            if (timer < jumpTime)
            {
                creature.Update();
            }

            if (timer < generalTestTimeLimit)
            {
                AABB.center = CalculateMeanCenterOfMass();

                foreach (Transform child in creature.handle.transform)
                {
                    if (null == child)
                    {
                        continue;
                    }
                    AABB.Encapsulate(child.position);
                }

                if (AABB.min.y > AABBBottomMaxHeight)
                {
                    AABBBottomMaxHeight = AABB.min.y;
                }

            }
            else
            {
                endCenterOfMass = CalculateMeanCenterOfMass();
                float distanceFitness = Vector2.Distance(new Vector2(initialCenterOfMass.x, initialCenterOfMass.z), new Vector2(endCenterOfMass.x, endCenterOfMass.z));
                
                creature.SetFitness(0, AdjustFitnessByGeoCount(AABBBottomMaxHeight));
                creature.SetFitness(1, AdjustFitnessByGeoCount(distanceFitness));
                finished = true;
                Destroy(creature.handle);
            }
        }

        private void HighestAABBTopHighestAABBBottomTest()
        {
            Bounds AABB = new Bounds();
            float weightedHeightFitness = 0;
            float weightedBottomFitness = 0;

            if (timer < jumpTime)
            {
                creature.Update();
            }

            if (timer < generalTestTimeLimit)
            {
                AABB.center = CalculateMeanCenterOfMass();

                foreach (Transform child in creature.handle.transform)
                {
                    if (null == child)
                    {
                        continue;
                    }
                    AABB.Encapsulate(child.position);
                }

                if (AABB.max.y > AABBTopMaxHeight)
                {
                    AABBTopMaxHeight = AABB.max.y;
                    weightedHeightFitness = AABBTopMaxHeight / 2;
                }
                if (AABB.min.y > AABBBottomMaxHeight)
                {
                    AABBBottomMaxHeight = AABB.min.y;
                    weightedBottomFitness = AABBBottomMaxHeight * 2;
                }

            }
            else
            {
                creature.SetFitness(0, AdjustFitnessByGeoCount(weightedHeightFitness));
                creature.SetFitness(1, AdjustFitnessByGeoCount(weightedBottomFitness));
                finished = true;
                Destroy(creature.handle);
            }
        }

        public float AdjustFitnessByGeoCount(float inputFitness)
        {
            if (creature.geoCounter > 25)
            {
                return 0;
            }

            if (creature.geoCounter >= 8)
            {
                return inputFitness * (8 / creature.geoCounter);
            }
            //else if (creature.geoCounter < 8)
            //{
            //    return inputFitness * 1 / (8 / creature.geoCounter);
            //}

            return inputFitness;
        }

        private void StaticHighestAABBTopTest()
        {
            Bounds AABB = new Bounds();

            if (startSizeCalculated)
            {
                AABB.center = CalculateMeanCenterOfMass();

                foreach (Transform child in creature.handle.transform)
                {
                    if (null == child)
                    {
                        continue;
                    }
                    AABB.Encapsulate(child.position);
                }

                float fitness = AABB.size.y;
                creature.SetFitness(0, AdjustFitnessByGeoCount(fitness));

                startSizeCalculated = true;
            }
           

            if (timer < jumpTime)
            {
                creature.Update();
            }

            if (timer < generalTestTimeLimit)
            {
                AABB.center = CalculateMeanCenterOfMass();

                foreach (Transform child in creature.handle.transform)
                {
                    if (null == child)
                    {
                        continue;
                    }
                    AABB.Encapsulate(child.position);
                }

                if (AABB.max.y > AABBTopMaxHeight)
                {
                    AABBTopMaxHeight = AABB.max.y;
                }

            }
            else
            {
                creature.SetFitness(1, AdjustFitnessByGeoCount(AABBTopMaxHeight));
                finished = true;
                Destroy(creature.handle);
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

        void HorizontalDistanceTraveledTest()
        {
            creature.Update();

            if (timer > generalTestTimeLimit)
            {
                endCenterOfMass = CalculateMeanCenterOfMass();
                float distanceFitness = Vector2.Distance(new Vector2(initialCenterOfMass.x, initialCenterOfMass.z), new Vector2(endCenterOfMass.x, endCenterOfMass.z));
                creature.SetFitness(0, AdjustFitnessByGeoCount(distanceFitness));
                finished = true;
                Destroy(creature.handle);
            }
        }

        void StaticAABBTopTest()
        {
            Bounds AABB = new Bounds();
            AABB.center = CalculateMeanCenterOfMass();

            foreach (Transform child in creature.handle.transform)
            {
                if (null == child)
                {
                    continue;
                }
                AABB.Encapsulate(child.position);
            }

            //OnDrawGizmosSelected(AABB);

            float fitness = AABB.size.y;
            creature.SetFitness(0, AdjustFitnessByGeoCount(fitness));
            finished = true;
            Destroy(creature.handle);
        }
    }
}

//    public class Test
//    {
//        public bool finished = false;
//        public Creature creature;
//        public float fitness;
//        float testTime;
//        float timer = 0;
//        Vector3 initialCenterOfMass, endCenterOfMass;

//        public Test(Creature creature, float testTime)
//        {
//            this.testTime = testTime;
//            this.creature = creature;
//            initialCenterOfMass = CalculateMeanCenterOfMass();
//        }

//        public void Update()
//        {
//            if (!finished)
//            {
//                timer += Time.deltaTime;
//                creature.Update();

//           //     creature.handle.GetComponent<Collider>();

//                if (timer > testTime)
//                {
//                    endCenterOfMass = CalculateMeanCenterOfMass();
//                    float distanceTravelled = Vector2.Distance(new Vector2(initialCenterOfMass.x, initialCenterOfMass.z), new Vector2(endCenterOfMass.x, endCenterOfMass.z));
//                    fitness = distanceTravelled;
//                    creature.fitness = fitness;
//                    finished = true;
//                    Destroy(creature.handle);
//                }
//            }
//        }


//        public void PrepareForClear()
//        {
//            Destroy(creature.handle);
//        }

//        Vector3 CalculateMeanCenterOfMass()
//        {
//            Vector3 center = new Vector3();
//            for (int i = 0; i < creature.geometry.Count; i++)
//            {
//                if (creature.geometry[i] == null)
//                {
//                    continue;
//                }

//                if (creature.geometry[i].TryGetComponent<Rigidbody>(out Rigidbody rb))
//                {
//                    center += rb.worldCenterOfMass;
//                }
//            }

//            center = center / creature.geometry.Count;

//            return center;
//        }        
//    }
//}
