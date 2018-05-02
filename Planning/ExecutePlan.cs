using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System;
using SteeringNamespace;
using TimelineClipsNamespace;

namespace PlanningNamespace
{
    public class ExecutePlan : MonoBehaviour
    {

        public bool execute = false;

        // Timeline Fields
        public PlayableDirector playableDirector;
        public TimelineAsset executeTimeline;
        public TrackAsset steerTrack, lerpTrack, ctrack, attachTrack;

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
                execute = false;
                Execute();
                
            }
        }

        public void Execute()
        {
            executeTimeline = (TimelineAsset)ScriptableObject.CreateInstance("TimelineAsset");
            steerTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "steerTrack");
            lerpTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "lerpTrack");
            attachTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "attachTrack");
            ctrack = executeTimeline.CreateTrack<ControlTrack>(null, "control_track");

            execute = false;
            var planStringList = planner.PlanSteps;
            double startTime = 0;
            double accumulatedTime = 0;
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
                var goWithAction = GameObject.Find(stepItems.First());
                var action = goWithAction.GetComponent<UnityActionOperator>();
                var terms = new List<GameObject>();
                foreach (var term in stepItems.Skip(1))
                {
                    terms.Add(GameObject.Find(term));
                }

                // Follow Unity Instructions
                var instructions = action.UnityInstructions;


                accumulatedTime = 0;
                foreach (var instruction in instructions)
                {
                    ProcessInstruction(goWithAction, instruction, terms, startTime + accumulatedTime, 0.5);
                    accumulatedTime += .5;
                }
                startTime = startTime + accumulatedTime;
            }
            playableDirector.playableAsset = executeTimeline;
            playableDirector.Play(executeTimeline);
        }


        public void ProcessInstruction(GameObject goWithAction, string instruction, List<GameObject> terms, double startTime, double duration)
        {
            var instructionParts = instruction.Split(' ');
            var instructionType = instructionParts[0];

            var CI = new ClipInfo(this.playableDirector, startTime, duration, instruction);

            if (instructionType.Equals("play"))
            {
                // implies only 1 argument, unless refactored later
                var agent = terms[Int32.Parse(instructionParts[1])];

                // Clones action and sets binding
                var goClone = SetAgentToGenericAction(goWithAction, agent);
                var controlTrackClip = ctrack.CreateDefaultClip();
                CI.display = string.Format("playing timeline {0}", goWithAction.name);
                AnimateClip(controlTrackClip, goClone, CI);
            }

            if (instructionType.Equals("attach"))
            {
                var parent = terms[Int32.Parse(instructionParts[1])];
                var child = terms[Int32.Parse(instructionParts[2])];

                var attachClip = attachTrack.CreateClip<AttachToParent>();
                attachClip.start = CI.start;
                attachClip.duration = CI.duration;
                attachClip.displayName = string.Format("attach parent={0} child={1}", parent.name, child.name);
                AttachToParent aClip = attachClip.asset as AttachToParent;
                AttachBind(aClip, parent, child);
            }

            if (instructionType.Equals("transform"))
            {
                // parse 5 argument instructions
                var agent = terms[Int32.Parse(instructionParts[1])];
                var origin = terms[Int32.Parse(instructionParts[2])];
                var originHeight = float.Parse(instructionParts[3]);
                var destination = terms[Int32.Parse(instructionParts[4])];
                var destinationHeight = float.Parse(instructionParts[5]);

                // Creating new transforms with custom heights
                var originTransform = new GameObject();
                originTransform.transform.position = new Vector3(origin.transform.position.x, destinationHeight, origin.transform.position.z);
                var destinationTransform = new GameObject();
                destinationTransform.transform.position = new Vector3(destination.transform.position.x, destinationHeight, destination.transform.position.z);
                //destinationTransform.transform.localScale = agent.transform.localScale;
                SimpleLerpClip(agent, originTransform.transform, destinationTransform.transform, CI);
                //SimpleToLerpClip(agent, destinationTransform.transform, CI);
            }

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

        public GameObject SetAgentToGenericAction(GameObject actionToAnimate, GameObject animatingObject)
        {
            GameObject animTimelineObject = GameObject.Instantiate(actionToAnimate);
            var director01 = animTimelineObject.GetComponent<PlayableDirector>();
            var timeline01 = director01.playableAsset as TimelineAsset;
            var anim01 = animatingObject.GetComponent<Animator>();
            anim01.applyRootMotion = actionToAnimate.GetComponent<Animator>().applyRootMotion;
            foreach (var track in timeline01.GetOutputTracks())
            {
                var animTrack = track as AnimationTrack;
                if (animTrack == null)
                    continue;
                var binding = director01.GetGenericBinding(animTrack);
                if (binding == null)
                    continue;

                director01.SetGenericBinding(animTrack, anim01);
            }
            return animTimelineObject;
        }


        public void TransformToBind(LerpToMoveObjectAsset tpObj, GameObject obj_to_move, Transform end_pos)
        {

            tpObj.ObjectToMove.exposedName = UnityEditor.GUID.Generate().ToString();
            tpObj.LerpMoveTo.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(tpObj.ObjectToMove.exposedName, obj_to_move);
            playableDirector.SetReferenceValue(tpObj.LerpMoveTo.exposedName, end_pos);
        }

        public void AttachBind(AttachToParent atpObj, GameObject parent, GameObject child)
        {
            atpObj.Parent.exposedName = UnityEditor.GUID.Generate().ToString();
            atpObj.Child.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(atpObj.Parent.exposedName, parent);
            playableDirector.SetReferenceValue(atpObj.Child.exposedName, child);
        }

        public void TransformBind(LerpMoveObjectAsset tpObj, GameObject obj_to_move, Transform start_pos, Transform end_pos)
        {
            tpObj.ObjectToMove.exposedName = UnityEditor.GUID.Generate().ToString();
            tpObj.LerpMoveTo.exposedName = UnityEditor.GUID.Generate().ToString();
            tpObj.LerpMoveFrom.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(tpObj.ObjectToMove.exposedName, obj_to_move);
            playableDirector.SetReferenceValue(tpObj.LerpMoveTo.exposedName, end_pos);
            playableDirector.SetReferenceValue(tpObj.LerpMoveFrom.exposedName, start_pos);
        }

        public void AnimateBind(ControlPlayableAsset cpa, GameObject ato)
        {
            cpa.sourceGameObject.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(cpa.sourceGameObject.exposedName, ato);
        }

        public void AnimateClip(TimelineClip cpa, GameObject ato, ClipInfo CI)
        {
            cpa.start = CI.start;
            cpa.duration = CI.duration;
            cpa.displayName = CI.display;
            var controlAnim = cpa.asset as ControlPlayableAsset;
            AnimateBind(controlAnim, ato);
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

        public void SimpleToLerpClip(GameObject agent, Transform goalPos, ClipInfo CI)
        {
            var lerpClip = lerpTrack.CreateClip<LerpToMoveObjectAsset>();
            lerpClip.start = CI.start;
            lerpClip.duration = CI.duration;
            lerpClip.displayName = string.Format("SimpleLerp {0}", goalPos.name);
            LerpToMoveObjectAsset lerp_clip = lerpClip.asset as LerpToMoveObjectAsset;
            TransformToBind(lerp_clip, agent, goalPos);
        }

        public void SimpleLerpClip(GameObject agent, Transform startPos, Transform goalPos, ClipInfo CI)
        {
            var lerpClip = lerpTrack.CreateClip<LerpMoveObjectAsset>();
            lerpClip.start = CI.start;
            lerpClip.duration = CI.duration;
            lerpClip.displayName = string.Format("SimpleLerp {0}-{1}", startPos.name, goalPos.name);
            LerpMoveObjectAsset lerp_clip = lerpClip.asset as LerpMoveObjectAsset;
            TransformBind(lerp_clip, agent, startPos, goalPos);
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