using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraNamespace;
using PlanningNamespace;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using BoltFreezer.PlanTools;
using BoltFreezer.Interfaces;
using System.Linq;
using System;
using Cinemachine;
using TimelineClipsNamespace;
using BoltFreezer.DecompTools;

namespace PlanningNamespace {

    [ExecuteInEditMode]
    public class ReadDecomp : MonoBehaviour {

        [SerializeField]
        List<ClipSchema<FabulaAsset>> fabulaClips = new List<ClipSchema<FabulaAsset>>();

        [SerializeField]
        List<ClipSchema<DiscourseAsset>> discourseClips = new List<ClipSchema<DiscourseAsset>>();

        [SerializeField]
        List<ClipSchema<FabulaAsset>> globalConstraints = new List<ClipSchema<FabulaAsset>>();

        public bool readClips = false;
        public bool assembleDecomp = false;

        private PlayableDirector playableDirector;
        private Decomposition Decomp;
        public List<string> Parameters;

        // Update is called once per frame
        void Update()
        {
            if (playableDirector == null)
            {
                playableDirector = GetComponent<PlayableDirector>();
            }

            if (readClips)
            {
                readClips = false;
                ReadClips();
            }

            if (assembleDecomp)
            {
                assembleDecomp = false;
                AssembleDecomp();
            }
        }

        public void AssembleDecomp()
        {
            // Collect step-typed variables
            var fabClipStepMap = new Dictionary<ClipSchema<FabulaAsset>, PlanStep>();
            foreach(var fabClip in fabulaClips)
            {
                var planStep = ReadStepVariable(fabClip.asset);
                fabClipStepMap.Add(fabClip, planStep);
            }
            // ... including camera-step variables
            var discClipStepMap = new Dictionary<ClipSchema<DiscourseAsset>, PlanStep>();
            foreach(var discClip in discourseClips)
            {
                var camStep = ReadCamStepVariable(discClip.asset);
                discClipStepMap.Add(discClip, camStep);
            }

            // (1) filter and create candidates for each
            // (2) compare to global constraints
        }

        public void ReadClips()
        {
            Parameters = new List<string>();
            // get params by perceiving playable director.

            var timelineasset = playableDirector.playableAsset as TimelineAsset;

            foreach (var track in timelineasset.GetRootTracks())
            {
                if (track.name.Equals("ConstraintTrack")){
                    foreach (var clip in track.GetClips())
                    {
                        var cschema = new ClipSchema<FabulaAsset>(clip.start, clip.duration, clip.displayName, clip.asset as FabulaAsset);
                        globalConstraints.Add(cschema);
                    }
                }
                if (track.name.Equals("FabulaTrack"))
                {
                    foreach (var clip in track.GetClips())
                    {
                        var cschema = new ClipSchema<FabulaAsset>(clip.start, clip.duration, clip.displayName, clip.asset as FabulaAsset);
                       
                        fabulaClips.Add(cschema);
                        Parameters.Add(clip.displayName);
                    }
                }
                if (track.name.Equals("DiscourseTrack"))
                {
                    foreach (var clip in track.GetClips())
                    {
                        var cschema = new ClipSchema<DiscourseAsset>(clip.start, clip.duration, clip.displayName, clip.asset as DiscourseAsset);
                        discourseClips.Add(cschema);
                        Parameters.Add(clip.displayName);
                    }
                }
            }
        }

        public PlanStep ReadStepVariable(FabulaAsset fabAsset)
        {
            PlanStep ps;

            var terms = new List<ITerm>();
            var preconditions = new List<IPredicate>();
            var effects = new List<IPredicate>();
            string schemaName = "";

            foreach(var constraint in fabAsset.Constraints)
            {
                var constraintParts = constraint.Split(' ');
                var instruction = constraintParts[0];
                if (instruction.Equals("terms"))
                {
                    foreach(var instructArg in constraintParts.Skip(1))
                    {
                        terms.Add(new Term(instructArg, true) as ITerm);
                    }
                }
                if (instruction.Equals("term"))
                {
                    var argPos = Int32.Parse(constraintParts[1]);
                    if (argPos >= terms.Count - 1)
                    {
                        // if the arg position is not the next item in term list, then 
                        while (argPos != terms.Count)
                        {
                            // add placeholder terms
                            terms.Add(new Term(terms.Count.ToString()) as ITerm);
                        }
                        terms.Add(new Term(constraintParts[2], true) as ITerm);
                    }
                    else if(argPos < terms.Count)
                    {
                        terms.Insert(argPos, new Term(constraintParts[2], true) as ITerm);
                    }

                }
                if (instruction.Equals("precond") || instruction.Equals("has-precond") || instruction.Equals("precondition"))
                {
                    var pred = ProcessPredicateString(constraintParts);
                    preconditions.Add(pred);
                }

                if (instruction.Equals("effect") || instruction.Equals("has-effect"))
                {
                    var pred = ProcessPredicateString(constraintParts);
                    effects.Add(pred);
                }

                if (instruction.Equals("schema"))
                {
                    // then this operator gets a particular name
                    schemaName = constraintParts[1];
                }

                if (instruction.Equals("not"))
                {
                    // TODO: assemble list of negative constraints and propagate to filtering
                }

            }

            var root = new Operator(new Predicate(schemaName,terms,true), preconditions, effects);

            ps = new PlanStep(root as IOperator);
            return ps;
        }

        public PlanStep ReadCamStepVariable(DiscourseAsset discAsset)
        {
            PlanStep ps;
            var termList = new List<ITerm>()
            {
                new Term(discAsset.camSchema.scale.ToString(), true) as ITerm,
                new Term(discAsset.camSchema.hangle.ToString(), true) as ITerm,
                new Term(discAsset.camSchema.vangle.ToString(), true) as ITerm
            };

            foreach (var constraint in discAsset.Constraints)
            {
                var constraintParts = constraint.Split(' ');
                if (constraintParts[0].Equals("target"))
                {
                    termList.Add(new Term(constraintParts[1], true) as ITerm);
                }
                if (constraintParts[0].Equals("2-shot"))
                {
                    // create new target here? 
                }
                // TODO: do something with them. so far, unneeded...
            }

            var root = new Operator(new Predicate("", termList, true), new List<IPredicate>(), new List<IPredicate>());
            ps = new PlanStep(root as IOperator);
            return ps;
        }


        // assumes predString's first arg is whether its precondition or effect, or some other type
        public IPredicate ProcessPredicateString(string [] predString)
        {
            var predTerms = new List<ITerm>();
            var signage = true;
            int skippable = 2;
            string predicateName = predString[1];
            if (predicateName.Equals("not"))
            {
                skippable = 3;
                signage = false;
                predicateName = predString[2];
            }
            foreach (var instructArg in predString.Skip(skippable))
            {
                predTerms.Add(new Term(instructArg, true) as ITerm);
                // either this references a term or is a constant name. 
            }
            var pred = new Predicate(predicateName, predTerms, signage) as IPredicate;
            return pred;
        }

    }

    [Serializable]
    public class ClipSchema<T>
    {
        [SerializeField]
        public double start;

        [SerializeField]
        public double duration;

        [SerializeField]
        public string display;

        [SerializeField]
        public T asset;

        public ClipSchema(double startTime, double duration, string display, T clipAsset){
            asset = clipAsset;
            start =  startTime;
            this.duration = duration;
            this.display = display;
        }

    }
}