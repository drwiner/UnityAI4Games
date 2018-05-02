using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class AttachToParentPlayable : PlayableBehaviour
    {
        private GameObject _parent;
        private GameObject _child;
        private Transform OriginalParent;

        public void Initialize(GameObject parent, GameObject child)
        {
            _parent = parent;
            _child = child;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playable.GetTime() <= 0 || _parent == null || _child == null)
                return;

            _child.transform.parent = _parent.transform;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            OriginalParent = _child.transform.parent;
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

}