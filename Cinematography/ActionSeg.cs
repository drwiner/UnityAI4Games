using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CameraNamespace
{
    [Serializable]
    public class ActionSeg
    {
        public string actionVarName = "";
        public string targetVarName = "";
        public double startPercent = 0;
        public double endPercent = 1;

        public ActionSeg(string actionvar, double start, double end)
        {
            actionVarName = actionvar;
            startPercent = start;
            endPercent = end;
        }

        public ActionSeg()
        {
            // No target specified. This just represents free space. 
            // These should be inserted for each pair of targets that are consecutive but not contiguous.
        }

    }
}
