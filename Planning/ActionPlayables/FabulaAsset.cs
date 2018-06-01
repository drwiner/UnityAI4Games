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
        public ExposedReference<UnityActionOperator> Schema;
        [SerializeField]
        public List<string> Constraints = new List<string>();
        [SerializeField]
        public int agentOrient = - 1;
        [SerializeField]
        public string location = "";

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<FabulaPlayable>.Create(graph);
            var fabPlayable = playable.GetBehaviour();

            //var schema = Schema.GetComponent<UnityActionOperator>();
            Schema.defaultValue = null;
            var schema = Schema.Resolve(playable.GetGraph().GetResolver());
            if (schema == null)
            {
                fabPlayable.Initialize(null, Constraints);
            }
            else
            {
                fabPlayable.Initialize(schema, Constraints);
            }
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