using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class CamFollowPlayable : PlayableBehaviour
    {
        private CinemachineVirtualCamera _cvc;
        private Transform _target;
        private Transform _oldTarget;

        public void Initialize(CinemachineVirtualCamera cvc, Transform target)
        {
            _cvc = cvc;
            _target = target;
        }


        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _oldTarget = _cvc.m_Follow;
            _cvc.m_Follow = _target;

        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _oldTarget = _cvc.m_LookAt;
        }
    }

}