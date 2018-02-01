using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphNamespace;

namespace GoalNamespace
{

    public class PathGoal : MonoBehaviour
    {

        private GameObject previousGoal;
        public GameObject goalObject;
        private Vector3 goalPosition;
        public Material non_goal_material;
        public Material goal_material;

        void Start()
        {
            previousGoal = null;
            goalPosition = transform.position;

            if (goalObject != null)
            {
                goalObject.GetComponent<Renderer>().material = goal_material;
            }

        }


        public void setGoal(TileNode tn)
        {
            goalObject = tn.gameObject;
            if (previousGoal != null)
                previousGoal.GetComponent<Renderer>().material = non_goal_material;
            goalObject.GetComponent<Renderer>().material = goal_material;
            goalPosition = QuantizeLocalize.Localize(tn);
        }

        public Vector3 getGoal()
        {
            return goalPosition;
        }


    }
}