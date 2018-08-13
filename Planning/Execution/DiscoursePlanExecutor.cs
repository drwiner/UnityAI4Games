using BoltFreezer.Camera;
using BoltFreezer.Camera.CameraEnums;
using BoltFreezer.Interfaces;
using BoltFreezer.Utilities;
using CameraNamespace;
using Cinemachine;
using Cinemachine.Timeline;
using Cinematography;
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

       // protected Dictionary<GameObject, GameObject> LastAttachMap;

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

        //    LastAttachMap = new Dictionary<GameObject, GameObject>();

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
                
                // Create camera from schema
                var cameraClone = CamGen.CreateCameraFromSchema(camSchema);
                
                cameraClone.transform.parent = cameraOptionsHost.transform;
                //var cameraInstance = CamPlan.FindCameraInstance(cameraOptionsHost, camSchema);
                //var cameraClone = GameObject.Instantiate(cameraInstance);

                cameraClone.SetActive(true);

                // target location
                Transform targetLocation = GameObject.Find(camSchema.targetLocation).transform;

                // Relevant Camera components for cinematography
                var cvc = cameraClone.GetComponent<CinemachineVirtualCamera>();
                var ccb = cameraClone.GetComponent<CinemachineCameraBody>();

                //CamDirective CameraAimType = step.directive;

                //cvc.m_LookAt = GameObject.Find(camSchema.targetLocation).transform;

                // for each action segment, create a time travel clip to the 
                var targetSchema = step.TargetDetails;

                string lastFocus = "";
                TimelineClip lastFocusAsset = null;

                string lastPan = "";
                TimelineClip lastPanAsset = null;

                string lastCoord = "";
                TimelineClip lastCoordAsset = null;
                //var lastDirective = null;
                //var lastCoordinate = null;

                bool hasNavBeforeEnd = false;

                accumulatedTime = 0;
                //foreach (var actionSeg in targetSchema.ActionSegs)
                for (int asegIndex = 0; asegIndex < targetSchema.ActionSegs.Count; asegIndex++)
                {
                    var actionSeg = targetSchema.ActionSegs[asegIndex];

                    var targetOfFocus = CreateTarget(actionSeg.targetVarName).transform;

                    // The fabula ClipInfo associated with entire Action (in future, could track all sub-clips
                    var timelineclip = mappingActionSegIDsToDurativeClips[actionSeg.ActionID];

                    // Duration of clip is based on start and end percent this camera shows
                    var amountOfDuration = (timelineclip.duration * actionSeg.endPercent) - (timelineclip.duration * actionSeg.startPercent);

                    // Start time of clip
                    var intoClipStart = timelineclip.start + (actionSeg.startPercent * timelineclip.duration);

                    // Create display
                    var displayOfClip = string.Format("{0} of {1} from {2} to {3}", camSchema.scale, timelineclip.display, actionSeg.startPercent.ToString(), actionSeg.endPercent.ToString());
                    var originalCameraCloneName = cameraClone.name;
                    cameraClone.name += "_//_" + timelineclip.display + "_// focusOn: " + actionSeg.targetVarName + "// achor: " + targetSchema.location;

                    // Create ClipInfo unit
                    var CI = new ClipInfo(director, startTime + accumulatedTime, amountOfDuration, displayOfClip);

                    // Reference to Unity Action Operator game object
                    var unityActionOperatorHost = GameObject.Find(timelineclip.display);

                    // If this Unity Action is a "navigation" action, then extra steps are required
                    if (unityActionOperatorHost.tag.Equals("Navigation"))
                    {
                        bool lastSeg = false;
                        if (asegIndex == targetSchema.ActionSegs.Count - 1 && actionSeg.directive.Equals(CamDirective.Follow))
                        {
                            lastSeg = true;
                        }
                        else
                        {
                            hasNavBeforeEnd = true;
                        }
                        FilmNavigation(cameraClone, cvc, ccb, CI, timelineclip, actionSeg, actionSeg.directive, intoClipStart, targetOfFocus, targetLocation, lastSeg);
                    }
                    else // It's not a navigation action.
                    {
                        if (actionSeg.directive.Equals(CamDirective.Stationary))
                        {
                            cvc.m_LookAt = targetLocation;
                        }
                        else if (actionSeg.directive.Equals(CamDirective.GroupAim))
                        {
                            // Set orientation of camera to a CinematicGroupTarget

                            //var gc = cvc.AddCinemachineComponent<CinemachineGroupComposer>();
                            //var gc = cvc.AddCinemachineComponent<CinemachineComposer>();

                            if (targetOfFocus.name.Equals(lastPan))
                            {
                                lastPanAsset.duration += (float)CI.duration;
                            }
                            else
                            {
                                cvc.m_LookAt = targetOfFocus;
                                lastPanAsset = PanClip((float)CI.start, (float)CI.duration, CI.display, cvc, targetOfFocus);
                                lastPan = targetOfFocus.name;
                            }

                            //return groupTarget.transform;

                        }
                        else
                        {

                            if (targetOfFocus.name.Equals(lastPan))
                            {
                                lastPanAsset.duration += (float)CI.duration;
                            }
                            else
                            {
                                cvc.m_LookAt = targetOfFocus;
                                lastPanAsset = PanClip((float)CI.start, (float)CI.duration, CI.display, cvc, targetOfFocus);
                                lastPan = targetOfFocus.name;
                            }
                        }

                            //FilmStationary(cameraClone, cvc, ccb, CI, actionSeg, actionSeg.directive, targetOfFocus, targetLocation);
                        }

                    // Add new focus clip if app
                    if (targetOfFocus.name.Equals(lastFocus))
                    {
                        lastFocusAsset.duration += (float)CI.duration;
                    }
                    else
                    {
                        var focusAsset = FocusClip((float)CI.start, (float)CI.duration, CI.display, ccb, targetOfFocus);
                        lastFocusAsset = focusAsset;
                        lastFocus = targetOfFocus.name;
                    }


                    // Each action seg has composition
                    if (actionSeg.screenxy.Equals(lastCoord))
                    {
                        lastCoordAsset.duration += (float)CI.duration;
                    }

                    else
                    {
                        var xzValue = CinematographyAttributes.ScreenComposition[actionSeg.screenxy];

                        lastCoordAsset = ScreenCoordinateClip((float)CI.start, (float)CI.duration, CI.display, cvc, xzValue);
                        lastCoord = actionSeg.screenxy.ToString();
                    }
                        // Create a Cinemachine Timeline Clip
                        // Compare lastFilmClip to this one
                        //if (originalCameraCloneName.Equals(lastCameraName))
                        //{
                        //    // Alter ending time
                        //    var lastClip = filmTrack.GetClips().ToList()[lastFilmClipIndex];
                        //    lastClip.duration += CI.duration;
                        //}
                        //else
                        //{
                        //    FilmClip((float)CI.start, (float)CI.duration, CI.display, cvc);
                        //}

                        //lastCameraName = cameraClone.name;
                        //lastFilmClipIndex++;

                    accumulatedTime += amountOfDuration;
                }
                // Create a TimeTravel Clip
                var firstTimelineClip = mappingActionSegIDsToDurativeClips[targetSchema.ActionSegs[0].ActionID];
                var directorOfThisSegment = firstTimelineClip.director;
                var intoClipStartFirstSegment = firstTimelineClip.start + (targetSchema.ActionSegs[0].startPercent * firstTimelineClip.duration);
                TimeClip((float)startTime, (float)accumulatedTime- .12f, cameraClone.name, directorOfThisSegment, (float)intoClipStartFirstSegment);
                FilmClip((float)startTime, (float)accumulatedTime, cameraClone.name, cvc);
                startTime += accumulatedTime;

                // Add clip at end that returns camera to original position.
                if (hasNavBeforeEnd)
                {
                    var lerpClip = lerpTrack.CreateClip<LerpToMoveObjectAsset>();
                    lerpClip.start = startTime;
                    lerpClip.duration = 0.1f;
                    lerpClip.displayName = "return camera to starting pos";
                    LerpToMoveObjectAsset lClip = lerpClip.asset as LerpToMoveObjectAsset;
                    TransformToBind(lClip, cameraClone, cameraClone.transform);
                }
            }

            director.playableAsset = timeline;
          
        }

        public GameObject CreateTarget(string targetNames)
        {
            var tsplit = targetNames.Split(' ');

            if (tsplit.Count() == 1)
            {
                return GameObject.Find(targetNames);
            }

            var groupTarget = new GameObject(targetNames);
            var gt = groupTarget.AddComponent<CinemachineTargetGroup>();
            gt.m_Targets = new CinemachineTargetGroup.Target[tsplit.Count()];

            for (int i =0; i < tsplit.Count(); i++)
            {
                if (tsplit[i].Equals(""))
                {
                    // this way it doesn't matter if we've got a blank element
                    gt.m_Targets[i].weight = 0;
                    continue;
                }
                gt.m_Targets[i].target = GameObject.Find(tsplit[i]).transform;
                gt.m_Targets[i].weight = 1;
            }
            return groupTarget;
        }

        //public Transform FilmStationary(GameObject cameraClone, CinemachineVirtualCamera cvc, CinemachineCameraBody ccb, ClipInfo CI, ActionSeg actionSeg, CamDirective CameraAimType, Transform targetOfFocus, Transform targetLocation)
        //{
            

            

        //    return targetOfFocus;
        //}

        public void FilmNavigation(GameObject cameraClone, CinemachineVirtualCamera cvc, CinemachineCameraBody ccb, ClipInfo CI, ClipInfo timelineclip, 
            ActionSeg actionSeg, CamDirective CameraAimType, double intoClipStart, Transform targetOfFocus, Transform targetLocation, bool lastSeg)
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

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
            ///////////////  Split Navigation Clip into parts based on where the camera will cut - enables successful time travel //////
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
            if (actionSeg.startPercent > 0)
            {
                // Save relevant information
                var originalArrive = sa.arrive;
                var originalDestination = sa.endPos;

                // set new end position
                //sa.endPos = whereObjectWillBeAtStart;

                var remainingTime = (tc.end - intoClipStart);

                // This clip will no longer require an Arrival slow-down. // Removed, and that's it.
                //sa.arrive = false;

                //tc.duration = tc.duration - remainingTime;

                // create a new clip from endPos to startpos
                var remainderClip = ta.GetRootTrack(0).CreateClip<SteeringAsset>();

                // REPLACING the timeline clip's reference, because now this is the clip that we are cutting to.
                var newClipInfo = new ClipInfo(timelineclip.director, intoClipStart-.12f, remainingTime + .12f, tc.displayName + "-continued");

                // Agent doing steering; may not be camera's target.
                var boid = GameObject.Find(tc.displayName.Split(' ')[1]);

                // Bind clip to fabula director timeline
                SteerClip(timelineclip.director, remainderClip, boid, whereObjectWillBeAtStart, originalDestination, false, originalArrive, false, newClipInfo);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
            ///////////////  Determine how to position and move camera for/during navigation ///////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            /// if it's a "stationary" camera, then we should place it halfway between travel distance observed in action segment.
            if (CameraAimType.Equals(CamDirective.Stationary))
            {
                // stationary cam
                // displace by halfway point from starting

                
                halfwayPointDistanceTraveled = whereObjectWillBeAtStart + (whereObjectWillBeAtEnd - whereObjectWillBeAtStart) / 2;

                /////////////////////////// Legacy: now, this is calculated a priori //////////////////////////////////////////////
                //var displacement = halfwayPointDistanceTraveled - targetLocation.position;
                //displacement = new Vector3(displacement.x, 0f, displacement.z);
                //cameraClone.transform.position = cameraClone.transform.position + displacement;
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var fakeGO = new GameObject();
                fakeGO.transform.position = halfwayPointDistanceTraveled;

                PanClip((float)CI.start, (float)CI.duration, CI.display, cvc, fakeGO.transform);
                // for right now, set to look at location
                cvc.m_LookAt = fakeGO.transform;
            }
            else if (CameraAimType.Equals(CamDirective.Follow))
            {

                /////////////////////////// Legacy: now, this is calculated a priori //////////////////////////////////////////////
                //var displacement = whereObjectWillBeAtStart - targetLocation.position;
                //displacement = new Vector3(displacement.x, 0f, displacement.z);
                //cameraClone.transform.position = cameraClone.transform.position + displacement;
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                //cameraClone.transform.LookAt(targetLocation);
                cvc.m_LookAt = targetLocation;

                PanClip((float)CI.start + 0.0833f, (float)CI.duration - 0.0833f, CI.display, cvc, targetOfFocus);

                var parent = targetOfFocus;
                var attachClip = attachTrack.CreateClip<AttachToParent>();
                attachClip.start = CI.start;
                attachClip.duration = CI.duration - .22f;
                attachClip.displayName = string.Format("attach parent={0} ;{1}", parent.name, CI.display);
                AttachToParent aClip = attachClip.asset as AttachToParent;
                AttachBind(aClip, parent.gameObject, cameraClone);


                var dettachClip = attachTrack.CreateClip<Dettach2ToParent>();
                
                dettachClip.start = CI.start + CI.duration - 0.21f;
                dettachClip.duration = 0.2f;
                dettachClip.displayName = string.Format("de-attach parent={0} ; {1}", parent.name, CI.display);
                Dettach2ToParent dClip = dettachClip.asset as Dettach2ToParent;
                DettachBind(dClip, cameraClone, cameraClone.transform.parent.gameObject);

                
                // Also, let's return the camera to its location
                
                if (lastSeg)
                {
                    var lerpClip = lerpTrack.CreateClip<LerpToMoveObjectAsset>();
                    lerpClip.start = CI.start + CI.duration;
                    lerpClip.duration = 0.1f;
                    lerpClip.displayName = "return camera to starting pos";
                    LerpToMoveObjectAsset lClip = lerpClip.asset as LerpToMoveObjectAsset;
                    TransformToBind(lClip, cameraClone, cameraClone.transform);
                }
                else
                {
                    // it's not the last clip, so we need to add some safe amount of time before clip.
                }
                

                //cvc.m_Follow = targetLocation;
                //var cft = cvc.AddCinemachineComponent<CinemachineTransposer>();

                //FollowClip((float)CI.start, (float)CI.duration, CI.display, cvc, targetOfFocus);
            }
            else if (CameraAimType.Equals(CamDirective.GroupAim))
            {
                // Set orientation of camera to a CinematicGroupTarget
                var groupTarget = new GameObject();
                var gt = groupTarget.AddComponent<CinemachineTargetGroup>();

                // The two targets are the orientTowards and the targetlocation
                gt.m_Targets = new CinemachineTargetGroup.Target[2];
                //gt.m_Targets[0] = new CinemachineTargetGroup.Target();
                gt.m_Targets[0].target = targetLocation;
                gt.m_Targets[0].weight = 1;
                gt.m_Targets[1].target = targetOfFocus;
                gt.m_Targets[1].weight = 1;

                cvc.m_LookAt = groupTarget.transform;
                //cvc.m_LookAt = targetOfFocus;
                //cvc.m_Follow = groupTarget.transform;

                // Group Composer causes dolly and zoom behavior
                //var gc = cvc.AddCinemachineComponent<CinemachineGroupComposer>();


                //gc.m_ScreenX = //
                //var cClearShot = cameraClone.AddComponent<CinemachineClearShot>();
                // cClearShot.m_LookAt = groupTarget.transform;

                PanClip((float)CI.start, (float)CI.duration, CI.display, cvc, groupTarget.transform);
            }

            else //default is pan
            {
                // Pan Cam
                //
                //halfwayPointDistanceTraveled = whereObjectWillBeAtStart + (whereObjectWillBeAtEnd - whereObjectWillBeAtStart) / 2;
                //var displacement = halfwayPointDistanceTraveled - targetLocation.position;
                //displacement = new Vector3(displacement.x, 0f, displacement.z);
               // cameraClone.transform.position = cameraClone.transform.position + displacement;
                cvc.m_LookAt = targetLocation;
                

                PanClip((float)CI.start + 0.0833f, (float)CI.duration - 0.0833f, CI.display, cvc, targetOfFocus);
            }

        }

        //public static GameObject FindCameraInstance(GameObject CamerasHost, CamSchema criteria)
        //{
        //    for (int i = 0; i < CamerasHost.transform.childCount; i++)
        //    {
        //        var camToCheck = CamerasHost.transform.GetChild(i);
        //        var camAttributes = camToCheck.GetComponent<CamAttributesStruct>();
        //        if (criteria.IsConsistent(camAttributes.AsSchema()))
        //        {
        //            return camToCheck.gameObject;
        //        }
        //    }
        //    return null;
        //}

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

        public void ScreenCoordinateBind(CamScreenCompositionAsset xyasset, CinemachineVirtualCamera cvc, Tuple<double, double> screenxz)
        {
            xyasset.CVC.exposedName = UnityEditor.GUID.Generate().ToString();
            xyasset.Target = screenxz;
            director.SetReferenceValue(xyasset.CVC.exposedName, cvc);
        }

        public void AttachBind(AttachToParent atpObj, GameObject parent, GameObject child)
        {
            atpObj.Parent.exposedName = UnityEditor.GUID.Generate().ToString();
            atpObj.Child.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(atpObj.Parent.exposedName, parent);
            director.SetReferenceValue(atpObj.Child.exposedName, child);
        }

        public void DettachBind(Dettach2ToParent dtpObj, GameObject child, GameObject oldParent)
        {
            dtpObj.Child.exposedName = UnityEditor.GUID.Generate().ToString();
            dtpObj.OriginalParent.exposedName =  UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(dtpObj.Child.exposedName, child);
            director.SetReferenceValue(dtpObj.OriginalParent.exposedName, oldParent);
        }

        public void TransformToBind(LerpToMoveObjectAsset tpObj, GameObject obj_to_move, Transform end_pos)
        {

            tpObj.ObjectToMove.exposedName = UnityEditor.GUID.Generate().ToString();
            tpObj.LerpMoveTo.exposedName = UnityEditor.GUID.Generate().ToString();
            director.SetReferenceValue(tpObj.ObjectToMove.exposedName, obj_to_move);
            director.SetReferenceValue(tpObj.LerpMoveTo.exposedName, end_pos);
        }

        public TimelineClip ScreenCoordinateClip(float start, float duration, string displayname, CinemachineVirtualCamera cvc, Tuple<double, double> screenxz)
        {
            TimelineClip tc = screenTrack.CreateClip<CamScreenCompositionAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayname;

            var compClip = tc.asset as CamScreenCompositionAsset;
            ScreenCoordinateBind(compClip, cvc, screenxz);
            return tc;
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

        public TimelineClip FocusClip(float start, float duration, string displayName,  CinemachineCameraBody ccb, Transform focus)
        {
            TimelineClip tc = focusTrack.CreateClip<CamFocusAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayName;

            var focus_clip = tc.asset as CamFocusAsset;
            FocusBind(focus_clip,ccb, focus);
            return tc;
        }

        public TimelineClip FollowClip(float start, float duration, string displayName, CinemachineVirtualCamera cvc, Transform focus)
        {
            TimelineClip tc = followTrack.CreateClip<CamFollowAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayName;

            var focus_clip = tc.asset as CamFollowAsset;
            FollowBind(focus_clip, cvc, focus);
            return tc;
        }

        public TimelineClip PanClip(float start, float duration, string displayName, CinemachineVirtualCamera cvc, Transform focus)
        {
            TimelineClip tc = panTrack.CreateClip<CamPanAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayName;

            var focus_clip = tc.asset as CamPanAsset;
            PanBind(focus_clip, cvc, focus);
            return tc;
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
