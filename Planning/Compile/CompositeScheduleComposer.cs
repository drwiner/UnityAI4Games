using BoltFreezer.DecompTools;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Scheduling;
using BoltFreezer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlanningNamespace;

namespace CompilationNamespace
{
    public class CompositeScheduleComposer
    {
        UnityTimelineDecomp decompschema;
        TimelineDecomposition gdecomp;
        List<IPredicate> Preconditions;
        List<IPredicate> Effects;

        IPlanStep InitialStep;
        IPlanStep GoalStep;
        Dictionary<int, List<Tuple<double, double>>> ActionPercentDict;
        List<Tuple<IPlanStep, List<Tuple<double, double>>>> MissingActionIntervals;

        List<IPlanStep> StepsObservedToStart;
        List<IPlanStep> StepsNotObservedToStart;
        List<IPlanStep> StepsNotObservedToEnd;

        List<CausalLink<IPlanStep>> LinksWithInitial;
        List<CausalLink<IPlanStep>> LinksWithGoal;

        List<IPredicate> ObservedEffectsList;
        List<Tuple<IPredicate, IPlanStep>> ObservedEffectTuples;

        //CompositeSchedule CS;

        public CompositeScheduleComposer(UnityTimelineDecomp dschema, TimelineDecomposition gdeco)
        {
            decompschema = dschema;
            gdecomp = gdeco;

            InitialStep = new PlanStep(new Operator("DummyInit", new List<IPredicate>(), new List<IPredicate>()));
            GoalStep = new PlanStep(new Operator("DummyGoal", new List<IPredicate>(), new List<IPredicate>()));
            LinksWithInitial = new List<CausalLink<IPlanStep>>();
            LinksWithGoal = new List<CausalLink<IPlanStep>>();
            CreateActionPercentDict();
            GeneratePreconditionsAndEffects();
        }

        public CompositeSchedule CreateCompositeSchedule()
        {
            var compOp = new Operator(decompschema.name, gdecomp.Terms, new Hashtable(), Preconditions, Effects);
            compOp.Height = 1;
            compOp.NonEqualities = new List<List<ITerm>>();
            CompositeSchedule comp = new CompositeSchedule(compOp);
            // this also limits preconditions and effects
            comp.InitialStep = InitialStep;
            comp.GoalStep = GoalStep;
            comp.ApplyDecomposition(gdecomp.Clone() as TimelineDecomposition);
            // applying the decomposition adds c-pairs, orderings, links, and updates initial and final action materials


            foreach (var link in LinksWithInitial)
            {
                link.Tail.OpenConditions.Remove(link.Predicate);
                comp.SubLinks.Add(link);
            }

            foreach (var link in LinksWithGoal)
            {
                link.Tail.OpenConditions.Remove(link.Predicate);
                comp.SubLinks.Add(link);
            }

            // Handled by AddCompositeSubStep in TimelineDecopmositionHelper

            //// This is not going to be suffcient when the gdecomp initial action segment is actually the initial action segment of a composite sub-step. Do we need separate reference, or separate slot?
            //if (gdecomp.discourseSubSteps.Count == 0)
            //{
            //    // this IMPLIES that only sub-step is composite.
            //    var onlyCompositeSubStep = gdecomp.SubSteps[0] as CompositeSchedulePlanStep;

            //    comp.InitialActionSeg = onlyCompositeSubStep.InitialActionSeg;
            //    comp.FinalActionSeg = onlyCompositeSubStep.FinalActionSeg;

            //    comp.InitialAction = onlyCompositeSubStep.InitialAction;
            //    comp.FinalAction = onlyCompositeSubStep.FinalAction;

            //    comp.InitialCamAction = onlyCompositeSubStep.InitialCamAction;
            //    comp.FinalCamAction = onlyCompositeSubStep.FinalCamAction;

            //    if (comp.Terms.Count < 2)
            //    {
            //        var numTerms = onlyCompositeSubStep.Terms.Count;
            //        comp.Terms.Add(onlyCompositeSubStep.Terms[numTerms - 2]);
            //        comp.Terms.Add(onlyCompositeSubStep.Terms[numTerms - 1]);
            //    }
            //    return comp;
            //}


            //comp.InitialActionSeg = gdecomp.discourseSubSteps[0].TargetDetails.ActionSegs[0].Clone();

            //comp.InitialAction = gdecomp.fabulaActionNameMap[comp.InitialActionSeg.actionVarName];

            //// Assign the action type ID // decomp's fabulaActionNameMap is left behind.
            //comp.InitialActionSeg.actiontypeID = comp.InitialAction.Action.ID;

            
            //comp.InitialCamAction =

            //// Retrieve the last camera schedule action
            //var targetDetails = gdecomp.discourseSubSteps[gdecomp.discourseSubSteps.Count - 1].TargetDetails;

            //// Assign the Final Action Segment of the composite step
            //comp.FinalActionSeg = targetDetails.ActionSegs[targetDetails.ActionSegs.Count - 1].Clone();

            //comp.FinalAction = gdecomp.fabulaActionNameMap[comp.FinalActionSeg.actionVarName];

            //// Assign the action type ID
            //comp.FinalActionSeg.actiontypeID = comp.FinalAction.Action.ID;

            // Assign the InitialAction

            // If gdecomp doesn't have at least 2 terms,t hen we need to add final 2 terms: these are used to determine orientation because they are always two locations.
            if (gdecomp.Terms.Count < 2)
            {
                var numTerms = gdecomp.SubSteps[0].Terms.Count;
                comp.Terms.Add(gdecomp.SubSteps[0].Terms[numTerms - 2]);
                comp.Terms.Add(gdecomp.SubSteps[0].Terms[numTerms - 1]);
            }

            return comp;
        }

