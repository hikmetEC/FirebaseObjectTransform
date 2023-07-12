using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase;
using Firebase.Extensions;
using System;

public class Manager : MonoBehaviour
{
    public bool isFirebaseReady { get; set; } //firebase flag


    
    public GameObject[] objects;  // object prefabs
    private GameObject[] instanceObjects; //runtime instances of objects

    [Serializable]
    public class ObjectTransform //Class for JSON conversion of transform values
    {

        public float xPos;
        public float yPos;
        public float zPos;

        public float xRot;
        public float yRot;
        public float zRot;

        public float xScale;
        public float yScale;
        public float zScale;
    }
    

    void Start()
    {
        instanceObjects = objects; //setting instances as prefabs at the start. This makes them non-empty(to avoid bugs)
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => { //Firebase initialization
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                isFirebaseReady = true;
                Debug.Log("Firebase Ready!");
                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                isFirebaseReady = false;
                Debug.LogError(String.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    public void SaveAll() //This code saves all objects transform values
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (isFirebaseReady)
            {
                // operations for to-Json conversion
                ObjectTransform objectTransform = new ObjectTransform();
                
                objectTransform.xPos = instanceObjects[i].transform.position.x;
                objectTransform.yPos = instanceObjects[i].transform.position.y;
                objectTransform.zPos = instanceObjects[i].transform.position.z;

                objectTransform.xRot = instanceObjects[i].transform.rotation.x;
                objectTransform.yRot = instanceObjects[i].transform.rotation.y;
                objectTransform.zRot = instanceObjects[i].transform.rotation.z;

                objectTransform.xScale = instanceObjects[i].transform.localScale.x;
                objectTransform.yScale = instanceObjects[i].transform.localScale.y;
                objectTransform.zScale = instanceObjects[i].transform.localScale.z;
                string json = JsonUtility.ToJson(objectTransform);
                // operations for to-Json conversion

                //Sending the information to the server
                FirebaseDatabase.DefaultInstance.GetReference(instanceObjects[i].tag).SetRawJsonValueAsync(json);
                Debug.Log("Position Saved!");
            }
        }
    }

    public void LoadAll() //this code deletes all instances and updates them according to the server data
    {
        for (int i = 0; i < 3; i++)
        {
            if (instanceObjects[i].activeInHierarchy) Destroy(instanceObjects[i]);
            SpawnObject(i);
        }
    }

    public void SpawnObject(int index) // this code spawns the object stated by it's index
    {
        FirebaseDatabase.DefaultInstance.GetReference(objects[index].tag).GetValueAsync().ContinueWithOnMainThread(task =>
        { //got the object by it's tag
            if (task.IsFaulted) // if operation fails just create a default transform object
            {
                Debug.LogError("Couldn't get anything. Maybe there isn't a cube created!");
                instanceObjects[index] = Instantiate(objects[index], objects[index].transform.position, objects[index].transform.rotation);
                instanceObjects[index].name = objects[index].name;
            }
            else if (task.IsCompleted) // if got data 
            {
                DataSnapshot snapshot = task.Result;

                string data = snapshot.GetRawJsonValue();
                if (data == "" || data == null) // if there is no transform data intantiate a default transform object
                {
                    instanceObjects[index] = Instantiate(objects[index], objects[index].transform.position, objects[index].transform.rotation);
                    instanceObjects[index].name = objects[index].name;
                    Debug.Log("No transform info!");
                    return;
                }
                //else create the object accoring to the server
                Debug.Log(data);

                //From-Json conversion operations
                ObjectTransform cubeTransformReceived = new ObjectTransform();
                cubeTransformReceived = JsonUtility.FromJson<ObjectTransform>(data);
                Vector3 cubePos = new Vector3(cubeTransformReceived.xPos, cubeTransformReceived.yPos, cubeTransformReceived.zPos);
                Quaternion cubeRot = new Quaternion(cubeTransformReceived.xRot, cubeTransformReceived.yRot, cubeTransformReceived.zRot, 0);
                Vector3 cubeScale = new Vector3(cubeTransformReceived.xScale, cubeTransformReceived.yScale, cubeTransformReceived.zScale);
                //From-Json conversion operations
                instanceObjects[index] = Instantiate(objects[index], cubePos, cubeRot);
                instanceObjects[index].name = objects[index].name;
                Debug.Log("Position Loaded!");
            }
        });
    }

}
