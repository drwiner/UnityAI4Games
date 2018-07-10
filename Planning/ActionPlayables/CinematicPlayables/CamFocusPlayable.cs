using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class CamFocusPlayable : PlayableBehaviour
    {
        private CinemachineCameraBody _ccb;
        private Transform _target;
        private Transform _oldTarget;

        public void Initialize(CinemachineCameraBody ccb, Transform target)
        {
            _ccb = ccb;
            _target = target;
            _oldTarget = ccb.FocusTransform;
        }


        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _oldTarget = _ccb.FocusTransform;

            _ccb.FocusTransform = _target;

        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _ccb.FocusTransform = _oldTarget;
        }
    }

}