        public void GenerateEffects()
        {
            /* Effects
                * 
                * foreach action that is NOT observed to end, give effect that we observed it start
                * foreach action that IS observed to end, give effect that we observed its effects
                */

            foreach (var item in decompschema.ignoreDidNotEnd)
            {

                var planStep = gdecomp.fabulaActionNameMap[item];

                if (StepsNotObservedToEnd.Contains(planStep))
                {
                    StepsNotObservedToEnd.Remove(planStep);
                }
                foreach (var eff in planStep.Effects)
                {
                    ObservedEffectsList.Add(eff);
                    ObservedEffectTuples.Add(new Tuple<IPredicate, IPlanStep>(eff, planStep));
                }
            }

            var effects  = UnityGroundActionFactory.CreatePredicatesWithStepTermsViaName(StepsNotObservedToEnd, "obs-starts");
            //var effects = new List<IPredicate>();
            foreach (var observedEffect in ObservedEffectsList)
            {
                IPlanStep actingStep = new PlanStep();
                foreach (var tupleItem in ObservedEffectTuples)
                {
                    if (tupleItem.First.Equals(observedEffect))
                    {
                        actingStep = tupleItem.Second;
                        break;
                    }
                }
                // cast predicate as term?
                //  var obsTerm = new Predicate("obs", new List<ITerm>() { observedEffect as ITerm}, true);
                //effects.Add(obsTerm);
                effects.Add(observedEffect);

                GoalStep.Preconditions.Add(observedEffect);
                LinksWithGoal.Add(new CausalLink<IPlanStep>(observedEffect, actingStep, GoalStep));
                GoalStep.OpenConditions.Remove(observedEffect);
            }
            foreach (var eff in gdecomp.Effects)
            {
                if (!Effects.Contains(eff) && !effects.Contains(eff))
                {
                    effects.Add(eff);
                }
            }
            foreach(var eff in effects)
            {
                Effects.Add(eff);
            }
        }

        public void GeneratePreconditions()
        {
            /* Preconditions
                * 
                * foreach action  that is not observed to start, give precondition to observe its start
                * foreach action that IS observed to start, and is the first step observed
                */

            foreach (var item in decompschema.ignoreDidNotStart)
            {

                var planStep = gdecomp.fabulaActionNameMap[item];

                if (StepsNotObservedToStart.Contains(planStep))
                {
                    StepsNotObservedToStart.Remove(planStep);
                    StepsObservedToStart.Add(planStep);
                }
            }


            var preconditions = UnityGroundActionFactory.CreatePredicatesWithStepTermsViaName(StepsNotObservedToStart, "obs-starts");
            //var preconditions = new List<IPredicate>();
            foreach (var stepAction in StepsObservedToStart)
            {
                foreach (var precon in stepAction.Preconditions)
                {
                    //var obsTerm = new Predicate("obs", new List<ITerm>() { precon as ITerm }, true);
                    // preconditions.Add(obsTerm);
                    preconditions.Add(precon);
                    InitialStep.Effects.Add(precon);
                    LinksWithInitial.Add(new CausalLink<IPlanStep>(precon, InitialStep, stepAction));
                    stepAction.OpenConditions.Remove(precon);
                }
            }
            foreach (var precon in gdecomp.Preconditions)
            {
                if (!Preconditions.Contains(precon) && !preconditions.Contains(precon))
                {
                    preconditions.Add(precon);
                }
            }
            
            foreach(var precon in preconditions)
            {
                Preconditions.Add(precon);
            }
        }

