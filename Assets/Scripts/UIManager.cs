using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text time;
    public Text currentGeneration;
    public Text maxPopulationSize;
    public Text populationTested;

    float runTime;

    // Start is called before the first frame update
    void Start()
    {
        runTime = 0;
        currentGeneration.text = "Generation: 1";
        maxPopulationSize.text = "Population Size: 100";
        populationTested.text = "Populations Tested: 0";
    }

    // Update is called once per frame
    void Update()
    {
        runTime += Time.deltaTime;

        time.text = "Time: " + (int)runTime;
    }

    public void SetCurrentGeneration(float value)
    {
        currentGeneration.text = "Generation: " + value;
    }
    public void SetMaxPopulationSize(float value)
    {
        maxPopulationSize.text = "Population Size: " + value;
    }
    public void SetPopulationsTested(float value)
    {
        populationTested.text = "Populations Tested: " + value;
    }
}
