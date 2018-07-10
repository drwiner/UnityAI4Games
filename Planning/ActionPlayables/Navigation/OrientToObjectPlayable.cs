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
      //  private Quaternion startingOrientation;

       // private bool m_FirstFrameHappened;
        //private float finalRotation;

        private Quaternion _originalRotation;

        public void Initialize(GameObject gameObject,Vector3 orientTo)
        {
            _gameObject = gameObject;
            _steerTo = orientTo;
            _Controller = _gameObject.GetComponent<DynoBehavior_TimelineControl>();;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _Controller.playingClip = true;

            //if (m_FirstFrameHappened && info.frameId == 1)
            //{
            //    _gameObject.transform.rotation = startingOrientation;
            //}

            //if (!m_FirstFrameHappened)
            //{
            //    startingOrientation = _gameObject.transform.rotation;
            //    m_FirstFrameHappened = true;
            //}

            

            // if it's the last frame, set to final rotation.

        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_gameObject != null)
            {
                //_originalRotation = _gameObject.transform.rotation;
                //finalRotation = _Controller.Orient(_steerTo);
                _Controller.Orient(_steerTo);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_gameObject != null)
            {
                //_originalRotation = _gameObject.transform.rotation;
            }
        }

        //public override void OnPlayableDestroy(Playable playable)
        //{
        //    m_FirstFrameHappened = false;
        //}

        //public override void OnGraphStart(Playable playable)
        //{
        //    Debug.Log("on graph start");
        //    base.OnGraphStart(playable);
        //    if (startingOrientation != null){
        //        Debug.Log("starting Orientation is NOTNOTNOT Null");
        //        _gameObject.transform.rotation = startingOrientation;
        //    }
        //}
    }
}