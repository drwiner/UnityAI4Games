using SteeringNamespace;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class OrientToObjectPlayable : PlayableBehaviour
    {
        private DynoBehavior_TimelineControl _Controller;
        private GameObject _gameObject;
        private Vector3 _steerTo;

        private Quaternion _originalRotation;

        public void Initialize(GameObject gameObject,Vector3 orientTo)
        {
            _gameObject = gameObject;
            _steerTo = orientTo;
            _Controller = _gameObject.GetComponent<DynoBehavior_TimelineControl>();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _Controller.playingClip = true;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_gameObject != null)
            {
                _originalRotation = _gameObject.transform.rotation;
                _Controller.Orient(_steerTo);

            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_gameObject != null)
            {
                _originalRotation = _gameObject.transform.rotation;
            }
        }

    }
}