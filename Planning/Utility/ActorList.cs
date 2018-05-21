using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNamespace
{

    [ExecuteInEditMode]
    public class ActorList : MonoBehaviour
    {

        public bool recollectChildren = false;
        public bool returnChildrenToDefault = false;
        public List<GameObject> children;
        // Update is called once per frame
        void Update()
        {
            if (recollectChildren)
            {
                recollectChildren = false;
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i).gameObject;
                    children.Add(child);
                }
            }
            if (returnChildrenToDefault)
            {
                returnChildrenToDefault = false;
                RecollectChildren();
            }
        }

        public void RecollectChildren()
        {
            foreach (var child in children)
            {
                child.SetActive(true);
                child.GetComponent<DefaultAttributes>().ReturnToDefault();
            }
        }
    }
}