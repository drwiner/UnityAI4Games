
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System;
using SteeringNamespace;

namespace PlanningNamespace
{
    public class ExecutePlan : MonoBehaviour
    {

        public bool execute = false;

        // Timeline Fields
        public PlayableDirector playableDirector;
        public TimelineAsset executeTimeline;
        public TrackAsset steerTrack, lerpTrack;

        public RunPlanner planner;
        public void Awake()
        {
            planner = GameObject.FindGameObjectWithTag("Planner").GetComponent<RunPlanner>();
            playableDirector = GetComponent<PlayableDirector>();
            playableDirector.Stop();
        }

        public void Update()
        {
            if (execute)
            {
                Execute();
                execute = false;
            }
        }

        public void Execute()
        {
            executeTimeline = (TimelineAsset)ScriptableObject.CreateInstance("TimelineAsset");
            steerTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "steerTrack");
            lerpTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "lerpTrack");

            execute = false;
            var planStringList = planner.PlanSteps;
            var startTime = 0;
            char[] charsToTrim = {'(', ')' };
            foreach (var step in planStringList)
            {
                var stepPart = step.Trim(charsToTrim).Split('-').First();
                if (stepPart.EndsWith(")"))
                {
                    stepPart = stepPart.TrimEnd(charsToTrim);
                }
                if (stepPart.Equals("initial") || stepPart.Equals("goal"))
                {
                    continue;
                }

                var stepItems = stepPart.Split(' ');

                // Find Action
                var action = GameObject.Find(stepItems.First()).GetComponent<UnityActionOperator>();
                var terms = new List<GameObject>();
                foreach (var term in stepItems.Skip(1))
                {
                    terms.Add(GameObject.Find(term));
                }

                // Follow Unity Instructions
                var instructions = action.UnityInstructions;
                var st = (double)startTime;
                foreach (var instruction in instructions)
                {
                    ProcessInstruction(instruction, terms, st, 2);
                }
                startTime += 2;
            }
            playableDirector.playableAsset = executeTimeline;
            playableDirector.Play(executeTimeline);
        }

        public void ProcessInstruction(string instruction, List<GameObject> terms, double startTime, double duration)
        {
            var instructionParts = instruction.Split(' ');
            var instructionType = instructionParts[0];

            
            
            var CI = new ClipInfo(this.playableDirector, startTime, duration, instruction);

            if (instructionType.Equals("steer"))
            {
                var agent = terms[Int32.Parse(instructionParts[1])];
                var source = terms[Int32.Parse(instructionParts[2])];
                var sink = terms[Int32.Parse(instructionParts[3])];

                var displayName = string.Format("{0} {1} {2} {3}", instructionType, agent.name, source.name, sink.name);
                CI.display = displayName;

                // Initiate the Steering capability of the agent
                var DS_TC = agent.GetComponent<DynoBehavior_TimelineControl>();
                DS_TC.InitiateExternally();

                var steerStart = new Vector3(source.transform.position.x, agent.transform.position.y, source.transform.position.z);
                var steerFinish = new Vector3(sink.transform.position.x, agent.transform.position.y, sink.transform.position.z);
                // arg 1 is agent, arg 2 is source, arg 3 is destination
                SteerClip(agent, steerStart, steerFinish, true, true, true, CI);
            }

            //if (instructionType.Equals("scale"))
            //{
            //    var rescale = new Vector3(float.Parse(instructionParts[2]), float.Parse(instructionParts[3]), float.Parse(instructionParts[4]));
            //    var agent = terms[Int32.Parse(instructionParts[1])];
            //    var destination = new Trans(agent.transform);
            //    destination.localScale = rescale;
            //    SimpleLerpClip(agent, destination, CI);

            //}
            //else if (instructionType.Equals("takePosition"))
            //{
            //    // argument 1 is to take position of second
            //    var destination = terms[Int32.Parse(instructionParts[2])].transform;
            //    SimpleLerpClip(terms[1], new Trans(destination), CI);
            //}
            //else if (instructionType.Equals("addPosition"))
            //{
            //    var agent = terms[Int32.Parse(instructionParts[1])];
            //    // adds vector3
            //    var addition = new Vector3(float.Parse(instructionParts[2]), float.Parse(instructionParts[3]), float.Parse(instructionParts[4]));
            //    var destination = new Trans(agent.transform);

            //    destination.position += addition;
            //    SimpleLerpClip(agent, destination, CI);
            //}
            //else if (instructionType.Equals("parent"))
            //{

            //}
            //else if (instructionType.Equals("deparent"))
            //{

            //}
            //else if (instructionType.Equals("steer"))
            //{
            //    // noninstantaneous...
            //}
            //else
            //{
            //    Debug.Log(string.Format("instruction type {0} not yet written.", instructionType));
            //    throw new System.Exception();
            //}

        }


        public void TransformBind(LerpToMoveObjectAsset tpObj, GameObject obj_to_move, Trans end_pos)
        {
            var GO = new GameObject();
            GO.transform.position = end_pos.position;
            GO.transform.rotation = end_pos.rotation;
            GO.transform.localScale = end_pos.localScale;
            tpObj.ObjectToMove.exposedName = UnityEditor.GUID.Generate().ToString();
            tpObj.LerpMoveTo.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(tpObj.ObjectToMove.exposedName, obj_to_move);
            playableDirector.SetReferenceValue(tpObj.LerpMoveTo.exposedName, GO.transform);
        }
        public void AnimateBind(ControlPlayableAsset cpa, GameObject ato)
        {
            cpa.sourceGameObject.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(cpa.sourceGameObject.exposedName, ato);
        }

        public void SteerBind(SteeringAsset sa, GameObject boid, Vector3 startSteer, Vector3 endSteer, bool depart, bool arrive, bool isMaster)
        {
            sa.Boid.exposedName = UnityEditor.GUID.Generate().ToString();
            sa.arrive = arrive;
            sa.depart = depart;
            sa.startPos = startSteer;
            sa.endPos = endSteer;
            sa.master = isMaster;
            playableDirector.SetReferenceValue(sa.Boid.exposedName, boid);
        }

        public void SimpleLerpClip(GameObject agent, Trans goalPos, ClipInfo CI)
        {
            var lerpClip = lerpTrack.CreateClip<LerpToMoveObjectAsset>();

            lerpClip.start = CI.start;
            lerpClip.duration = CI.duration;
            LerpToMoveObjectAsset lerp_clip = lerpClip.asset as LerpToMoveObjectAsset;

            TransformBind(lerp_clip, agent, goalPos);
        }

        public void SteerClip(GameObject go, Vector3 startPos, Vector3 goalPos, bool depart, bool arrival, bool isMaster, ClipInfo CI)
        {
            var steerClip = steerTrack.CreateClip<SteeringAsset>();
            steerClip.start = CI.start;
            steerClip.duration = CI.duration;
            steerClip.displayName = CI.display;
            SteeringAsset steer_clip = steerClip.asset as SteeringAsset;
            SteerBind(steer_clip, go, startPos, goalPos, depart, arrival, isMaster);
        }

        public static GameObject MakeCustomizedTransform(Vector3 pos, float orientation)
        {
            GameObject t = new GameObject();
            t.transform.position = pos;
            t.transform.rotation = Quaternion.Euler(0f, orientation, 0f);
            return t;
        }
    }

    public class ClipInfo
    {
        public double start;
        public double duration;
        public string display;
        public PlayableDirector director;
        public ClipInfo(PlayableDirector _director, double strt, double dur, string dis)
        {
            director = _director;
            start = strt;
            duration = dur;
            display = dis;
        }
    }
    }