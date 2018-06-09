using Cinemachine;
using Cinemachine.Timeline;
using System.Collections;
using System.Collections.Generic;
using TimelineClipsNamespace;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using PlanningNamespace;

namespace CameraNamespace
{
    public class CamPlan : MonoBehaviour
    {

        public bool execute = false;

        // Timeline Fields
        public PlayableDirector playableDirector;
        public TimelineAsset executeTimeline;
        public TrackAsset lerpTrack, ctrack, attachTrack, ttTrack, filmTrack;

        public void InitiateExternally()
        {
            playableDirector = GetComponent<PlayableDirector>();

            executeTimeline = (TimelineAsset)ScriptableObject.CreateInstance("TimelineAsset");
            lerpTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "lerpTrack");
            attachTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "attachTrack");
            ctrack = executeTimeline.CreateTrack<ControlTrack>(null, "control_track");
            ttTrack = executeTimeline.CreateTrack<PlayableTrack>(null, "timeTravelTrack");
            filmTrack = executeTimeline.CreateTrack<CinemachineTrack>(null, "film_track");

            var main_camera_object = GameObject.FindGameObjectWithTag("MainCamera");
            playableDirector.SetGenericBinding(filmTrack, main_camera_object);
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
                playableDirector.Play(executeTimeline);
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

        public void TimeBind(TimeTravelAsset tta, PlayableDirector fabulaPD, float new_val)
        {

            tta.PD.exposedName = UnityEditor.GUID.Generate().ToString();
            tta.newTime = new_val;
            playableDirector.SetReferenceValue(tta.PD.exposedName, fabulaPD);

        }

        public void CamBind(CinemachineShot cshot, CinemachineVirtualCamera vcam)
        {
            cshot.VirtualCamera.exposedName = UnityEditor.GUID.Generate().ToString();
            playableDirector.SetReferenceValue(cshot.VirtualCamera.exposedName, vcam);
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

            tc.start = start + .015f;// .06f;
            tc.duration = duration;
            tc.displayName = displayName;

            var film_clip = tc.asset as CinemachineShot;
            CamBind(film_clip, cvc);
        }
    }
}