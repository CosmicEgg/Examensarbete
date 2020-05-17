using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeoInfo : MonoBehaviour
{
    private Vector3 refAxis;
    private Vector3 posRelParent;
    private Vector3 parentToChildDir;
    public int recursiveNumb = -1;
    public int jointType = -1;

    private void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int JointType
    {
        get { return jointType; }
        set { jointType = value; }
    }

    public Vector3 RefAxis
    {
        get { return refAxis; }
        set { refAxis = value; }
    }
    public Vector3 PosRelParent
    {
        get { return posRelParent; }
        set { posRelParent = value; }
    }

    public Vector3 ParentToChildVector
    {
        get { return parentToChildDir; }
        set { parentToChildDir = value; }
    }
}
