using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNamespace
{

    [ExecuteInEditMode]
    public class SavedPlans : MonoBehaviour
    {

        public List<ListWrapper> Saved;

        public bool reset;


        public void Update()
        {
            if (reset)
            {
                reset = false;
                Saved = new List<ListWrapper>();
            }
        }

        public List<string> Retrieve(int index)
        {
            return Saved[index].Plan;
        }

        public void AddPlan(List<string> planToSave)
        {
            if (Saved == null)
            {
                Saved = new List<ListWrapper>();
            }
            Saved.Add(new ListWrapper(planToSave));
        }
    }


    [System.Serializable]
    public class ListWrapper
    {
        public List<string> Plan;
        public ListWrapper(List<string> plan)
        {
            Plan = plan;
        }
    }

}