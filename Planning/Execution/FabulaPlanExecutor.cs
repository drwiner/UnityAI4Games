using BoltFreezer.Interfaces;
using SteeringNamespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimelineClipsNamespace;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace  PlanningNamespace
{
    public class FabulaPlanExecutor : IFabulaExecutor
    {
        public PlayableDirector director;
        protected Dictionary<GameObject, GameObject> LastAttachMap;

        public PlayableDirector Director
        {
            get { return director; }
            set { director = value; }
        }

        protected TimelineAsset timeline;
        protected TrackAsset steerTrack, orientTrack, lerpTrack, ctrack, attachTrack;

        public FabulaPlanExecutor(PlayableDirector pd)
        {
            director = pd;
            timeline = (TimelineAsset)ScriptableObject.CreateInstance("TimelineAsset");
            steerTrack = timeline.CreateTrack<PlayableTrack>(null, "steerTrack");
            orientTrack = timeline.CreateTrack<PlayableTrack>(null, "orientTrack");
            lerpTrack = timeline.CreateTrack<PlayableTrack>(null, "lerpTrack");
            attachTrack = timeline.CreateTrack<PlayableTrack>(null, "attachTrack");
            ctrack = timeline.CreateTrack<ControlTrack>(null, "control_track");
            LastAttachMap = new Dictionary<GameObject, GameObject>();
        }

        public void Play()
        {
            director.Play(timeline);
        }

        public void Stop()
        {
            director.Stop();
        }

        public Dictionary<int, ClipInfo> PopulateTimeline(List<IPlanStep> plan)
        {
            Dictionary<int, ClipInfo> InfoList = new Dictionary<int, ClipInfo>();

            double startTime = 0;
            double accumulatedTime = 0;
            foreach (var step in plan)
            {
                ClipInfo ActionInfo;

                // all actions should have a name that is operator type
                var actionGameObjectHost = GameObject.Find(step.Name);
                var action = actionGameObjectHost.GetComponent<UnityActionOperator>();
                var terms = new List<GameObject>();
                foreach (var term in step.Terms)
                {
                    terms.Add(GameObject.Find(term.Constant));
                }

                // Follow Unity Instructions
                var instructions = action.UnityInstructions;

                var defaultIncrementTime = 1.4;
                //if (actionGameObjectHost.tag == "Navigation")
                //{
                //    // then we need to calculate the increment time for the "steer" instruction
                //}

                accumulatedTime = 0;
                string instructionToAlwaysPlay = null;

                foreach (var instruction in instructions)
                {
                    // Unused reference to instruction's resulting ClipInfo
                    ClipInfo instructionCI;

                    // Extract Instruction Type, always first sub-string
                    var instructionType = instruction.Split(' ')[0];

                    
                    if (instructionType.Equals("steer"))
                    {
                        instructionCI = ProcessSteering(actionGameObjectHost, instruction, terms, startTime + accumulatedTime);
                        accumulatedTime = accumulatedTime + instructionCI.duration;
                    }
                    else if (instructionType.Equals("always-play"))
                    {
                        // then this duration should be the duration of the entire action, and shouldn't add accumulated Time.
                        instructionToAlwaysPlay = instruction;
                    }
                    else
                    {
                        instructionCI = ProcessInstruction(actionGameObjectHost, instruction, terms, startTime + accumulatedTime, defaultIncrementTime);
                        accumulatedTime += defaultIncrementTime;
                    }


                    //InfoList.Add(instructionCI);
                }

                if (instructionToAlwaysPlay != null)
                {
                    // ToDo - check that animations loop
                    var instructionCI = ProcessInstruction(actionGameObjectHost, instructionToAlwaysPlay, terms, startTime, accumulatedTime);
                }

                ActionInfo = new ClipInfo(director, startTime, accumulatedTime, step.Name);
                InfoList[step.ID] = ActionInfo;
                startTime = startTime + accumulatedTime;

            }

            director.playableAsset = timeline;
            return InfoList;
        }

        public static double CalculateSteeringDuration(SteeringParams sp, Vector3 origin, Vector3 destination)
        {
            return ((sp.MAXSPEED / (Vector3.Distance(destination, origin))) / 0.03) + 0.166; //.166 buffer to arrive at destination
        }

        public ClipInfo ProcessSteering(GameObject goWithActionName, string instruction, List<GameObject> terms, double startTime)
        {
            var instructionParts = instruction.Split(' ');
            var instructionType = instructionParts[0];

            var agent = terms[Int32.Parse(instructionParts[1])];
            var source = terms[Int32.Parse(instructionParts[2])];
            var sink = terms[Int32.Parse(instructionParts[3])];

            var duration = CalculateSteeringDuration(agent.GetComponent<SteeringParams>(), source.transform.position, sink.transform.position);

            var CI = new ClipInfo(director, startTime, duration, instruction);

            var displayName = string.Format("{0} {1} {2} {3}", instructionType, agent.name, source.name, sink.name);
            CI.display = displayName;

            // Initiate the Steering capability of the agent
            var DS_TC = agent.GetComponent<DynoBehavior_TimelineControl>();
            DS_TC.InitiateExternally();

            var steerStart = new Vector3(source.transform.position.x, agent.transform.position.y, source.transform.position.z);
            var steerFinish = new Vector3(sink.transform.position.x, agent.transform.position.y, sink.transform.position.z);
            // arg 1 is agent, arg 2 is source, arg 3 is destination
            SteerClip(agent, steerStart, steerFinish, true, true, true, CI);

            // Also, add a brief execution to bring steering to finish
            var followUpCI = new ClipInfo(director, startTime + duration, 0.3f, "follow up");
            SteerClip(agent, steerFinish, steerFinish, true, false, false, followUpCI);

            return CI;
        }

        public ClipInfo ProcessInstruction(GameObject goWithAction, string instruction, List<GameObject> terms, double startTime, double duration)
        {
            var instructionParts = instruction.Split(' ');
            var instructionType = instructionParts[0];

            var CI = new ClipInfo(director, startTime, duration, instruction);

            if (instructionType.Equals("always-play"))
            {
                // implies only 1 argument, unless refactored later
                var agent = terms[Int32.Parse(instructionParts[1])];
                var animationHost = agent.transform.GetChild(0).gameObject;

                // Clones action and sets binding
                var goClone = SetAgentToGenericAction(goWithAction, animationHost);
                var controlTrackClip = ctrack.CreateDefaultClip();
                CI.display = string.Format("playing timeline {0}", goWithAction.name);
                AnimateClip(controlTrackClip, goClone, CI);
            }

            if (instructionType.Equals("play"))
            {
                var agent = terms[Int32.Parse(instructionParts[1])];
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

                LastAttachMap[child] = parent;
            }

            if (instructionType.Equals("dettach"))
            {
                var child = terms[Int32.Parse(instructionParts[1])];

                var dettachClip = attachTrack.CreateClip<DettachToParent>();
                dettachClip.start = CI.start + 0.12f;
                dettachClip.duration = CI.duration - .12f;
                dettachClip.displayName = string.Format("dettach child={0}", child.name);
                DettachToParent aClip = dettachClip.asset as DettachToParent;
                DettachBind(aClip, child);
                var prevAttachment = LastAttachMap[child];
                var attachClip = attachTrack.CreateClip<AttachToParent>();
                attachClip.start = CI.start;
                attachClip.duration = 0.12f;
                attachClip.displayName = string.Format("attach First");
                AttachToParent pClip = attachClip.asset as AttachToParent;
                AttachBind(pClip, prevAttachment, child);
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
                originTransform.transform.position = new Vector3(origin.transform.position.x, originHeight, origin.transform.position.z);
                var destinationTransform = new GameObject();
                destinationTransform.transform.position = new Vector3(destination.transform.position.x, destinationHeight, destination.transform.position.z);

                SimpleLerpClip(agent, originTransform.transform, destinationTransform.transform, CI);
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

                //CI.duration -= (0.017 * 2);
                // arg 1 is agent, arg 2 is source, arg 3 is destination
                SteerClip(agent, steerStart, steerFinish, true, true, true, CI);
                //SteerClip(agent, steerFinish, steerFinish, false, false, false, CI);
            }

            if (instructionType.Equals("orient"))
            {
                var agent = terms[Int32.Parse(instructionParts[1])];
                var origin = terms[Int32.Parse(instructionParts[2])];
                var destination = terms[Int32.Parse(instructionParts[3])];

                // Initiate the Steering capability of the agent (if not already set; fine if redundant)
                var DS_TC = agent.GetComponent<DynoBehavior_TimelineControl>();
                DS_TC.InitiateExternally();

                CI.display = string.Format("orient {0} {1}", agent.name, destination.name);
                // var test = destination.transform.position + destination.transform.localPosition;
                OrientClip(agent, destination.transform.position, CI);

                var followUpCI = new ClipInfo(director, startTime, 0.2f, "follow up");
                var steerFinish = new Vector3(origin.transform.position.x, agent.transform.position.y, origin.transform.position.z);
                SteerClip(agent, steerFinish, steerFinish, true, false, false, followUpCI);
            }

            return CI;

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
            director.SetReferenceValue(tpObj.ObjectToMove.exposedName, obj_to_move);
            director.SetReferenceValue(tpObj.LerpMoveTo.exposedName, end_pos);
        }

        public void AttachBind(AttachToParent atpObj, GameObject parent, GameObject child)
        {
            atpObj.Parent.exposedName = UnityEditor.GUID.Generate().ToString();
            atpObj.Child.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(atpObj.Parent.exposedName, parent);
            director.SetReferenceValue(atpObj.Child.exposedName, child);
        }

        public void DettachBind(DettachToParent dtpObj, GameObject child)
        {
            dtpObj.Child.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(dtpObj.Child.exposedName, child);
        }

        public void TransformBind(LerpMoveObjectAsset tpObj, GameObject obj_to_move, Transform start_pos, Transform end_pos)
        {
            tpObj.ObjectToMove.exposedName = UnityEditor.GUID.Generate().ToString();
            tpObj.LerpMoveTo.exposedName = UnityEditor.GUID.Generate().ToString();
            tpObj.LerpMoveFrom.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(tpObj.ObjectToMove.exposedName, obj_to_move);
            director.SetReferenceValue(tpObj.LerpMoveTo.exposedName, end_pos);
            director.SetReferenceValue(tpObj.LerpMoveFrom.exposedName, start_pos);
        }

        public void AnimateBind(ControlPlayableAsset cpa, GameObject ato)
        {
            cpa.sourceGameObject.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(cpa.sourceGameObject.exposedName, ato);
        }

        public void AnimateClip(TimelineClip cpa, GameObject ato, ClipInfo CI)
        {
            cpa.start = CI.start;
            cpa.duration = CI.duration;
            cpa.displayName = CI.display;
            var controlAnim = cpa.asset as ControlPlayableAsset;
            //controlAnim.postPlayback = ActivationControlPlayable.PostPlaybackState.Active;
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
            director.SetReferenceValue(sa.Boid.exposedName, boid);
        }

        public void OrientBind(OrientToObjectAsset oa, GameObject boid, Vector3 endOrient)
        {
            oa.ObjectToMove.exposedName = UnityEditor.GUID.Generate().ToString();
            oa.endPos = endOrient;
            director.SetReferenceValue(oa.ObjectToMove.exposedName, boid);
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
            steerClip.start = CI.start + 0.06f;
            steerClip.duration = CI.duration-.12f;
            steerClip.displayName = CI.display;
            SteeringAsset steer_clip = steerClip.asset as SteeringAsset;
            SteerBind(steer_clip, go, startPos, goalPos, depart, arrival, isMaster);
        }

        public void OrientClip(GameObject go, Vector3 goalPos, ClipInfo CI)
        {
            var orientClip = orientTrack.CreateClip<OrientToObjectAsset>();
            orientClip.start = CI.start + .06f;
            orientClip.duration = CI.duration - .12f;
            orientClip.displayName = CI.display;
            OrientToObjectAsset orient_clip = orientClip.asset as OrientToObjectAsset;
            OrientBind(orient_clip, go, goalPos);
        }


        public static GameObject MakeCustomizedTransform(Vector3 pos, float orientation)
        {
            GameObject t = new GameObject();
            t.transform.position = pos;
            t.transform.rotation = Quaternion.Euler(0f, orientation, 0f);
            return t;
        }

    }
}
