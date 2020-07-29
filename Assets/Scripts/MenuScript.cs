using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuScript : MonoBehaviour
{
    private void Start()
    {
    }
    public void ChangeScene(int newOrReload)
    {
        PlayerPrefs.SetInt("newOrReload", newOrReload);
        Application.LoadLevel("SampleScene");
    }
}
