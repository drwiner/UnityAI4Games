using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class CamPanAsset : PlayableAsset
    {
        public ExposedReference<CinemachineVirtualCamera> CVC;
        public ExposedReference<Transform> Target;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CamPanPlayable>.Create(graph);
            var camPanPlayable = playable.GetBehaviour();

            var cvc = CVC.Resolve(playable.GetGraph().GetResolver());
            var transf = Target.Resolve(playable.GetGraph().GetResolver());

            camPanPlayable.Initialize(cvc, transf);
            return playable;
        }
    }

}