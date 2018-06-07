using BoltFreezer.DecompTools;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using System;
using Cinematography;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CameraNamespace;
using UnityEngine;
using SteeringNamespace;
using TimelineClipsNamespace;

namespace PlanningNamespace
{
    [Serializable]
    public class TimelineDecomposition : Decomposition
    {

        // Fabula
        public List<Tuple<IPlanStep, IPlanStep>> fabCntgs;
        // maps display name to planstep variable, then updated to map to real substep
        public Dictionary<string, PlanStep> fabulaActionNameMap;
        public List<Tuple<string, Tuple<PlanStep, PlanStep>>> fabConstraints;
        
        // Discourse
        public List<CamPlanStep> discourseSubSteps;
        public List<Tuple<CamPlanStep, CamPlanStep>> discCntgs;
        public List<Tuple<CamPlanStep, CamPlanStep>> discOrderings;
        public List<CausalLink<CamPlanStep>> discLinks;
        public List<Tuple<string, Tuple<CamPlanStep, CamPlanStep>>> discConstraints;

        public TimelineDecomposition() : base()
        {
            fabCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
            discCntgs = new List<Tuple<CamPlanStep, CamPlanStep>>();
            discourseSubSteps = new List<CamPlanStep>();
        }

        public TimelineDecomposition(string name, List<ITerm> terms, IOperator init, IOperator dummy, List<IPredicate> Preconditions, List<IPredicate> Effects, int ID)
            : base(name, terms, init, dummy, Preconditions, Effects, ID)
        {

        }

        public TimelineDecomposition(IOperator core, List<IPredicate> literals, List<IPlanStep> substeps, List<Tuple<IPlanStep, IPlanStep>> suborderings, List<CausalLink<IPlanStep>> sublinks)
            : base(core, literals, substeps, suborderings, sublinks)
        {

        }

        public TimelineDecomposition(IOperator core, List<IPredicate> literals,
            List<Tuple<IPlanStep, IPlanStep>> fcntgs, List<Tuple<CamPlanStep, CamPlanStep>> dcntgs,
            List<Tuple<string, Tuple<PlanStep, PlanStep>>> fconstraints, List<Tuple<string, Tuple<CamPlanStep, CamPlanStep>>> dconstraints,
            List<IPlanStep> substeps, List<CamPlanStep> camSteps,
            List<Tuple<IPlanStep, IPlanStep>> suborderings, List<Tuple<CamPlanStep, CamPlanStep>> dOrderings,
            List<CausalLink<IPlanStep>> sublinks, List<CausalLink<CamPlanStep>> dLinks,
            Dictionary<string, PlanStep> fabulaStepVariableNameDictionary)
            : base(core, literals, substeps, suborderings, sublinks)
        {
            fabCntgs = fcntgs;
            discCntgs = dcntgs;
            fabConstraints = fconstraints;
            discourseSubSteps = camSteps;
            discConstraints = dconstraints;
            discOrderings = dOrderings;
            discLinks = dLinks;
            fabulaActionNameMap = fabulaStepVariableNameDictionary;
        }

        public TimelineDecomposition(Decomposition decomp,
            List<Tuple<IPlanStep, IPlanStep>> fcntgs, List<Tuple<CamPlanStep, CamPlanStep>> dcntgs,
            List<CamPlanStep> discourseSteps,
            List<Tuple<string, Tuple<PlanStep, PlanStep>>> sconstraints, List<Tuple<string, Tuple<CamPlanStep, CamPlanStep>>> dconstraints,
            List<Tuple<CamPlanStep, CamPlanStep>> dOrderings, List<CausalLink<CamPlanStep>> dLinks,
            Dictionary<string, PlanStep> fabulaStepVariableNameDictionary)
            : base(new Operator(decomp.Name, decomp.Terms, new Hashtable(), decomp.Preconditions, decomp.Effects, decomp.ID) as IOperator, decomp.Literals, decomp.SubSteps, decomp.SubOrderings, decomp.SubLinks)
        {
            fabCntgs = fcntgs;
            discCntgs = dcntgs;
            fabConstraints = sconstraints;
            discourseSubSteps = discourseSteps;
            discConstraints = dconstraints;
            discOrderings = dOrderings;
            discLinks = dLinks;
            fabulaActionNameMap = fabulaStepVariableNameDictionary;
        }

        public void UpdateActionVarMap(Dictionary<int, IPlanStep> fabulaSubStepDict)
        {
            Dictionary<string, PlanStep> fabVarMap = new Dictionary<string, PlanStep>();
            foreach (var keyvalue in fabulaActionNameMap)
            {
                var newSubStep = fabulaSubStepDict[keyvalue.Value.ID];
                fabVarMap[keyvalue.Key] = newSubStep as PlanStep;
            }
            fabulaActionNameMap = fabVarMap;
        }

