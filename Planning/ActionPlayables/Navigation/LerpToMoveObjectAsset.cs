using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class LerpToMoveObjectAsset : PlayableAsset
{
    public ExposedReference<GameObject> ObjectToMove;
    public ExposedReference<Transform> LerpMoveTo;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<LerpToMoveObjectPlayable>.Create(graph);
        var lerpMoveObjectPlayable = playable.GetBehaviour();

        var objectToMove = ObjectToMove.Resolve(playable.GetGraph().GetResolver());
        var lerpMoveTo = LerpMoveTo.Resolve(playable.GetGraph().GetResolver());

        lerpMoveObjectPlayable.Initialize(objectToMove, lerpMoveTo);
        return playable;
    }
}
