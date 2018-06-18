using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class CamFollowAsset : PlayableAsset
    {
        public ExposedReference<CinemachineVirtualCamera> CVC;
        public ExposedReference<Transform> Target;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CamFollowPlayable>.Create(graph);
            var camFollowPlayable = playable.GetBehaviour();

            var cvc = CVC.Resolve(playable.GetGraph().GetResolver());
            var transf = Target.Resolve(playable.GetGraph().GetResolver());

            camFollowPlayable.Initialize(cvc, transf);
            return playable;
        }
    }

}