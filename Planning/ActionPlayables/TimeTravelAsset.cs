using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class TimeTravelAsset : PlayableAsset
    {
        public ExposedReference<PlayableDirector> PD;
        public float newTime;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimeTravelPlayable>.Create(graph);
            var timeTravelPlayable = playable.GetBehaviour();

            var director = PD.Resolve(playable.GetGraph().GetResolver());

            timeTravelPlayable.Initialize(director, newTime);
            return playable;
        }
    }

}