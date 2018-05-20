using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CamTestRaycast : MonoBehaviour {

    public GameObject SpecificCam;
    public GameObject Target;

    public bool RunTest = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (RunTest)
        {
            RunTest = false;
            var answer = IsValidShot(SpecificCam.transform.position, Target);
            Debug.Log(string.Format("is valid shot: {0}", answer));
        }
	}

    public bool IsValidShot(Vector3 camPosition, GameObject target)
    {

        var targetPosition = target.transform.position;
        var camDist = Vector3.Distance(camPosition, targetPosition);
        var vectorDirection = (targetPosition - camPosition);
        
        // First, just check if we can hit the target at all.
        if (Physics.Raycast(camPosition, vectorDirection, camDist))
        {
            Debug.DrawRay(camPosition, vectorDirection.normalized * (camDist ), Color.cyan);
            var newGo = new GameObject();
            newGo.transform.position = camPosition + (targetPosition - camPosition);
            return false;
        }

        Debug.Log("basic yes");

        // Next, check if we can hit every vertex on bounding box collider
        var vertexArray = GetColliderVertexPositions(target);
        int unseen = 0;
        for (int i = 0; i < 8; i++)
        {
            var vectorBetweenPointAndPosition = vertexArray[i] - camPosition;
            if (Physics.Raycast(camPosition, vectorBetweenPointAndPosition, Vector3.Distance(camPosition, vertexArray[i])))
            {
                Debug.DrawRay(camPosition, vectorBetweenPointAndPosition.normalized * (Vector3.Distance(camPosition, vertexArray[i])), Color.yellow);
                unseen++;
                if (unseen >= 6)
                {
                    return false;
                }
            }
        }
        Debug.Log("unseen: " + unseen.ToString());
        // If so, return valid
        return true;

    }

    public static Vector3[] GetColliderVertexPositions(GameObject obj)
    {
        var vertices = new Vector3[8];
        var thisMatrix = obj.transform.localToWorldMatrix;
        var storedRotation = obj.transform.rotation;
        obj.transform.rotation = Quaternion.identity;

        var extents = obj.GetComponent<BoxCollider>().bounds.extents;
        vertices[0] = thisMatrix.MultiplyPoint3x4(extents);
        vertices[1] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, extents.y, extents.z));
        vertices[2] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, extents.y, -extents.z));
        vertices[3] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, extents.y, -extents.z));
        vertices[4] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, -extents.y, extents.z));
        vertices[5] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, -extents.y, extents.z));
        vertices[6] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, -extents.y, -extents.z));
        vertices[7] = thisMatrix.MultiplyPoint3x4(-extents);

        obj.transform.rotation = storedRotation;
        return vertices;
    }
}
