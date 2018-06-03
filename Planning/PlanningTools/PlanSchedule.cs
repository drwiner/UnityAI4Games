using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoltFreezer.PlanTools;
using System;
using BoltFreezer.Interfaces;
using BoltFreezer.Utilities;

namespace PlanningNamespace
{
    [Serializable]
    public class PlanSchedule : Plan, IPlan
    {

        public Schedule<IPlanStep> Cntgs;

        public PlanSchedule() : base()
        {
            Cntgs = new Schedule<IPlanStep>();
        }

        public PlanSchedule(IPlan plan, Schedule<IPlanStep> cntgs) : base(plan.Steps, plan.Initial, plan.Goal, plan.InitialStep, plan.GoalStep, plan.Orderings, plan.CausalLinks, plan.Flaws)
        {
            Cntgs = cntgs;
        }

        public PlanSchedule(IPlan plan, HashSet<Tuple<IPlanStep, IPlanStep>> cntgs) : base(plan.Steps, plan.Initial, plan.Goal, plan.InitialStep, plan.GoalStep, plan.Orderings, plan.CausalLinks, plan.Flaws)
        {
            Cntgs = new Schedule<IPlanStep>(cntgs);
        }
        public PlanSchedule(IPlan plan, List<Tuple<IPlanStep, IPlanStep>> cntgs) : base(plan.Steps, plan.Initial, plan.Goal, plan.InitialStep, plan.GoalStep, plan.Orderings, plan.CausalLinks, plan.Flaws)
        {
            Cntgs = new Schedule<IPlanStep>(cntgs);
        }

