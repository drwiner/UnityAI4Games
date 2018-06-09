using BoltFreezer.Interfaces;
using BoltFreezer.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNamespace
{

    [ExecuteInEditMode]
    public class SavedPlans : MonoBehaviour
    {

        public static List<ListWrapper> Saved;

        public bool reset;


        public void Update()
        {
            if (reset)
            {
                reset = false;
                Saved = new List<ListWrapper>();
            }
        }

        public Tuple<List<string>, List<IPlanStep>> Retrieve(int index)
        {
            return new Tuple<List<string>, List<IPlanStep>>(Saved[index].PlanSteps, Saved[index].Plan);
        }

        public void AddPlan(List<string> planToSave, List<IPlanStep> planStepsToSave)
        {
            if (Saved == null)
            {
                Saved = new List<ListWrapper>();
            }
            Saved.Add(new ListWrapper(planToSave, planStepsToSave));
        }
    }


    [System.Serializable]
    public class ListWrapper
    {
        public List<string> PlanSteps;
        public List<IPlanStep> Plan;
        public ListWrapper(List<string> plan, List<IPlanStep> _plan)
        {
            PlanSteps = plan;
            Plan = _plan;
        }
    }

}