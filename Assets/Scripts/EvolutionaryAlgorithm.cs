using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EvolutionaryAlgorithm : MonoBehaviour
{
    public UIManager uiManager;
    private int maxAllowedTimeToStabilize = 10, currentBatchSize = 0;
    private int timeToStabilize = 5;
    public int batchSize = 5, population = 10;
    float testTime = 10;
    float timer = 0, testsFinished = 0, timeSinceSpawn = 0, testsStarted = 0;
    bool physicsInitiated = false, created = false;
    CreateCreature createCreature;
    List<Creature> creatures = new List<Creature>();
    Queue<Creature> creatureQueue = new Queue<Creature>();
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
            List<List<Node>> selection = SelectBest(population / 2, finishedTests);
            List<List<Node>> genomes = CrossOver(selection);
            //Mutate(ref genomes);
            selection.AddRange(genomes);
            List<Creature> newCreatures = RepopulateCreatureQueueFromGenomes(selection);
            tests.Clear();
            finishedTests.Clear();
            physicsInitiated = false;
            created = true;
            currentBatchSize = creatureQueue.Count;
            testsFinished = 0;
            timeSinceSpawn = 0;
            plane.SetActive(true);
        }
        else if (testsFinished >= currentBatchSize)
        {
            currentBatchSize = 0;
            testsFinished = 0;
            created = false;
            physicsInitiated = false;
        }   
    }

    private List<Creature> RepopulateCreatureQueueFromGenomes(List<List<Node>> genomes)
    {
        creatures.Clear();
        creatureQueue.Clear();

        for (int i = 0; i < genomes.Count; i++) 
        {
            Creature newCreature = createCreature.CreateCreatureFromNodes(genomes[i][0]);
            newCreature.handle.transform.Translate(new Vector3(20 * i, 10, 0));
            creatures.Add(newCreature);
            creatureQueue.Enqueue(newCreature);
        }

        return creatures;
    }

    List<List<Node>> SelectBest(int amountToSelect, List<Test> from)
    {
        List<List<Node>> selection = new List<List<Node>>();

        from.Sort((Test t, Test t2) => t2.fitness.CompareTo(t.fitness));

        for (int i = 0; i < amountToSelect; i++)
        {
            selection.Add(from[i].creature.nodes);
        }
        return selection;
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
        if (creatureQueue.Count != 0 && timeSinceSpawn > timeToStabilize)
        {
            if (ReadyToStart(creatureQueue.Peek()))
            {
                Evaluate();
            }
            else if (!ReadyToStart(creatureQueue.Peek()) && timeSinceSpawn > maxAllowedTimeToStabilize)
            {
                Creature toDestroy = creatureQueue.Dequeue();
                currentBatchSize--;
                creatures.Remove(toDestroy);
                Destroy(toDestroy.handle);
            }
        }

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
                    tests.RemoveAt(i);
                }
            }
        }


        if (!physicsInitiated && created)
        {
            foreach (Creature c in creatures)
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


            physicsInitiated = true;
        }
       
        if (!physicsInitiated && !created)
        {
            if (TryGetComponent<CreateCreature>(out createCreature)) { }
            else createCreature = gameObject.AddComponent<CreateCreature>();

            for (int i = 0; i < batchSize; i++)
            {
                currentBatchSize++;
                Creature creature = createCreature.Create();
                creature.handle.transform.Translate(new Vector3(20 * i, 5, 0)); 
                creatures.Add(creature);
                creatureQueue.Enqueue(creature);
            }

            timeSinceSpawn = 0;
            plane.SetActive(true);
            created = true;
        }     
    }

    void Evaluate()
    {
        if (creatureQueue.Count == 0)
        {
            return;
        }

        Test test = new Test(creatureQueue.Dequeue(), testTime);
        testsStarted++;
        tests.Add(test);
    }

    List<List<Node>> CrossOver(List<List<Node>> fittest)
    {
        List<List<Node>> children = new List<List<Node>>();
        List<Node> crossOverNodeBranch1 = new List<Node>();
        List<Node> crossOverNodeBranch2 = new List<Node>();

        for (int i = 0; i < fittest.Count - 1; i += 2)
        {
            //Select random node from nodeList
            int index1 = Random.Range(0, fittest[i].Count);
            Node originalCrossOverNode1 = fittest[i][index1];
            Node crossOverNode1 = new Node();
            //Retreive all nodes branching from this node
            createCreature.CopyNodeTree(originalCrossOverNode1, out crossOverNode1);
            //crossOverNodeBranch1 = createCreature.GetBranch(originalCrossOverNode1);

            //Select random node from nodeList
            int index2 = Random.Range(0, fittest[i + 1].Count);
            Node originalCrossOverNode2 = fittest[i + 1][index2];
            Node crossOverNode2 = new Node();
            //Retreive all nodes branching from this node
            createCreature.CopyNodeTree(originalCrossOverNode2, out crossOverNode2);
            //crossOverNodeBranch2 = createCreature.GetBranch(originalCrossOverNode2);

            //Node parent1 = new Node();
            //createCreature.CopyNodeTree(crossOverNode1.parent, out parent1);
            //Node parent2 = new Node();
            //createCreature.CopyNodeTree(crossOverNode2.parent, out parent2);

            if (originalCrossOverNode1.parent != null)
            {
                Edge toAdd = new Edge();
                Edge toRemove = new Edge();
                bool found = false;
                foreach (Edge e in originalCrossOverNode1.parent.edges)
                {
                    if (e.to.Equals(originalCrossOverNode1))
                    {
                        toAdd = new Edge(originalCrossOverNode1.parent, crossOverNode2, e.recursiveLimit, e.numOfTravels);
                        toRemove = e;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {

                }
                originalCrossOverNode1.parent.edges.Remove(toRemove);
                originalCrossOverNode1.parent.edges.Add(toAdd);
            }

            if (originalCrossOverNode2.parent != null)
            {
                Edge toAdd = new Edge();
                Edge toRemove = new Edge();
                bool found = false;
                foreach (Edge e in originalCrossOverNode2.parent.edges)
                {
                    if (e.to.Equals(originalCrossOverNode2))
                    {
                        toAdd = new Edge(originalCrossOverNode2.parent, crossOverNode1, e.recursiveLimit, e.numOfTravels);
                        toRemove = e;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {

                }
                originalCrossOverNode2.parent.edges.Remove(toRemove);
                originalCrossOverNode2.parent.edges.Add(toAdd);
            }
            
            crossOverNode1.parent = originalCrossOverNode2.parent;
            crossOverNode2.parent = originalCrossOverNode1.parent;

            children.Add(fittest[i]);
            children.Add(fittest[i+1]);
        }
        return children;
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
                    genomes[i][randomIndex].Mutate();
                }
            }
        }
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
