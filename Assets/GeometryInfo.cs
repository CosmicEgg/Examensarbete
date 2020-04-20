using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeometryInfo : MonoBehaviour
{
    [SerializeField]
    public int ID, parentID = -1;
    public GameObject parent;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetID(int ID)
    {
        this.ID = ID;
    }

    public void SetParentID(int ID)
    {
        this.parentID = ID;
    }

    private void OnCollisionStay(Collision collision)
    {
        //if (collision.gameObject == parent)
        //{

        //}.
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
