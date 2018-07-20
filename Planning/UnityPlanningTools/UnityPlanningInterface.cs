using BoltFreezer.CacheTools;
using BoltFreezer.Camera;
using BoltFreezer.FileIO;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanSpace;
using BoltFreezer.PlanTools;
using BoltFreezer.Scheduling;
using CompilationNamespace;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PlanningNamespace
{

    [ExecuteInEditMode]
    public class UnityPlanningInterface : MonoBehaviour
    {

        public UnityGroundActionFactory UGAF;
        public UnityProblemCompiler UPC;
        public CacheManager cacheManager;
        public bool UseCompositeSteps;
        public bool makePlan;
        public bool DeCacheIt = false;
        public bool savePlan;
        public int retrievePlan;
        public bool getPlan;
        public bool decachePlan = false;
        public bool justCacheMapsAndEffort = false;
        public int heightMax = 1;
        public float cutoffTime = 10000;

        public List<string> PlanSteps;
        public List<IPlanStep> Plan;
        public Dictionary<int, int> MergeManager;

       // private List<string> GroundSteps;
        private SavedPlans SavedPlansComponent;

        public void Awake()
        {
            makePlan = false;
            savePlan = false;
            getPlan = false;
            retrievePlan = 0;
            SavedPlansComponent = this.gameObject.GetComponent<SavedPlans>();
        }

        // Update is called once per frame
        void Update()
        {
            if (makePlan)
            {
                makePlan = false;
                PrepareAndRun();
            }

            if (savePlan)
            {
                savePlan = false;
                if (SavedPlansComponent == null) 
                    SavedPlansComponent = this.gameObject.GetComponent<SavedPlans>();

                SavedPlansComponent.AddPlan(PlanSteps, Plan);
            }
            if (getPlan)
            {
                getPlan = false;
                if (SavedPlansComponent == null)
                    SavedPlansComponent = this.gameObject.GetComponent<SavedPlans>();

                if (retrievePlan <= SavedPlans.Saved.Count() - 1)
                {
                    var tup = SavedPlansComponent.Retrieve(retrievePlan);
                    PlanSteps = tup.First;
                    Plan = tup.Second;
                }
            }

            if (decachePlan)
            {
                decachePlan = false;
                Plan = cacheManager.DecachePlan();
                PlanSteps = new List<string>();
                //Plan = plan.Orderings.TopoSort(plan.InitialStep).ToList();
                //MergeManager = plan.MM.ToRootMap();
                foreach (var step in Plan)
                {

                    PlanSteps.Add(step.ToString());
                }
            }

            
        }

        public static void AddObservedNegativeConditions(UnityProblemCompiler UPC)
        {
            foreach (var ga in GroundActionFactory.GroundActions)
            {
                foreach (var precon in ga.Preconditions)
                {
                    // if the precon is signed positive, ignore
                    if (precon.Sign)
                    {
                        continue;
                    }
                    // if initially the precondition reveresed is true, ignore
                    if (UPC.initialPredicateList.Contains(precon.GetReversed()))
                    {
                        continue;
                    }
                    
                    // then this precondition is negative and its positive correlate isn't in the initial state
                    var obsPred = new Predicate("obs", new List<ITerm>() { precon as ITerm }, true);

                    if (UPC.initialPredicateList.Contains(obsPred as IPredicate))
                    {
                        continue;
                    }

                    UPC.initialPredicateList.Add(obsPred as IPredicate);
                }
            }
        }

        public void PrepareAndRun()
        {
            Parser.path = @"D:\documents\frostbow\";
            UPC.ReadProblem();

            if (DeCacheIt)
            {
               // DeCacheIt = false;
                cacheManager.DeCacheIt();
            }
            else if (justCacheMapsAndEffort)
            {
                cacheManager.DecacheSteps();
                //AddObservedNegativeConditions(UPC);

                CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
                CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, UPC.goalPredicateList);
                CacheMaps.CacheAddReuseHeuristic(new State(UPC.initialPredicateList) as IState);
                CacheMaps.PrimaryEffectHack(new State(UPC.initialPredicateList) as IState);

                cacheManager.justCacheMapsAndEffort = true;
                cacheManager.CacheIt();
                cacheManager.justCacheMapsAndEffort = false;
                return;
            }
            else
            {
                UGAF.PreparePlanner(true);
                GroundActionFactory.GroundActions = new HashSet<IOperator>(GroundActionFactory.GroundActions).ToList();
               // AddObservedNegativeConditions(UPC);
                UGAF.CreateSteps(UPC, heightMax);

                cacheManager.CacheIt();
            }

            var initialPlan = PlannerScheduler.CreateInitialPlan(UPC.initialPredicateList, UPC.goalPredicateList);
            
        
            Debug.Log("Planner and initial plan Prepared");

            // MW-Loc-Conf
           // var solution = Run(initialPlan, new ADstar(false), new E0(new NumOpenConditionsHeuristic(), true), cutoffTime);
            var solution = Run(initialPlan, new ADstar(false), new E0(new AddReuseHeuristic(), true), cutoffTime);
            //var solution = Run(initialPlan, new ADstar(false), new E3(new AddReuseHeuristic()), cutoffTime);
            //var solution = Run(initPlan, new BFS(), new Nada(new ZeroHeuristic()), 20000);
            if (solution != null)
            {
                var savePath = @"D:\documents\frostbow\" + @"Results\" + "UnityBlocksWorld" + @"\Solutions\";
                Directory.CreateDirectory(savePath);

                //Debug.Log(solution.ToStringOrdered());
                PlanSteps = new List<string>();
                Plan = new List<IPlanStep>();
                var planSchedule = solution as PlanSchedule;
                MergeManager = planSchedule.MM.ToRootMap();
                foreach (var step in solution.Orderings.TopoSort(solution.InitialStep))
                {
                    var cps = step as CamPlanStep;
                    if (cps != null)
                    {
                        foreach (var seg in cps.TargetDetails.ActionSegs)
                        {
                            if (MergeManager.ContainsKey(seg.ActionID))
                            {
                                seg.ActionID = MergeManager[seg.ActionID];
                            }
                        }
                    }
                    Plan.Add(step);
                    PlanSteps.Add(step.ToString());

                    BinarySerializer.SerializeObject(savePath + "PlanSteps", Plan);
                    Debug.Log(step);
                }
            }
            else
            {
                
                Debug.Log("No good");
            }

            //BoltFreezer.Utilities.Logger.WriteItemsFromDatabaseToFile("HeuristicOld");
         //   BoltFreezer.Utilities.Logger.WriteItemsFromDatabaseToFile("HeuristicNew");
        }

        

        public IPlan Run(IPlan initPlan, ISearch SearchMethod, ISelection SelectMethod, float cutoff)
        {
            var directory = Parser.GetTopDirectory() + "/Results/";
            System.IO.Directory.CreateDirectory(directory);
            var POP = new PlannerScheduler(initPlan.Clone() as IPlan, SelectMethod, SearchMethod)
            {
                directory = directory,
                problemNumber = 0
            };
            Debug.Log("Running plan-search");
            var Solutions = POP.Solve(1, cutoff);
            if (Solutions != null)
            {
                return Solutions[0];
            }
            Debug.Log(string.Format("explored: {0}, expanded: {1}", POP.Open, POP.Expanded));
            return null;
        }
        
    }
}