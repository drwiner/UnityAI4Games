using BoltFreezer.Camera;
using BoltFreezer.Interfaces;
using CameraNamespace;
using SteeringNamespace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace PlanningNamespace
{
    [ExecuteInEditMode]
    public class UnityExecutionManager : MonoBehaviour
    {
        public UnityPlanningInterface PlanDirector;

        public GameObject FabulaTimelineHost;
        private PlayableDirector fabulaDirector;
        private FabulaPlanExecutor fabulaPlanExecutor;

        public GameObject DiscourseTimelineHost;
        private PlayableDirector discourseDirector;
        private DiscoursePlanExecutor discoursePlanExecutor;

        public List<string> fabulaStepNames = new List<string>();
        public static List<IPlanStep> fabulaSteps = new List<IPlanStep>();

       
        public List<string> discourseStepNames = new List<string>();
        public static List<CamPlanStep> discourseSteps = new List<CamPlanStep>();

        public Dictionary<int, int> mergeManager = new Dictionary<int, int>();

        public bool decachePlan = false;
        public bool reInitialize = false;
        public bool assembleClips = false;
        public bool assembleTimelines = false;
        public bool play = false;

        // Update is called once per frame
        void Update()
        {
            if (reInitialize)
            {
                reInitialize = false;

                fabulaDirector.time = 0;
                fabulaDirector.playableAsset = null;
                fabulaPlanExecutor = new FabulaPlanExecutor(fabulaDirector);
                FabulaTimelineHost.GetComponent<UnityPlanExecutor>().planExecutor = fabulaPlanExecutor;

                discourseDirector.time = 0;
                discourseDirector.playableAsset = null;
                discoursePlanExecutor = new DiscoursePlanExecutor(discourseDirector);
                DiscourseTimelineHost.GetComponent<UnityPlanExecutor>().planExecutor = discoursePlanExecutor;
            }


            if (decachePlan)
            {
                decachePlan = false;
                var PI = GameObject.FindGameObjectWithTag("Planner").GetComponent<UnityPlanningInterface>();
                PI.decachePlan = true;
            }

            if (assembleClips)
            {
                assembleClips = false;

                AssembleClips();
            }

            if (assembleTimelines)
            {
                assembleTimelines = false;

                fabulaDirector = FabulaTimelineHost.GetComponent<PlayableDirector>();
                fabulaPlanExecutor = new FabulaPlanExecutor(fabulaDirector);
                FabulaTimelineHost.GetComponent<UnityPlanExecutor>().planExecutor = fabulaPlanExecutor;

                discourseDirector = DiscourseTimelineHost.GetComponent<PlayableDirector>();
                discoursePlanExecutor = new DiscoursePlanExecutor(discourseDirector);
                DiscourseTimelineHost.GetComponent<UnityPlanExecutor>().planExecutor = discoursePlanExecutor;

                AssembleTimelines();
            }

            if (play)
            {
                play = false;
                if (discoursePlanExecutor == null)
                {
                    discoursePlanExecutor = DiscourseTimelineHost.GetComponent<UnityPlanExecutor>().planExecutor as DiscoursePlanExecutor;
                }
                discoursePlanExecutor.Play();
            }

            if (Input.GetKeyDown("space"))
            {
                discourseDirector.time = 0;
                discourseDirector.Stop();
                discourseDirector.Evaluate();
                discourseDirector.Play();
            }
               
        }

        public void AssembleClips()
        {
            // Execution Timelines
            //fabulaDirector = FabulaTimelineHost.GetComponent<PlayableDirector>();
            //discourseDirector = DiscoureTimelineHost.GetComponent<PlayableDirector>();

            var opNames = new List<string>();
            var domainOps = GameObject.FindGameObjectWithTag("ActionHost");
            for(int i = 0; i < domainOps.transform.childCount; i++)
            {
                if (!domainOps.transform.GetChild(i).gameObject.activeSelf)
                {
                    continue;
                }
                opNames.Add(domainOps.transform.GetChild(i).name);
            }

            var agentHost = GameObject.FindGameObjectWithTag("ActorHost");
            for(int i  = 0; i < agentHost.transform.childCount; i++)
            {
                if (!agentHost.transform.GetChild(i).gameObject.activeSelf)
                {
                    continue;
                }
                // otherwise, check if has steering
                var agent = agentHost.transform.GetChild(i).gameObject;
                if (agent.tag == "SteeringAgent")
                {
                    var dbtc = agent.GetComponent<DynoBehavior_TimelineControl>();
                    dbtc.initiatedExternally = false;
                    dbtc.playingClip = false;
                    dbtc.orienting = false;
                    dbtc.steering = false;
                }
            }

            discourseStepNames = new List<string>();
            discourseSteps = new List<CamPlanStep>();

            fabulaStepNames = new List<string>();
            fabulaSteps = new List<IPlanStep>();


            //double fabTimeCounter = 0;
            //double discTimeCounter = 0;
            foreach (var step in PlanDirector.Plan)
            {
                if (step.Height > 0)
                {
                    Debug.Log(step.ToString());
                    continue;
                }
                CamPlanStep cps = step as CamPlanStep;
                if (cps == null)
                {
                    if (!opNames.Contains(step.Name))
                    {
                        // then it's an initial or dummy
                        continue;
                    }
                    // then it's fabula
                    fabulaSteps.Add(step);
                    fabulaStepNames.Add(step.ToString());
                }
                else
                {
                    //foreach(var actionseg in cps.TargetDetails.ActionSegs)
                    //{
                    //    if (PlanDirector.MergeManager.ContainsKey(actionseg.ActionID))
                    //    {
                    //        actionseg.ActionID = PlanDirector.MergeManager[actionseg.ActionID];
                    //    }
                    //}

                    discourseSteps.Add(cps);
                    discourseStepNames.Add(step.ToString());
                }
            }

        }

        public void AssembleTimelines()
        {
            // First, assemble Fabula Plan
            var actionClipInfo = fabulaPlanExecutor.PopulateTimeline(fabulaSteps);

            // create a dictionary mapping the ID of every action segment to the ClipSchema<FabulaAsset> that it arrived in?
            discoursePlanExecutor.PopulateTimeline(discourseSteps, actionClipInfo);
        }
    }
}