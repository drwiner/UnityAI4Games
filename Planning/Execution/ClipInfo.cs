using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{
    public class ClipInfo
    {
        public double start;
        public double duration;
        public string display;
        public PlayableDirector director;
        public ClipInfo(PlayableDirector _director, double strt, double dur, string dis)
        {
            director = _director;
            start = strt;
            duration = dur;
            display = dis;
        }
    }
}