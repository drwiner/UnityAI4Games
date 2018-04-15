using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class UnityActionOperator : MonoBehaviour
    {
        // A Unity wrapper for operators, give parameters from typed hierarchy, and write out preconditions and effects

        public string Name;

        [SerializeField]
        public List<GameObject> MutableParameters;

        [SerializeField]
        public List<string> MutablePreconditions;

        [SerializeField]
        public List<string> MutableEffects;

        [SerializeField]
        public List<NonEqualTuple> NonEqualityConstraints;

        [SerializeField]
        public List<string> UnityInstructions;

        [SerializeField]
        public string opString;

        private Operator thisOp;
		
		public bool reset = false;

        public Operator ThisOp
        {
            get { return thisOp; }
            set { thisOp = value; }
        }

        //private DomainOperators OperatorComponent;

        public void Awake()
        {
            Name = this.gameObject.name;
            //GetOperatorComponent();
            CreateOperator();
        }

        public void Update()
        {
            if (Name == "")
            {
                Name = this.gameObject.name;
            }
			
			if(reset){
				reset = false;
				CreateOperator();
			}
        }


        public void CreateOperator()
        {
            // Instantiate Terms
            var terms = new List<ITerm>();
            for (int i = 0; i < MutableParameters.Count; i++)
            {
                var go = MutableParameters[i];
                var term = new Term(i.ToString());
                term.Type = go.name;
                terms.Add(term as ITerm);
            }

            // Instantiate Preconditions
            var preconditions = new List<IPredicate>();
            foreach (var precon in MutablePreconditions)
            {
                preconditions.Add(ProcessStringItem(precon));
            }

            // Instantiate Effects
            var effects = new List<IPredicate>();
            foreach (var eff in MutableEffects)
            {
                effects.Add(ProcessStringItem(eff));
            }

            // Instantiate Operator
            thisOp = new Operator(new Predicate(Name, terms, true), preconditions, effects);
            Debug.Log(thisOp.Predicate);

            // Instantiate Nonequality constraints
            thisOp.NonEqualities = new List<List<ITerm>>();
            foreach (var nonequality in NonEqualityConstraints)
            {
                thisOp.NonEqualities.Add(new List<ITerm>() { terms[nonequality.first], terms[nonequality.second] });
            }
            opString = thisOp.ToString();
        }

        public Predicate MutableToPredicate(MutablePredicate precon, List<ITerm> terms)
        {
            var preconTerms = new List<ITerm>();
            foreach (var t in precon.terms)
            {
                preconTerms.Add(terms[t] as ITerm);
            }
            var newPrecon = new Predicate(precon.predicateName, preconTerms, precon.sign);
            return newPrecon;
        }

        public Predicate ProcessStringItem(string stringItem)
        {
            var signage = true;
            var stringArray = stringItem.Split(' ');
            if (stringArray[0].Equals("not"))
            {
                signage = false;
                stringArray = stringArray.Skip(1).ToArray();
            }
            var predName = stringArray[0];
            var terms = new List<ITerm>();
            foreach (var item in stringArray.Skip(1))
            {
                var param = MutableParameters[Int32.Parse(item)];
                var newObj = new Term(item)
                {
                    Type = param.name
                };
                terms.Add(newObj as ITerm);
            }
            var newPredicate = new Predicate(predName, terms, signage);
            return newPredicate;
        }
    }

    [Serializable]
    public class NonEqualTuple
    {
        public int first;
        public int second;
    }

    [Serializable]
    public class MutablePredicate
    {
        public string predicateName;
        public List<int> terms;
        public bool sign;

        public MutablePredicate(string _name, List<int> _terms)
        {
            predicateName = _name;
            terms = _terms;
            sign = true;
        }

        public MutablePredicate(string _name, List<int> _terms, bool _sign)
        {
            predicateName = _name;
            terms = _terms;
            sign = _sign;
        }
    }

    [Serializable]
    public class MutableTerm
    {
        public string typeName;
    }


}