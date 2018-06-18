using BoltFreezer.Camera.CameraEnums;
using Cinemachine;
using Cinematography;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class CamScalePlayable : PlayableBehaviour
    {
        private CinemachineCameraBody _ccb;
        private FramingType _target;
        private int _fstopIndex;
        private int _lensIndex;
        private int _original_fstopIndex;
        private int _original_lensIndex;

        public void Initialize(CinemachineCameraBody ccb, FramingType scale)
        {
            _ccb = ccb;
            _target = scale;

            FramingParameters framing_data;
            try
            {
                framing_data = FramingParameters.FramingTable[scale];
            }
            catch
            {
                Debug.Log("here");
                throw new System.Exception();
            }

            _fstopIndex = CinematographyAttributes.fStops[framing_data.DefaultFStop];
            _lensIndex = CinematographyAttributes.lenses[framing_data.DefaultFocalLength];
        }


        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {

            _original_fstopIndex = _ccb.IndexOfFStop;
            _original_lensIndex = _ccb.IndexOfLens;
            _ccb.IndexOfFStop = _fstopIndex;
            _ccb.IndexOfLens = _lensIndex;

        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _original_fstopIndex = _ccb.IndexOfFStop;
            _original_lensIndex = _ccb.IndexOfLens;
        }
    }

}