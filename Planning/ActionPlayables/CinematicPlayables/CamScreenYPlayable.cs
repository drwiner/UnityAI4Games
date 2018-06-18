using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class CamScreenYPlayable : PlayableBehaviour
    {
        private CinemachineVirtualCamera _cvc;
        private double _target;
        private double _oldTarget;

        public void Initialize(CinemachineVirtualCamera cvc, double target)
        {
            _cvc = cvc;
            _target = target;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _oldTarget = _cvc.GetCinemachineComponent<CinemachineComposer>().m_ScreenY;
            _cvc.GetCinemachineComponent<CinemachineComposer>().m_ScreenY = (float)_target;

        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _oldTarget = _cvc.GetCinemachineComponent<CinemachineComposer>().m_ScreenY;
        }
    }

}