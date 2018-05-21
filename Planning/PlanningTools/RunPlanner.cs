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
    public class RunPlanner : MonoBehaviour
    {

        public bool makePlan;
        public bool savePlan;
        public int retrievePlan;
        public bool getPlan;

        public List<string> PlanSteps;

        private List<string> GroundSteps;
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
            var initPlan = PreparePlanner();
            Debug.Log("Planner and initial plan Prepared");

            // MW-Loc-Conf
            var solution = Run(initPlan, new ADstar(false), new E0(new AddReuseHeuristic(), true), 60000f);

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

        public IPlan PreparePlanner()
        {
            Parser.path = "/";

            // Update Domain Operators
            var domainOperatorComponent = GameObject.FindGameObjectWithTag("ActionHost").GetComponent<DomainOperators>();
            domainOperatorComponent.Reset();

            // Read and Create Problem
            var problem = CreateProblem(domainOperatorComponent.DomainOps);
            problem.ToString();
            GroundActionFactory.Reset();
            CacheMaps.Reset();

            // Create Domain
            
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

            // Create Problem Freezer.
            var PF = new ProblemFreezer("Unity", "", domain, problem);
            // Create Initial Plan
            var initPlan = PlanSpacePlanner.CreateInitialPlan(PF);

            GroundActionFactory.PopulateGroundActions(domain, problem);

            // Remove Irrelevant Actions (those which require an adjacent edge but which does not exist. In Refactoring--> make any static
            Debug.Log("removing irrelevant actions");
            GroundSteps = new List<string>();
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



          //  // Create Composites here. // Compose HTNs
            //var CompositeMethods = BlockWorldHTNs.ReadCompositeOperators();
            //Composite.ComposeHTNs(1, CompositeMethods);
          //  // Only need to do this if you added composite methods
           // Debug.Log("Caching maps");
           // CacheMaps.Reset();
           // CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
           // CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, initPlan.Goal.Predicates);

            Debug.Log("Caching Heuristic costs");
            CacheMaps.CacheAddReuseHeuristic(initPlan.Initial);

            // Recreate Initial Plan
            initPlan = PlanSpacePlanner.CreateInitialPlan(PF);

            return initPlan;
        }

        public IPlan Run(IPlan initPlan, ISearch SearchMethod, ISelection SelectMethod, float cutoff)
        {
            var POP = new PlanSpacePlanner(initPlan, SelectMethod, SearchMethod, false)
            {
                directory = "/",
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

        public Problem CreateProblem(List<Operator> DomainOps)
        {
            var ProblemHost = GameObject.FindGameObjectWithTag("Problem");
            var problemComponent = ProblemHost.GetComponent<ProblemStates>();
            problemComponent.ReadProblem();
            var prob = new Problem("SteerProblem", "SteerProblem", "Unity", "", GetObjects(), problemComponent.initialPredicateList, problemComponent.goalPredicateList);
            return prob;
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
    }
}