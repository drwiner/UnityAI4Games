using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNamespace
{
    [Serializable]
    public class CompositeSchedule : Composite, IComposite
    {
        public List<Tuple<IPlanStep, IPlanStep>> Cntgs;

        // used to create root 
        public CompositeSchedule(IOperator op) : base(op)
        {
            Cntgs = new List<Tuple<IPlanStep, IPlanStep>>();
        }

        public CompositeSchedule(Composite comp) : base(comp, comp.InitialStep, comp.GoalStep, comp.SubSteps, comp.SubOrderings, comp.SubLinks)
        {
            Cntgs = new List<Tuple<IPlanStep, IPlanStep>>();
        }

        public CompositeSchedule(Composite comp, List<Tuple<IPlanStep, IPlanStep>> cntgs) : base(comp, comp.InitialStep, comp.GoalStep, comp.SubSteps, comp.SubOrderings, comp.SubLinks)
        {
            Cntgs = cntgs;
        }

        /// <summary>
        /// The compositeschedule has terms, preconditions, and effects. 
        /// All preconditions and effects are expected to be ground because they are created based on the ground decomposition
        /// Thus, unlike the parent class, there is no need to propagate bindings to terms, preconditions, and effects.
        /// </summary>
        /// <param name="td"></param>
        public void ApplyDecomposition(TimelineDecomposition td)
        {
            subSteps = td.SubSteps;
            subOrderings = td.SubOrderings;
            subLinks = td.SubLinks;

            foreach (var substep in subSteps)
            {
                foreach (var term in substep.Terms)
                {
                    if (!td.Terms.Contains(term))
                    {
                        //var termAsPredicate = term as Predicate;
                        //if (termAsPredicate != null)
                        //{

                        //}
                        Terms.Add(term);
                    }
                }
            }

            Cntgs = td.fabCntgs;

            // The way things are done round here is just to group in discourse stuff with fabula stuff. We have two plans... but they can go in one plan.
            foreach (var camplanstep in td.discourseSubSteps)
            {
                SubSteps.Add(camplanstep as IPlanStep);
            }
            foreach (var dordering in td.discOrderings)
            {
                SubOrderings.Add(new Tuple<IPlanStep, IPlanStep>(dordering.First, dordering.Second));
            }
            foreach (var discCntg in td.discCntgs)
            {
                Cntgs.Add(new Tuple<IPlanStep, IPlanStep>(discCntg.First, discCntg.Second));
            }
            foreach (var dlink in td.discLinks)
            {
                SubLinks.Add(new CausalLink<IPlanStep>(dlink.Predicate, dlink.Head, dlink.Tail));
            }

        }

        public new System.Object Clone()
        {
            var CompositeBase = base.Clone() as Composite;
            var newCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
            foreach (var cntg in Cntgs)
            {
                newCntgs.Add(cntg);
            }
            var theClone = new CompositeSchedule(CompositeBase, newCntgs);
            return theClone;
        }
    }
}