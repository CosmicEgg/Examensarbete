using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionaryAlgorithm : MonoBehaviour
{
    private int maxAllowedTimeToStabilize = 10, currentBatchSize = 0;
    private int timeToStabilize = 5;
    public int batchSize = 5, generation = 100;
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

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceSpawn += Time.deltaTime;

        if (!physicsInitiated)
        {
            return;
        }


        if (testsFinished >= currentBatchSize)
        {
            currentBatchSize = 0;
            testsFinished = 0;
            created = false;
            physicsInitiated = false;
        }


        if (finishedTests.Count >= generation)
        {
            List<Creature> selection = SelectBest(generation / 2, finishedTests);
            List<List<Node>> genomes = CrossOver(selection);
            Mutate(ref genomes);

            selection.AddRange(CreateCreatures(genomes));
        }
    }

    private List<Creature> CreateCreatures(List<List<Node>> genomes)
    {
        List<Creature> creatures = new List<Creature>();

        foreach (List<Node> genome in genomes)
        {
            creatures.Add(createCreature.InterpretTree(genome[0]));
        }
    }

    List<Creature> SelectBest(int amountToSelect, List<Test> from)
    {
        List<Creature> selection = new List<Creature>();

        from.Sort((Test t, Test t2) => t2.fitness.CompareTo(t.fitness));

        for (int i = 0; i < amountToSelect; i++)
        {
            selection.Add(from[i].creature);

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
                    //dictionary.Add(t, t.fitness);
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
       
        if (!physicsInitiated)
        {
            if (TryGetComponent<CreateCreature>(out createCreature)) { }
            else createCreature = gameObject.AddComponent<CreateCreature>();

            for (int i = 0; i < batchSize; i++)
            {
                currentBatchSize++;
                Creature creature = createCreature.Create();
                creature.handle.transform.Translate(new Vector3(20 * i, 10, 0)); 
                creatures.Add(creature);
                creatureQueue.Enqueue(creature);
            }

            timeSinceSpawn = 0;
            plane.SetActive(true);
            created = true;
        }     
    }

    void Creation()
    {

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

    List<List<Node>> CrossOver(List<Creature> fittest)
    {
        List<List<Node>> children = new List<List<Node>>();

        List<Node> childGenome1 = new List<Node>();
        List<Node> childGenome2 = new List<Node>();

        for (int i = 0; i < fittest.Count; i += 2)
        {
            List<Node> child = new List<Node>();
            int index1 = Random.Range(0, fittest[i].nodes.Count);
            Node node1 = fittest[i].nodes[index1];
            childGenome1 = createCreature.GetBranch(node1);

            int index2 = Random.Range(0, fittest[i + 1].nodes.Count);
            Node node2 = fittest[i + 1].nodes[index2];
            childGenome2 = createCreature.GetBranch(node2);

            Edge parentEdgeTo1 = node1.parent.edges.Find((Edge edge) => edge.to == node1);
            parentEdgeTo1.to = node2;

            Edge parentEdgeTo2 = node2.parent.edges.Find((Edge edge) => edge.to == node1);
            parentEdgeTo2.to = node1;

            children.Add(fittest[i].nodes);
            children.Add(fittest[i + 1].nodes);
            //child.AddRange(childGenome1);
            //child.AddRange(childGenome2);

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
