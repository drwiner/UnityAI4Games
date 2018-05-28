﻿using CameraNamespace;
using PlanningNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace TimelineClipsNamespace
{
    public class DiscourseAsset : PlayableAsset
    {
        public CamSchema camSchema;
        public CamTargetSchema targetSchema;
        public List<string> Constraints;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DiscoursePlayable>.Create(graph);
            var discPlayable = playable.GetBehaviour();

            //var schema = Schema.Resolve(playable.GetGraph().GetResolver());

            discPlayable.Initialize(camSchema, targetSchema, Constraints);
            return playable;
        }
    }

}