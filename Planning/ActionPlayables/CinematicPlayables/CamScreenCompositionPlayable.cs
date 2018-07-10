using BoltFreezer.Utilities;
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class CamScreenCompositionPlayable : PlayableBehaviour
    {
        private CinemachineVirtualCamera _cvc;
        private Tuple<double, double> _target;
        private Tuple<double, double> _oldTarget;
        private CinemachineComposer cmc;

        public void Initialize(CinemachineVirtualCamera cvc, Tuple<double, double> target)
        {
            _cvc = cvc;
            _target = target;
            cmc = _cvc.GetCinemachineComponent<CinemachineComposer>();
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _oldTarget = new Tuple<double, double>(cmc.m_ScreenX, cmc.m_ScreenY);
            cmc.m_ScreenX = (float)_target.First;
            cmc.m_ScreenY = (float)_target.Second;

        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _oldTarget = new Tuple<double, double>(cmc.m_ScreenX, cmc.m_ScreenY);
        }
    }

}