using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DefaultAttributes : MonoBehaviour {

    public Transform defaultParent;
    public Vector3 defaultPosition;
    public Vector3 defaultRotation;

    public bool reset = false;

    public bool returnToOriginalState = false;

    void Awake()
    {
        if (reset)
        {
            reset = false;
            ResetAttributes();
        }

        
    }

    public void ReturnToDefault()
    {
        transform.parent = defaultParent;
        transform.position = defaultPosition;
        transform.eulerAngles = defaultRotation;
    }

    private void ResetAttributes()
    {
        defaultParent = this.transform.parent;
        defaultPosition = this.transform.position;
        defaultRotation = this.transform.eulerAngles;
    }

    void Update()
    {
        if (reset)
        {
            reset = false;
            ResetAttributes();
        }

        if (returnToOriginalState)
        {
            returnToOriginalState = false;
            ReturnToDefault();
        }
    }

}
