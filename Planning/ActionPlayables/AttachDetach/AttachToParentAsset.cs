using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class AttachToParent : PlayableAsset
    {
        public ExposedReference<GameObject> Child;
        public ExposedReference<GameObject> Parent;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AttachToParentPlayable>.Create(graph);
            var attachToParentPlayable = playable.GetBehaviour();

            var parent = Parent.Resolve(playable.GetGraph().GetResolver());
            var child = Child.Resolve(playable.GetGraph().GetResolver());

            attachToParentPlayable.Initialize(parent, child);
            return playable;
        }
    }

    public class DettachToParent : PlayableAsset
    {
        public ExposedReference<GameObject> Child;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DettachToParentPlayable>.Create(graph);
            var dettachToParentPlayable = playable.GetBehaviour();

            var child = Child.Resolve(playable.GetGraph().GetResolver());

            dettachToParentPlayable.Initialize(child);
            return playable;
        }
    }

    public class Dettach2ToParent : PlayableAsset
    {
        public ExposedReference<GameObject> Child;
        public ExposedReference<GameObject> OriginalParent;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<Dettach2ToParentPlayable>.Create(graph);
            var dettach2ToParentPlayable = playable.GetBehaviour();

            var child = Child.Resolve(playable.GetGraph().GetResolver());
            var oldParent = OriginalParent.Resolve(playable.GetGraph().GetResolver());

            dettach2ToParentPlayable.Initialize(child, oldParent);
            return playable;
        }
    }

}