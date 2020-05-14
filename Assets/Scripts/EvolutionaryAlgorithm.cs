using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionaryAlgorithm : MonoBehaviour
{
    public int populationSize;
    float testTime = 10;
    float timer = 0;
    CreateCreature createCreature;
    Queue<Creature> creatures = new Queue<Creature>();

    // Start is called before the first frame update
    void Start()
    {
        if (TryGetComponent<CreateCreature>(out createCreature)) { }
        else createCreature = gameObject.AddComponent<CreateCreature>();

        for (int i = 0; i < populationSize; i++)
        {
            creatures.Enqueue(createCreature.Create());
        }

        Evaluate();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Evaluate()
    {
        for (int i = 0; i < populationSize; i++)
        {   
            Test test = new Test(createCreature.Create(), testTime);
            
        }
    }

    void Recombine()
    {

    }

    void Mutate()
    {

    }

    public class Test
    {
        bool finished = false;
        Creature creature;
        float fitness;
        float testTime;
        float timer = 0;
        Vector3 initialCenterOfMass, endCenterOfMass;


        public Test(Creature creature, float testTime)
        {
            this.testTime = testTime;
            this.creature = creature;
            initialCenterOfMass = CalculateMeanCenterOfMass();
        }

        void Update()
        {
            if (!finished)
            {
                timer += Time.deltaTime;

                if (timer > testTime)
                {
                    float distanceTravelled = Vector2.Distance(new Vector2(initialCenterOfMass.x, initialCenterOfMass.z), new Vector2(CalculateMeanCenterOfMass().x, CalculateMeanCenterOfMass().z));
                    finished = true;
                }
            }
        }

        Vector3 CalculateMeanCenterOfMass()
        {
            Vector3 center = new Vector3();
            for (int i = 0; i < creature.geometry.Count; i++)
            {
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
