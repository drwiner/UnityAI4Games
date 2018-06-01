using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using CameraNamespace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNamespace {
    public class CamPlanStep : PlanStep, IPlanStep {

        // Initially used to reference prerequisite criteria.
        public CamSchema CamDetails = null;

        // Only assigned once it is grounded plan step.
        public GameObject CamObject = null;

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

            if (CamObject != null)
            {
                newstep.CamObject = GameObject.Instantiate(CamObject);
                newstep.CamObject.transform.parent = GameObject.Find("CamPlanSteps").transform;
            }
            if (CamDetails != null)
                newstep.CamDetails = CamDetails.Clone();
            if (TargetDetails != null)
                newstep.TargetDetails = TargetDetails.Clone();

            return newstep;
        }
    }
}
