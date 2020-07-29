using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

public class SaveLoadData
{ 
    // Start is called before the first frame update

    public void SaveCreature(Creature creature,int test, int generation)
    {
         GameObject prefab = PrefabUtility.SaveAsPrefabAsset(creature.handle, "Assets/Prefabs/Test " + test + "/Generation " + generation + "/" + creature.fitness + ".prefab");       
    }

    public void NewSaveFolder(int test , int generation)
    {
        string guid = AssetDatabase.CreateFolder("Assets/Prefabs/Test " + test, "Generation " + generation);
        string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);
    }
    public void NewTestFolder(int test)
    {
        string guid = AssetDatabase.CreateFolder("Assets/Prefabs", "Test " + test);
        string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);
    }

}