        // difference between this and overrided method is that this packages list of decompositions with a substep dictionary int -> replaced plan step
        public static List<Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>>> FilterDecompCandidates(TimelineDecomposition decomp)
        {
            // find and replace sub-steps 
            var comboList = new List<List<IOperator>>();
            var ID_List = new List<int>();
            foreach (var substep in decomp.SubSteps)
            {
                ID_List.Add(substep.ID);
                // each substep has ground terms that are already consistent. Composite IS-A Operator
                var cndts = ConsistentSteps(substep.Action as Operator);

                // If there's no cndts for this substep, then abandon this decomp.
                if (cndts.Count == 0)
                    return new List<Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>>>();

                comboList.Add(cndts);
            }

            // update to this method is to track, for each decomposition, which number substep goes to which grounded plan step
            List<Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>>> decompMap = new List<Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>>>();

            foreach (var combination in EnumerableExtension.GenerateCombinations(comboList))
            {
                var decompClone = decomp.Clone() as TimelineDecomposition;
                var newSubsteps = new List<IPlanStep>();
                var substepDict = new Dictionary<int, IPlanStep>();

                var order = 0;
                foreach (var item in combination)
                {
                    var originalID = ID_List[order++];
                    var newPlanStep = new PlanStep(item);
                    substepDict[originalID] = newPlanStep;
                    newSubsteps.Add(newPlanStep);
                }

                var newSuborderings = new List<Tuple<IPlanStep, IPlanStep>>();
                foreach (var subordering in decomp.SubOrderings)
                {
                    var first = substepDict[subordering.First.ID];
                    var second = substepDict[subordering.Second.ID];
                    newSuborderings.Add(new Tuple<IPlanStep, IPlanStep>(first, second));
                }

                var linkWorlds = new List<List<CausalLink<IPlanStep>>>();
                linkWorlds.Add(new List<CausalLink<IPlanStep>>());
                var newSublinks = new List<CausalLink<IPlanStep>>();
                foreach (var sublink in decomp.SubLinks)
                {
                    var head = substepDict[sublink.Head.ID];
                    var tail = substepDict[sublink.Tail.ID];
                    List<IPredicate> cndts = new List<IPredicate>();
                    //var cndts = head.Effects.Where(eff => eff.IsConsistent(sublink.Predicate) && tail.Preconditions.Any(pre => pre.Equals(eff)));
                    foreach(var eff in head.Effects)
                    {
                        foreach(var pre in tail.Preconditions)
                        {
                            if (eff.Equals(pre))
                            {
                                cndts.Add(eff);
                                //Debug.Log("here");
                            }
                        }
                    }

                    if (cndts.Count() == 0)
                    {
                        // forfeit this entire subplan
                        linkWorlds = new List<List<CausalLink<IPlanStep>>>();
                        continue;
                    }
                    if (cndts.Count() == 1)
                    {
                        var cndt = cndts.First();
                        var dependency = cndt.Clone() as Predicate;
                        var newLink = new CausalLink<IPlanStep>(dependency, head, tail);
                        newLink.Tail.Fulfill(cndt);
                        foreach (var linkworld in linkWorlds)
                        {
                            linkworld.Add(newLink);
                        }
                    }
                    else
                    {
                        foreach (var cndt in cndts)
                        {
                            var dependency = cndt.Clone() as Predicate;

                            var newLink = new CausalLink<IPlanStep>(dependency, head, tail);
                            newLink.Tail.Fulfill(cndt);

                            var clonedLinks = EnumerableExtension.CloneList(newSublinks);

                            linkWorlds.Add(clonedLinks);
                            foreach (var linkworld in linkWorlds)
                            {
                                linkworld.Add(newLink);
                            }
                        }
                    }
                }

                foreach (var linkworld in linkWorlds)
                {
                    var newDecomp = decomp.Clone() as TimelineDecomposition;
                    newDecomp.SubSteps = newSubsteps;
                    newDecomp.SubOrderings = newSuborderings;
                    newDecomp.SubLinks = linkworld;

                    var outputTuple = new Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>>(newDecomp, substepDict);
                    decompMap.Add(outputTuple);
                }

            }
            return decompMap;

        }


        public new System.Object Clone()
        {
            var baseDecomp = base.Clone() as Decomposition;
            
            var newSubsteps = new List<CamPlanStep>();
            foreach (var substep in discourseSubSteps)
            {
                var newsubstep = substep.Clone() as CamPlanStep;
                newsubstep.Action = substep.Action.Clone() as Operator;
                newSubsteps.Add(newsubstep);
            }

            return new TimelineDecomposition(baseDecomp, fabCntgs, discCntgs, newSubsteps, fabConstraints, discConstraints, discOrderings, discLinks, fabulaActionNameMap);
        }
    }
}