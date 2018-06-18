using BoltFreezer.Camera;
using BoltFreezer.Interfaces;
using CameraNamespace;
using Cinemachine;
using Cinemachine.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimelineClipsNamespace;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace PlanningNamespace
{
    public class DiscoursePlanExecutor : IDiscourseExecutor
    {
        public PlayableDirector director;

        public PlayableDirector Director
        {
            get { return director; }
            set { director = value; }
        }


        protected TimelineAsset timeline;
        protected TrackAsset steerTrack, lerpTrack, ctrack, attachTrack, ttTrack, filmTrack, focusTrack, panTrack, followTrack, screenTrack;

        public DiscoursePlanExecutor(PlayableDirector pd)
        {
            director = pd;

            timeline = (TimelineAsset)ScriptableObject.CreateInstance("TimelineAsset");
            lerpTrack = timeline.CreateTrack<PlayableTrack>(null, "lerpTrack");
            attachTrack = timeline.CreateTrack<PlayableTrack>(null, "attachTrack");
            ctrack = timeline.CreateTrack<ControlTrack>(null, "control_track");
            ttTrack = timeline.CreateTrack<PlayableTrack>(null, "timeTravelTrack");
            filmTrack = timeline.CreateTrack<CinemachineTrack>(null, "film_track");
            focusTrack = timeline.CreateTrack<PlayableTrack>(null, "focus_track");
            panTrack = timeline.CreateTrack<PlayableTrack>(null, "pan_track");
            followTrack = timeline.CreateTrack<PlayableTrack>(null, "follow_track");
            screenTrack = timeline.CreateTrack<PlayableTrack>(null, "screen_track");

            var main_camera_object = GameObject.FindGameObjectWithTag("MainCamera");
            director.SetGenericBinding(filmTrack, main_camera_object);

        }

        public void Play()
        {
            director.Play(timeline);
        }

        public void Stop()
        {
            director.Stop();
        }

        //public Dictionary<int, ClipInfo> PopulateTimeline(List<IPlanStep> planSteps) 
        

        public void PopulateTimeline(List<CamPlanStep> discourseSteps, Dictionary<int, ClipInfo> mappingActionSegIDsToDurativeClips)
        {
            var cameraOptionsHost = GameObject.FindGameObjectWithTag("CameraHost");

            double startTime = 0;
            double accumulatedTime = 0;
            char[] charsToTrim = { '(', ')' };
            var CIList = new List<ClipInfo>();
            foreach (var step in discourseSteps)
            {
                // Extract Schemata
                var camSchema = step.CamDetails;

                var cameraInstance = CamPlan.FindCameraInstance(cameraOptionsHost, camSchema);
                var cameraClone = GameObject.Instantiate(cameraInstance);
                cameraClone.SetActive(true);

                // target location
                Transform targetLocation = GameObject.Find(camSchema.targetLocation).transform;

                // Relevant Camera components for cinematography
                var cvc = cameraClone.GetComponent<CinemachineVirtualCamera>();
                var ccb = cameraClone.GetComponent<CinemachineCameraBody>();

                int CameraAimType = UnityEngine.Random.Range(0, 3);

                //cvc.m_LookAt = GameObject.Find(camSchema.targetLocation).transform;

                // for each action segment, create a time travel clip to the 
                var targetSchema = step.TargetDetails;

                accumulatedTime = 0;
                foreach (var actionSeg in targetSchema.ActionSegs)
                {
                    var targetOfFocus = GameObject.Find(actionSeg.targetVarName).transform;

                    // The fabula ClipInfo associated with entire Action (in future, could track all sub-clips
                    var timelineclip = mappingActionSegIDsToDurativeClips[actionSeg.ActionID];
                    
                    // Duration of clip is based on start and end percent this camera shows
                    var amountOfDuration = (timelineclip.duration * actionSeg.endPercent) - (timelineclip.duration * actionSeg.startPercent);

                    // Start time of clip
                    var intoClipStart = timelineclip.start + (actionSeg.startPercent * timelineclip.duration);

                    // Create display
                    var displayOfClip = string.Format("{0} of {1} from {2} to {3}", camSchema.scale, timelineclip.display, actionSeg.startPercent.ToString(), actionSeg.endPercent.ToString());

                    // Create ClipInfo unit
                    var CI = new ClipInfo(director, startTime + accumulatedTime, amountOfDuration, displayOfClip);

                    // Reference to Unity Action Operator game object
                    var unityActionOperatorHost = GameObject.Find(timelineclip.display);

                    // If this Unity Action is a "navigation" action, then extra steps are required
                    if (unityActionOperatorHost.tag.Equals("Navigation"))
                    {
                        Vector3 halfwayPointDistanceTraveled;
                        Vector3 whereObjectWillBeAtStart = Vector3.zero;
                        Vector3 whereObjectWillBeAtEnd = Vector3.zero;

                        // Access to Director's timeline
                        var ta = timelineclip.director.playableAsset as TimelineAsset;

                        TimelineClip tc = null;
                        // For each clip in the "Steering" timeline track
                        foreach (var clip in ta.GetRootTrack(0).GetClips())
                        {
                            // If this clip is the one that starts with the timeline clip, then this is the clip 
                            ///(NOTE: this assumes that steering unity instruction starts with the beginning of the action. If not, then we need to update the mappingActionSegIDsToDurativeClips
                            // ClipStart ------------- ClipEnd
                            // ---intoClipStart ---- SegEnd---
                            if (clip.start >= timelineclip.start && clip.start <= intoClipStart + 0.06 && clip.end > intoClipStart)
                            {
                                tc = clip;
                                break;
                            }
                        }
                        if (tc == null)
                        {
                            
                            throw new System.Exception("did not find right timeline clip");
                        }


                        // This is the clip we're looking for. 
                        var sa = tc.asset as SteeringAsset;
 
                        // Find where end of previous clip
                        var howMuchOfDistance = actionSeg.startPercent * Vector3.Distance(sa.startPos, sa.endPos);
                        var direction = (sa.endPos - sa.startPos).normalized;

                        whereObjectWillBeAtStart = sa.startPos + (float)howMuchOfDistance * direction;
                        whereObjectWillBeAtEnd = sa.startPos + (float)(actionSeg.endPercent * Vector3.Distance(sa.startPos, sa.endPos)) * direction;


                        if (actionSeg.startPercent > 0)
                        {

                            // Save relevant information
                            var originalArrive = sa.arrive;
                            var originalDestination = sa.endPos;

                            // set new end position
                            sa.endPos = whereObjectWillBeAtStart;

                            var remainingTime = (tc.end - intoClipStart);

                            // This clip will no longer require an Arrival slow-down.
                            sa.arrive = false;

                            tc.duration = tc.duration - remainingTime;

                            // create a new clip from endPos to startpos
                            var remainderClip = ta.GetRootTrack(0).CreateClip<SteeringAsset>();

                            // REPLACING the timeline clip's reference, because now this is the clip that we are cutting to.
                            var newClipInfo = new ClipInfo(timelineclip.director, intoClipStart, remainingTime, tc.displayName + "-continued");

                            // Bind clip to fabula director timeline
                            SteerClip(timelineclip.director, remainderClip, targetOfFocus.gameObject, sa.endPos, originalDestination, false, originalArrive, sa.master, newClipInfo);
                        }

                        // Second, if this clip is navigation, then we need to reposition camera, possibly creating a new camera (but only if it's not a "follow" camera).
                        /// if it's a "follow" camera, then it should place camera relative to targetOfFocus.gameObject
                        // 1) calculate displacement of location referenced by cameraClone with halfwayPoint of distance traveled
                        // 2) transform cameraClone by displacement



                        /// if it's a "stationary" camera, then we should place it halfway between travel distance observed in action segment.
                        if (CameraAimType == 0)
                        {
                            // stationary cam
                            // displace by halfway point from starting
                            halfwayPointDistanceTraveled = whereObjectWillBeAtStart + (whereObjectWillBeAtEnd - whereObjectWillBeAtStart) / 2;
                            var displacement = halfwayPointDistanceTraveled - targetLocation.position;
                            displacement = new Vector3(displacement.x, 0f, displacement.z);

                            cameraClone.transform.position = cameraClone.transform.position + displacement;
                            var fakeGO = new GameObject();
                            fakeGO.transform.position = halfwayPointDistanceTraveled;
                            PanClip((float)CI.start, (float)CI.duration, CI.display, cvc, fakeGO.transform);
                            // for right now, set to look at location
                            cvc.m_LookAt = fakeGO.transform;
                        }
                        else if (CameraAimType == 1)
                        {

                            //follow cam -- displace it from sa.startPos to sa.EndPos
                            // do not displace.
                            var displacement = whereObjectWillBeAtStart - targetLocation.position;
                            displacement = new Vector3(displacement.x, 0f, displacement.z);
                            cameraClone.transform.position = cameraClone.transform.position + displacement;

                            cameraClone.transform.LookAt(targetLocation);

                            //cvc.GetCinemachineComponent<>
                            cvc.m_Follow = targetLocation;
                            cvc.AddCinemachineComponent<CinemachineFramingTransposer>();
                            //var cft = cvc.GetComponent<CinemachineFramingTransposer>();

                            // Now, reposition camera
                            //cameraClone.transform.position = cameraClone.transform.position + displacement;
                            //cameraClone.transform.LookAt(targetLocation);
                            FollowClip((float)CI.start, (float)CI.duration, CI.display, cvc, targetOfFocus);
                        }
                        else
                        {
                            // Pan Cam
                            halfwayPointDistanceTraveled = whereObjectWillBeAtStart + (whereObjectWillBeAtEnd - whereObjectWillBeAtStart) / 2;
                            var displacement = halfwayPointDistanceTraveled - targetLocation.position;
                            displacement = new Vector3(displacement.x, 0f, displacement.z);

                            cameraClone.transform.position = cameraClone.transform.position + displacement;
                            cvc.m_LookAt = targetLocation;

                            PanClip((float)CI.start, (float)CI.duration, CI.display, cvc, targetOfFocus);
                        }
                    }
                    else
                    {
                        // no displacement
                        cvc.m_LookAt = targetLocation;

                        // Create a CameraFocusClip - essentially, a pan.
                        PanClip((float)CI.start, (float)CI.duration, CI.display, cvc, targetOfFocus);
                    }

                    // All default clips should focus on target
                    FocusClip((float)CI.start, (float)CI.duration, CI.display, ccb, targetOfFocus);

                    // Create a TimeTravel Clip
                    TimeClip((float)CI.start, (float)CI.duration, CI.display, timelineclip.director, (float)intoClipStart);

                    // Create a Cinemachine Timeline Clip
                    FilmClip((float)CI.start, (float)CI.duration, CI.display, cvc);

                    

                    accumulatedTime += amountOfDuration;
                }
                startTime += accumulatedTime;
            }

            director.playableAsset = timeline;
          
        }

        public static GameObject FindCameraInstance(GameObject CamerasHost, CamSchema criteria)
        {
            for (int i = 0; i < CamerasHost.transform.childCount; i++)
            {
                var camToCheck = CamerasHost.transform.GetChild(i);
                var camAttributes = camToCheck.GetComponent<CamAttributesStruct>();
                if (criteria.IsConsistent(camAttributes.AsSchema()))
                {
                    return camToCheck.gameObject;
                }
            }
            return null;
        }

        public void TimeBind(TimeTravelAsset tta, PlayableDirector fabulaPD, float new_val)
        {

            tta.PD.exposedName = UnityEditor.GUID.Generate().ToString();
            tta.newTime = new_val;
            director.SetReferenceValue(tta.PD.exposedName, fabulaPD);

        }

        public void CamBind(CinemachineShot cshot, CinemachineVirtualCamera vcam)
        {
            cshot.VirtualCamera.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(cshot.VirtualCamera.exposedName, vcam);
        }

        public void FocusBind(CamFocusAsset fclip, CinemachineCameraBody ccb, Transform focus)
        {
            fclip.CCB.exposedName = UnityEditor.GUID.Generate().ToString();
            fclip.Target.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(fclip.CCB.exposedName, ccb);
            director.SetReferenceValue(fclip.Target.exposedName, focus);
        }

        public void FollowBind(CamFollowAsset fclip, CinemachineVirtualCamera cvc, Transform focus)
        {
            fclip.CVC.exposedName = UnityEditor.GUID.Generate().ToString();
            fclip.Target.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(fclip.CVC.exposedName, cvc);
            director.SetReferenceValue(fclip.Target.exposedName, focus);
        }

        public void PanBind(CamPanAsset fclip, CinemachineVirtualCamera cvc, Transform focus)
        {
            fclip.CVC.exposedName = UnityEditor.GUID.Generate().ToString();
            fclip.Target.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(fclip.CVC.exposedName, cvc);
            director.SetReferenceValue(fclip.Target.exposedName, focus);
        }

        public void TimeClip(float start, float duration, string displayname, PlayableDirector fabulaPD, float newTime)
        {

            var tc = ttTrack.CreateClip<TimeTravelAsset>();
            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayname;
            var time_travel_clip = tc.asset as TimeTravelAsset;
            TimeBind(time_travel_clip, fabulaPD, newTime);
        }

        public void FilmClip(float start, float duration, string displayName, CinemachineVirtualCamera cvc)
        {

            TimelineClip tc = filmTrack.CreateDefaultClip();

            tc.start = start;// + .015f;// .06f;
            tc.duration = duration;
            tc.displayName = displayName;

            var film_clip = tc.asset as CinemachineShot;
            CamBind(film_clip, cvc);
        }

        public void FocusClip(float start, float duration, string displayName,  CinemachineCameraBody ccb, Transform focus)
        {
            TimelineClip tc = focusTrack.CreateClip<CamFocusAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayName;

            var focus_clip = tc.asset as CamFocusAsset;
            FocusBind(focus_clip,ccb, focus);
        }

        public void FollowClip(float start, float duration, string displayName, CinemachineVirtualCamera cvc, Transform focus)
        {
            TimelineClip tc = followTrack.CreateClip<CamFollowAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayName;

            var focus_clip = tc.asset as CamFollowAsset;
            FollowBind(focus_clip, cvc, focus);
        }

        public void PanClip(float start, float duration, string displayName, CinemachineVirtualCamera cvc, Transform focus)
        {
            TimelineClip tc = panTrack.CreateClip<CamPanAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayName;

            var focus_clip = tc.asset as CamPanAsset;
            PanBind(focus_clip, cvc, focus);
        }


        public static void SteerBind(PlayableDirector dir, SteeringAsset sa, GameObject boid, Vector3 startSteer, Vector3 endSteer, bool depart, bool arrive, bool isMaster)
        {
            sa.Boid.exposedName = UnityEditor.GUID.Generate().ToString();
            sa.arrive = arrive;
            sa.depart = depart;
            sa.startPos = startSteer;
            sa.endPos = endSteer;
            sa.master = isMaster;
            dir.SetReferenceValue(sa.Boid.exposedName, boid);
        }

        public static void SteerClip(PlayableDirector dir, TimelineClip steerClip, GameObject go, Vector3 startPos, Vector3 goalPos, bool depart, bool arrival, bool isMaster, ClipInfo CI)
        {
            steerClip.start = CI.start + 0.03f;
            steerClip.duration = CI.duration - .06f;
            steerClip.displayName = CI.display;
            SteeringAsset steer_clip = steerClip.asset as SteeringAsset;
            SteerBind(dir, steer_clip, go, startPos, goalPos, depart, arrival, isMaster);
        }

    }
}
