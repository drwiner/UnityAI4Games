using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DeleteChildren : MonoBehaviour {

    public bool delete = false;
	
	// Update is called once per frame
	void Update () {
		if (delete)
        {
            delete = false;
            var assembleThemFirst = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                assembleThemFirst.Add(transform.GetChild(i));   
            }
            foreach(var item in assembleThemFirst)
            {
                GameObject.DestroyImmediate(item.gameObject);
            }
        }
	}
}
