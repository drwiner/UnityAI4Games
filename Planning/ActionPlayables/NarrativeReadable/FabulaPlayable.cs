using PlanningNamespace;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class FabulaPlayable : PlayableBehaviour
    {
        private UnityActionOperator _schema;
        private List<string> _constraints;

        public void Initialize(UnityActionOperator schema, List<string> constraints)
        {
            _schema = schema;
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