using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;

public class GenericTest : MonoBehaviour
{

    [System.Serializable]
    public class SaveClass
    {
        private Vector3 oneVector = Vector3.zero;

        public Vector3 OneVector
        {
            get { return oneVector; }
            set { oneVector = value; }
        }
    }

    void Start()
    {
        BinaryFormatter bf = new BinaryFormatter();
        SurrogateSelector surrogateSelector = new SurrogateSelector();
        Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();

        surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
        bf.SurrogateSelector = surrogateSelector;

        FileStream file = File.Create(Path.Combine(Application.dataPath, "SerializeTest"));
        SaveClass saveClass = new SaveClass();
        saveClass.OneVector = new Vector3(1f, 2f, 3f);

        bf.Serialize(file, saveClass);
        file.Close();

        file = File.Open(Path.Combine(Application.dataPath, "SerializeTest"), FileMode.Open);
        SaveClass loadClass = (SaveClass)bf.Deserialize(file);
        Debug.Log(loadClass.OneVector);
    }
}