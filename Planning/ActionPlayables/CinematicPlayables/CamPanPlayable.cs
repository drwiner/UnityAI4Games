using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class CamPanPlayable : PlayableBehaviour
    {
        private CinemachineVirtualCamera _cvc;
        private Transform _target;
        private Transform _oldTarget;

        public void Initialize(CinemachineVirtualCamera cvc, Transform target)
        {
            _cvc = cvc;
            _target = target;
            _oldTarget = cvc.m_LookAt;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            //_oldTarget = _cvc.m_LookAt;
            _cvc.m_LookAt = _target;

        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _cvc.m_LookAt = _oldTarget;
            //_oldTarget = _cvc.m_LookAt = _oldTarget;
        }
    }

}