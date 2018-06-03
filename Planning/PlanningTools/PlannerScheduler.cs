using BoltFreezer.CacheTools;
using BoltFreezer.DecompTools;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanSpace;
using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanningNamespace
{
    [Serializable]
    public class PlannerScheduler : PlanSpacePlanner, IPlanner
    {

        public PlannerScheduler(IPlan initialPlan, ISelection _selection, ISearch _search) : base(initialPlan, _selection, _search, false)
        {

        }

        public new static IPlan CreateInitialPlan(ProblemFreezer PF)
        {
            return CreateInitialPlan(PF.testProblem);
        }

        public static IPlan CreateInitialPlan(Problem problem)
        {
            var initialPlan = new PlanSchedule(new Plan(new State(problem.Initial) as IState, new State(problem.Goal) as IState), new List<Tuple<IPlanStep, IPlanStep>>());
            foreach (var goal in problem.Goal)
                initialPlan.Flaws.Add(initialPlan, new OpenCondition(goal, initialPlan.GoalStep as IPlanStep));
            initialPlan.Orderings.Insert(initialPlan.InitialStep, initialPlan.GoalStep);
            return initialPlan;
        }

        public static IPlan CreateInitialPlan(List<IPredicate> Initial, List<IPredicate> Goal)
        {
            var initialPlan = new PlanSchedule(new Plan(new State(Initial) as IState, new State(Goal) as IState), new List<Tuple<IPlanStep, IPlanStep>>());
            foreach (var goal in Goal)
                initialPlan.Flaws.Add(initialPlan, new OpenCondition(goal, initialPlan.GoalStep as IPlanStep));
            initialPlan.Orderings.Insert(initialPlan.InitialStep, initialPlan.GoalStep);
            return initialPlan;
        }

        public new void Insert(IPlan plan)
        {
            var planschedule = plan as PlanSchedule;
            if (planschedule.Cntgs.HasFault(plan.Orderings))
            {
                return;
            }
            base.Insert(plan);
        }

        public new void AddStep(IPlan plan, OpenCondition oc)
        {

            foreach (var cndt in CacheMaps.GetCndts(oc.precondition))
            {
                if (cndt == null)
                    continue;

                var planClone = plan.Clone() as IPlan;
                IPlanStep newStep;
                if (cndt.Height > 0)
                {
                    //continue;
                    var compCndt = cndt.Clone() as CompositeSchedule;
                    newStep = new CompositeSchedulePlanStep(compCndt)
                    {
                        Depth = oc.step.Depth
                    };
                }
                else
                {
                    newStep = new PlanStep(cndt.Clone() as IOperator)
                    {
                        Depth = oc.step.Depth
                    };
                }
                //newStep.Height = cndt.Height;
                planClone.Insert(newStep);
                planClone.Repair(oc, newStep);

                // check if inserting new Step (with orderings given by Repair) add cndts/risks to existing open conditions, affecting their status in the heap
                //planClone.Flaws.UpdateFlaws(planClone, newStep);

                if (oc.isDummyGoal)
                {
                    if (newStep.Height > 0)
                    {
                        var compNewStep = newStep as CompositeSchedulePlanStep;
                        planClone.Orderings.Insert(oc.step.InitCndt, compNewStep.InitialStep);
                    }
                    else
                    {
                        planClone.Orderings.Insert(oc.step.InitCndt, newStep);
                    }
                }
                planClone.DetectThreats(newStep);
                Insert(planClone);
            }
        }

    }
    
}