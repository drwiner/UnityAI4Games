using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultAttributes : MonoBehaviour {

    public Transform defaultParent;

    void Awake()
    {
        defaultParent = this.transform.parent;
    }    

    
}
