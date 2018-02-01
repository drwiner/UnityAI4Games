﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoalNamespace;

namespace SteeringNamespace
{

    public class DynoSeek_PathFollow : MonoBehaviour
    {

        private SteeringParams sp;
        private PathGoal goalObject;
        private Vector3 goal;
        private DynoSteering steering;

        // Use this for initialization
        void Start()
        {
            sp = GetComponent<SteeringParams>();
            goalObject = GetComponent<PathGoal>();
        }

        // Update is called once per frame
        public DynoSteering getSteering()
        {
            steering = new DynoSteering();

            goal = goalObject.getGoal();
            steering.force = goal - transform.position;
            steering.force.Normalize();
            steering.force = steering.force * sp.MAXACCEL;
            steering.torque = 0f;

            return steering;
        }
    }

}