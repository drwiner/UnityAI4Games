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
using BoltFreezer.Scheduling;
using BoltFreezer.DecompTools;
using CompilationNamespace;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class UnityGroundActionFactory : MonoBehaviour
    {

        public bool compilePrimitiveSteps = false;
        //public bool compileCompositeSteps = false;
        //public bool regenerateInitialPlanWithComposite = false;
        //public bool checkEffects = false;

        public int PrimitiveSteps;
        public List<UnityTimelineDecomp> DecompositionSchemata;
       // public int CompositeSteps;
        //private List<IOperator> PrimitiveOps;
        //private List<IOperator> CompositeOps;

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
                PrimitiveSteps = GroundActionFactory.GroundActions.Count;
                //CompositeSteps = 0;
            }

            //if (compileCompositeSteps)
            //{
            //    compileCompositeSteps = false;
            //    CreateSteps(GameObject.FindGameObjectWithTag("ProblemHost").GetComponent<UnityProblemCompiler>(), 1);
            //}

            //if (checkEffects)
            //{
            //    checkEffects = false;
            //    foreach(var compstep in CompositeOps)
            //    {
            //        foreach(var effect in compstep.Effects)
            //        {
            //            if (effect.Name != "obs") // && effect.Name != "obs-starts")
            //            {
            //                continue;
            //            }
            //            Debug.Log(effect.ToString());
            //        }
            //    }
                // do we have what we need?
            //}

            //if (regenerateInitialPlanWithComposite)
            //{
            //    regenerateInitialPlanWithComposite = false;

            //    Parser.path = "/";
            //    var domainOperatorComponent = GameObject.FindGameObjectWithTag("ActionHost").GetComponent<DomainOperators>();
            //    domainOperatorComponent.Reset();
            //    var problem = CreateProblem(domainOperatorComponent.DomainOps);
            //    var domain = CreateDomain(domainOperatorComponent);
            //    var PF = new ProblemFreezer("Unity", "", domain, problem);
            //    var initPlan = PlannerScheduler.CreateInitialPlan(PF);
            //    CacheMaps.CacheAddReuseHeuristic(initPlan.Initial);
            //    PrimaryEffectHack(InitialPlan.Initial);
            //}

        }

        //public void CompileCompositeSteps(int heightMax)
        //{
            

        //    CompositeSteps = 0;
        //    CompositeOps = new List<IOperator>();
        //    foreach (var unitydecomp in DecompositionSchemata)
        //    {
        //        if (unitydecomp.NumGroundDecomps == 0)
        //        {
        //            unitydecomp.Read();
        //            unitydecomp.Assemble();
        //        }
        //    }
        //    // Now, all composite steps with height 1 are created, and the TimelineDecompositionHelper is loaded.\
        // //   for (int )
        //    var compositeSteps = GroundDecompositionsToCompositeSteps(DecompositionSchemata);
        //    foreach (var comp in compositeSteps)
        //    {
        //        CompositeOps.Add(comp as IOperator);
        //        CompositeSteps++;
        //    }
        //    AddCompositeStepsToGroundActionFactory(compositeSteps);
        //}


        public void CreateSteps(UnityProblemCompiler UPC, int heightMax)
        {
            DiscourseDecompositionHelper.SetCamsAndLocations(GameObject.FindGameObjectWithTag("CameraHost"), GameObject.FindGameObjectWithTag("Locations"));
            
            // Compile the Decomposition Schedule Schemata
            foreach (var unitydecomp in DecompositionSchemata)
            {
                unitydecomp.GroundDecomps = new List<TimelineDecomposition>();
                unitydecomp.reset = true;
                unitydecomp.Read();
                unitydecomp.Assemble();
                unitydecomp.NonGroundTimelineDecomposition();
                //unitydecomp.Filter();
                Debug.Log("Read and Assemble for unity decomp: " + unitydecomp.name);
            }
            
            // For each height
            for (int h = 0; h < heightMax; h++)
            {
                var newopsThisRound = new List<IOperator>();
                foreach(var utd in DecompositionSchemata)
                {
                    var td = utd.PartialDecomp;
                    var gdecomps = TimelineDecompositionHelper.Compose(h, td);
                    foreach (var gdecomp in gdecomps)
                    {
                        var csc = new CompositeScheduleComposer(utd, gdecomp);
                        var comp = csc.CreateCompositeSchedule();
                        if (comp.Effects.Count == 0)
                        {
                            Debug.Log("couldn't create " + comp.ToString());
                            continue;
                        }
                        comp.Height = h+1;
                        newopsThisRound.Add(comp as IOperator);
                    }
                }

                foreach(var op in newopsThisRound)
                {
                    GroundActionFactory.InsertOperator(op);
                }
                if (newopsThisRound.Count == 0)
                {
                    break;
                }
            }
            
            CacheMaps.Reset();
            CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
            CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, UPC.goalPredicateList);
            
            CacheMaps.PrimaryEffectHack(new State(UPC.initialPredicateList) as IState);

            //  var compositeSteps = GroundDecompositionsToCompositeSteps(DecompositionSchemata);
            // AddCompositeStepsToGroundActionFactory(UPC.initialPredicateList, UPC.goalPredicateList, compositeSteps);
        }

        //public static void AddCompositeStepsToGroundActionFactory(List<IPredicate> Initial, List<IPredicate> Goal, List<CompositeSchedule> compositeSteps)
        //{
        //    var originalOps = GroundActionFactory.GroundActions;
        //    //CacheMaps.CacheLinks(originalOps);
        //    var IOpList = new List<IOperator>();
        //    foreach (var compstep in compositeSteps)
        //    {
        //        var asIOp = compstep as IOperator;
        //        IOpList.Add(asIOp);
        //        GroundActionFactory.InsertOperator(asIOp);
        //    }

        //    // Update Heuristic value for primary effects.
        //    CacheMaps.PrimaryEffectHack(new State(Initial) as IState);

        //    // Amonst themselves
        //    CacheMaps.CacheLinks(IOpList);

        //    // as consequents to the originals
        //    CacheMaps.CacheLinks(originalOps, IOpList);

        //    // as antecedants to the originals
        //    CacheMaps.CacheLinks(IOpList, originalOps);

        //    // as antecedants to goal conditions
        //    CacheMaps.CacheGoalLinks(originalOps, Goal);
        //    CacheMaps.CacheGoalLinks(IOpList, Goal);
        //}

        //public void AddCompositeStepsToGroundActionFactory(List<CompositeSchedule> compositeSteps)
        //{
        //    var goalConditions = InitialPlan.GoalStep.Preconditions;
        //    var originalOps = GroundActionFactory.GroundActions;

        //    var IOpList = new List<IOperator>();
        //    foreach (var compstep in compositeSteps)
        //    {
        //        var asIOp = compstep as IOperator;
        //        IOpList.Add(asIOp);
        //        GroundActionFactory.InsertOperator(asIOp);
        //    }

        //    // Update Heuristic value for primary effects.
        //    PrimaryEffectHack(InitialPlan.Initial);

        //    // Amonst themselves
        //    CacheMaps.CacheLinks(IOpList);

        //    // as antecedants to the originals
        //    CacheMaps.CacheLinks(IOpList, originalOps);

        //    // as consequents to the originals
        //    CacheMaps.CacheLinks(originalOps, IOpList);

        //    // as antecedants to goal conditions
        //    CacheMaps.CacheGoalLinks(IOpList, goalConditions);

        //    // is is possible to have a new precondition here that is static? 
        //    /// this raises a larger point. 
        //    /// should we say that initially we observe the way the world is? (yes)
        //    /// should we create simple camera shots for conveying actions so that effects of actions are all observable?
        //    /// this is an experimental condition of sorts. In this case, there is no non-static condition that is not observable.
        //    /// therefore, (no), there is no need for (extra) statics.

        //    // There is also no need to cache addreuseheuristic again because primitive values.
        //    //CacheMaps.CacheAddReuseHeuristic(InitialPlan.Initial);
        //}

        public static List<CompositeSchedule> GroundDecompositionsToCompositeSteps(List<UnityTimelineDecomp> DecompositionSchemata)
        {
            List<CompositeSchedule> compositeSteps = new List<CompositeSchedule>();
            foreach (var decompschema in DecompositionSchemata)
            {
                foreach (var gdecomp in decompschema.GroundDecomps)
                {
                    // use the fabulaActionNameMap to reference action var names to substeps
                    //var obseffects = new List<Tuple<CamPlanStep, List<Tuple<double, double>>>>();
                    // keeping track of every action's percentage observance
                    var csc = new CompositeScheduleComposer(decompschema, gdecomp);
                    var comp = csc.CreateCompositeSchedule();

                    //var comp = GroundDecompToCompositeStep(decompschema, gdecomp);

                    //foreach(var tup in newLinksWithInitial)
                    //{
                    //    comp.SubLinks.Add(new CausalLink<IPlanStep>(tup.First, comp.InitialStep, tup.Second));
                    //}
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
                var stepTerm = new Term(step.Action.ID.ToString(), true)
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
            domain.AddTypePair("Walkable", "Location");
            domain.AddTypePair("Aux", "Location");
            domain.AddTypePair("", "SteeringAgent");
            domain.AddTypePair("", "Block");
            domain.AddTypePair("", "Walkable");
            domain.AddTypePair("", "Aux");


            return domain;
        }

        public static bool IsStatic(string s)
        {
            if (s.Equals("adjacent"))
            {
                return true;
            }
            if (s.Equals("placeable"))
            {
                return true;
            }
            return false;
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
                    if (HeuristicMethods.visitedPreds == null || HeuristicMethods.visitedPreds.Get(true).Count == 0)
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
            var adjInitial = initPlan.Initial.Predicates.Where(state => IsStatic(state.Name));
            var replacedActions = new List<IOperator>();
            foreach (var ga in GroundActionFactory.GroundActions)
            {
                // If this action has a precondition with name adjacent this is not in initial state, then it's impossible. True ==> impossible. False ==> OK!
                var isImpossible = ga.Preconditions.Where(pre => IsStatic(pre.Name) && pre.Sign).Any(pre => !adjInitial.Contains(pre));
                if (isImpossible)
                    continue;
                replacedActions.Add(ga);
            }
            GroundActionFactory.Reset();
            GroundActionFactory.GroundActions = replacedActions;
            GroundActionFactory.GroundLibrary = replacedActions.ToDictionary(item => item.ID, item => item);

            // Detect Statics
            Debug.Log("Detecting Statics");
            GroundActionFactory.DetectStatics();
            RemoveStaticPreconditions(GroundActionFactory.GroundActions);

            // Cache links, now not bothering with statics
            CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
            CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, problem.Goal);


            Debug.Log("Caching Heuristic costs");
            CacheMaps.CacheAddReuseHeuristic(initPlan.Initial);

            // Recreate Initial Plan
            initPlan = PlannerScheduler.CreateInitialPlan(PF);

            return initPlan;
        }

        public List<IObject> GetObjects()
        {
            var locationHost = GameObject.FindGameObjectWithTag("Locations");
            var locations = Enumerable.Range(0, locationHost.transform.childCount).Select(i => locationHost.transform.GetChild(i)).Where(item => item.gameObject.activeSelf);
            var actorHost = GameObject.FindGameObjectWithTag("ActorHost");
            var actors = Enumerable.Range(0, actorHost.transform.childCount).Select(i => actorHost.transform.GetChild(i));

            // Calculate Objects
            var objects = new List<IObject>();
            foreach (var location in locations)
            {
                objects.Add(new Obj(location.name, location.tag) as IObject);
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

        public static void RemoveStaticPreconditions(List<IOperator> groundActions)
        {
            foreach (var ga in groundActions)
            {
                List<IPredicate> newPreconds = new List<IPredicate>();
                foreach (var precon in ga.Preconditions)
                {
                    if (GroundActionFactory.Statics.Contains(precon))
                    {
                        continue;
                    }
                    //if (IsPrimaryEffect(precon))
                    //{
                    //    var termAsPred = precon.Terms[0] as IPredicate;
                    //    if (termAsPred != null)
                    //    {
                    //        if (GroundActionFactory.Statics.Contains(termAsPred))
                    //        {
                    //            continue;
                    //        }
                    //    }
                    //}
                    newPreconds.Add(precon);
                }

                ga.Preconditions = newPreconds;
            }
        }

    }

}