using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class DomainOperators : MonoBehaviour
    {

        private List<Operator> DomainOps;

        public List<string> OperatorNames;

        public bool reset;

        public void Awake()
        {
            Reset();
        }
        public void Update()
        {
            if (reset)
            {
                Reset();
            }

        }

        public void Reset()
        {
            DomainOps = new List<Operator>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var opParam = transform.GetChild(i).GetComponent<UnityActionOperator>();
                if (opParam == null)
                {
                    continue;
                }
                DomainOps.Add(opParam.ThisOp);
            }

            ExtractNames();
            reset = false;
        }

        public void ExtractNames()
        {
            OperatorNames = new List<string>();
            foreach (var op in DomainOps)
            {
                OperatorNames.Add(op.Predicate.ToString());
            }
        }

    }
}