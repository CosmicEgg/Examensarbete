using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionaryAlgorithm : MonoBehaviour
{
    public int populationSize;
    float testTime = 10;
    float timer = 0;
    bool physicsInitiated = false, created = false;
    CreateCreature createCreature;
    List<Creature> creatures = new List<Creature>();
    Queue<Test> tests = new Queue<Test>();
    List<Test> finishedTests = new List<Test>();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!physicsInitiated)
        {
            return;
        }

        if (tests.Count > 0)
        {
            if (tests.Peek().finished == false)
            {
                tests.Peek().Update();
            }
            else
            {
                finishedTests.Add(tests.Dequeue());
            }
        }
    }

    void FixedUpdate()
    {
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

            Evaluate();
            physicsInitiated = true;
        }
       


        if (!physicsInitiated)
        {
            if (TryGetComponent<CreateCreature>(out createCreature)) { }
            else createCreature = gameObject.AddComponent<CreateCreature>();

            for (int i = 0; i < populationSize; i++)
            {
                Creature creature = createCreature.Create(0, i);
                creatures.Add(creature);
            }

            created = true;
        }     
    }

    void Evaluate()
    {
        for (int i = 0; i < populationSize; i++)
        {   
            //Test test = new Test(creatures.Dequeue(), testTime);
            //tests.Enqueue(test);
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
        public bool finished = false;
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

        public void Update()
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
