using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoltFreezer.CacheTools;
using BoltFreezer.PlanTools;
using System.Linq;
using BoltFreezer.Interfaces;
using BoltFreezer.FileIO;
using BoltFreezer.PlanSpace;
using BoltFreezer.Utilities;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class UnityGroundActionFactory : MonoBehaviour
    {

        public bool compilePrimitiveSteps = false;
        public bool compileCompositeSteps = false;
        public bool regenerateInitialPlanWithComposite = false;
        public bool checkEffects = false;

        public int PrimitiveSteps;
        public List<UnityTimelineDecomp> DecompositionSchemata;
        public int CompositeSteps;
        private List<IOperator> PrimitiveOps;
        private List<IOperator> CompositeOps;

        private IPlan initialPlan;

        public IPlan InitialPlan
        {
            get { return initialPlan; }
            set { initialPlan = value; }
        }

        // Update is called once per frame
        void Update()
        {
            if (compilePrimitiveSteps)
            {
                compilePrimitiveSteps = false;
                initialPlan = PreparePlanner(true);
                PrimitiveOps = GroundActionFactory.GroundActions;
                PrimitiveSteps = PrimitiveOps.Count;
                CompositeSteps = 0;
            }

            if (compileCompositeSteps)
            {
                compileCompositeSteps = false;
                CompileCompositeSteps();
            }

            if (checkEffects)
            {
                checkEffects = false;
                foreach(var compstep in CompositeOps)
                {
                    foreach(var effect in compstep.Effects)
                    {
                        if (effect.Name != "obs") // && effect.Name != "obs-starts")
                        {
                            continue;
                        }
                        Debug.Log(effect.ToString());
                    }
                }
                // do we have what we need?
            }

            if (regenerateInitialPlanWithComposite)
            {
                regenerateInitialPlanWithComposite = false;

                Parser.path = "/";
                var domainOperatorComponent = GameObject.FindGameObjectWithTag("ActionHost").GetComponent<DomainOperators>();
                domainOperatorComponent.Reset();
                var problem = CreateProblem(domainOperatorComponent.DomainOps);
                var domain = CreateDomain(domainOperatorComponent);
                var PF = new ProblemFreezer("Unity", "", domain, problem);
                var initPlan = PlannerScheduler.CreateInitialPlan(PF);
                CacheMaps.CacheAddReuseHeuristic(initPlan.Initial);
                PrimaryEffectHack(InitialPlan.Initial);
            }

        }

        public void CompileCompositeSteps()
        {
            CompositeSteps = 0;
            CompositeOps = new List<IOperator>();
            foreach (var unitydecomp in DecompositionSchemata)
            {
                if (unitydecomp.NumGroundDecomps == 0)
                {
                    unitydecomp.Read();
                    unitydecomp.Assemble();
                    unitydecomp.Filter();
                }
            }
            var compositeSteps = GroundDecompositionsToCompositeSteps(DecompositionSchemata);
            foreach (var comp in compositeSteps)
            {
                CompositeOps.Add(comp as IOperator);
                CompositeSteps++;
            }
            AddCompositeStepsToGroundActionFactory(compositeSteps);
        }


        public static void CreateSteps(UnityProblemCompiler UPC, List<UnityTimelineDecomp> DecompositionSchemata)
        {
            foreach (var unitydecomp in DecompositionSchemata)
            {
                if (unitydecomp.NumGroundDecomps == 0)
                {
                    unitydecomp.Read();
                    unitydecomp.Assemble();
                    unitydecomp.Filter();
                    Debug.Log("Read,Assemble, and Filter for unity decomp: " + unitydecomp.name);
                }
            }
            var compositeSteps = GroundDecompositionsToCompositeSteps(DecompositionSchemata);
            AddCompositeStepsToGroundActionFactory(UPC.initialPredicateList, UPC.goalPredicateList, compositeSteps);
        }

        public static void AddCompositeStepsToGroundActionFactory(List<IPredicate> Initial, List<IPredicate> Goal, List<CompositeSchedule> compositeSteps)
        {
            var originalOps = GroundActionFactory.GroundActions;
            var IOpList = new List<IOperator>();
            foreach (var compstep in compositeSteps)
            {
                var asIOp = compstep as IOperator;
                IOpList.Add(asIOp);
                GroundActionFactory.InsertOperator(asIOp);
            }

            // Update Heuristic value for primary effects.
            PrimaryEffectHack(new State(Initial) as IState);

            // Amonst themselves
            CacheMaps.CacheLinks(IOpList);

            // as antecedants to the originals
            CacheMaps.CacheLinks(IOpList, originalOps);

            // as consequents to the originals
            CacheMaps.CacheLinks(originalOps, IOpList);

            // as antecedants to goal conditions
            CacheMaps.CacheGoalLinks(IOpList, Goal);
        }

            public void AddCompositeStepsToGroundActionFactory(List<CompositeSchedule> compositeSteps)
        {
            var goalConditions = InitialPlan.GoalStep.Preconditions;
            var originalOps = GroundActionFactory.GroundActions;

            var IOpList = new List<IOperator>();
            foreach (var compstep in compositeSteps)
            {
                var asIOp = compstep as IOperator;
                IOpList.Add(asIOp);
                GroundActionFactory.InsertOperator(asIOp);
            }

            // Update Heuristic value for primary effects.
            PrimaryEffectHack(InitialPlan.Initial);

            // Amonst themselves
            CacheMaps.CacheLinks(IOpList);

            // as antecedants to the originals
            CacheMaps.CacheLinks(IOpList, originalOps);

            // as consequents to the originals
            CacheMaps.CacheLinks(originalOps, IOpList);

            // as antecedants to goal conditions
            CacheMaps.CacheGoalLinks(IOpList, goalConditions);

            // is is possible to have a new precondition here that is static? 
            /// this raises a larger point. 
            /// should we say that initially we observe the way the world is? (yes)
            /// should we create simple camera shots for conveying actions so that effects of actions are all observable?
            /// this is an experimental condition of sorts. In this case, there is no non-static condition that is not observable.
            /// therefore, (no), there is no need for (extra) statics.

            // There is also no need to cache addreuseheuristic again because primitive values.
            //CacheMaps.CacheAddReuseHeuristic(InitialPlan.Initial);
        }

        public static List<CompositeSchedule> GroundDecompositionsToCompositeSteps(List<UnityTimelineDecomp> DecompositionSchemata)
        {
            var compositeSteps = new List<CompositeSchedule>();
            foreach (var decompschema in DecompositionSchemata)
            {
                foreach (var gdecomp in decompschema.GroundDecomps)
                {
                    // use the fabulaActionNameMap to reference action var names to substeps
                    //var obseffects = new List<Tuple<CamPlanStep, List<Tuple<double, double>>>>();
                    // keeping track of every action's percentage observance
                    var actionPercentDict = new Dictionary<int, List<Tuple<double, double>>>();

                    // gdecomp.fabulaActionNameMap[action.Name]

                    for (int i = 0; i < gdecomp.discourseSubSteps.Count; i++)
                    {
                        var shot = gdecomp.discourseSubSteps[i];
                        foreach (var actionseg in shot.TargetDetails.ActionSegs)
                        {
                            var actionsubstep = gdecomp.fabulaActionNameMap[actionseg.actionVarName];
                            if (!actionPercentDict.ContainsKey(actionsubstep.ID))
                            {
                                actionPercentDict[actionsubstep.ID] = new List<Tuple<double, double>>();
                            }
                            actionPercentDict[actionsubstep.ID].Add(new Tuple<double, double>(actionseg.startPercent, actionseg.endPercent));
                        }
                    }

                    // Track which intervals of actions are missing.
                    var missingIntervalsInActions = new List<Tuple<IPlanStep, List<Tuple<double, double>>>>();

                    // Track which steps are not observed to start or end
                    List<IPlanStep> StepsObservedToStart = new List<IPlanStep>();
                    List<IPlanStep> StepsNotObservedToStart = new List<IPlanStep>();
                    List<IPlanStep> StepsNotObservedToEnd = new List<IPlanStep>();
                    //List<IPlanStep> StepsObservedToEnd = new List<IPlanStep>();

                    double latestTimeAccountedFor;
                    bool observedEnding;
                    var observedEffectsList = new List<IPredicate>();
                    for (int i =0; i < gdecomp.SubSteps.Count; i++)
                    //foreach (var action in gdecomp.SubSteps)
                    {
                        var action = gdecomp.SubSteps[i];
                        // Reset latest time accounted for
                        latestTimeAccountedFor = 0;

                        var missingTimes = new List<Tuple<double, double>>();
                        var actionTuplesList = actionPercentDict[action.ID];
                        observedEnding = true;
                        for (int j = 0; j < actionTuplesList.Count; j++)
                        {
                            // Access action percent tuple
                            var actionsegTuple = actionTuplesList[j];

                            // Check if not observed to start
                            if (j == 0 && actionsegTuple.First > 0.06)
                            {
                                StepsNotObservedToStart.Add(action);
                            } 
                            else if (i == 0 && j==0 && actionsegTuple.First <= 0.06)
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

                            // Check if missing interval
                            if (actionsegTuple.First > latestTimeAccountedFor + 0.06)
                            {
                                // then there is missing time.
                                var missingTime = new Tuple<double, double>(latestTimeAccountedFor, actionsegTuple.First);
                                missingTimes.Add(missingTime);
                            }

                            // Check if not observed to end
                            if (j == actionTuplesList.Count - 1 && actionsegTuple.Second < 1 - 0.06)
                            {
                                StepsNotObservedToEnd.Add(action);
                                observedEnding = false;
                            }

                            // Update latest time in action account for.
                            latestTimeAccountedFor = actionsegTuple.Second;
                        }

                        if (observedEnding)
                        {
                            foreach (var eff in action.Effects)
                            {
                                var reversedEff = eff.GetReversed();
                                if (observedEffectsList.Contains(reversedEff))
                                {
                                    observedEffectsList.Remove(reversedEff);
                                }
                                {
                                    observedEffectsList.Add(eff);
                                }
                            }

                            // Add missing times per action to list of tuples
                            missingIntervalsInActions.Add(new Tuple<IPlanStep, List<Tuple<double, double>>>(action, missingTimes));
                        }
                    }

                    /* Preconditions
                        * 
                        * foreach action  that is not observed to start, give precondition to observe its start
                        * foreach action that IS observed to start, and is the first step observed
                        */

                    var preconditions = CreatePredicatesWithStepTermsViaName(StepsNotObservedToStart, "obs-starts");
                    foreach(var stepAction in StepsObservedToStart)
                    {
                        foreach(var precon in stepAction.Preconditions)
                        {
                            var obsTerm = new Predicate("obs", new List<ITerm>() { precon as ITerm }, true);
                            preconditions.Add(obsTerm);
                            preconditions.Add(precon);
                        }
                    }
                    /* Effects
                        * 
                        * foreach action that is NOT observed to end, give effect that we observed it start
                        * foreach action that IS observed to end, give effect that we observed its effects
                        */

                    var effects = CreatePredicatesWithStepTermsViaName(StepsNotObservedToEnd, "obs-starts");
                    foreach (var observedEffect in observedEffectsList)
                    {
                        // cast predicate as term?
                        var obsTerm = new Predicate("obs", new List<ITerm>() { observedEffect as ITerm}, true);
                        effects.Add(obsTerm);
                        effects.Add(observedEffect);
                    }
                        
                    // Create a composite step
                    var compOp = new Operator(decompschema.name, preconditions, effects);
                    compOp.Height = 1;
                    compOp.NonEqualities = new List<List<ITerm>>();
                    var comp = new CompositeSchedule(compOp);
                    comp.ApplyDecomposition(gdecomp.Clone() as TimelineDecomposition);

                    // Add new composite step to list and add its string component to displayable gameobject component
                    compositeSteps.Add(comp);
                }
            }
            return compositeSteps;
        }

        public static List<IPredicate> CreatePredicatesWithStepTermsViaName(List<IPlanStep> stepsToCreatePredicatesWith, string predicateName)
        {
            var preds = new List<IPredicate>();
            foreach (var step in stepsToCreatePredicatesWith)
            {
                var stepTerm = new Term(step.ID.ToString(), true)
                {
                    Variable = step.ToString()
                };

                var stepPredicate = new Predicate(predicateName, new List<ITerm>() { stepTerm as ITerm }, true);
                preds.Add(stepPredicate as IPredicate);
            }
            return preds;
        }

        public Problem CreateProblem(List<Operator> DomainOps)
        {
            var ProblemHost = GameObject.FindGameObjectWithTag("Problem");
            var problemComponent = ProblemHost.GetComponent<UnityProblemCompiler>();
            problemComponent.ReadProblem();
            var prob = new Problem("SteerProblem", "SteerProblem", "Unity", "", GetObjects(), problemComponent.initialPredicateList, problemComponent.goalPredicateList);
            return prob;
        }

        public Domain CreateDomain(DomainOperators domainOperatorComponent)
        {
            var newOps = new List<IOperator>();

            foreach (var domainOp in domainOperatorComponent.DomainOps)
            {
                newOps.Add(domainOp as IOperator);
            }
            var domain = new Domain("unityWorld", BoltFreezer.Enums.PlanType.PlanSpace, newOps);
            domain.AddTypePair("SteeringAgent", "Agent");
            domain.AddTypePair("Block", "Item");
            domain.AddTypePair("", "SteeringAgent");
            domain.AddTypePair("", "Block");
            domain.AddTypePair("", "Location");

            return domain;
        }

        public IPlan PreparePlanner(bool resetCache)
        {
            
            Parser.path = "/";

            // Update Domain Operators
            var domainOperatorComponent = GameObject.FindGameObjectWithTag("ActionHost").GetComponent<DomainOperators>();
            domainOperatorComponent.Reset();

            // Read and Create Problem
            var problem = CreateProblem(domainOperatorComponent.DomainOps);

            // Create Domain
            var domain = CreateDomain(domainOperatorComponent);

            // Create Problem Freezer.
            var PF = new ProblemFreezer("Unity", "", domain, problem);

            // Create Initial Plan
            var initPlan = PlannerScheduler.CreateInitialPlan(PF);

            if (!resetCache)
            {
                if (GroundActionFactory.GroundActions != null)
                {
                    if (HeuristicMethods.visitedPreds == null || HeuristicMethods.visitedPreds.Count == 0)
                    {
                        CacheMaps.CacheAddReuseHeuristic(initPlan.Initial);
                        //PrimaryEffectHack(initPlan.Initial);
                    }
                    Debug.Log("test");
                    return initPlan;
                }
                
            }

            // Reset Cache
            GroundActionFactory.Reset();
            CacheMaps.Reset();

            GroundActionFactory.PopulateGroundActions(domain, problem);

            // Remove Irrelevant Actions (those which require an adjacent edge but which does not exist. In Refactoring--> make any static
            Debug.Log("removing irrelevant actions");
            var adjInitial = initPlan.Initial.Predicates.Where(state => state.Name.Equals("adjacent"));
            var replacedActions = new List<IOperator>();
            foreach (var ga in GroundActionFactory.GroundActions)
            {
                // If this action has a precondition with name adjacent this is not in initial state, then it's impossible. True ==> impossible. False ==> OK!
                var isImpossible = ga.Preconditions.Where(pre => pre.Name.Equals("adjacent") && pre.Sign).Any(pre => !adjInitial.Contains(pre));
                if (isImpossible)
                    continue;
                replacedActions.Add(ga);
            }
            GroundActionFactory.Reset();
            GroundActionFactory.GroundActions = replacedActions;
            GroundActionFactory.GroundLibrary = replacedActions.ToDictionary(item => item.ID, item => item);


            CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
            CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, problem.Goal);


            // Detect Statics
            Debug.Log("Detecting Statics");
            GroundActionFactory.DetectStatics(CacheMaps.CausalMap, CacheMaps.ThreatMap);


            Debug.Log("Caching Heuristic costs");
            CacheMaps.CacheAddReuseHeuristic(initPlan.Initial);

            // Recreate Initial Plan
            initPlan = PlannerScheduler.CreateInitialPlan(PF);

            return initPlan;
        }

        public List<IObject> GetObjects()
        {
            var locationHost = GameObject.FindGameObjectWithTag("Locations");
            var locations = Enumerable.Range(0, locationHost.transform.childCount).Select(i => locationHost.transform.GetChild(i));
            var actorHost = GameObject.FindGameObjectWithTag("ActorHost");
            var actors = Enumerable.Range(0, actorHost.transform.childCount).Select(i => actorHost.transform.GetChild(i));

            // Calculate Objects
            var objects = new List<IObject>();
            foreach (var location in locations)
            {
                objects.Add(new Obj(location.name, "Location") as IObject);
            }
            foreach (var actor in actors)
            {
                var superordinateTypes = GetSuperOrdinateTypes(actor.tag);
                objects.Add(new Obj(actor.name, actor.tag) as IObject);
            }
            return objects;
        }

        public List<string> GetSuperOrdinateTypes(string subtype)
        {
            var parentGo = GameObject.Find(subtype).transform;

            var superOrdinateTypes = new List<string>();
            superOrdinateTypes.Add(subtype);
            while (true)
            {
                parentGo = parentGo.parent;
                var parentName = parentGo.name;
                if (parentName.Equals("TypeHierarchy"))
                {
                    break;
                }
                superOrdinateTypes.Add(parentName);
            }

            return superOrdinateTypes;
        }

        /// <summary>
        /// Given a primary effect (one that is not the effect of a primitive step), calculate heuristic value.
        /// Let that heuristic value be the shortest (height) step that can contribute, plus all of its preconditions.
        /// Recursively, if any of its preconditions are primary effects, then repeat until we have either a step that is true in the initial state or has no primary effects as preconditions.
        /// </summary>
        /// <param name="InitialState"></param>
        /// <param name="primaryEffect"></param>
        /// <returns></returns>
        public static void PrimaryEffectHack(IState InitialState)
        {
            var initialMap = new Dictionary<Literal, int>();
            var primaryEffectsInInitialState = new List<IPredicate>();
            foreach(var item in InitialState.Predicates)
            {
                if (IsPrimaryEffect(item))
                {
                    primaryEffectsInInitialState.Add(item);
                    initialMap[new Literal(item)] = 0;
                }
            }

            var heurDict = PrimaryEffectRecursiveHeuristicCache(initialMap, primaryEffectsInInitialState);

            foreach(var keyvalue in heurDict)
            {
                HeuristicMethods.visitedPreds[keyvalue.Key] = keyvalue.Value;
            }
        }

        private static Dictionary<Literal, int> PrimaryEffectRecursiveHeuristicCache(Dictionary<Literal, int> currentMap, List<IPredicate> InitialConditions)
        {
            var initiallyRelevant = new List<IOperator>();
            var CompositeOps = GroundActionFactory.GroundActions.Where(act => act.Height > 0);
            foreach(var compOp in CompositeOps)
            {
                var initiallySupported = true;
                foreach(var precond in compOp.Preconditions)
                {
                    if (IsPrimaryEffect(precond))
                    {
                        // then this is a primary effect.
                        if (!InitialConditions.Contains(precond))
                        {
                            initiallySupported = false;
                            break;
                        }
                    }
                }
                if (initiallySupported)
                {
                    initiallyRelevant.Add(compOp);
                }
            }
           
            // a boolean tag to decide whether to continue recursively. If checked, then there is some new effect that isn't in initial conditions.
            bool toContinue = false;

            // for each step whose preconditions are executable given the initial conditions
            foreach (var newStep in initiallyRelevant)
            {
                // sum_{pre in newstep.preconditions} currentMap[pre]
                int thisStepsValue = 0;
                foreach (var precon in newStep.Preconditions)
                {
                    var preLiteral = new Literal(precon);
                    if (IsPrimaryEffect(precon))
                    {
                        thisStepsValue += currentMap[preLiteral];
                    }
                    else
                    {
                        thisStepsValue += HeuristicMethods.visitedPreds[preLiteral];
                    }
                }

                foreach (var eff in newStep.Effects)
                {
                    var effLiteral = new Literal(eff);
                    if (!IsPrimaryEffect(eff))
                    {
                        continue;
                    }

                    // ignore effects we've already seen; these occur "earlier" in planning graph
                    if (currentMap.ContainsKey(effLiteral))
                        continue;

                    // If we make it this far, then we've reached an unexplored literal effect
                    toContinue = true;

                    // The current value of this effect is 1 (this new step) + the sum of the preconditions of this step in the map.
                    currentMap[effLiteral] = 1 + thisStepsValue;

                    // Add this effect to the new initial Condition for subsequent round
                    InitialConditions.Add(eff);
                }
            }

            // Only continue recursively if we've explored a new literal effect. Pass the map along to maintain a global item.
            if (toContinue)
                return PrimaryEffectRecursiveHeuristicCache(currentMap, InitialConditions);

            // Otherwise, return our current map
            return currentMap;

        }

        public static bool IsPrimaryEffect(IPredicate pred)
        {
            return (pred.Name.Equals("obs") || pred.Name.Equals("obs-starts"));
        }

    }

}