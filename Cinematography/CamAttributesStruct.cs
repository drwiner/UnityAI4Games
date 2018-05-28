using Cinematography;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraNamespace
{

    public class CamAttributesStruct : MonoBehaviour
    {
        // Composition
        public string targetLocation;
        public int targetOrientation = -1;

        // Cinematography
        public FramingType scale = FramingType.None;
        public int hangle = -1;
        public int vangle = -1;

        public void Set(FramingType _scale, string _tLoc, int _tOrient, int _hangle, int _vangle)
        {
            scale = _scale;
            targetLocation = _tLoc;
            targetOrientation = _tOrient;
            hangle = _hangle;
            vangle = _vangle;
        }

        public bool SameScale(CamAttributesStruct other)
        {
            return other.scale == scale;
        }


    }

    [Serializable]
    public class CamSchema
    {
        [SerializeField]
        public FramingType scale = FramingType.None;

        [SerializeField]
        public string targetLocation;

        [SerializeField]
        public int targetOrientation = -1;

        [SerializeField]
        public int hangle = -1;

        [SerializeField]
        public int vangle = -1;

        public CamSchema(FramingType _scale, string _tLoc, int _tOrient, int _hangle, int _vangle)
        {
            scale = _scale;
            targetLocation = _tLoc;
            targetOrientation = _tOrient;
            hangle = _hangle;
            vangle = _vangle;
        }

        public CamSchema Duplicate()
        {
            return new CamSchema(scale, targetLocation, targetOrientation, hangle, vangle);
        }

        public override string ToString()
        {
            return string.Format("Shot({0}.{1}.{2}.{3}.{4}", scale, targetLocation, targetOrientation, hangle, vangle);
        }

        public bool IsConsistent(CamAttributesStruct cas)
        {
            if (scale != FramingType.None)
            {
                if (scale != cas.scale)
                {
                    //Debug.Log("not same scale");
                    return false;
                }
            }

            if (targetLocation != "" && targetLocation != null)
            {
                if (targetLocation != cas.targetLocation)
                {
                    //Debug.Log("not same location");
                    return false;
                }
            }

            if (targetOrientation != -1)
            {
                if (targetOrientation != cas.targetOrientation)
                {
                    //Debug.Log("not same target Orient");
                    return false;
                }
            }

            if (hangle != -1)
            {
                if (hangle != cas.hangle)
                {
                    //Debug.Log("not same hangle");
                    return false;
                }
            }

            if (vangle != -1)
            {
                if (vangle != cas.vangle)
                {
                    //Debug.Log("not same vangle");
                    return false;
                }
            }

            return true;
        }

        public CamSchema Clone()
        {
            return new CamSchema(scale, targetLocation, targetOrientation, hangle, vangle);
        }
    }

    [Serializable]
    public class CamTargetSchema
    {
        [SerializeField]
        public int orient = -1;

        [SerializeField]
        public string location = "";

        [SerializeField]
        public List<ActionSeg> ActionSegs = new List<ActionSeg>();

        public CamTargetSchema()
        {

        }

        public CamTargetSchema(int _orient, string _location, List<ActionSeg> _actionSegs)
        {
            orient = _orient;
            location = _location;
            ActionSegs = _actionSegs;
        }

        public CamTargetSchema(int _orient, List<ActionSeg> _actionSegs)
        {
            orient = _orient;
            ActionSegs = _actionSegs;
        }

        public CamTargetSchema(List<ActionSeg> _actionSegs)
        {
            ActionSegs = _actionSegs;
        }

        public CamTargetSchema Clone()
        {
            return new CamTargetSchema(orient, location, ActionSegs);
        }
    }

    [Serializable]
    public class ActionSeg
    {
        [SerializeField]
        public string actionVarName = "";

        [SerializeField]
        public string targetVarName = "";

        [SerializeField]
        public double startPercent = 0;

        [SerializeField]
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
