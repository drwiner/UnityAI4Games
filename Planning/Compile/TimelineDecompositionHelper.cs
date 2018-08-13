using BoltFreezer.Camera;
using BoltFreezer.Camera.CameraEnums;
using BoltFreezer.DecompTools;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Scheduling;
using BoltFreezer.Utilities;
using CameraNamespace;
using Cinematography;
using GraphNamespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CompilationNamespace
{
    public static class TimelineDecompositionHelper
    {
        public static Dictionary<Edge, Dictionary<double, List<CamSchema>>> NavCamDict;
        public static Dictionary<string, Vector3> LocationMap;
        public static TileGraph LocationGraph;
        public static List<CamSchema> CamOptions;

        /// <summary>
        /// The Decomposition is composed of a sub-plan with at least sub-step at height "height"
        /// </summary>
        /// <returns>A list of decompositions with ground terms and where each sub-step is ground. </returns>
        public static List<TimelineDecomposition> Compose(int height, TimelineDecomposition TD)
        {
            Debug.Log("Composing HTN for " + TD.Name + " at height: " + height.ToString());
            ///////////////////////////////////////
            // START BY ADDING BINDINGS TO TERMS //
            ///////////////////////////////////////
            var permList = new List<List<string>>();
            foreach (Term variable in TD.Terms)
            {
                permList.Add(GroundActionFactory.TypeDict[variable.Type] as List<string>);
            }

            var decompList = new List<TimelineDecomposition>();
            foreach (var combination in EnumerableExtension.GenerateCombinations(permList))
            {
                // Add bindings
                var decompClone = TD.Clone() as TimelineDecomposition;
                var termStringList = from term in decompClone.Terms select term.Variable;
                var constantStringList = combination;

                decompClone.AddBindings(termStringList.ToList(), constantStringList.ToList());

                /////////////////////////////////////////////////////////
                // PROPAGATE BINDINGS TO NONEQUALITY CONSTRAINTS
                /////////////////////////////////////////////////////////
                var newNonEqualities = new List<List<ITerm>>();
                if (TD.NonEqualities == null)
                {
                    TD.NonEqualities = new List<List<ITerm>>();
                }
                else
                {
                    foreach (var nonequals in TD.NonEqualities)
                    {
                        var newNonEquals = new List<ITerm>();
                        newNonEquals.Add(decompClone.Terms.First(dterm => dterm.Variable.Equals(nonequals[0].Variable)));
                        newNonEquals.Add(decompClone.Terms.First(dterm => dterm.Variable.Equals(nonequals[1].Variable)));
                        newNonEqualities.Add(newNonEquals);
                    }
                    decompClone.NonEqualities = newNonEqualities;

                    if (!decompClone.NonEqualTermsAreNonequal())
                    {
                        continue;
                    }
                }

                // zip to dict
                var varDict = EnumerableExtension.Zip(termStringList, constantStringList).ToDictionary(x => x.Key, x => x.Value);

                /////////////////////////////////////////////////////////
                // BINDINGS ARE ADDED. NEED TO APPLY BINDINGS TO SUBSTEPS
                /////////////////////////////////////////////////////////

                // Need to propagate bindings to sub-steps
                foreach (var substep in decompClone.SubSteps)
                {
                    var op = substep.Action as Operator;

                    foreach (var term in substep.Terms)
                    {
                        if (varDict.ContainsKey(term.Variable))
                        {
                            op.AddBinding(term.Variable, varDict[term.Variable]);
                        }
                    }

                    foreach (var precon in substep.Preconditions)
                    {
                        for (int i = 0; i < precon.Terms.Count; i++)// term in precon.Terms)
                        {
                            var term = precon.Terms[i];
                            if (!term.Bound)
                            {
                                // 
                                var decompTerm = decompClone.Terms.First(dterm => dterm.Variable.Equals(term.Variable));
                                op.AddBinding(term.Variable, decompTerm.Constant);
                                term.Constant = decompTerm.Constant;
                                //op.AddBinding(term.Variable, varDict[decompTerm.Variable]);
                            }
                        }
                    }
                    foreach (var eff in substep.Effects)
                    {
                        foreach (var term in eff.Terms)
                        {
                            if (!term.Bound)
                            {
                                var decompTerm = decompClone.Terms.First(dterm => dterm.Variable.Equals(term.Variable));
                              //  op.Terms.Add(term);
                                op.AddBinding(term.Variable, decompTerm.Constant);
                                term.Constant = decompTerm.Constant;

                              //  if (varDict.ContainsKey(decompTerm.Constant))
                              //  {
                              //      op.AddBinding(term.Variable, varDict[decompTerm.Variable]);
                             //   }
                            }
                        }
                    }
                }

                ////////////////////////////////////////////////////////////////
                // FILTER CANDIDATES FOR SUBSTEPS AND PASS BACK GROUNDED DECOMPS
                ////////////////////////////////////////////////////////////////

                // legacy for fabula subplan. returns a list of decomp packages, whose second member is a map from fabula substep IDs to substeps
                var fabulaGroundedDecompMap = TimelineDecomposition.FilterDecompCandidates(decompClone);

                // foreach decomposition including a mapping from fabula substep IDs to substeps
                for (int decompPackageIndex = 0; decompPackageIndex < fabulaGroundedDecompMap.Count; decompPackageIndex++)
               // foreach (Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>> decompPackage in fabulaGroundedDecompMap)
                {
                    var decompPackage = fabulaGroundedDecompMap[decompPackageIndex];

                    // Update references to ground actions and initial and final actions.
                    var iai = decompPackage.First.InitialActionSeg;
                    var initialactionRef = decompPackage.First.fabulaActionNameMap[iai.actionVarName];
                    decompPackage.First.InitialAction = decompPackage.Second[initialactionRef.ID];
                    iai.ActionID = decompPackage.First.InitialAction.ID;
                    iai.actiontypeID = decompPackage.First.InitialAction.Action.ID;

                    var fai = decompPackage.First.FinalActionSeg;
                    var finalactionRef = decompPackage.First.fabulaActionNameMap[fai.actionVarName];
                    decompPackage.First.FinalAction = decompPackage.Second[finalactionRef.ID];
                    fai.ActionID = decompPackage.First.FinalAction.ID;
                    fai.actiontypeID = decompPackage.First.FinalAction.Action.ID;

                    // If the height is h, then the decomp must have a composite sub-step with height h-1
                    if (height > 0)
                    {
                        // Extract candidate composite sub-steps and rewrite timeline decomp; note: mutation is okay
                        var groundDecompsWithCompositeSubSteps = CreateConnective(decompPackage);

                        // Ror each decomposition that has been modified to have at least 1 composite sub-step
                        foreach (var newGroundDecomp in groundDecompsWithCompositeSubSteps)
                        {
                            // Repackage
                            decompPackage = new Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>>(newGroundDecomp, decompPackage.Second);

                            // Find permutation of camera schedule actions to ground the decomposition fully
                            var newGroundDecomps = DiscourseDecompositionHelper.FilterTimelineDecompCandidates(TD, decompPackage, height);

                            foreach (var gdecomp in newGroundDecomps)
                            {
                                // would weed out gdecomps with insufficient height here.

                                // This function updates the mapping from action variable names to step-variables, and repoints mapping to grounded fabula substep. These names used by camera shots.
                                gdecomp.UpdateActionVarMap(decompPackage.Second);

                                // Add new ground decomposition to map.
                                decompList.Add(gdecomp);
                            }

                            if (newGroundDecomps.Count == 0)
                            {
                                // no discourse substeps, it's good to go as is.
                                decompList.Add(decompPackage.First);
                            }
                        }
                        
                    }
                    // Else, we can ignore the need to add composite sub-steps and proceed to find camera actions
                    else
                    {
                        RewriteFabSubSteps(decompPackage.First, decompPackage.Second);
                        // Find permutation of camera schedule actions to ground the decomposition fully
                        var newGroundDecomps = DiscourseDecompositionHelper.FilterTimelineDecompCandidates(TD, decompPackage, height);

                        foreach (var gdecomp in newGroundDecomps)
                        {
                            

                            // This function updates the mapping from action variable names to step-variables, and repoints mapping to grounded fabula substep. These names used by camera shots.
                            gdecomp.UpdateActionVarMap(decompPackage.Second);
 
                            // Add new ground decomposition to map.
                            decompList.Add(gdecomp);
                        }
                    }
                    
                }
            }

            return decompList;
        }

        public static List<TimelineDecomposition> CreateConnective(Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>> decompPackage)
        {
            ////////////////////////////////////////////////////////////////
            // Find candidates for adding composite sub-steps if this step has height > 0
            ////////////////////////////////////////////////////////////////

            var newConnectiveDecompSchedules = new List<TimelineDecomposition>();
            var decompClone = decompPackage.First;

            // Must be at least one discoures substep; if not throw error
            if (decompClone.discourseSubSteps.Count == 0)
            {
                throw new System.Exception("Cannot have decomposition schedule with a single schedule operator");
            }

            // This is the first segment // Needs to be updated so that Action ID reflects correct item.
            //var initialActionSeg = decompClone.discourseSubSteps[0].TargetDetails.ActionSegs[0];
            //var fabulaActionNameOfFirstSeg = initialActionSeg.actionVarName;
            //var variableAction = decompClone.fabulaActionNameMap[fabulaActionNameOfFirstSeg];
            //var groundAction = decompPackage.Second[variableAction.ID];
            //initialActionSeg.actiontypeID = groundAction.Action.ID;

            // Check if the shot continues
            if (decompClone.InitialCamAction.TargetDetails.ActionSegs.Count > 1)
            {
                // Then this step cannot be a connective. Return empty list.
                return newConnectiveDecompSchedules;
            }

            bool justOne = decompClone.discourseSubSteps.Count == 1;

            // then, need to find at least one composite
            foreach (var ga in GroundActionFactory.GroundActions)
            {
                if (ga.Height == 0)
                {
                    continue;
                }

                
                var comp = ga as CompositeSchedule;

                if (justOne && comp.NumberSegments == 1 && comp.FinalActionSeg.startPercent == decompClone.InitialActionSeg.startPercent && comp.FinalActionSeg.endPercent == decompClone.InitialActionSeg.endPercent)
                {
                    // they cannot both be just one segment of same size, or else nothing is gained. 
                    continue;
                }

                // check if final action interval is consistent
                if (comp.FinalActionSeg.CanReplace(decompClone.InitialActionSeg))
                {
                    var decompCloneClone = decompClone.Clone() as TimelineDecomposition;
                    AddCompositeSubStep(comp.FinalActionSeg, decompClone.InitialActionSeg, comp.Clone() as CompositeSchedule, decompCloneClone, decompPackage.Second);
                    newConnectiveDecompSchedules.Add(decompCloneClone);
                }
            }

            return newConnectiveDecompSchedules;
        }


        public static void RewriteFabSubStepWithComposite(IPlanStep origActionInReplacee, CompositeSchedulePlanStep substep, TimelineDecomposition decomp, Dictionary<int, IPlanStep> actionReferenceMap)
        {
            // for each ordering referencing actionInReplacee, replace with substep
            var newOrderings = new List<Tuple<IPlanStep, IPlanStep>>();
            foreach (var ord in decomp.SubOrderings)
            {
                if (ord.First.Equals(origActionInReplacee))
                {
                    var newOrd = new Tuple<IPlanStep, IPlanStep>(substep as IPlanStep, actionReferenceMap[ord.Second.ID]);
                    newOrderings.Add(newOrd);
                }
                else if (ord.Second.Equals(origActionInReplacee))
                {
                    throw new System.Exception("cannot be an ordering that precedes this...");
                    var newOrd = new Tuple<IPlanStep, IPlanStep>(ord.First, substep as IPlanStep);
                    newOrderings.Add(newOrd);
                }
                else
                {
                    newOrderings.Add(new Tuple<IPlanStep, IPlanStep>(actionReferenceMap[ord.First.ID], actionReferenceMap[ord.Second.ID]));
                }
            }
            decomp.SubOrderings = newOrderings;

            var newLinks = new List<CausalLink<IPlanStep>>();
            foreach (var link in decomp.SubLinks)
            {
                if (link.Head.Equals(origActionInReplacee))
                {
                    var newLink = new CausalLink<IPlanStep>(link.Predicate, substep as IPlanStep, actionReferenceMap[link.Tail.ID]);
                    newLinks.Add(newLink);
                }
                else if (link.Tail.Equals(origActionInReplacee))
                {
                    var newLink = new CausalLink<IPlanStep>(link.Predicate, actionReferenceMap[link.Head.ID], substep as IPlanStep);
                    newLinks.Add(newLink);
                }
                else
                {
                    newLinks.Add(new CausalLink<IPlanStep>(link.Predicate, actionReferenceMap[link.Head.ID], actionReferenceMap[link.Tail.ID]));
                }
            }

            decomp.SubLinks = newLinks;

            var newCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
            foreach (var cntg in decomp.fabCntgs)
            {
                if (cntg.First.Equals(origActionInReplacee))
                {
                    var newCntg = new Tuple<IPlanStep, IPlanStep>(substep as IPlanStep, actionReferenceMap[cntg.Second.ID]);
                    newCntgs.Add(newCntg);
                }
                else if (cntg.Second.Equals(origActionInReplacee))
                {
                    throw new System.Exception("cntg for action from first action interval is tail. how so?");
                }
                else
                {
                    newCntgs.Add(new Tuple<IPlanStep, IPlanStep>(actionReferenceMap[cntg.First.ID], actionReferenceMap[cntg.Second.ID]));
                }
            }

            decomp.fabCntgs = newCntgs;

        }

        public static void RewriteFabSubSteps(TimelineDecomposition decomp, Dictionary<int, IPlanStep> actionReferenceMap)
        {
            // didn't need this... is that always true?
            //var newOrderings = new List<Tuple<IPlanStep, IPlanStep>>();
            //foreach (var ord in decomp.SubOrderings)
            //{
            //    newOrderings.Add(new Tuple<IPlanStep, IPlanStep>(actionReferenceMap[ord.First.ID], actionReferenceMap[ord.Second.ID]));
            //}
            //decomp.SubOrderings = newOrderings;


            //var newLinks = new List<CausalLink<IPlanStep>>();
            //foreach (var link in decomp.SubLinks)
            //{
            //    newLinks.Add(new CausalLink<IPlanStep>(link.Predicate, actionReferenceMap[link.Head.ID], actionReferenceMap[link.Tail.ID]));
                
            //}
            //decomp.SubLinks = newLinks;
            var newCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
            foreach (var cntg in decomp.fabCntgs)
            {
                newCntgs.Add(new Tuple<IPlanStep, IPlanStep>(actionReferenceMap[cntg.First.ID], actionReferenceMap[cntg.Second.ID]));
                
            }
            decomp.fabCntgs = newCntgs;
        }

        public static void RewriteDiscSubStepWithComposite(CamPlanStep camstep, CompositeSchedulePlanStep substep, TimelineDecomposition decomp)
        {
            var newDiscOrderings = new List<Tuple<CamPlanStep, CamPlanStep>>();
            foreach (var ord in decomp.discOrderings)
            {
                if (ord.First.Equals(camstep))
                {
                    var newOrd = new Tuple<IPlanStep, IPlanStep>(substep as IPlanStep, ord.Second);
                    decomp.SubOrderings.Add(newOrd);
                }
                else if (ord.Second.Equals(camstep))
                {
                    throw new System.Exception("cannot be an ordering that precedes this...");
                }
                else
                {
                    newDiscOrderings.Add(ord);
                }

            }
            decomp.discOrderings = newDiscOrderings;

            var newDiscCntgs = new List<Tuple<CamPlanStep, CamPlanStep>>();
            foreach (var cntg in decomp.discCntgs)
            {
                if (cntg.First.Equals(camstep))
                {
                    var newCntg = new Tuple<IPlanStep, IPlanStep>(substep as IPlanStep, cntg.Second);
                    decomp.fabCntgs.Add(newCntg);
                }
                else if (cntg.Second.Equals(camstep))
                {
                    throw new System.Exception("cntg for action from first action interval is tail. how so?");
                }
                else
                {
                    newDiscCntgs.Add(cntg);
                }
            }
            decomp.discCntgs = newDiscCntgs;

        }


        public static void AddCompositeSubStep(ActionSeg replacer, ActionSeg replacee, CompositeSchedule substep, TimelineDecomposition decomp, Dictionary<int, IPlanStep> actionReferenceMap)
        {
            // this uses the replacer action seg to add substep as a substep and rewrites td
            // for now, assume that the replacer is a final action interval of substep and the replacee is an initial action segment of the decomp

            // First step is to remove the action seg replacee.
            var origActionInReplacee = decomp.fabulaActionNameMap[replacee.actionVarName];
            var actionInReplacee = actionReferenceMap[origActionInReplacee.ID];
            decomp.SubSteps.Remove(actionInReplacee);

            var newSubStep = new CompositeSchedulePlanStep(substep);

            RewriteFabSubStepWithComposite(origActionInReplacee, newSubStep, decomp, actionReferenceMap);

            //var shotInReplacee = decomp.discourseSubSteps[0];
            // we know this shot has only a single action interval.
            var camstep = decomp.discourseSubSteps[0];
            decomp.discourseSubSteps.RemoveAt(0);
            RewriteDiscSubStepWithComposite(camstep, newSubStep, decomp);

            // For each remaining discourse step, if actioninReplacee is referenced, now reference substep itself.
            foreach (var discourseSubStep in decomp.discourseSubSteps)
            {
                foreach(var actionSeg in discourseSubStep.TargetDetails.ActionSegs)
                {
                    //var actionReferenced = decomp.fabulaActionNameMap[replacee.actionVarName];
                    if (actionSeg.actionVarName.Equals(replacee.actionVarName))
                    {
                        actionSeg.ActionID = substep.ID;
                        actionSeg.actionVarName = substep.ToString();
                    }
                }
            }

            // update initial action materials to be these.
            decomp.InitialActionSeg = substep.InitialActionSeg;
            decomp.InitialAction = newSubStep as IPlanStep;
            decomp.InitialCamAction = substep.InitialCamAction;

            if (decomp.discourseSubSteps.Count == 0)
            {
                decomp.FinalActionSeg = substep.FinalActionSeg;
                decomp.FinalAction = decomp.InitialAction;
                decomp.FinalCamAction = substep.FinalCamAction;
            }
            else
            {
                // for now, final action materials are local, not connective point
                // if final action is consumed by sub-step, then final action is sub-step. This should be relatively unlikely.
                if (decomp.FinalAction.Equals(actionInReplacee))
                {
                    decomp.FinalAction = decomp.InitialAction;
                }
            }

            if (decomp.Terms.Count < 2)
            {

            }

            // Add substep as a sub-step of decomp. Now, decomp's sub-steps include composite actions, so technically they are \textit{regular} actions
            decomp.SubSteps.Add(decomp.InitialAction as IPlanStep);
        }

    }
}