        public void GeneratePreconditionsAndEffects()
        {
            Preconditions = new List<IPredicate>();
            Effects = new List<IPredicate>();

            bool ignorePrecons = false;
            bool ignoreEffects = false;

            if (gdecomp.SubSteps[0].Height > 0)
            {
                ignorePrecons = true;
                GeneratePreconditionsFromCompositeSubStep();
            }
            if (gdecomp.SubSteps[gdecomp.SubSteps.Count-1].Height > 0)
            {
                ignoreEffects = true;
                GenerateEffectsFromCompositeSubStep();
            }

            SimulateObservationsFromSchedule(ignorePrecons, ignoreEffects);
            GeneratePreconditions();
            GenerateEffects();

        }

        
        public void GeneratePreconditionsFromCompositeSubStep()
        {
            // This is a composite sub-step.
            var composite = gdecomp.SubSteps[0] as CompositePlanStep;
            foreach (var precon in gdecomp.SubSteps[0].Preconditions)
            {
                if (precon.Name.Equals("obs-starts"))
                {
                    continue;
                    // i.e. if precon is special type with operator-ID term // TODO: sub-stype predicate such as PrimaryPredicate or Scenario or Context (because it's a condition about a step)
                }
                Preconditions.Add(precon);
                gdecomp.SubSteps[0].OpenConditions.Remove(precon);

                // A composite sub-step, in order to maintain the local sub-plan action reference rule, should be a link with Composite not it's initial step.
                LinksWithInitial.Add(new CausalLink<IPlanStep>(precon, InitialStep, composite as IPlanStep));
            }
        }

        public void GenerateEffectsFromCompositeSubStep()
        {
            var lastIndex = gdecomp.SubSteps.Count - 1;
            var composite = gdecomp.SubSteps[lastIndex] as CompositePlanStep;
            foreach (var eff in gdecomp.SubSteps[lastIndex].Effects)
            {
                //var contextEff = eff as ContextPredicate;
                //if (contextEff != null)
                //{
                //    continue;
                //}
                if (eff.Name.Equals("obs-starts"))
                {
                    continue;
                    // i.e. if precon is special type with operator-ID term // TODO: sub-stype predicate such as PrimaryPredicate or Scenario or Context (because it's a condition about a step)
                }
                Effects.Add(eff);
                // A composite sub-step, in order to maintain the local sub-plan action reference rule, should be a link with Composite not it's goal step.
                LinksWithGoal.Add(new CausalLink<IPlanStep>(eff, composite as IPlanStep, GoalStep));
            }
        }

        public void CreateActionPercentDict()
        {
            ActionPercentDict = new Dictionary<int, List<Tuple<double, double>>>();

            // gdecomp.fabulaActionNameMap[action.Name]

            // For each camera sub-step, we are building reference to actions that appear as action segments.
            // A camera plan step may reference an action of a grand-sub-step (or further decompositionally).
            for (int i = 0; i < gdecomp.discourseSubSteps.Count; i++)
            {
                var shot = gdecomp.discourseSubSteps[i];
                foreach (var actionseg in shot.TargetDetails.ActionSegs)
                {
                    var actionsubstep = gdecomp.fabulaActionNameMap[actionseg.actionVarName];
                    if (!ActionPercentDict.ContainsKey(actionsubstep.ID))
                    {
                        ActionPercentDict[actionsubstep.ID] = new List<Tuple<double, double>>();
                    }
                    ActionPercentDict[actionsubstep.ID].Add(new Tuple<double, double>(actionseg.startPercent, actionseg.endPercent));
                }
            }
        }

