using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class CamScreenYAsset : PlayableAsset
    {
        public ExposedReference<CinemachineVirtualCamera> CVC;
        public double Target;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CamScreenYPlayable>.Create(graph);
            var camPanPlayable = playable.GetBehaviour();

            var cvc = CVC.Resolve(playable.GetGraph().GetResolver());

            camPanPlayable.Initialize(cvc, Target);
            return playable;
        }
    }

}