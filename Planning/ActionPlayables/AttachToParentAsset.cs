﻿using UnityEngine;
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

}