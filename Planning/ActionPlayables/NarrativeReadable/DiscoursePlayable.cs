using BoltFreezer.Camera;
using CameraNamespace;
using PlanningNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class DiscoursePlayable : PlayableBehaviour
    {
        private CamSchema _schema;
        private CamTargetSchema _tschema;
        private List<string> _constraints;

        public void Initialize(CamSchema schema, CamTargetSchema tschema, List<string> constraints)
        {
            _schema = schema;
            _tschema = tschema;
            _constraints = constraints;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playable.GetTime() <= 0 || _schema == null || _constraints == null)
                return;
        }


        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
            }
        }
    }



}