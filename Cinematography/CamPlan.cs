using Cinemachine;
using Cinemachine.Timeline;
using System.Collections;
using System.Collections.Generic;
using TimelineClipsNamespace;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using PlanningNamespace;
using BoltFreezer.Camera;

namespace CameraNamespace
{
    public class CamPlan : MonoBehaviour
    {

        public bool execute = false;

        // Timeline Fields
        public static PlayableDirector playableDirector;
        public static TimelineAsset executeTimeline;
        public static TrackAsset lerpTrack, ctrack, attachTrack, ttTrack, filmTrack, focusTrack, panTrack;

        public void InitiateExternally()
        {
            playableDirector = GetComponent<PlayableDirector>();

            executeTimeline = (TimelineAsset)ScriptableObject.CreateInstance("TimelineAsset");
            lerpTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "lerpTrack");
            attachTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "attachTrack");
            ctrack = executeTimeline.CreateTrack<ControlTrack>(null, "control_track");
            ttTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "timeTravelTrack");
            filmTrack = executeTimeline.CreateTrack<CinemachineTrack>(null, "film_track");
            focusTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "focus_track");
            panTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "pan_track");

            var main_camera_object = GameObject.FindGameObjectWithTag("MainCamera");
            playableDirector.SetGenericBinding(filmTrack, main_camera_object);
        }


        public static void Execute(List<CamPlanStep> discourseSteps, Dictionary<int, ClipInfo> mappingActionSegIDsToDurativeClips,
            PlayableDirector fabDirector, PlayableDirector discDirector)
        {

            playableDirector = discDirector;

            executeTimeline = (TimelineAsset)ScriptableObject.CreateInstance("TimelineAsset");
            lerpTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "lerpTrack");
            attachTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "attachTrack");
            ctrack = executeTimeline.CreateTrack<ControlTrack>(null, "control_track");
            ttTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "timeTravelTrack");
            filmTrack = executeTimeline.CreateTrack<CinemachineTrack>(null, "film_track");
            focusTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "focus_track");

            var main_camera_object = GameObject.FindGameObjectWithTag("MainCamera");
            playableDirector.SetGenericBinding(filmTrack, main_camera_object);

            var cameraOptionsHost = GameObject.FindGameObjectWithTag("CameraHost");

            double startTime = 0;
            double accumulatedTime = 0;
            char[] charsToTrim = { '(', ')' };
            var CIList = new List<ClipInfo>();
            foreach (var step in discourseSteps)
            {
                // Extract Schemata
                var camSchema = step.CamDetails;

                var cameraInstance = FindCameraInstance(cameraOptionsHost, camSchema);
                var cameraClone = GameObject.Instantiate(cameraInstance);
                cameraClone.SetActive(true);
                var cvc = cameraClone.GetComponent<CinemachineVirtualCamera>();
                var ccb = cameraClone.GetComponent<CinemachineCameraBody>();

                // for each action segment, create a time travel clip to the 
                var targetSchema = step.TargetDetails;

                accumulatedTime = 0;
                foreach(var actionSeg in targetSchema.ActionSegs)
                {
                    var targetOfFocus = GameObject.Find(actionSeg.targetVarName).transform;
                    var timelineclip = mappingActionSegIDsToDurativeClips[actionSeg.ActionID];
                    // what percentofduration
                    var amountOfDuration = (timelineclip.duration * actionSeg.endPercent) - (timelineclip.duration * actionSeg.startPercent);
                    var displayOfClip = string.Format("{0} of {1} from {2} to {3}", camSchema.scale, timelineclip.display, actionSeg.startPercent.ToString(), actionSeg.endPercent.ToString());
                    var CI = new ClipInfo(playableDirector, startTime + accumulatedTime, amountOfDuration, displayOfClip);

                    // Create a TimeTravel Clip
                    TimeClip((float)CI.start, (float)CI.duration, CI.display, fabDirector, (float)timelineclip.start);

                    // Create a Cinemachine Timeline Clip
                    FilmClip((float)CI.start, (float)CI.duration, CI.display, cvc);

                    // Create a CameraFocusClip
                    FocusClip((float)CI.start, (float)CI.duration, CI.display, ccb, targetOfFocus);

                    accumulatedTime += amountOfDuration;
                }
                startTime += accumulatedTime;
            }

            playableDirector.playableAsset = executeTimeline;
            playableDirector.Play(executeTimeline);
        }

        public static GameObject FindCameraInstance(GameObject CamerasHost, CamSchema criteria)
        {
            for(int i = 0; i < CamerasHost.transform.childCount; i++)
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
            

        public void ResetExternally()
        {
            var tracksToDelete = executeTimeline.GetRootTracks();
            foreach (var track in tracksToDelete)
            {
                executeTimeline.DeleteTrack(track);
            }

            executeTimeline = null;
            ttTrack = null;
            lerpTrack = null;
            attachTrack = null;
            ctrack = null;
            filmTrack = null;
        }

        public void Update()
        {
            if (execute)
            {
                execute = false;
                var pa = GetComponent<PlayableDirector>();
                var ta = pa.playableAsset as TimelineAsset;

                var fa = GameObject.FindGameObjectWithTag("FabulaTimeline").GetComponent<CamPlan>();
                //pa.

                pa.Play(ta);
                //GetComponent<PlayableDirector>().Play(executeTimeline);
                //Execute();

            }
        }

        public void ExecuteExternally()
        {
            Debug.Log("Executing in Edit Mode");
            playableDirector.playableAsset = executeTimeline;
            playableDirector.Play(executeTimeline);
        }

        public void AddClip(CinemachineVirtualCamera cam, PlayableDirector FabulaPD, float startTime, float storyTime, float duration, string displayName)
        {
            TimeClip(startTime, duration, displayName, FabulaPD, storyTime);
            FilmClip(startTime, duration, displayName, cam);
        }

        public static void TimeBind(TimeTravelAsset tta, PlayableDirector fabulaPD, float new_val)
        {

            tta.PD.exposedName = UnityEditor.GUID.Generate().ToString();
            tta.newTime = new_val;
            playableDirector.SetReferenceValue(tta.PD.exposedName, fabulaPD);

        }

        public static void CamBind(CinemachineShot cshot, CinemachineVirtualCamera vcam)
        {
            cshot.VirtualCamera.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(cshot.VirtualCamera.exposedName, vcam);
        }

        public static void FocusBind(CamFocusAsset fclip, CinemachineCameraBody ccb, Transform focus)
        {
            fclip.CCB.exposedName = UnityEditor.GUID.Generate().ToString();
            fclip.Target.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(fclip.CCB.exposedName, ccb);
            playableDirector.SetReferenceValue(fclip.Target.exposedName, focus);
        }

        public static void PanBind(CamPanAsset fclip, CinemachineVirtualCamera cvc, Transform focus)
        {
            fclip.CVC.exposedName = UnityEditor.GUID.Generate().ToString();
            fclip.Target.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(fclip.CVC.exposedName, cvc);
            playableDirector.SetReferenceValue(fclip.Target.exposedName, focus);
        }

        public static void TimeClip(float start, float duration, string displayname, PlayableDirector fabulaPD, float newTime)
        {

            var tc = ttTrack.CreateClip<TimeTravelAsset>();
            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayname;
            var time_travel_clip = tc.asset as TimeTravelAsset;
            TimeBind(time_travel_clip, fabulaPD, newTime);
        }

        public static void FilmClip(float start, float duration, string displayName, CinemachineVirtualCamera cvc)
        {

            TimelineClip tc = filmTrack.CreateDefaultClip();

            tc.start = start + .015f;// .06f;
            tc.duration = duration;
            tc.displayName = displayName;

            var film_clip = tc.asset as CinemachineShot;
            CamBind(film_clip, cvc);
        }

        public static void FocusClip(float start, float duration, string displayName, CinemachineCameraBody ccb, Transform focus)
        {
            TimelineClip tc = focusTrack.CreateClip<CamFocusAsset>();

            tc.start = start;
            tc.duration = duration;
            tc.displayName = displayName;

            var focus_clip = tc.asset as CamFocusAsset;
            FocusBind(focus_clip, ccb, focus);
        }
    }
}