        public new void InsertDecomp(ICompositePlanStep newStep)
        {
            Decomps += 1;
            var IDMap = new Dictionary<int, IPlanStep>();

            // Clone, Add, and Order Initial step
            var dummyInit = newStep.InitialStep.Clone() as IPlanStep;
            dummyInit.Depth = newStep.Depth;
            IDMap[newStep.InitialStep.ID] = dummyInit;
            Steps.Add(dummyInit);
            Orderings.Insert(InitialStep, dummyInit);
            Orderings.Insert(dummyInit, GoalStep);

            // Clone, Add, and order Goal step
            var dummyGoal = newStep.GoalStep.Clone() as IPlanStep;
            dummyGoal.Depth = newStep.Depth;
            dummyGoal.InitCndt = dummyInit;
            InsertPrimitiveSubstep(dummyGoal, dummyInit.Effects, true);
            IDMap[newStep.GoalStep.ID] = dummyGoal;
            Orderings.Insert(dummyInit, dummyGoal);


            var newStepCopy = new PlanStep(new Operator(newStep.Action.Predicate as Predicate, new List<IPredicate>(), new List<IPredicate>()));
            Steps.Add(newStepCopy);
            //  orderings.Insert(dummyInit, newStepCopy);
            //  orderings.Insert(newStepCopy, dummyGoal);
            newStepCopy.Height = newStep.Height;
            var newSubSteps = new List<IPlanStep>();

            foreach (var substep in newStep.SubSteps)
            {
                // substep is either a IPlanStep or ICompositePlanStep
                if (substep.Height > 0)
                {
                    var compositeSubStep = new CompositeSchedulePlanStep(substep.Clone() as IPlanStep)
                    {
                        Depth = newStep.Depth + 1
                    };

                    Orderings.Insert(compositeSubStep.GoalStep, dummyGoal);
                    Orderings.Insert(dummyInit, compositeSubStep.InitialStep);
                    IDMap[substep.ID] = compositeSubStep;
                    compositeSubStep.InitialStep.InitCndt = dummyInit;
                    newSubSteps.Add(compositeSubStep);
                    Insert(compositeSubStep);
                    // Don't bother updating hdepth yet because we will check on recursion
                }
                else
                {
                    var newsubstep = new PlanStep(substep.Clone() as IPlanStep)
                    {
                        Depth = newStep.Depth + 1
                    };
                    Orderings.Insert(newsubstep, dummyGoal);
                    Orderings.Insert(dummyInit, newsubstep);
                    IDMap[substep.ID] = newsubstep;
                    newSubSteps.Add(newsubstep);
                    newsubstep.InitCndt = dummyInit;
                    InsertPrimitiveSubstep(newsubstep, dummyInit.Effects, false);

                    if (newsubstep.Depth > Hdepth)
                    {
                        Hdepth = newsubstep.Depth;
                    }
                }
            }

            foreach (var tupleOrdering in newStep.SubOrderings)
            {

                // Don't bother adding orderings to dummies
                if (tupleOrdering.First.Equals(newStep.InitialStep))
                    continue;
                if (tupleOrdering.Second.Equals(newStep.GoalStep))
                    continue;

                var head = IDMap[tupleOrdering.First.ID];
                var tail = IDMap[tupleOrdering.Second.ID];
                if (head.Height > 0)
                {
                    // can you pass it back?
                    var temp = head as ICompositePlanStep;
                    head = temp.GoalStep as IPlanStep;
                }
                if (tail.Height > 0)
                {
                    var temp = tail as ICompositePlanStep;
                    tail = temp.InitialStep as IPlanStep;
                }
                Orderings.Insert(head, tail);
            }

            // in this world, all composite plan steps are composite schedule plan steps.
            var schedulingStepComponent = newStep as CompositeSchedulePlanStep;
            foreach (var cntg in schedulingStepComponent.Cntgs)
            {
                var head = IDMap[cntg.First.ID];
                var tail = IDMap[cntg.Second.ID];
                if (head.Height > 0)
                {
                    // how do we describe a composite as being contiguous with another step?
                    var temp = head as ICompositePlanStep;
                    head = temp.GoalStep as IPlanStep;
                }
                if (tail.Height > 0)
                {
                    var temp = tail as ICompositePlanStep;
                    tail = temp.InitialStep as IPlanStep;
                }
                Cntgs.Insert(head, tail);
                // also add orderings just in case
                Orderings.Insert(head, tail);
            }

            foreach (var clink in newStep.SubLinks)
            {
                var head = IDMap[clink.Head.ID];
                var tail = IDMap[clink.Tail.ID];
                if (head.Height > 0)
                {
                    var temp = head as CompositePlanStep;
                    head = temp.GoalStep as IPlanStep;
                }
                if (tail.Height > 0)
                {
                    var temp = tail as CompositePlanStep;
                    tail = temp.InitialStep as IPlanStep;
                }

                var newclink = new CausalLink<IPlanStep>(clink.Predicate, head, tail);
                CausalLinks.Add(newclink);
                Orderings.Insert(head, tail);

                // check if this causal links is threatened by a step in subplan
                foreach (var step in newSubSteps)
                {
                    // Prerequisite criteria 1
                    if (step.ID == head.ID || step.ID == tail.ID)
                    {
                        continue;
                    }

                    // Prerequisite criteria 2
                    if (!CacheMaps.IsThreat(clink.Predicate, step))
                    {
                        continue;
                    }

                    // If the step has height, need to evaluate differently
                    if (step.Height > 0)
                    {
                        var temp = step as ICompositePlanStep;
                        if (Orderings.IsPath(head, temp.InitialStep))
                        {
                            continue;
                        }
                        if (Orderings.IsPath(temp.GoalStep, tail))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (Orderings.IsPath(head, step))
                        {
                            continue;
                        }
                        if (Orderings.IsPath(step, tail))
                        {
                            continue;
                        }
                    }

                    if (step.Height > 0)
                    {
                        // Then we need to dig deeper to find the step that threatens
                        DecomposeThreat(clink, step as ICompositePlanStep);
                    }
                    else
                    {
                        Flaws.Add(new ThreatenedLinkFlaw(newclink, step));
                    }
                }
            }


            // This is needed because we'll check if these substeps are threatening links
            newStep.SubSteps = newSubSteps;
            newStep.InitialStep = dummyInit;
            newStep.GoalStep = dummyGoal;

            foreach (var pre in newStep.OpenConditions)
            {
                Flaws.Add(this, new OpenCondition(pre, dummyInit as IPlanStep));
            }
        }

