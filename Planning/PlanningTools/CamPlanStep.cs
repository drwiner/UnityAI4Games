using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using CameraNamespace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PlanningNamespace {

    [Serializable]
    public class CamPlanStep : PlanStep, IPlanStep {

        // Initially used to reference prerequisite criteria.
        public CamSchema CamDetails = null;

        // Only assigned once it is grounded plan step.
        public string CamObject = null;

        // A totally and temporally ordered list of action segments
        public CamTargetSchema TargetDetails = null;

        public CamPlanStep(IOperator groundAction) : base(groundAction)
        {
        }

        public CamPlanStep(PlanStep planStep) : base(planStep)
        {
        }

        //public CamPlanStep(IOperator groundAction, CamSchema camObject, List<CamTargetSchema> targets) : base(groundAction)
        //{
        //    // make a clone
        //    var camObjectClone = GameObject.Instantiate(camObject.gameObject);
        //    CamObject = camObjectClone.GetComponent<CamAttributesStruct>();


        //}

        public new CamPlanStep Clone()
        {
            //var baseClone = base.Clone() as PlanStep;
            var newstep = new CamPlanStep(base.Clone() as PlanStep);

            //if (CamObject != null && CamObject != "")
            //{
            //    var parent = GameObject.Find("Cameras");
            //    var camPlanStepsObject = GameObject.Find("CamPlanSteps").gameObject;
            //    var possibleCamObj = FindUnderParent(parent.gameObject, CamObject);
            //    if (possibleCamObj == null)
            //    {
            //        possibleCamObj = FindUnderParent(camPlanStepsObject, CamObject);
            //    }

            //    GameObject newGO = GameObject.Instantiate(possibleCamObj);
            //    newGO.transform.parent = camPlanStepsObject.transform;
            //    newstep.CamObject = newGO.name;
            //}

            if (CamDetails != null)
                newstep.CamDetails = CamDetails.Clone();
            if (TargetDetails != null)
                newstep.TargetDetails = TargetDetails.Clone();

            return newstep;
        }

        //public static GameObject FindUnderParent(GameObject parent, string name)
        //{
        //    for (int i = 0; i < parent.transform.childCount; i++)
        //    {
        //        var childObject = parent.transform.GetChild(i).gameObject;
        //        if (childObject.name.Equals(name))
        //        {
        //            return childObject;
        //        }
        //    }
        //    return null;
        //}
    }
}
