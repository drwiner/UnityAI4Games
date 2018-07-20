using BoltFreezer.Camera;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraNamespace
{

    [ExecuteInEditMode]
    public class CamGrammar : MonoBehaviour
    {

        public CamGen camCollection;

        public List<CamTransitionSchema> camTransitionSchemas;

        public int numItemToDuplicate = 0;

        public bool initialize = false;
        public bool duplicate = false;
        public bool test = false;
        public bool findCandidates = false;
       

        // public List<CamTransition> camTransitions;

        // Use this for initialization
        void Start()
        {
            if (initialize)
            {
                initialize = false;
                Initialize();
            }
        }

        private void Initialize()
        {

            camTransitionSchemas = new List<CamTransitionSchema>();
            if (camCollection == null)
            {
                var camGenObject = GameObject.FindGameObjectWithTag("Cameras");
                camCollection = camGenObject.GetComponent<CamGen>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (initialize)
            {
                initialize = false;
                Initialize();
            }

            if (duplicate)
            {
                duplicate = false;
                var item = camTransitionSchemas[numItemToDuplicate];
                var newItem = item.Duplicate();
                camTransitionSchemas.Add(newItem);
            }

            if (test)
            {
                test = false;
                Debug.Log("not yet implemented");
            }
            //if (findCandidates)
            //{
            //    findCandidates = false;
            //    Debug.Log(string.Format("processing candidates from {0} gameobjects", camCollection.CameraList.Count));
            //    FindCandidates();
            //}

        }

        //public void FindCandidates()
        //{
            
        //    foreach (var cts in camTransitionSchemas)
        //    {
        //        // find all that are candidates
        //        var cndtsForShot1 = FindCandidatesForCamSchema(cts.Shot1);

        //        if (cndtsForShot1.Count == 0)
        //        {
        //            Debug.Log(string.Format("no candidates found for {0}", cts.Shot1));
        //            //continue;
        //        }

        //        var cndtsForShot2 = FindCandidatesForCamSchema(cts.Shot2);

        //        if (cndtsForShot1.Count == 0)
        //        {
        //            Debug.Log(string.Format("no candidates found for {0}", cts.Shot2));
        //            continue;
        //        }

        //        cts.camTransitions = new List<CamTransition>();

        //        foreach (var cndtShot1 in cndtsForShot1)
        //        {
        //            foreach (var cndtShot2 in cndtsForShot2)
        //            {
        //                bool passesConstraints = true;
        //                // Check for pair constraints
        //                foreach (var constraint in cts.Constraints)
        //                {
        //                    if (!CheckConstraint(cndtShot1.GetComponent<CamAttributesStruct>(), cndtShot2.GetComponent<CamAttributesStruct>(), constraint))
        //                    {
        //                        passesConstraints = false;
        //                        break;
        //                    }
        //                }

        //                if (passesConstraints)
        //                    cts.camTransitions.Add(new CamTransition(cndtShot1, cndtShot2));
        //            }
        //        }

        //    }
        //    Debug.Log("finished processing candidates");

        //}

        public bool CheckConstraint(CamAttributesStruct shot1, CamAttributesStruct shot2, string constraint)
        {
            var constraintParts = constraint.Split(' ');
            var constraintType = constraintParts[0];
            if (constraintType.Equals("="))
            {
                var dimension = constraintParts[1];
                if (dimension.Equals("scale"))
                {
                    if (shot1.scale != shot2.scale){
                        return false;
                    }
                }

                if (dimension.Equals("location"))
                {
                    if (shot1.targetLocation != shot2.targetLocation)
                    {
                        return false;
                    }
                }

                if (dimension.Equals("hangle"))
                {
                    if (shot1.hangle != shot2.hangle)
                    {
                        return false;
                    }
                }
            }

            if (constraintType.Equals("not"))
            {
                var constraintName = constraintParts[1];
                if (constraintName.Equals("="))
                {
                    var dimension = constraintParts[0];
                    if (dimension.Equals("scale"))
                    {
                        if (shot1.scale == shot2.scale)
                        {
                            return false;
                        }
                    }

                    if (dimension.Equals("hangle"))
                    {
                        if (shot1.hangle == shot2.hangle)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        //public List<GameObject> FindCandidatesForCamSchema(CamSchema cschema)
        //{
        //    List<GameObject> cndts = new List<GameObject>();

        //    foreach (var cam in camCollection.CameraList)
        //    {
        //        if (cschema.IsConsistent(cam.GetComponent<CamAttributesStruct>().AsSchema()))
        //        {
        //            cndts.Add(cam);
        //        }
        //    }

        //    return cndts;
        //}
    }

    [Serializable]
    public class CamTransitionSchema
    {
        [SerializeField]
        public CamSchema Shot1;

        [SerializeField]
        public CamSchema Shot2;

        [SerializeField]
        public List<string> Constraints;

        [SerializeField]
        public List<CamTransition> camTransitions;

        public CamTransitionSchema(CamSchema s1, CamSchema s2)
        {
            Shot1 = s1;
            Shot2 = s2;
        }

        public CamTransitionSchema Duplicate()
        {
            return new CamTransitionSchema(Shot1.Duplicate(), Shot2.Duplicate());
        }
    }

    [Serializable]
    public class CamTransition
    {
        [SerializeField]
        public GameObject Shot1;
        [SerializeField]
        public GameObject Shot2;

        public CamTransition(GameObject shot1, GameObject shot2)
        {
            Shot1 = shot1;
            Shot2 = shot2;
        }

    }
}