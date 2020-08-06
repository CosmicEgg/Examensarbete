using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TreeContainer
{
    [Serializable]
    public struct SerializableNode
    {
        public int numOfRecursiveChildren;
        public float randUniScale;
        public bool startOfRecurssion;
        //public Node referenceNode;
        public List<SerializableVector3> referenceNodeAxisList;
        public bool hasReferenceNode;
        public bool createdGeo;
        public bool created;
        public bool symmetry;
        public int occurence;
        public float scaleFactor;
        public int recursionJointType;
        public PrimitiveType primitiveType;
        public SerializableVector3 scale;
        public SerializableVector3 rotation;
        public SerializableVector3 parentToChildDir;
        //public List<GameObject> gameObjects;
        public List<SerializableVector3> axisList;
        public int numOfChildren;
        public bool stacked;
        public int id;
        public bool partOfGraph;
        //public Node parent;
        public SerializableVector3 color;
        public int seed;
        public int uniqueId;
        //public List<Edge> edges;
        public List<SerializableEdge> serializableEdges;

        public int parentIndex;
        public int childCount;
        public int indexOfFirstChild;
    }

    [Serializable]
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public SerializableVector3() { }
 
        public Vector3 Convert()
        {
            return new Vector3(x, y, z);
        }

        public SerializableVector3 ConvertToSerialized(Vector3 vector)
        {
            return new SerializableVector3() { x = vector.x, y = vector.y, z = vector.z };
        }
    }

    [Serializable]
    public struct SerializableEdge
    {
        public int numOfTravels;
        public int recursiveLimit;
        public bool traversed;
        public int recursiveNumb;

        public int fromUniqueId, toUniqueId;
    }

    [NonSerialized]
    Node root /*= new Node()*/;
    [SerializeField][HideInInspector]
    public List<SerializableNode> serializedNodes = new List<SerializableNode>();

    public Node GetRoot()
    {
        return root;
    }

    public void SetRoot(Node root)
    {
        this.root = root;
    }

    public void BeforeSerialize()
    {
       // Debug.Log("EnteringOnBeforeSeriaLize");
        if (serializedNodes == null)
        {
            serializedNodes = new List<SerializableNode>();
        }

        if (root == null)
        {
            Debug.Log("root is null");
            root = new Node();
        }

        serializedNodes.Clear();
        AddNodeToSerializedNodes(root);
       // Debug.Log("ExitingOnBeforeSeriaLize");

    }

    void AddNodeToSerializedNodes(Node n)
    {
        var serializedNode = new SerializableNode()
        {
            numOfRecursiveChildren = n.numOfRecursiveChildren,
            randUniScale = n.randUniScale,
            startOfRecurssion = n.startOfRecurssion,
            createdGeo = n.createdGeo,
            created = n.created,
            symmetry = n.symmetry,
            occurence = n.occurence,
            scaleFactor = n.scaleFactor,
            recursionJointType = n.recursionJointType,
            primitiveType = n.primitiveType,
            scale = new SerializableVector3(n.scale.x, n.scale.y, n.scale.z),
            rotation = new SerializableVector3(n.rotation.x, n.rotation.y, n.rotation.z),
            parentToChildDir = new SerializableVector3(n.parentToChildDir.x, n.parentToChildDir.y, n.parentToChildDir.z),
            numOfChildren = n.numOfChildren,
            stacked = n.stacked,
            id = n.id,
            partOfGraph = n.partOfGraph,
            seed = n.seed,
            uniqueId = n.uniqueId,
            //Datatyper som är oklara hur de serialize:as
            axisList = new List<SerializableVector3>(),
            referenceNodeAxisList = new List<SerializableVector3>(),
            color = new SerializableVector3(n.color.r, n.color.g, n.color.b),
            serializableEdges = new List<SerializableEdge>(),
            //gameObjects = n.gameObjects,

            //referenceNode = n.referenceNode,
            //edges = n.edges,

            parentIndex = serializedNodes.Count - 1,
            childCount = n.edges.Count,
            indexOfFirstChild = serializedNodes.Count + 1
        };

        if (n.referenceNode != null)
        {
            serializedNode.hasReferenceNode = true;

            foreach (Vector3 v in n.referenceNode.axisList)
            {
                serializedNode.referenceNodeAxisList.Add(new SerializableVector3(v.x, v.y, v.z));
            }
        }
        else
        {
            serializedNode.hasReferenceNode = false;
        }


        foreach (Vector3 v in n.axisList)
        {
            serializedNode.axisList.Add(new SerializableVector3(v.x, v.y, v.z));
        }

        foreach (Edge e in n.edges)
        {
            SerializableEdge serializableEdge = new SerializableEdge()
            {
                numOfTravels = e.numOfTravels,
                recursiveLimit = e.recursiveLimit,
                traversed = e.traversed,
                recursiveNumb = e.recursiveNumb,
                fromUniqueId = n.uniqueId,
                toUniqueId = e.to.uniqueId,
            };

            serializedNode.serializableEdges.Add(serializableEdge);
        }


        serializedNodes.Add(serializedNode);

        //Kanske borde ta hänsyn till rekursiva edges och ifall e.to är null
        foreach (Edge e in n.edges)
        {
            AddNodeToSerializedNodes(e.to);
        }
    }

    public void AfterDeserialize()
    {
        if (serializedNodes.Count > 0)
        {
            ReadNodeFromSerializedNodes(0, out root);
        }
        else
            root = new Node();
    }

    int ReadNodeFromSerializedNodes(int index, out Node node)
    {
        var serializedNode = serializedNodes[index];

        Node newNode = new Node()
        {
            numOfRecursiveChildren = serializedNode.numOfRecursiveChildren,
            randUniScale = serializedNode.randUniScale,
            startOfRecurssion = serializedNode.startOfRecurssion,
            createdGeo = serializedNode.createdGeo,
            created = serializedNode.created,
            symmetry = false/*serializedNode.symmetry*/,
            occurence = serializedNode.occurence,
            scaleFactor = serializedNode.scaleFactor,
            recursionJointType = serializedNode.recursionJointType,
            primitiveType = serializedNode.primitiveType,
            scale = serializedNode.scale.Convert(),
            rotation = serializedNode.rotation.Convert(),
            parentToChildDir = serializedNode.parentToChildDir.Convert(),
            numOfChildren = serializedNode.numOfChildren,
            stacked = serializedNode.stacked,
            id = serializedNode.id,
            partOfGraph = serializedNode.partOfGraph,

            axisList = new List<Vector3>(),
            color = new Color(serializedNode.color.x, serializedNode.color.y, serializedNode.color.z),
            seed = serializedNode.seed,
            uniqueId = serializedNode.uniqueId,
            parent = new Node(),
            gameObjects = new List<GameObject>(),
            edges = new List<Edge>(),
            children = new List<Node>()
        };

        if (serializedNode.hasReferenceNode)
        {
            newNode.referenceNode = new Node();

            foreach (SerializableVector3 v in serializedNode.referenceNodeAxisList)
            {
                newNode.referenceNode.axisList.Add(v.Convert());
            }
        }

        foreach (SerializableVector3 v in serializedNode.axisList)
        {
            newNode.axisList.Add(v.Convert());
        }

        if (serializedNode.parentIndex < 0)
        {
            newNode.parent = null;
        }

        // The tree needs to be read in depth-first, since that's how we wrote it out.
        for (int i = 0; i != serializedNode.childCount; i++)
        {
            Node childNode;
            index = ReadNodeFromSerializedNodes(++index, out childNode);
            childNode.parent = newNode;
            newNode.children.Add(childNode);
        }

        for (int i = 0; i != newNode.children.Count; i++)
        {
            SerializableEdge match = serializedNode.serializableEdges.Find(e => e.toUniqueId == newNode.children[i].uniqueId);
            Edge edge = new Edge(newNode, newNode.children[i], match.recursiveLimit, match.numOfTravels);
            edge.recursiveNumb = match.recursiveNumb;
            edge.traversed = match.traversed;
            newNode.edges.Add(edge);
        }

        node = newNode;
        return index;
    }
}

