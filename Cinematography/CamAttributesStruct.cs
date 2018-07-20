using Cinematography;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoltFreezer.Camera.CameraEnums;
using BoltFreezer.Camera;

namespace CameraNamespace
{
    [Serializable]
    public class CamAttributesStruct : MonoBehaviour
    {
        // Composition
        public string targetLocation;
        public Orient targetOrientation = Orient.None;

        // Cinematography
        public FramingType scale = FramingType.None;
        public Hangle hangle = Hangle.None;
        public Vangle vangle = Vangle.None;

        public int OrientInt
        {
            get { return CamSchema.OrientToInt(targetOrientation); }
        }

        public int HangleInt
        {
            get { return CamSchema.HangleToInt(hangle); }
        }

        public int VangleInt
        {
            get { return CamSchema.VangleToInt(vangle); }
        }

        public void Set(FramingType _scale, string _tLoc, Orient _tOrient, Hangle _hangle, Vangle _vangle)
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

  
}
