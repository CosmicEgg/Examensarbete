using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EvolutionaryAlgorithm : MonoBehaviour
{
    public UIManager uiManager;
    private bool createFromQueue = false;
    private int maxAllowedTimeToStabilize = 10, currentBatchSize = 0;
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
    public GameObject plane;
    Dictionary<Test, float> dictionary = new Dictionary<Test, float>();
    private float currentGeneration = 0;

    // Start is called before the first frame update
    void Start()
    {
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
            generationGenomes.Clear();
            List<Test> bestTests = SelectBestTests(amountToSelect, finishedTests);
            List<List<Node>> bestGenomes = CreateGenomesFromTests(bestTests);
            generationGenomes.AddRange(bestGenomes);
            generationGenomes.AddRange(CrossOver(bestTests));
            Mutate(ref generationGenomes);
            List<Creature> newCreatures = CreatePopulationFromGenomes(generationGenomes);

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

    private List<Test> SelectBestTests(int amountToSelect, List<Test> finishedTests)
    {
        List<Test> selection = new List<Test>();

        tests.Sort((Test t, Test t2) => t2.fitness.CompareTo(t.fitness));

        for (int i = 0; i < amountToSelect; i++)
        {
            selection.Add(finishedTests[i]);
        }

        return selection;
    }

    List<List<Node>> CreateGenomesFromTests(List<Test> bestTests)
    {
        List<List<Node>> genomes = new List<List<Node>>();

        for (int i = 0; i < bestTests.Count; i++)
        {
            List<Node> nodes = creationManager.CreateNodesFromSeed(bestTests[i].creature.seed);
            genomes.Add(nodes);
        }

        return genomes;
    }

    List<List<Node>> CrossOver(List<Test> bestTests)
    {
        List<List<Node>> genomes = new List<List<Node>>();

        for (int i = 0; i < bestTests.Count; i++)
        {
            List<Node> nodes = creationManager.GetExpandedNodesList(creationManager.CreateNodesFromSeed(bestTests[i].creature.seed)[0]);
            genomes.Add(nodes);
            
        }

        List<List<Node>> newGenomes = new List<List<Node>>();
        List<Node> crossOverNodeBranch1 = new List<Node>();
        List<Node> crossOverNodeBranch2 = new List<Node>();

        for (int i = 0; i < genomes.Count - 1; i += 2)
        {
            //Select random node from nodeList
            int index1 = Random.Range(0, genomes[i].Count);
            Node originalCrossOverNode1 = genomes[i][index1];
            Node crossOverNode1 = new Node();
            //Retreive all nodes branching from this node
            creationManager.CopyNodeTree(originalCrossOverNode1, out crossOverNode1);

            //Select random node from nodeList
            int index2 = Random.Range(0, genomes[i + 1].Count);
            Node originalCrossOverNode2 = genomes[i + 1][index2];
            Node crossOverNode2 = new Node();
            //Retreive all nodes branching from this node
            creationManager.CopyNodeTree(originalCrossOverNode2, out crossOverNode2);

            if (originalCrossOverNode1.parent != null)
            {
                Edge toAdd = new Edge();
                Edge toRemove = new Edge();
                //bool found = false;
                foreach (Edge e in originalCrossOverNode1.parent.edges)
                {
                    if (e.to.Equals(originalCrossOverNode1))
                    {
                        toAdd = new Edge(originalCrossOverNode1.parent, crossOverNode2, e.recursiveLimit, e.numOfTravels);
                        toRemove = e;
                        //found = true;
                        break;
                    }
                }

                originalCrossOverNode1.parent.edges.Remove(toRemove);
                originalCrossOverNode1.parent.edges.Add(toAdd);
            }

            if (originalCrossOverNode2.parent != null)
            {
                Edge toAdd = new Edge();
                Edge toRemove = new Edge();
                //bool found = false;
                foreach (Edge e in originalCrossOverNode2.parent.edges)
                {
                    if (e.to.Equals(originalCrossOverNode2))
                    {
                        toAdd = new Edge(originalCrossOverNode2.parent, crossOverNode1, e.recursiveLimit, e.numOfTravels);
                        toRemove = e;
                        //found = true;
                        break;
                    }
                }
                originalCrossOverNode2.parent.edges.Remove(toRemove);
                originalCrossOverNode2.parent.edges.Add(toAdd);
            }

            crossOverNode1.parent = originalCrossOverNode2.parent;
            crossOverNode2.parent = originalCrossOverNode1.parent;

            newGenomes.Add(genomes[i]);
            newGenomes.Add(genomes[i + 1]);
        }
        return newGenomes;
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
                    int randomIndex = Random.Range(0, genomes[i].Count);
                    //genomes[i][randomIndex].Mutate();
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
        foreach (GameObject g in creature.geometry)
        {
            if (g != null)
            {
                if (g.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    if (rb.velocity.magnitude > 0.5)
                    {
                        return false;
                    }
                    else if (rb.angularVelocity.magnitude > 0.5)
                    {
                        return false;
                    }
                }
            }
        }
        
        return true;
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
