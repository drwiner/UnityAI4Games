using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System;
using SteeringNamespace;
using TimelineClipsNamespace;
using BoltFreezer.Interfaces;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class UnityPlanExecutor : MonoBehaviour
    {
        public IExecutor planExecutor;
        public bool play = false;
        public bool stop = false;

        void Update()
        {
            if (play)
            {
                play = false;
                planExecutor.Play();
            }    

            if (stop)
            {
                stop = false;
                planExecutor.Stop();
            }
        }

    }

    
}