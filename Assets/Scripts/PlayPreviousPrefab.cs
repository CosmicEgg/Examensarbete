using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayPreviousPrefab : MonoBehaviour
{
    public GameObject creature;
    public Material material;

    List<ConfigurableJoint> muscles;
    Dictionary<ConfigurableJoint, Vector3> allMuscles;
    bool loadCreature;
    float time,cycle,cycleSpeed;
    JointDrive joint;
    int count;

    // Start is called before the first frame update
    void Start()
    {
        muscles = new List<ConfigurableJoint>();
        allMuscles = new Dictionary<ConfigurableJoint, Vector3>();
        loadCreature = true;
        cycleSpeed = 3f;
        joint = new JointDrive();
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        cycle = Mathf.Sin(time * cycleSpeed);

        if (loadCreature)
        {
            GameObject newCreature  = Instantiate(creature , new Vector3(0, 5, 0), Quaternion.identity);

            foreach (Transform child in newCreature.transform)
            {
                child.GetComponent<Renderer>().material = material;

                for (int i = 0; i < child.gameObject.GetComponents<ConfigurableJoint>().Length; i++)
                {
                    ConfigurableJoint cj = child.gameObject.GetComponents<ConfigurableJoint>()[i];
                    muscles.Add(cj);
                }


                if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.interpolation = RigidbodyInterpolation.Extrapolate;
                }
            }

            foreach(ConfigurableJoint c in muscles)
            {
                Vector3 tagetPosition = new Vector3();
                tagetPosition = c.targetPosition;

                allMuscles.Add(c, tagetPosition);
            }

            loadCreature = false;
        }

        foreach(KeyValuePair<ConfigurableJoint, Vector3> m in allMuscles)
        {
            if(cycle > 0)
            {
                joint.positionSpring = 200f;
                joint.maximumForce = m.Key.xDrive.maximumForce;
                joint.positionDamper = 0;

                m.Key.xDrive = joint;
                m.Key.yDrive = joint;
                m.Key.zDrive = joint;

                m.Key.targetPosition = Vector3.zero;
            }
            else
            {
                joint.positionSpring = 1f;
                joint.maximumForce = m.Key.xDrive.maximumForce;
                joint.positionDamper = 0;

                m.Key.xDrive = joint;
                m.Key.yDrive = joint;
                m.Key.zDrive = joint;

                m.Key.targetPosition = new Vector3(10, 10, 10); 
            }
        }
    }
}
