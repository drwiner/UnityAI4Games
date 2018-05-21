using BoltFreezer.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BoltFreezer.PlanTools;
using GraphNamespace;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class ProblemStates : MonoBehaviour
    {

        public List<string> InitialState;
        public List<string> GoalState;

        public List<IPredicate> initialPredicateList;
        public List<IPredicate> goalPredicateList;

        public bool reset;
        public bool setInitialState;

        public void Awake()
        {
            ReadProblem();
            reset = false;
            setInitialState = false;
        }

        public void Update()
        {
            if (reset)
            {
                ReadProblem();
                reset = false;
            }

            if (setInitialState)
            {
                SetInitialState();
                setInitialState = false;
            }
        }

        public void SetInitialState()
        {
            var actors = GameObject.FindGameObjectWithTag("ActorHost");
            actors.GetComponent<ActorList>().RecollectChildren();

            ReadProblem();
            foreach( var initPredicate in initialPredicateList)
            {
                if (initPredicate.Name.Equals("at"))
                {
                    // arg 0 is item name
                    // arg 1 is location name
                    var locationPosition = GameObject.Find(initPredicate.Terms[1].Constant).transform.position;
                    var item = GameObject.Find(initPredicate.Terms[0].Constant);
                    
                    var newPosition = new Vector3(locationPosition.x, item.transform.position.y, locationPosition.z);
                    item.transform.position = newPosition;
                }
            }
        }

        public void ReadProblem()
        {
            initialPredicateList = new List<IPredicate>();
            goalPredicateList = new List<IPredicate>();

            if (InitialState.Count > 0)
            {
                Debug.Log("Initial State");
                foreach (var stringItem in InitialState)
                {
                    var pred = ProcessStringItem(stringItem);
                    initialPredicateList.Add(pred as IPredicate);
                    Debug.Log(pred.ToString());
                }
            }
            if (GoalState.Count > 0)
            {
                Debug.Log("Goal State");
                foreach (var stringItem in GoalState)
                {
                    var pred = ProcessStringItem(stringItem);
                    goalPredicateList.Add(pred as IPredicate);
                    Debug.Log(pred.ToString());
                }
            }

            // Also collect location edges at Location host
            var locationHost = GameObject.FindGameObjectWithTag("Locations");
            var adjContainer = locationHost.GetComponent<TileGraph>();
            foreach (var edge in adjContainer.Edges)
            {
                var pred = ProcessStringItem(string.Format("adjacent {0} {1}", edge.S.name, edge.T.name));
                initialPredicateList.Add(pred as IPredicate);
                var pred2 = ProcessStringItem(string.Format("adjacent {1} {0}", edge.S.name, edge.T.name));
                initialPredicateList.Add(pred2 as IPredicate);
                Debug.Log(pred.ToString());
                Debug.Log(pred2.ToString());
            }
        }

        public static Predicate ProcessStringItem(string stringItem)
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
                var go = GameObject.Find(item);
                var newObj = new Term(item, go.name, go.tag);
                //terms.Add(new Term(item, true) as ITerm);
                terms.Add(newObj as ITerm);
            }
            var newPredicate = new Predicate(predName, terms, signage);
            return newPredicate;
        }
    }

}