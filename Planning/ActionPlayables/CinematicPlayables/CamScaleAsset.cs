using BoltFreezer.Camera.CameraEnums;
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class CamScaleAsset : PlayableAsset
    {
        public ExposedReference<CinemachineCameraBody> CCB;
        public FramingType Scale;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CamScalePlayable>.Create(graph);
            var camScalePlayable = playable.GetBehaviour();

            var ccb= CCB.Resolve(playable.GetGraph().GetResolver());

            camScalePlayable.Initialize(ccb, Scale);
            return playable;
        }
    }

}