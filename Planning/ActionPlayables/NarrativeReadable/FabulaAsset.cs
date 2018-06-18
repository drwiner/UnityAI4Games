using BoltFreezer.Camera.CameraEnums;
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
        public Orient agentOrient = Orient.None;
        [SerializeField]
        public string location = "";

        public UnityActionOperator schema;

        //protected ScriptPlayable<FabulaPlayable> playableFabula;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            
            var playable = ScriptPlayable<FabulaPlayable>.Create(graph);

            var fabPlayable = playable.GetBehaviour();

            //var schema = Schema.GetComponent<UnityActionOperator>();
            Schema.defaultValue = null;
            schema = Schema.Resolve(playable.GetGraph().GetResolver());
            Debug.Log(schema.name);

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


}