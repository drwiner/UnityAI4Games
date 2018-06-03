using BoltFreezer.CacheTools;
using BoltFreezer.FileIO;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanSpace;
using BoltFreezer.PlanTools;

using System.Collections;
using System.Collections.Generic;
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
        public bool DeCacheIt;
        public bool savePlan;
        public int retrievePlan;
        public bool getPlan;

        public List<string> PlanSteps;

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

                SavedPlansComponent.AddPlan(PlanSteps);
            }
            if (getPlan)
            {
                getPlan = false;
                if (SavedPlansComponent == null)
                    SavedPlansComponent = this.gameObject.GetComponent<SavedPlans>();

                if (retrievePlan <= SavedPlansComponent.Saved.Count() - 1)
                {
                    PlanSteps = SavedPlansComponent.Retrieve(retrievePlan);
                }
            }

            
        }

        public void PrepareAndRun()
        {
            Parser.path = @"D:\documents\frostbow\";
            UPC.ReadProblem();

            if (DeCacheIt)
            {
                DeCacheIt = false;
                cacheManager.DeCacheIt();
                CacheMaps.CacheAddReuseHeuristic(new State(UPC.initialPredicateList) as IState);
                UnityGroundActionFactory.PrimaryEffectHack(new State(UPC.initialPredicateList) as IState);
            }
            else
            {
                UGAF.PreparePlanner(true);
                GroundActionFactory.GroundActions = new HashSet<IOperator>(GroundActionFactory.GroundActions).ToList();
                UnityGroundActionFactory.CreateSteps(UPC, UGAF.DecompositionSchemata);

                cacheManager.CacheIt();
            }

            var initialPlan = PlannerScheduler.CreateInitialPlan(UPC.initialPredicateList, UPC.goalPredicateList);

        
            Debug.Log("Planner and initial plan Prepared");

            // MW-Loc-Conf
            //var solution = Run(initPlan, new ADstar(false), new E0(new AddReuseHeuristic(), true), 60000f);
            var solution = Run(initialPlan, new ADstar(false), new E3(new AddReuseHeuristic()), 60000f);
            //var solution = Run(initPlan, new BFS(), new Nada(new ZeroHeuristic()), 20000);
            if (solution != null)
            {
                //Debug.Log(solution.ToStringOrdered());
                PlanSteps = new List<string>();
                foreach (var step in solution.Orderings.TopoSort(solution.InitialStep))
                {
                    Debug.Log(step);
                    PlanSteps.Add(step.ToString());
                }
            }
            else
            {
                
                Debug.Log("No good");
            }
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