using System;
using UnityEngine;
 
[Serializable]
 public class Trans
 {
    [SerializeField]
    public Vector3 position;
    [SerializeField]
    public Quaternion rotation;
    [SerializeField]
    public Vector3 localScale;
 
     public Trans (Vector3 newPosition, Quaternion newRotation, Vector3 newLocalScale)
     {
         position = newPosition;
         rotation = newRotation;
         localScale = newLocalScale;
     }
 
     public Trans ()
     {
         position = Vector3.zero;
         rotation = Quaternion.identity;
         localScale = Vector3.one;
     }
 
     public Trans (Transform transform)
     {
         copyFrom (transform);
     }
 
     public void copyFrom (Transform transform)
     {
         position = transform.position;
         rotation = transform.rotation;
         localScale = transform.localScale;
     }
 
     public void copyTo (Transform transform)
     {
         transform.position = position;
         transform.rotation = rotation;
         transform.localScale = localScale;
     }
 
 }