        public void SimulateObservationsFromSchedule(bool ignorePreconds, bool ignoreEffects)
        {
            // Track which intervals of actions are missing.
            MissingActionIntervals = new List<Tuple<IPlanStep, List<Tuple<double, double>>>>();

            // Track which steps are not observed to start or end
            StepsObservedToStart = new List<IPlanStep>();
            StepsNotObservedToStart = new List<IPlanStep>();
            StepsNotObservedToEnd = new List<IPlanStep>();
            //List<IPlanStep> StepsObservedToEnd = new List<IPlanStep>();

            double latestTimeAccountedFor;
            bool observedEnding;
            ObservedEffectsList = new List<IPredicate>();
            ObservedEffectTuples = new List<Tuple<IPredicate, IPlanStep>>();

            for (int i = 0; i < gdecomp.SubSteps.Count; i++)
            //foreach (var action in gdecomp.SubSteps)
            {
                var action = gdecomp.SubSteps[i];

                if (action.Height > 0 && i == 0)
                {
                    // then this action's preconditions become the preconditions of the composite.
                    continue;
                }
                else if (action.Height > 0 && i == gdecomp.SubSteps.Count - 1)
                {
                    // This is a final composite action whose effects already are contained.
                    continue;
                }
                // Reset latest time accounted for
                latestTimeAccountedFor = 0;

                var missingTimes = new List<Tuple<double, double>>();
                var actionTuplesList = ActionPercentDict[action.ID];
                observedEnding = true;
                for (int j = 0; j < actionTuplesList.Count; j++)
                {
                    // Access action percent tuple
                    var actionsegTuple = actionTuplesList[j];

                    if (!ignorePreconds)
                    { 
                        // Check if not observed to start
                        if (j == 0 && actionsegTuple.First > 0.06)
                        {
                            //if (!decompschema.IgnoreStarts.Contains(action.))
                            StepsNotObservedToStart.Add(action);
                        }
                        else if (i == 0 && j == 0 && actionsegTuple.First <= 0.06)
                        {
                            StepsObservedToStart.Add(action);
                            //else
                            //{
                            //    List<IPredicate> actionPreconditions = action.Preconditions.ToList();
                            //    for(int k = i-1; k >= 0; k--)
                            //    {
                            //        var priorAction = gdecomp.SubSteps[k];
                            //        foreach(var eff in priorAction.Effects)
                            //        {
                            //            if (actionPreconditions.Contains(eff))
                            //            {
                            //                // create a d-link
                            //                var dlinkPred = new Predicate("obs", new List<ITerm>() { eff as ITerm }, true);
                            //                var newDLink = new CausalLink<CamPlanStep>(dlinkPred as IPredicate, )
                            //            }
                            //        }
                            //    }
                            //}
                        }
                    }

                    // Check if missing interval
                    if (actionsegTuple.First > latestTimeAccountedFor + 0.06)
                    {
                        // then there is missing time.
                        var missingTime = new Tuple<double, double>(latestTimeAccountedFor, actionsegTuple.First);
                        missingTimes.Add(missingTime);
                    }

                    if (!ignoreEffects)
                    {
                        // Check if not observed to end
                        if (j == actionTuplesList.Count - 1 && actionsegTuple.Second < 1 - 0.06)
                        {
                            StepsNotObservedToEnd.Add(action);
                            observedEnding = false;
                        }
                    }

                    // Update latest time in action account for.
                    latestTimeAccountedFor = actionsegTuple.Second;
                }

                if (observedEnding && !ignoreEffects)
                {
                    foreach (var eff in action.Effects)
                    {
                        if (ObservedEffectsList.Contains(eff))
                        {

                            foreach (var tupleItem in ObservedEffectTuples)
                            {
                                if (tupleItem.First.Equals(eff))
                                {
                                    ObservedEffectTuples.Remove(tupleItem);
                                    ObservedEffectTuples.Add(new Tuple<IPredicate, IPlanStep>(eff, action));
                                    break;
                                }
                            }
                        }

                        var reversedEff = eff.GetReversed();
                        if (ObservedEffectsList.Contains(reversedEff))
                        {
                            ObservedEffectsList.Remove(reversedEff);
                        }
                        else
                        {
                            ObservedEffectsList.Add(eff);
                            ObservedEffectTuples.Add(new Tuple<IPredicate, IPlanStep>(eff, action));
                        }
                    }

                    // Add missing times per action to list of tuples
                    MissingActionIntervals.Add(new Tuple<IPlanStep, List<Tuple<double, double>>>(action, missingTimes));
                }
            }
        }
    }
}
