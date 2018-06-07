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

            long before = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            before = watch.ElapsedMilliseconds;
            if (planschedule.Cntgs.HasFault(plan.Orderings))
            {
                LogTime("CheckFaults", watch.ElapsedMilliseconds - before);
                return;
            }
            LogTime("CheckFaults", watch.ElapsedMilliseconds - before);

            base.Insert(plan);
        }

        public new void AddStep(IPlan plan, OpenCondition oc)
        {
            long before = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var cndt in CacheMaps.GetCndts(oc.precondition))
            {
                if (cndt == null)
                    continue;
                if (cndt.Height == 0)
                {
                    continue;
                }

                before = watch.ElapsedMilliseconds;
                
                
                var planClone = plan.Clone() as PlanSchedule;
                IPlanStep newStep;
                if (cndt.Height > 0)
                {
                    //continue;
                    var compCndt = cndt as CompositeSchedule;
                    newStep = new CompositeSchedulePlanStep(compCndt.Clone() as IComposite, compCndt.Cntgs)
                    {
                        Depth = oc.step.Depth
                    };
                    
                }
                else
                {
                    // only add composite steps...
                    //continue;
                    newStep = new PlanStep(cndt.Clone() as IOperator)
                    {
                        Depth = oc.step.Depth
                    };
                }
                LogTime("CloneCndt", watch.ElapsedMilliseconds - before);

                

                before = watch.ElapsedMilliseconds;
                planClone.Insert(newStep);
                LogTime("InsertDecomp", watch.ElapsedMilliseconds - before);

                //newStep.Height = cndt.Height;
                

               // planClone.Insert(newStep);
                

                before = watch.ElapsedMilliseconds;
                planClone.Repair(oc, newStep);
                LogTime("RepairDecomp", watch.ElapsedMilliseconds - before);

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
                

                before = watch.ElapsedMilliseconds;
                planClone.DetectThreats(newStep);
                LogTime("DetectThreats", watch.ElapsedMilliseconds - before);

                before = watch.ElapsedMilliseconds;
                Insert(planClone);
                LogTime("InsertPlan", watch.ElapsedMilliseconds - before);
            }
        }

    }
    
}