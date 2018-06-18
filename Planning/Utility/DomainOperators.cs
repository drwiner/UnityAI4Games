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
        private List<Operator> domainOps;

        // Hides this as serializable field on game object
        public List<Operator> DomainOps {
            get { return domainOps;}
            set { domainOps = value; }
        }

        [SerializeField]
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
                if (!transform.GetChild(i).gameObject.activeSelf)
                {
                    continue;
                }
                var opParam = transform.GetChild(i).GetComponent<UnityActionOperator>();
                if (opParam == null)
                {
                    continue;
                }
                opParam.CreateOperator();
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