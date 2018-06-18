using BoltFreezer.Camera;
using BoltFreezer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public interface IExecutor
    {
        PlayableDirector Director { get; set; }

        void Play();

        void Stop();
    }

    public interface IFabulaExecutor : IExecutor
    {
        Dictionary<int, ClipInfo> PopulateTimeline(List<IPlanStep> planSteps);
    }

    public interface IDiscourseExecutor : IExecutor
    {
        void PopulateTimeline(List<CamPlanStep> planSteps, Dictionary<int, ClipInfo> mappingActionSegIDsToDurativeClips);
    }
}
