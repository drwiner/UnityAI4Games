using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class CamScreenCompositionAsset : PlayableAsset
    {
        public ExposedReference<CinemachineVirtualCamera> CVC;
        public BoltFreezer.Utilities.Tuple<double, double> Target;
        public double x, y;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CamScreenCompositionPlayable>.Create(graph);
            var camPanPlayable = playable.GetBehaviour();

            var cvc = CVC.Resolve(playable.GetGraph().GetResolver());

            x = Target.First;
            y = Target.Second;

            camPanPlayable.Initialize(cvc, Target);
            return playable;
        }
    }

}