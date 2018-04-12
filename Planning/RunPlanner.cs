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

        public bool getPlan;

        public List<string> PlanSteps;

        public List<string> GroundSteps;

        public void Awake()
        {
            getPlan = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (getPlan)
            {
                PrepareAndRun();
                getPlan = false;
            }
        }

        public void PrepareAndRun()
        {
            var initPlan = PreparePlanner();
            Debug.Log("Planner and initial plan Prepared");
            var solution = Run(initPlan, new ADstar(), new E3(new AddReuseHeuristic()), 10000);
            if (solution != null)
            {
                Debug.Log(solution.ToStringOrdered());
                PlanSteps = new List<string>();
                foreach (var step in solution.Orderings.TopoSort(solution.InitialStep))
                {
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

            var domainOperatorComponent = GameObject.FindGameObjectWithTag("ActionHost").GetComponent<DomainOperators>();
            domainOperatorComponent.Reset();

            var problem = CreateProblem(domainOperatorComponent.DomainOps);
            var domain = new Domain();
            var PF = new ProblemFreezer("Unity", "", domain, problem);
            var initPlan = PlanSpacePlanner.CreateInitialPlan(PF);

            // Assemble Operators
            var newOps = new List<IOperator>();
            foreach (var domainOp in domainOperatorComponent.DomainOps)
            {
                newOps.Add(domainOp as IOperator);
            }

            GroundActionFactory.PopulateGroundActions(newOps, problem);
            GroundSteps = new List<string>();
            foreach (var ga in GroundActionFactory.GroundActions)
            {
                GroundSteps.Add(ga.ToString());
            }
            CacheMaps.Reset();
            CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
            CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, initPlan.Goal.Predicates);
            GroundActionFactory.DetectStatics(CacheMaps.CausalMap, CacheMaps.ThreatMap);
            return initPlan;
        }

        public IPlan Run(IPlan initPlan, ISearch SearchMethod, ISelection SelectMethod, float cutoff)
        {
            var POP = new PlanSpacePlanner(initPlan, SelectMethod, SearchMethod, false)
            {
                directory = "/",
                problemNumber = 0
            };
            var Solutions = POP.Solve(1, cutoff);
            if (Solutions != null)
            {
                return Solutions[0];
            }
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
                objects.Add(new Obj(actor.name, actor.tag, superordinateTypes) as IObject);
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