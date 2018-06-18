using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class CamFocusAsset : PlayableAsset
    {
        public ExposedReference<CinemachineCameraBody> CCB;
        public ExposedReference<Transform> Target;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CamFocusPlayable>.Create(graph);
            var camFocusPlayable = playable.GetBehaviour();

            var ccb= CCB.Resolve(playable.GetGraph().GetResolver());
            var transf = Target.Resolve(playable.GetGraph().GetResolver());

            camFocusPlayable.Initialize(ccb, transf);
            return playable;
        }
    }

}