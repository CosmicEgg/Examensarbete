using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using UnityEngine;

public class SerializationTest
{
    string singleCreatureSavePath = Application.persistentDataPath + "/save.dat";
    string populationSavePath = Application.persistentDataPath + "/populationSave.dat";

    public void SerializeTree(Node root)
    {
        TreeContainer toSerialize = new TreeContainer();
        toSerialize.SetRoot(root);
        toSerialize.BeforeSerialize();

        BinaryFormatter bf = new BinaryFormatter();

        using (FileStream fs = new FileStream(singleCreatureSavePath, FileMode.Create))
        {
            bf.Serialize(fs, toSerialize);
        }
    }

    public void SerializePopulation(List<Node> roots)
    {
        List<TreeContainer> treeContainers = new List<TreeContainer>();

        for (int i = 0; i < roots.Count; i++)
        {
            TreeContainer toSerialize = new TreeContainer();
            toSerialize.SetRoot(roots[i]);
            toSerialize.BeforeSerialize();
            treeContainers.Add(toSerialize);
        }

        BinaryFormatter bf = new BinaryFormatter();

        using (FileStream fs = new FileStream(populationSavePath, FileMode.Create))
        {
            bf.Serialize(fs, treeContainers);
        }    
    }

    public List<Node> DeserializePopulation()
    {
        List<Node> roots = new List<Node>();

        if (File.Exists(populationSavePath))
        {
            using (Stream stream = File.Open(populationSavePath, FileMode.Open))
            {
                var bformatter = new BinaryFormatter();

                List<TreeContainer> treeContainers = (List<TreeContainer>)bformatter.Deserialize(stream);

                foreach (TreeContainer t in treeContainers)
                {
                    t.AfterDeserialize();
                    roots.Add(t.GetRoot());
                }
            }
        }

        return roots;
    }

    public Node DeserializeTree()
    {
        if (File.Exists(singleCreatureSavePath))
        {
            using (Stream stream = File.Open(singleCreatureSavePath, FileMode.Open))
            {
                var bformatter = new BinaryFormatter();

                TreeContainer graph = (TreeContainer)bformatter.Deserialize(stream);
                graph.AfterDeserialize();
                return graph.GetRoot();
            }
        }

        return null;
    }
}
