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
    public class ConstraintAsset : PlayableAsset
    {
        public List<EditingConstraints> editingConstraints;

        [SerializeField]
        public List<string> Constraints = new List<string>();


        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            
            var playable = ScriptPlayable<FabulaPlayable>.Create(graph);
           
            return playable;
        }

    }


}