        /// <summary>
        /// throws all cntgs, orderings, and causal links containing step1 swaps with step2
        /// </summary>
        /// <param name="step1"></param>
        /// <param name="step2"></param>
        public void MergeSteps(IPlanStep step1, IPlanStep step2)
        {

            var newCntgs = new List<Tuple<IPlanStep, IPlanStep>>();
            foreach (var cntg in Cntgs.edges)
            {
                if (cntg.First.Equals(step1))
                {
                    newCntgs.Add(new Tuple<IPlanStep, IPlanStep>(step2, cntg.Second));
                }
                else if (cntg.Second.Equals(step1))
                {
                    newCntgs.Add(new Tuple<IPlanStep, IPlanStep>(cntg.First, step2));
                }
                else
                {
                    newCntgs.Add(cntg);
                }
            }
            var newOrderings = new List<Tuple<IPlanStep, IPlanStep>>();

            foreach (var cntg in Orderings.edges)
            {
                if (cntg.First.Equals(step1))
                {
                    newOrderings.Add(new Tuple<IPlanStep, IPlanStep>(step2, cntg.Second));
                }
                else if (cntg.Second.Equals(step1))
                {
                    newOrderings.Add(new Tuple<IPlanStep, IPlanStep>(cntg.First, step2));
                }
                else
                {
                    newOrderings.Add(cntg);
                }
            }
            var newLinks = new List<CausalLink<IPlanStep>>();
            foreach (var cntg in CausalLinks)
            {
                if (cntg.Head.Equals(step1))
                {
                    newLinks.Add(new CausalLink<IPlanStep>(cntg.Predicate, step2, cntg.Tail));
                }
                else if (cntg.Tail.Equals(step1))
                {
                    newLinks.Add(new CausalLink<IPlanStep>(cntg.Predicate, cntg.Head, step2));
                }
                else
                {
                    newLinks.Add(cntg);
                }
            }

            Orderings.edges = new HashSet<Tuple<IPlanStep, IPlanStep>>();
            foreach (var newordering in newOrderings)
            {
                Orderings.Insert(newordering.First, newordering.Second);
            }

            Cntgs.edges = new HashSet<Tuple<IPlanStep, IPlanStep>>(newCntgs);
            CausalLinks = newLinks;

            var openConditions = new List<OpenCondition>();
            foreach (var oc in Flaws.OpenConditions)
            {
                if (!oc.step.Equals(step1))
                {
                    openConditions.Add(oc);
                }
            }
            Flaws.OpenConditions = openConditions;
            steps.Remove(step1);
            // can there be a threatened causal link flaw? it would be prioritize and addressed prior
        }

        public void RepairWithComposite(OpenCondition oc, CompositeSchedulePlanStep repairStep)
        {

            var needStep = Find(oc.step);
            if (!needStep.Name.Equals("DummyGoal") && !needStep.Name.Equals("DummyInit"))
                needStep.Fulfill(oc.precondition);

            // need to merge all steps that are being connected by this predicate:
            if (oc.precondition.Name.Equals("obs-starts"))
            {
                var stepThatNeedsToBeMerged = oc.precondition.Terms[0];
                IPlanStep ReferencedStep1 = new PlanStep();
                IPlanStep ReferencedStep2 = new PlanStep();
                foreach (var step in Steps)
                {
                    if (step.Action.ID.ToString().Equals(stepThatNeedsToBeMerged))
                    {
                        if (repairStep.SubSteps.Contains(step))
                            ReferencedStep1 = step;

                        else
                            ReferencedStep2 = step;
                    }
                }
                if (ReferencedStep1.Name.Equals("") || ReferencedStep2.Name.Equals(""))
                {
                    Debug.Log("never found steps to merge");
                    throw new System.Exception();
                }
                if (ReferencedStep1.OpenConditions.Count > ReferencedStep2.OpenConditions.Count)
                    MergeSteps(ReferencedStep1, ReferencedStep2);
                else
                    MergeSteps(ReferencedStep2, ReferencedStep1);
            }

            orderings.Insert(repairStep.GoalStep as IPlanStep, needStep);
            var clink = new CausalLink<IPlanStep>(oc.precondition as Predicate, repairStep.GoalStep as IPlanStep, needStep);
            causalLinks.Add(clink);
        }

        public new System.Object Clone()
        {
            var basePlanClone = base.Clone() as Plan;
            var newPlan = new PlanSchedule(basePlanClone, new HashSet<Tuple<IPlanStep, IPlanStep>>(Cntgs.edges));
            return newPlan;
        }
    }
}
