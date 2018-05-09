using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TimelineClipsNamespace
{

    public class OrientToObjectAsset : PlayableAsset
    {
        public ExposedReference<GameObject> ObjectToMove;
        public Vector3 endPos;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<OrientToObjectPlayable>.Create(graph);
            var orientToObjectPlayable = playable.GetBehaviour();

            var objectToMove = ObjectToMove.Resolve(playable.GetGraph().GetResolver());

            orientToObjectPlayable.Initialize(objectToMove, endPos);
            return playable;
        }
    }

}