using PlanningNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    [Serializable]
    public class FabulaAsset : PlayableAsset
    {
        [SerializeField]
        public UnityActionOperator Schema;
        [SerializeField]
        public List<string> Constraints;
        [SerializeField]
        public int agentOrient;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FabulaPlayable>.Create(graph);
            var fabPlayable = playable.GetBehaviour();

            //var schema = Schema.Resolve(playable.GetGraph().GetResolver());

            fabPlayable.Initialize(Schema, Constraints);
            return playable;
        }
    }

    //[Serializable]
    //public class Constraint
    //{
    //    [SerializeField]
    //    public string name;

    //}

}