using PlanningNamespace;
using UnityEngine;
using UnityEngine.Playables;

namespace TimelineClipsNamespace
{

    public class TimeTravelPlayable : PlayableBehaviour
    {
        private PlayableDirector _pd;
        private float _newTime;

        public void Initialize(PlayableDirector pd, float newtime)
        {
            _pd = pd;
            _newTime = newtime;
        }

        //public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        //{
        //    if (playable.GetTime() <= 0 || _pd == null)
        //        return;
        //}

        public void Rewind(float ft)
        {
            while (ft < _pd.time)
            {
                _pd.time -= .1f;
                _pd.Evaluate();
            }
        }

        public void FastForward(float ft)
        {
            while (ft > _pd.time)
            {
                _pd.time += .1f;
                _pd.Evaluate();
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _pd.Pause();
            if (_newTime > _pd.time)
            {
                FastForward(_newTime);
            }
            else
            {
                var UPC = GameObject.FindGameObjectWithTag("Problem").GetComponent<UnityProblemCompiler>();
                Rewind(_newTime+.06f);
                if (_newTime == 0)
                {
                    UPC.SetInitialState();
                }
               // UPC.SetInitialState();
                //Rewind(_newTime - 0.06f);
               // FastForward(_newTime);
            }

            Debug.Log("setFabTime: " + _pd.time + " and: " + _pd.state);

            if (_pd.state != PlayState.Playing)
            {
                _pd.RebuildGraph();
                _pd.Play();
            }
        }
        //public override void OnBehaviourPause(Playable playable, FrameData info)
        //{
        //    if (info.evaluationType == FrameData.EvaluationType.Playback)
        //    {
        //    }
        //}
    }

}