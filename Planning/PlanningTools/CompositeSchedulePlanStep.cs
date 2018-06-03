using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PlanningNamespace {

    // An instantiation of CompositeSchedule
	[Serializable]
    public class CompositeSchedulePlanStep : CompositePlanStep, ICompositePlanStep
    {
        // has cntgs in addition to sub orderings
        public List<Tuple<IPlanStep, IPlanStep>> Cntgs;

        public CompositeSchedulePlanStep(CompositeSchedule comp) : base(comp as Composite)
        {
            Cntgs = comp.Cntgs;
        }

        public CompositeSchedulePlanStep(IPlanStep ps) : base(ps)
        {
            var ca = ps.Action as CompositeSchedule;
            Cntgs = ca.Cntgs;
        }


        public new System.Object Clone()
        {
            var cps = base.Clone() as CompositePlanStep;
            var newCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
            foreach (var cntg in Cntgs)
            {
                // due dilligence
                newCntgs.Add(new Tuple<IPlanStep, IPlanStep>(cntg.First.Clone() as IPlanStep, cntg.Second.Clone() as IPlanStep));
            }
            return new CompositeSchedulePlanStep(cps)
            {
                Cntgs = newCntgs
            };
        }

    }
}