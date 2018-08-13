using BoltFreezer.Camera;
using BoltFreezer.Camera.CameraEnums;
using BoltFreezer.DecompTools;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using CameraNamespace;
using GraphNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CompilationNamespace
{
    public static class DiscourseDecompositionHelper 
    {
        public static List<CamSchema> CamOptions;
        public static Dictionary<Edge, Dictionary<double, List<CamSchema>>> NavCamDict;
        public static Dictionary<string, Vector3> LocationMap;
        public static TileGraph LocationGraph;

        public static void SetCamsAndLocations(GameObject CameraHost, GameObject LocationHost)
        {
            CameraCacheManager.DecacheCams(CameraHost.GetComponent<CamGen>().cacheFileName);
            NavCamDict = CameraCacheManager.CalculateNavCamDictFromCache();
            //NavCamDict = CameraHost.GetComponent<CamGen>().navCamDictionary;


            LocationMap = new Dictionary<string, Vector3>();
            for (int i = 0; i < LocationHost.transform.childCount; i++)
            {
                var locationObj = LocationHost.transform.GetChild(i);
                if (!locationObj.gameObject.activeSelf)
                {
                    continue;
                }
                LocationMap[locationObj.name] = locationObj.transform.position;
            }

            LocationGraph = LocationHost.GetComponent<TileGraph>();
        }

        public static Tuple<Dictionary<int, Orient>, Dictionary<int, string>> GetOrientsAndLocations(Decomposition decomp)
        {
            var orientDict = new Dictionary<int, Orient>();
            var locationDict = new Dictionary<int, string>();
            var locs = GroundActionFactory.TypeDict["Location"];
                //.Contains(decomp.SubSteps[0].Terms[2].Constant)
            foreach (var substep in decomp.SubSteps)
            {
                var orientEnum = MapToNearestOrientation(substep as PlanStep);
                orientDict[substep.ID] = orientEnum;

                string earliestTermHack = "";
                foreach (var term in substep.Terms)
                {
                    if (locs.Contains(term.Constant))
                    //if (term.Type.Equals("Location"))
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
        public static Orient MapToNearestOrientation(PlanStep step)
        {
            // determine what the orientation is
            Orient orientEnum;
            // What is orientation?
            var orientFloat = OrientInFloat(LocationMap[step.Terms[step.Terms.Count - 1].Constant], LocationMap[step.Terms[step.Terms.Count - 2].Constant]);

            return MapToNearest(orientFloat);
        }

        public static Orient MapToNearest(float orientFloat)
        {
            while (orientFloat > 360)
            {
                orientFloat -= 360;
            }
            while (orientFloat < 0)
            {
                orientFloat += 360;
            }
            orientFloat = 360 - orientFloat;

            var bestSoFar = Orient.None;
            var bestDistance = 400f;
            foreach (var orient in System.Enum.GetValues(typeof(Orient)).Cast<Orient>())
            {
                if (orient == Orient.None)
                {
                    continue;
                }
                var floatValue = orient.ToString().Split('o')[1];
                var dist = Mathf.Abs(Mathf.RoundToInt(orientFloat) - Int32.Parse(floatValue));
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestSoFar = orient;
                }
            }
            
            return bestSoFar;
        }

        public static List<List<CamPlanStep>> GetPermutationCameraShots(TimelineDecomposition decomp,
            List<CamPlanStep> discourseSubSteps,
            Dictionary<int, IPlanStep> fabsubstepDict,
            Dictionary<string, PlanStep> fabulaActionNameMap,
            Dictionary<int, Orient> orientDict,
            Dictionary<int, string> locationDict)
        {
            List<List<CamPlanStep>> permList = new List<List<CamPlanStep>>();
            List<CamPlanStep> cndtSet;

            var enumeratedActionNames = new List<string>();
            // Create "worlds" of different camera shots.
            foreach (CamPlanStep discStep in discourseSubSteps)
            {
                //////////////////////////////////////////////////
                ////    Mutatable discourse step     /////////////
                //////////////////////////////////////////////////

                var discStepClone = discStep.Clone() as CamPlanStep;

                // The action being filmed must be in the right location and orientation. Here is the name of target. // Default is 0
                var actionSeg = discStep.TargetDetails.ActionSegs[discStep.TargetDetails.actionSegOfFocus];

                // The name of the action reference
                var nameOfTargetOrAction = actionSeg.actionVarName;

                // Record the names of the original (i.e. variable name) of the referenced action
                enumeratedActionNames.Add(nameOfTargetOrAction);

                // This referenced action variable name should correspond to a displayed action name on the Fabula Track.
                if (!fabulaActionNameMap.ContainsKey(nameOfTargetOrAction))
                {
                    Debug.Log("target was not an action, so it must be a composite discourse step, but we haven't designed for that yet.");
                    throw new System.Exception();
                }

                // The action itself from the fabula track
                var fabtrackAction = fabulaActionNameMap[nameOfTargetOrAction];

                // The ground action from the grounded fabula sub-plan (the sub-plan written for the fabula track)
                var substepTarget = fabsubstepDict[fabtrackAction.ID];

                //////////////////////////////////////////////////
                ///////////////    Location     //////////////////
                //////////////////////////////////////////////////

                string targetLocation = discStepClone.TargetDetails.location;
                //string cinematicTargetLocation = discStep.CamDetails.targetLocation;

                // Recall, this substepTarget has a location, but first, 
                if (!targetLocation.Equals(""))
                {
                    // ! Priority: overriding with targetLocation
                    targetLocation = decomp.Terms.Single(term => term.Variable.Equals(targetLocation)).Constant;
                }
                else
                {
                    // Uses the "earliest term hack"
                    targetLocation = locationDict[substepTarget.ID];
                }

                // Either way, it has been replaced
                discStepClone.TargetDetails.location = targetLocation;

                // ?? Don't override with the cam details - this should not be specified.

                //////////////////////////////////////////////////
                ///////////////    Look Direction     ////////////
                //////////////////////////////////////////////////

                string orientTowards = discStep.TargetDetails.orientTowards;

                if (!orientTowards.Equals(""))
                {
                    orientTowards = decomp.Terms.Single(term => term.Variable.Equals(orientTowards)).Constant;
                }
                // Keep blank if unspecified

                //////////////////////////////////////////////////
                ///////////////    ORIENT     /////////////////////
                //////////////////////////////////////////////////

                Orient targetOrient = orientDict[substepTarget.ID];
                // Uses the MapToNearestOrientation method
                // You don't specify an orientation explicitly..

                // If orientTowards is specified, then the earliest term hack may be at play for targetLocation; otherwise, it's the specified target location
                if (!orientTowards.Equals("") && !targetLocation.Equals(""))
                {
                    // Use the orient oriention from targetlocation to orientTowards and find corresponding orient value
                    targetOrient = MapToNearest(OrientInFloat(LocationMap[targetLocation], LocationMap[orientTowards]));
                }

                // Set the orient for Cinematography and Composition
                discStepClone.TargetDetails.orient = targetOrient;
                discStepClone.CamDetails.targetOrientation = targetOrient;


                //////////////////////////////////////////////////
                ///////////////    Navigation     ////////////////
                //////////////////////////////////////////////////

                // All domain actions must be tagged as navigational to receive special treatment:
                var unityActionTag = GameObject.Find(substepTarget.Name).tag;

                if (unityActionTag == "Navigation")
                {
                    // Find graph edge traversed for this action.
                    Tuple<Edge, int> traversedDirectedEdge;
                    if (!orientTowards.Equals("") && !targetLocation.Equals(""))
                    {
                        traversedDirectedEdge = LocationGraph.FindRelevantDirectedEdge(targetLocation, orientTowards);
                    }
                    else
                    {
                        // This has default of being 1, and 2 indices of step's term.
                        traversedDirectedEdge = LocationGraph.FindRelevantDirectedEdge(substepTarget.Terms[1].Constant, substepTarget.Terms[2].Constant);
                    }

                    Edge traversededge = traversedDirectedEdge.First;
                    int direction = traversedDirectedEdge.Second;

                    List<CamSchema> navCamOptions;
                    if (actionSeg.directive == CamDirective.Follow)
                    {
                        if (direction == -1)
                        {
                            navCamOptions = CamGen.GetCamsForEdgeAndPercent(NavCamDict, traversededge, 1 - actionSeg.startPercent);
                        }
                        else
                        {
                            navCamOptions = CamGen.GetCamsForEdgeAndPercent(NavCamDict, traversededge, actionSeg.startPercent);

                        }

                    }

                    // Narrow cam options to those that target the location mapped most closely to the percentage of the traversed edge
                    else
                    {
                        double perc = actionSeg.startPercent + ((actionSeg.endPercent - actionSeg.startPercent) / 2);
                        if (direction == -1)
                        {
                            navCamOptions = CamGen.GetCamsForEdgeAndPercent(NavCamDict, traversededge, 1 - perc);
                        }
                        else
                        {
                            navCamOptions = CamGen.GetCamsForEdgeAndPercent(NavCamDict, traversededge, perc);

                        }

                    }
                    // var navCamOptions = NavCamDict[traversededge][actionSeg.startPercent + ((actionSeg.endPercent - actionSeg.startPercent) / 2)];

                    //////////////////////////////////////////////////
                    ///////////////    Reset Locations     ///////////
                    //////////////////////////////////////////////////

                    discStepClone.CamDetails.targetLocation = navCamOptions[0].targetLocation;
                    discStepClone.TargetDetails.location = navCamOptions[0].targetLocation;

                    // Filter camera candidates to that location
                    cndtSet = new List<CamPlanStep>();
                    foreach (var navcamoption in navCamOptions)
                    {
                        // Cam Schema must be consistent with option
                        if (!discStepClone.CamDetails.IsConsistent(navcamoption))
                        {
                            continue;
                        }

                        if (!navcamoption.targetOrientation.Equals(targetOrient))
                        {
                            continue;
                        }

                        var navCamAction = discStepClone.Clone() as CamPlanStep;
                        navCamAction.CamDetails = navcamoption.Clone();

                        cndtSet.Add(navCamAction);
                    }
                    if (cndtSet.Count == 0)
                    {
                        Debug.Log(string.Format("Camera Action {0} had no options", discStepClone.ToString()));
                    }
                    permList.Add(cndtSet);
                    continue;
                }

                // Filter camera candidates (non-navigational actions)
                cndtSet = new List<CamPlanStep>();

                // Filtering stage: For each camera object...
                foreach (var camOption in CameraCacheManager.CachedCams)
                {
                    // Cam Schema must be consistent with option
                    if (!discStepClone.CamDetails.IsConsistent(camOption))
                    {
                        continue;
                    }

                    // Camera target location must be equal
                    if (!camOption.targetLocation.Equals(targetLocation))
                    {
                        continue;
                    }

                    // Camera target needs agent orientation, must be equal
                    if (!camOption.targetOrientation.Equals(targetOrient))
                    {
                        continue;
                    }

                    var groundDiscStep = discStepClone.Clone() as CamPlanStep;
                    groundDiscStep.CamDetails = camOption.Clone();

                    // This camera is an option.
                    cndtSet.Add(groundDiscStep);
                }

                if (cndtSet.Count == 0)
                {
                    Debug.Log(string.Format("Camera Action {0} had no options", discStepClone.ToString()));
                }

                // for each discourse sub-step, cndtSet is the list of candidate and valid camera shots to rewrite.
                permList.Add(cndtSet);
            }

            return permList;
        }

        public static List<TimelineDecomposition> FilterTimelineDecompCandidates(TimelineDecomposition TD, Tuple<TimelineDecomposition, Dictionary<int, IPlanStep>> decompPackage, int height)
        {
            var timelineDecompList = new List<TimelineDecomposition>();

            var decomp = decompPackage.First;
            var substepDict = decompPackage.Second;

            // Mpping ID of substeps to orientations
            var orientLocationTuple = GetOrientsAndLocations(decomp);
            var orientDict = orientLocationTuple.First;
            var locationDict = orientLocationTuple.Second;

            //////////////////////////////////////////////////
            //////// Get Camera Permutations ////////////////
            //////////////////////////////////////////////////

            var permList = GetPermutationCameraShots(decomp, decomp.discourseSubSteps, substepDict, decomp.fabulaActionNameMap, orientDict, locationDict);

            if (permList.Count == 0)
            {
                return timelineDecompList;
            }

            //////////////////////////////////////////////////
            ////// Create Discourse Sub-Plans ////////////////
            //////////////////////////////////////////////////

            // Foreach combination, check if step constraints are true
            foreach (var combination in EnumerableExtension.GenerateCombinations(permList))
            {
                var decompClone = decomp.Clone() as TimelineDecomposition;

                // Substep dictionary, maps schema of actions to ground actions
                var camSubStepDict = new Dictionary<int, CamPlanStep>();

                // List of ground camera schedule steps (camera plan steps), the sub-steps of a discourse sub-plan
                var newDiscourseSubSteps = new List<CamPlanStep>();

                // For each cam action, update its action segment variables: targetVarName, actionID, actiontypeID
                for (int j = 0; j < combination.Count; j++)
                {
                    // A reference to the j'th discourse (camera) action schema
                    var camActionSchema = decomp.discourseSubSteps[j] as CamPlanStep;

                    // The camera details of the camera action schema
                    var groundCamAction = combination[j].Clone() as CamPlanStep;

                    // The first action segment's referenced action ID (the a_i of segment a_is_i)
                    int firstID = -1;

                    // For each Action Segment of the camera action schema
                    for (int k = 0; k < groundCamAction.TargetDetails.ActionSegs.Count; k++)
                    {
                        var actionseg = groundCamAction.TargetDetails.ActionSegs[k];

                        // The action referenced by the action segment
                        var ps = substepDict[decomp.fabulaActionNameMap[actionseg.actionVarName].ID];

                        // The first action referenced's ID
                        if (firstID == -1)
                        {
                            firstID = ps.ID;
                        }

                        // Note: need to keep the "actionVarName" the same so that we can reference later... UGAF GroundDecompositionsToCompositeSteps (Line 212)
                        //actionseg.actionVarName = ps.ToString(); // DO NOT

                        /////////////////////////////////////////////
                        ///////////////  ActionID ////////////
                        /////////////////////////////////////////////

                        // Reference the ID, which is how the action is referenced
                        actionseg.ActionID = ps.ID;

                        /////////////////////////////////////////////
                        ///////////////  Target Var Name ////////////
                        /////////////////////////////////////////////

                        // If the target is not provided, then take the first Term of the referenced action
                        var splitTarget = actionseg.targetVarName.Split(' ');
                        if (actionseg.targetVarName.Equals(""))
                        {
                            actionseg.targetVarName = ps.Terms[0].Constant;
                        }
                        else if (splitTarget.Count() > 1)
                        {
                            var newTargetName = "";
                            foreach (var item in splitTarget)
                            {
                                newTargetName += decompClone.Terms.Single(term => term.Variable.Equals(item)).Constant + " ";
                            }
                            actionseg.targetVarName = newTargetName.TrimEnd(' ');
                        }
                        else
                        {
                            // Otherwise, it ought to be a term that is referenced by the decomposition as a labeled variable
                            actionseg.targetVarName = decompClone.Terms.Single(term => term.Variable.Equals(actionseg.targetVarName)).Constant;
                        }
                       
                    }

                    camSubStepDict[camActionSchema.ID] = groundCamAction;
                    newDiscourseSubSteps.Add(groundCamAction);
                }

                //////////////////////////////////////////////////////////////////////
                // Check that this camera shot sequence obeys all dictated constraints
                //////////////////////////////////////////////////////////////////////
                var boolOutcome = ValidateConstraints(substepDict, camSubStepDict, decomp.fabConstraints, decomp.discConstraints, orientDict);

                if (!boolOutcome)
                {
                    continue;
                }


                //////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////
                // Compile Sub-Plan 
                //////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////

                //////////////////////////////////////////////////////////////////////
                // Story cntgs
                //////////////////////////////////////////////////////////////////////

                // Can now update the couple fabcntgs that are of note
                var newFabCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
                foreach (var subCntg in decomp.fabCntgs)
                {
                    if (camSubStepDict.ContainsKey(subCntg.Second.ID))
                    {
                        var newcntg = new Tuple<IPlanStep, IPlanStep>(subCntg.First, camSubStepDict[subCntg.Second.ID]);
                        newFabCntgs.Add(newcntg);
                    }
                    else
                    {
                        newFabCntgs.Add(subCntg);
                    }
                }
                decomp.fabCntgs = newFabCntgs;

                //////////////////////////////////////////////////////////////////////
                // Camera orderings 
                //////////////////////////////////////////////////////////////////////

                var newDOrderings = new List<Tuple<CamPlanStep, CamPlanStep>>();
                foreach (var subOrdering in decomp.discOrderings)
                {
                    var newOrdering = new Tuple<CamPlanStep, CamPlanStep>(camSubStepDict[subOrdering.First.ID], camSubStepDict[subOrdering.Second.ID]);
                    newDOrderings.Add(newOrdering);
                }

                //////////////////////////////////////////////////////////////////////
                // Story orderings
                //////////////////////////////////////////////////////////////////////

                var newFOrderings = new List<Tuple<IPlanStep, IPlanStep>>();
                foreach (var subordering in decomp.SubOrderings)
                {
                    if (camSubStepDict.ContainsKey(subordering.Second.ID))
                    {
                        var newOrdering = new Tuple<IPlanStep, IPlanStep>(subordering.First, camSubStepDict[subordering.Second.ID]);
                        newFOrderings.Add(newOrdering);

                    }
                    else
                    {
                        newFOrderings.Add(subordering);
                    }
                }
                decomp.SubOrderings = newFOrderings;

                //////////////////////////////////////////////////////////////////////
                // Camera cntgs
                //////////////////////////////////////////////////////////////////////

                var newDiscCntgs = new List<Tuple<CamPlanStep, CamPlanStep>>();
                foreach (var subCntg in decomp.discCntgs)
                {
                    var newcntg = new Tuple<CamPlanStep, CamPlanStep>(camSubStepDict[subCntg.First.ID], camSubStepDict[subCntg.Second.ID]);
                    newDiscCntgs.Add(newcntg);
                }

                //////////////////////////////////////////////////////////////////////
                // Permutations of 'd' links Causal Links
                //////////////////////////////////////////////////////////////////////


                var linkWorlds = new List<List<CausalLink<CamPlanStep>>>();
                linkWorlds.Add(new List<CausalLink<CamPlanStep>>());
                var newSublinks = new List<CausalLink<CamPlanStep>>();
                foreach (var subLink in decomp.discLinks)
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
                    newDecomp.discourseSubSteps = newDiscourseSubSteps;
                    newDecomp.InitialCamAction = newDiscourseSubSteps[0];
                    newDecomp.FinalCamAction = newDiscourseSubSteps[newDiscourseSubSteps.Count - 1];
                    newDecomp.discOrderings = newDOrderings;
                    newDecomp.fabCntgs = newFabCntgs;
                    newDecomp.discCntgs = newDiscCntgs;
                    newDecomp.discLinks = linkworld;

                    timelineDecompList.Add(newDecomp);
                }
            }

            return timelineDecompList;
        }

        public static bool ValidateConstraints(

            // Fabula and Discourse Action-Segment ActionID-referenced Sub-steps
            Dictionary<int, IPlanStep> fabsubstepDict, 
            Dictionary<int, CamPlanStep> discsubstepDict,

            // fabula and Discourse Constraints
            List<Tuple<string, Tuple<PlanStep, PlanStep>>> fabConstraints, 
            List<Tuple<string, Tuple<CamPlanStep, CamPlanStep>>> discConstraints,

            // A dictionary for orientations?
            Dictionary<int, Orient> orientDict)
        {
            // return status: Do Not Fail
            bool fail = false;

            // Fabula Constraints
            foreach (var constraint in fabConstraints)
            {
                //////////////////////////////////////////////
                //// Check for orient-based constraints //////
                //////////////////////////////////////////////
                if (constraint.First.Equals("orient reverse"))
                {
                    // reference new substeps
                    var first = fabsubstepDict[constraint.Second.First.ID];
                    var second = fabsubstepDict[constraint.Second.Second.ID];

                    // Check that reverse
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

            // Discourse Constraints
            foreach (var constraint in discConstraints)
            {
                //////////////////////////////////////////////
                //// Check for Editing Constraints Here //////
                //////////////////////////////////////////////
                if (constraint.First.Equals("hangle reverse"))
                {
                    if (false)
                    {
                        fail = true;
                        break;
                    }
                }


                if (constraint.First.Equals("180 rule"))
                {

                }
                // Jump Cut
                // 180 degree rule
                // Continuity formula above threshold?
                
            }
            if (fail)
            {
                return false;
            }


            return true;
        }
    }
}