using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EA2 : MonoBehaviour
{
    public UIManager uiManager;
    CreateCreature creatureCreator;
    List<Creature> currentBatch = new List<Creature>();
    List<Creature> totalPopulation = new List<Creature>();
    List<Creature> newGeneration = new List<Creature>();
    public int totalPopulationSize;
    public int batchSize;
    public int testTime = 10;
    public int allowedTimeToGetReady = 5;
    int generations = 1;
    public GameObject plane;
    bool batchDone = false, firstGeneration = true, populationDone = false;

    private void CreateFirstGenerationCreatures()
    {
        plane.SetActive(false);

        for (int i = 0; i < batchSize; i++)
        {
            Creature creature = creatureCreator.Create();
            creature.handle.transform.Translate(new Vector3(20 * i, 10, 0));
            currentBatch.Add(creature);
        }

        plane.SetActive(true);
    }

    public void Awake()
    {
        //creatureCreator = new CreateCreature();
        //CreateFirstGenerationCreatures();     
    }

    public void Update()
    {
        uiManager.SetCurrentGeneration(generations);
        uiManager.SetPopulationsTested(totalPopulation.Count);
        uiManager.SetMaxPopulationSize(totalPopulationSize);
    }

    public void FixedUpdate()
    {
        for (int i = 0; i < currentBatch.Count; i++)
        {
            if (!currentBatch[i].active)
            {
                ActivateCreature(currentBatch[i]);
                currentBatch[i].active = true;
            }

            if (!currentBatch[i].readyToStart)
            {
                currentBatch[i].timeToGetReady += Time.deltaTime;

                if (ReadyToStart(currentBatch[i]))
                {
                    currentBatch[i].initialCenterOfMass = CalculateMeanCenterOfMass(currentBatch[i]);
                    currentBatch[i].readyToStart = true;
                }
                else if (currentBatch[i].timeToGetReady > allowedTimeToGetReady)
                {
                    Destroy(currentBatch[i].handle);
                    currentBatch[i] = creatureCreator.Create();
                }
            }

            if (currentBatch[i].readyToStart && currentBatch[i].active && !currentBatch[i].finished)
            {
                currentBatch[i].Update();
            }

            if (currentBatch[i].timer >= testTime)
            {
                currentBatch[i].finalCenterOfMass = CalculateMeanCenterOfMass(currentBatch[i]);
                currentBatch[i].fitness = Vector3.Distance(currentBatch[i].finalCenterOfMass, currentBatch[i].initialCenterOfMass);
                currentBatch[i].finished = true;
                Destroy(currentBatch[i].handle);
            }
        }

        batchDone = true;

        foreach (Creature c in currentBatch)
        {
            if (!c.finished)
            {
                batchDone = false;
            }
        }

        if(batchDone)
            totalPopulation.AddRange(currentBatch);

        if (totalPopulation.Count >= totalPopulationSize)
        {
            foreach(Creature c in currentBatch)
            {
                Destroy(c.handle);
            }
            CreateNewGeneration();
            totalPopulation.Clear();
            currentBatch.Clear();
            batchDone = false;
            generations++;
            CreateNewCreaturesFromNewGeneration();
        }

        if (batchDone)
        {
            currentBatch.Clear();

            if (generations == 1)
            {
                CreateFirstGenerationCreatures();
            }
            else
            { 
                CreateNewCreaturesFromNewGeneration();
            }
        }      
    }

    private void CreateNewCreaturesFromNewGeneration()
    {
        plane.SetActive(false);
        int j = 0;

        for (int i = 0; i < batchSize; i++)
        {
            if(totalPopulation.Count > 0)
            {
                j = -1;
            }

            //List<Node> test = creatureCreator.CreateNodesFromSeed(newGeneration[totalPopulation.Count + i + j].seed);
            Creature creature = creatureCreator.CreateCreatureFromNodes(newGeneration[totalPopulation.Count + i +j].nodes[0]);
            creature.handle.transform.Translate(new Vector3(20 * i, 10, 0));
            currentBatch.Add(creature);
        }

        plane.SetActive(true);
    }

    private void CreateNewGeneration()
    {
        totalPopulation.Sort((Creature c, Creature other) => other.fitness.CompareTo(c.fitness));
        totalPopulation.RemoveRange(totalPopulationSize/2-1, totalPopulationSize/2);
        List<Creature> selectedOriginals = new List<Creature>();
        List<Creature> selectedDuplicates = new List<Creature>();
        newGeneration = new List<Creature>();

        selectedDuplicates = totalPopulation;
        selectedOriginals = totalPopulation;

        CrossOver(ref selectedDuplicates);

        newGeneration.AddRange(selectedOriginals);
        newGeneration.AddRange(selectedDuplicates);

        //Mutate(ref newGeneration);

    }

    void Mutate(ref List<Creature> creatures, float mutationRate = 0.01f)
    {
        for (int i = 0; i < creatures.Count; i++)
        {
            float mutationChance = Random.Range(0.0f, 1.0f);

            if (mutationChance <= mutationRate)
            {
                Node toMutate = new Node();
                creatures[i].nodes[Random.Range(0, creatures[i].nodes.Count + 1)] = toMutate;
                //genomes[i][randomIndex].Mutate();
            }
        }
    }

    private void CrossOver(ref List<Creature> creatures)
    {
        for (int i = 0; i < creatures.Count - 2; i =  i + 2)
        {
            Node node1 = creatures[i].nodes[Random.Range(0, creatures[i].nodes.Count)];
            Node node2 = creatures[i + 1].nodes[Random.Range(0, creatures[i + 1].nodes.Count)];

            if (node1.parent != null)
            {
                List<Edge> toAdd = new List<Edge>();
                List<Edge> toRemove = new List<Edge>();

                foreach (Edge e in node1.parent.edges)
                {
                    if (e.to.Equals(node1))
                    {
                        toAdd.Add(new Edge(node1.parent, node2, e.recursiveLimit, e.numOfTravels));
                        toRemove.Add(e);
                    }
                }

                node1.parent.edges.AddRange(toAdd);
                foreach (Edge e in toRemove)
                {
                    node1.parent.edges.Remove(e);
                }
            }


            if (node2.parent != null)
            {
                List<Edge> toAdd = new List<Edge>();
                List<Edge> toRemove = new List<Edge>();

                foreach (Edge e in node2.parent.edges)
                {
                    if (e.to.Equals(node2))
                    {
                        toAdd.Add(new Edge(node2.parent, node1, e.recursiveLimit, e.numOfTravels));
                        toRemove.Add(e);
                    }
                }

                node2.parent.edges.AddRange(toAdd);
                foreach (Edge e in toRemove)
                {
                    node2.parent.edges.Remove(e);
                }
            }

            Node temp = new Node();
            temp = node1.parent;
            node1.parent = node2.parent;
            node2.parent = temp;
        }
    } 

    private void ActivateCreature(Creature c)
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

    Vector3 CalculateMeanCenterOfMass(Creature creature)
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
