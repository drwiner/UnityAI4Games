﻿using BoltFreezer.DecompTools;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using CameraNamespace;
using Cinematography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PlanningNamespace
{
    public static class TimelineDecompositionHelper
    {
        public static Tuple<Dictionary<int, Orient>, Dictionary<int, string>> GetOrientsAndLocations(Decomposition decomp, Dictionary<string, Vector3> locationMap)
        {
            var orientDict = new Dictionary<int, Orient>();
            var locationDict = new Dictionary<int, string>();

            foreach (var substep in decomp.SubSteps)
            {
                var orientEnum = MapToNearestOrientation(substep as PlanStep, locationMap);
                orientDict[substep.ID] = orientEnum;

                string earliestTermHack = "";
                foreach (var term in substep.Terms)
                {
                    if (term.Type.Equals("Location"))
                    {
                        earliestTermHack = term.Constant;
                        break;
                    }
                }
                locationDict[substep.ID] = earliestTermHack;
            }
            return new Tuple<Dictionary<int, Orient>, Dictionary<int, string>>(orientDict, locationDict);
        }

        public static float OrientInFloat(Vector3 origin, Vector3 destination)
        {
            var direction = (origin - destination).normalized;
            return Mathf.Rad2Deg * Mathf.Atan2(-direction.z, direction.x);
        }

        // TODO: this is hacky because it's not actually based on Orientation Codes in cineamtography attributes
        public static Orient MapToNearestOrientation(PlanStep step, Dictionary<string, Vector3> locationMap)
        {
            // determine what the orientation is
            Orient orientEnum;
            var orientFloat = OrientInFloat(locationMap[step.Terms[step.Terms.Count - 1].Constant], locationMap[step.Terms[step.Terms.Count - 2].Constant]);
            while (orientFloat > 360)
            {
                orientFloat -= 360;
            }
            while (orientFloat < 0)
            {
                orientFloat += 360;
            }
            
            if ((orientFloat < 25 && orientFloat > -25) || (orientFloat > 335 && orientFloat < 385))
            {
                orientEnum = Orient.o0;
            }
            else if (orientFloat > 65 && orientFloat < 115)
            {
                orientEnum = Orient.o90;
            }
            else if (orientFloat > 155 && orientFloat < 205)
            {
                orientEnum = Orient.o180;
            }
            else if (orientFloat > 245 && orientFloat < 295)
            {
                orientEnum = Orient.o270;
            }
            else
            {
                Debug.Log(orientFloat);
                Debug.Log("not a good orientation calculation or else not correct positioning of locations");
                throw new System.Exception();
            }
            return orientEnum;
        }

        /// <summary>
        /// The Decomposition is composed of a sub-plan with at least sub-step at height "height"
        /// </summary>
        /// <returns>A list of decompositions with ground terms and where each sub-step is ground. </returns>
        public static List<TimelineDecomposition> Compose(int height, TimelineDecomposition TD, List<CamSchema> camOptions, Dictionary<string, Vector3> locationMap)
        {
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
                        op.AddBinding(term.Variable, varDict[term.Variable]);
                    }
                    foreach (var precon in substep.Preconditions)
                    {
                        foreach (var term in precon.Terms)
                        {
                            if (!term.Bound)
                            {
                                var decompTerm = decompClone.Terms.First(dterm => dterm.Variable.Equals(term.Variable));
                                op.Terms.Add(term);
                                op.AddBinding(term.Variable, decompTerm.Constant);
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
                                op.Terms.Add(term);
                                op.AddBinding(term.Variable, decompTerm.Constant);
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
                foreach (Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>> decompPackage in fabulaGroundedDecompMap)
                {
                    // get candidates and ground for camera steps
                    var newGroundDecomps = TimelineDecompositionHelper.FilterTimelineDecompCandidates(TD, decompPackage, height, camOptions, locationMap);

                    foreach (var gdecomp in newGroundDecomps)
                    {
                        // This function updates the mapping from action variable names to step-variables, and repoints mapping to grounded fabula substep. These names used by camera shots.
                        gdecomp.UpdateActionVarMap(decompPackage.Second);
                        // Add new ground decomposition to map.
                        decompList.Add(gdecomp);
                    }
                }
            }

            return decompList;
        }


        public static bool ValidateConstraints(Dictionary<int, IPlanStep> fabsubstepDict, Dictionary<int, CamPlanStep> discsubstepDict, 
            List<Tuple<string, Tuple<PlanStep, PlanStep>>> fabConstraints, List<Tuple<string, Tuple<CamPlanStep, CamPlanStep>>> discConstraints,
            Dictionary<int, Orient> orientDict)
        {
            bool fail = false;

            // check with constraints. If we had separate constraints for fabula and discourse here, then we can filter fabula steps by these constraints earlier.
            foreach (var constraint in fabConstraints)
            {
                if (constraint.First.Equals("orient reverse"))
                {
                    // reference new substeps
                    var first = fabsubstepDict[constraint.Second.First.ID];
                    var second = fabsubstepDict[constraint.Second.Second.ID];

                    var firstOrient = orientDict[first.ID];
                    var secondOrient = orientDict[second.ID];

                    if (firstOrient - secondOrient != 180 && secondOrient - firstOrient != 180)
                    {
                        fail = true;
                        break;
                    }
                }
                else if (constraint.First.Equals("orient ="))
                {

                }

            }
            if (fail)
            {
                return false;
            }
            fail = false;
            foreach (var constraint in discConstraints)
            {
                if (constraint.First.Equals("hangle reverse"))
                {
                    if (false)
                    {
                        fail = true;
                        break;
                    }
                }
            }
            if (fail)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Takes a fabula-valid decomposition and generates a list of discourse worlds (different camera shots to display the fabula)
        /// </summary>
        /// <param name="decompPackage"> Grounded fabula substeps and a mapping from step variables to those substeps </param>
        /// <param name="height"> Among CamOptions would include composite options... but may </param>
        /// <param name="camOptions"> From "Cameras" GameObject </param>
        /// <param name="locationMap"> Mapping location names to specific coordinates (used to determine orientation in space, and possibly for navigation estimates. </param>
        /// <returns> A list of TimelineDecomposition which all have fabula-valid and discourse-valid sub-plan</returns>
        public static List<TimelineDecomposition> FilterTimelineDecompCandidates(TimelineDecomposition TD, Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>> decompPackage, int height,
            List<CamSchema> camOptions, Dictionary<string, Vector3> locationMap)
        {
            var timelineDecompList = new List<TimelineDecomposition>();

            var decomp = decompPackage.First;
            var substepDict = decompPackage.Second;

            // mapping ID of substeps to orientations
            var orientLocationTuple = GetOrientsAndLocations(decomp, locationMap);
            var orientDict = orientLocationTuple.First;
            var locationDict = orientLocationTuple.Second;

            // Easier list to reference later
            //var discourseSubStepList = new List<CamPlanStep>();

            // create permutation for each combination of legal subcams
            var permList = GetPermutationCameraShots(TD.discourseSubSteps, substepDict, TD.fabulaActionNameMap, orientDict, locationDict, camOptions);
            //ist<CamPlanStep> discourseSubSteps, Dictionary<int, IPlanStep> fabsubstepDict, Dictionary<string, IPlanStep> fabulaActionNameMap,
            //Dictionary<int, int> orientDict, Dictionary< int, string> locationDict, List<CamAttributesStruct> camOptions

            // foreach combination, check if step constraints are true
            foreach (var combination in EnumerableExtension.GenerateCombinations(permList))
            {
                // clone decomp
                var decompClone = decomp.Clone() as TimelineDecomposition;
                var camSubStepDict = new Dictionary<int, CamPlanStep>();
                var newDiscourseSubSteps = new List<CamPlanStep>();
                for (int j = 0; j < combination.Count; j++)
                {
                    // a reference to the camera object candidate
                    //var camObj = combination[j].AttributesToSchema();

                    // a reference to the j'th discourse step
                    var camStep = TD.discourseSubSteps[j].Clone() as CamPlanStep;
                    camStep.CamDetails = combination[j].Clone() as CamSchema;
                    //camStep.CamObject = camObj.gameObject;

                    // a cloning of the cam plan step
                    //var newPlanStep = camStep.Clone();
                    //newPlanStep.CamObject = camObj.gameObject;

                    // storing a mapping from old cam plan step ID to new cam plan step
                    camSubStepDict[camStep.ID] = camStep;
                    newDiscourseSubSteps.Add(camStep);
                }

                var boolOutcome = ValidateConstraints(substepDict, camSubStepDict, TD.fabConstraints, TD.discConstraints, orientDict);
            //    public static bool ValidateConstraints(Dictionary<int, IPlanStep> fabsubstepDict, Dictionary<int, CamPlanStep> discsubstepDict,
            //List<Tuple<string, Tuple<IPlanStep, IPlanStep>>> fabConstraints, List<Tuple<string, Tuple<IPlanStep, IPlanStep>>> discConstraints,
            //Dictionary<int, int> orientDict)
                if (!boolOutcome)
                {
                    continue;
                }

                // these are done here, but they could/should have been performed during regular legacy decomposition filtering.
                var newFabCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
                foreach (var subCntg in TD.fabCntgs)
                {
                    var newcntg = new Tuple<IPlanStep, IPlanStep>(substepDict[subCntg.First.ID], substepDict[subCntg.Second.ID]);
                    newFabCntgs.Add(newcntg);
                }


                var newDOrderings = new List<Tuple<CamPlanStep, CamPlanStep>>();
                foreach (var subOrdering in TD.discOrderings)
                {
                    var newOrdering = new Tuple<CamPlanStep, CamPlanStep>(camSubStepDict[subOrdering.First.ID], camSubStepDict[subOrdering.Second.ID]);
                    newDOrderings.Add(newOrdering);
                }

                var newDiscCntgs = new List<Tuple<CamPlanStep, CamPlanStep>>();
                foreach (var subCntg in TD.discCntgs)
                {
                    var newcntg = new Tuple<CamPlanStep, CamPlanStep>(camSubStepDict[subCntg.First.ID], camSubStepDict[subCntg.Second.ID]);
                    newDiscCntgs.Add(newcntg);
                }


                var linkWorlds = new List<List<CausalLink<CamPlanStep>>>();
                linkWorlds.Add(new List<CausalLink<CamPlanStep>>());
                var newSublinks = new List<CausalLink<CamPlanStep>>();
                foreach (var subLink in TD.discLinks)
                {
                    var head = camSubStepDict[subLink.Head.ID];
                    var tail = camSubStepDict[subLink.Tail.ID];
                    var cndts = head.Effects.Where(eff => eff.IsConsistent(subLink.Predicate) && tail.Preconditions.Any(pre => pre.Equals(eff)));
                    if (cndts.Count() == 0)
                    {
                        // forfeit this entire subplan
                        linkWorlds = new List<List<CausalLink<CamPlanStep>>>();
                        break;
                    }
                    if (cndts.Count() == 1)
                    {
                        var cndt = cndts.First();
                        var dependency = cndt.Clone() as Predicate;
                        var newLink = new CausalLink<CamPlanStep>(dependency, head, tail);
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

                            var newLink = new CausalLink<CamPlanStep>(dependency, head, tail);
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
                    newDecomp.discourseSubSteps = TD.discourseSubSteps;
                    newDecomp.discOrderings = newDOrderings;
                    newDecomp.fabCntgs = newFabCntgs;
                    newDecomp.discCntgs = newDiscCntgs;
                    newDecomp.discLinks = linkworld;

                    timelineDecompList.Add(newDecomp);
                }
            }

            return timelineDecompList;
        }

        public static List<List<CamSchema>> GetPermutationCameraShots(List<CamPlanStep> discourseSubSteps, Dictionary<int, IPlanStep> fabsubstepDict, Dictionary<string, PlanStep> fabulaActionNameMap,
            Dictionary<int, Orient> orientDict, Dictionary<int, string> locationDict, List<CamSchema> camOptions)
        {
            List<List<CamSchema>> permList = new List<List<CamSchema>>();
            List<CamSchema> cndtSet;

            var enumeratedActionNames = new List<string>();
            // Create "worlds" of different camera shots.
            foreach (CamPlanStep discStep in discourseSubSteps)
            {
                var discStepClone = discStep.Clone() as CamPlanStep;
                string targetLocation = "";
                Orient targetOrient = Orient.None;

                // The action being filmed must be in the right location and orientation. Here is the name of target.
                var nameOfTargetOrAction = discStep.TargetDetails.ActionSegs[0].actionVarName;
                enumeratedActionNames.Add(nameOfTargetOrAction);

                // the target should be an Action.
                if (fabulaActionNameMap.ContainsKey(nameOfTargetOrAction))
                {
                    // figure out which old variable is being referenced with this string
                    var actionVar = fabulaActionNameMap[nameOfTargetOrAction];

                    // use old ID of actionVar to get new updated plan step
                    var substepTarget = fabsubstepDict[actionVar.ID];

                    // use new substep to access location of action
                    targetLocation = locationDict[substepTarget.ID];
                    discStepClone.TargetDetails.location = targetLocation;
                    // camschema.targetLocation = 
                    //  discClipSchema.asset.targetSchema.location = locationDict[substepTarget.ID];

                    // use new substep to access orientation of action
                    targetOrient = orientDict[substepTarget.ID];
                    discStepClone.TargetDetails.orient = targetOrient;
                    // discClipSchema.asset.targetSchema.orient = orientDict[substepTarget.ID];
                }
                else
                {
                    // If the target is not the name of an action, then it's a composite discourse step... and we will get to that
                    Debug.Log("target was not an action, so it must be a composite discourse step, but we haven't designed for that yet.");
                    throw new System.Exception();
                }

                cndtSet = new List<CamSchema>();
                // Filtering stage: For each camera object...
                foreach (var camOption in camOptions)
                {
                    // Cam Schema must be consistent with option
                    if (!discStepClone.CamDetails.IsConsistent(camOption))
                    {
                        continue;
                    }
                    if (!camOption.targetLocation.Equals(targetLocation))
                    {
                        /// TODO: need solution for navigation items - then determine if end location is cam option.
                        continue;
                    }
                    if (!camOption.targetOrientation.Equals(targetOrient))
                    {
                        continue;
                    }
                    cndtSet.Add(camOption);
                }
                // for each discourse sub-step, cndtSet is the list of candidate and valid camera shots to rewrite.
                permList.Add(cndtSet);
            }

            return permList;
        }
    }
}
