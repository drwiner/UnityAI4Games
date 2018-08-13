using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class AttachToParentPlayable : PlayableBehaviour
    {
        private GameObject _parent;
        private GameObject _child;

        public void Initialize(GameObject parent, GameObject child)
        {
            _parent = parent;
            _child = child;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            

            base.PrepareFrame(playable, info);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playable.GetTime() <= 0 || _parent == null || _child == null)
                return;

            _child.transform.parent = _parent.transform;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _child.transform.parent = _parent.transform;
        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
                // Instead, keep an internal reference on game object of original parent transform.
                //_child.transform.parent = OriginalParent;
            }
        }
    }

    public class DettachToParentPlayable : PlayableBehaviour
    {
        private GameObject _child;
        private Transform defaultParent;

        public void Initialize(GameObject child)
        {
            _child = child;
            defaultParent = _child.GetComponent<DefaultAttributes>().defaultParent;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playable.GetTime() <= 0 || _child == null || defaultParent == null)
                return;

            _child.transform.parent = defaultParent;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _child.transform.parent = defaultParent;
        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
                // Instead, keep an internal reference on game object of original parent transform.
                //_child.transform.parent = OriginalParent;
            }
        }
    }

    public class Dettach2ToParentPlayable : PlayableBehaviour
    {
        private GameObject _child;
        private Transform defaultParent;

        public void Initialize(GameObject child, GameObject originalParent)
        {
            _child = child;
            defaultParent = originalParent.transform;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playable.GetTime() <= 0 || _child == null || defaultParent == null)
                return;

            _child.transform.parent = defaultParent;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _child.transform.parent = defaultParent;
        }
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
                // Instead, keep an internal reference on game object of original parent transform.
                //_child.transform.parent = OriginalParent;
            }
        }
    }

}