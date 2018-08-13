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
using BoltFreezer.Utilities;
using BoltFreezer.Camera;
using GraphNamespace;
using CompilationNamespace;
using BoltFreezer.Camera.CameraEnums;

namespace PlanningNamespace {

    [ExecuteInEditMode]
    public class UnityTimelineDecomp : MonoBehaviour {

        public List<GameObject> objectParameterTypes;
        public List<string> TermNames;
        public List<string> FabulaStepNames;
        public List<string> DiscourseStepNames;
        public List<NonEqualTuple> NonEqualities;

        public List<IPredicate> Preconditions;
        public List<IPredicate> Effects;
        public List<string> ignoreDidNotStart = new List<string>();
        public List<string> ignoreDidNotEnd = new List<string>();

        // used to assemble clips from timeline
        private PlayableDirector playableDirector;
        public GameObject CameraHost;
        public GameObject LocationHost;


        // Populated by "ReadDecomp"
        List<ClipSchema<FabulaAsset>> fabulaClips;
        List<ClipSchema<DiscourseAsset>> discourseClips;
        List<ClipSchema<ConstraintAsset>> globalConstraints;

        // Populated by "AssembleDecomp"
        Dictionary<ClipSchema<FabulaAsset>, PlanStep> fabClipStepMap;
        Dictionary<ClipSchema<DiscourseAsset>, CamPlanStep> discClipStepMap;

        Dictionary<string, PlanStep> fabVarStepMap;
        Dictionary<string, CamPlanStep> discVarStepMap;

        // decomposition constraints/ requirements
        public List<ITerm> Terms;
        public List<IPlanStep> SubSteps;
        public List<CamPlanStep> DSubSteps;

        //private List<IPlanStep> DSubSteps;
        private List<Tuple<string, Tuple<PlanStep, PlanStep>>> stepConstraints;
        private List<Tuple<string, Tuple<CamPlanStep, CamPlanStep>>> dstepConstraints;

        private List<Tuple<IPlanStep, IPlanStep>> orderings;
        private List<Tuple<CamPlanStep, CamPlanStep>> dorderings;

        private List<Tuple<IPlanStep, IPlanStep>> cntgs;
        private List<Tuple<CamPlanStep, CamPlanStep>> dcntgs;

        private List<CausalLink<IPlanStep>> links;
        private List<CausalLink<CamPlanStep>> dlinks;

        public TimelineDecomposition PartialDecomp;
        // these are ground decompositions that can be used to create composite steps.
        [NonSerialized]
        public List<TimelineDecomposition> GroundDecomps;

        public int NumGroundDecomps;

        // triggers for update events
        public bool readClips = false;
        public bool setCamsAndLocs = false;
        public bool assembleDecomp = false;
        public bool filterCndts = false;
        public bool reset = false;

        public int testOne = 0;
        public bool visualizeTest = false;

        
        // Update is called once per frame
        void Update()
        {
            if (reset)
            {
                reset = false;
                GroundDecomps = new List<TimelineDecomposition>();
                if (TermNames.Count == 0)
                {
                    Terms = new List<ITerm>();
                }
            }
            if (GroundDecomps == null)
            {
                NumGroundDecomps = 0;
            }
            else if (GroundDecomps.Count != NumGroundDecomps)
            {
                NumGroundDecomps = GroundDecomps.Count;
            }
            
            if (playableDirector == null)
            {
                playableDirector = GetComponent<PlayableDirector>();
            }

            if (readClips)
            {
                readClips = false;
                if (GroundActionFactory.GroundActions == null || GroundActionFactory.GroundActions.Count == 0 || GroundActionFactory.TypeDict == null)
                {
                    var UGAF = GameObject.Find("GroundActionFactory").GetComponent<UnityGroundActionFactory>();
                    UGAF.PreparePlanner(true);
                }
                Read();
            }

            if (setCamsAndLocs)
            {
                setCamsAndLocs = false;
                DiscourseDecompositionHelper.SetCamsAndLocations(CameraHost, LocationHost);
            }

            if (assembleDecomp)
            {
                assembleDecomp = false;
                GroundDecomps = new List<TimelineDecomposition>();
                Assemble();
                TermNames = new List<string>();
                foreach (var term in Terms)
                {
                    TermNames.Add(term.ToString());
                }
                FabulaStepNames= new List<string>();
                foreach(var substep in SubSteps)
                {
                    FabulaStepNames.Add(substep.ToString());
                }
                DiscourseStepNames = new List<string>();
                foreach (var substep in DSubSteps)
                {
                    DiscourseStepNames.Add(substep.ToString());
                }
            }

            if (filterCndts)
            {
                filterCndts = false;
                Filter();
            }

            if (visualizeTest)
            {
                visualizeTest = false;
                InstantiateTest(testOne);
            }
        }

        public void Assemble()
        {
            Preconditions = new List<IPredicate>();
            Effects = new List<IPredicate>();

            SubSteps = new List<IPlanStep>();
            DSubSteps = new List<CamPlanStep>();

            // Collect step-typed variables
            fabClipStepMap = new Dictionary<ClipSchema<FabulaAsset>, PlanStep>();
            discClipStepMap = new Dictionary<ClipSchema<DiscourseAsset>, CamPlanStep>();

            // used to identify references of constraints
            fabVarStepMap = new Dictionary<string, PlanStep>();
            discVarStepMap = new Dictionary<string, CamPlanStep>();

            // if there's a gap, then it's a "before" relation
            orderings = new List<Tuple<IPlanStep, IPlanStep>>();
            dorderings = new List<Tuple<CamPlanStep, CamPlanStep>>();

            // if the clip is contiguous with the last one, then it's "cntg"
            cntgs = new List<Tuple<IPlanStep, IPlanStep>>();
            dcntgs = new List<Tuple<CamPlanStep, CamPlanStep>>();

            // keep a map of literals based on names so that one can assign precondition and effects; specified in global constraints, only used for causal links locally
            var literalMap = new Dictionary<string, IPredicate>();

            // collect links specified in constraints
            links = new List<CausalLink<IPlanStep>>();
            dlinks = new List<CausalLink<CamPlanStep>>();

            // constraints that are stored between shots or between actions, each a tuple
            stepConstraints = new List<Tuple<string, Tuple<PlanStep, PlanStep>>>();
            dstepConstraints = new List<Tuple<string, Tuple<CamPlanStep, CamPlanStep>>>();

            // keeping track of last clip to determine ordering and cntg relations with last.
            // TODO: support multiple fabula timelines
            ClipSchema<FabulaAsset> last = fabulaClips[0];
            var visitedVariables = new List<string>();
            for (int i = 0; i < fabulaClips.Count; i++)
            {
                var fabClip = fabulaClips[i];

                // read the step variable from the asset
                var planStep = ReadStepVariable(fabClip);

                // Add terms to decomp terms, only if terms are 0
                if (Terms.Count == 0)
                {
                    foreach (var term in planStep.Terms)
                    {
                        if (visitedVariables.Contains(term.Variable))
                        {
                            term.Variable = term.Variable + planStep.GetHashCode().ToString();
                        }
                        visitedVariables.Add(term.Variable);
                        Terms.Add(term);
                    }
                }

                SubSteps.Add(planStep);

                // now check against the last item in there for timing.
                if (i > 0)
                {
                    var lastItemEndTime = last.start + last.duration;
                    if (fabClip.start - lastItemEndTime <= 0.06)
                    {
                        // these are cntg
                        var newCntg = new Tuple<IPlanStep, IPlanStep>(fabClipStepMap[last], planStep);
                        cntgs.Add(newCntg);
                    }
                    else if(fabClip.start - lastItemEndTime >= 0.12)
                    {
                        // what's the maximum amount of space that can go into here? This is calculated by looking at specific action.
                        var ordering = new Tuple<IPlanStep, IPlanStep>(fabClipStepMap[last], planStep);
                        orderings.Add(ordering);
                    }
                }
                last = fabClip;
                fabClipStepMap.Add(fabClip, planStep);
                fabVarStepMap.Add(fabClip.display, planStep);
            }
            // ... including camera-step variables
            ClipSchema<DiscourseAsset> dlast = discourseClips[0];
            for (int i =0; i < discourseClips.Count; i++)
            //foreach (var discClip in discourseClips)
            {
                var discClip = discourseClips[i];
                var camstep = ReadCamStepVariable(discClip);

                DSubSteps.Add(camstep);

                /// Thus, the camera plan shot cannot itself hold a fixed duration as a term as input (though a maximum default would be practical).
                /// Precisely what is needed are those details related to global constraints... 
                /// The benefit of breaking them up is that it's clear that a new camera shot can be inserted between them with a new action segment. 
                discClipStepMap[discClip] = camstep;
                discVarStepMap[discClip.display] = camstep;

                if (i > 0)
                {
                    var lastItemEndTime = dlast.start + dlast.duration;
                    if (discClip.start - lastItemEndTime < 0.06)
                    {
                        // these are cntg
                        var newCntg = new Tuple<CamPlanStep, CamPlanStep>(discClipStepMap[dlast], camstep);
                        dcntgs.Add(newCntg);
                    }
                    else if (discClip.start - lastItemEndTime >= 0.3)
                    {
                        // what's the maximum amount of space that can go into here? This is calculated by looking at specific action.
                        var ordering = new Tuple<CamPlanStep, CamPlanStep>(discClipStepMap[dlast], camstep);
                        dorderings.Add(ordering);
                    }
                }
                
                dlast = discClip;
            }

            ignoreDidNotStart = new List<string>();
            ignoreDidNotEnd = new List<string>();

            // iterate through global constraints
            foreach (var constraintClipSchema in globalConstraints)
            {
                var constraints = constraintClipSchema.asset.Constraints;
                foreach (var constraint in constraints)
                {
                    var constraintParts = constraint.Split(' ');

                    if (constraint.Equals("refresh orderings discoursetrack"))
                    {
                        dorderings = new List<Tuple<CamPlanStep,CamPlanStep>>();
                    }
                    if (constraintParts[0].Equals("d<"))
                    {
                        var prec = discVarStepMap[constraintParts[1]];
                        var cons = discVarStepMap[constraintParts[2]];
                        var ordering = new Tuple<CamPlanStep, CamPlanStep>(prec, cons);
                        dorderings.Add(ordering);
                    }

                    if (constraintParts[0].Equals("="))
                    {
                        // this could mean that 2 variables are equal, or that a new constant is to be declared that has the name and terms
                    }

                    if (constraintParts[0].Equals("hangle"))
                    {
                        // hangle shot1 (between value1 value2) [[[[ this should be on single shot]]]]
                        // hangle shot1 (reverse shot2)
                        if (constraintParts[2].Equals("reverse"))
                        {
                            // do something
                        }
                        // hangle shot1 (differ 90 shot2)
                        if (constraintParts[2].Equals("differ"))
                        {
                            // do something
                        }
                    }

                    if (constraintParts[0].Equals("orient"))
                    {
                        // orient action1 reverse action2
                        if (constraintParts[2].Equals("reverse"))
                        {
                            var planstepTuple = new Tuple<PlanStep, PlanStep>(fabVarStepMap[constraintParts[1]], fabVarStepMap[constraintParts[3]]);
                            var stepConstraintTuple = new Tuple<string, Tuple<PlanStep, PlanStep>>("orient reverse", planstepTuple);
                            stepConstraints.Add(stepConstraintTuple);
                        }
                        // orient action1 = action2
                        if (constraintParts[2].Equals("="))
                        {
                            var planstepTuple = new Tuple<PlanStep, PlanStep>(fabVarStepMap[constraintParts[1]], fabVarStepMap[constraintParts[3]]);
                            var stepConstraintTuple = new Tuple<string, Tuple<PlanStep, PlanStep>>("orient =", planstepTuple);
                            stepConstraints.Add(stepConstraintTuple);
                        }
                    }

                    // user to define a literal as variable -- essentially only used for causal link dependencies
                    if (constraintParts[0].Equals("literal"))
                    {
                        // literal literalAsVariableName predicatename, term_0, term_1...., term_k
                        // indicates declaration of literal
                        var literalAsVariableName = constraintParts[1];
                        var predicate = ProcessPredicateString(constraintParts.Skip(1).ToArray());
                        literalMap[literalAsVariableName] = predicate;
                    }

                    // if constraints[0] is some kind of ordering constraint that's different from those 
                    if (constraintParts[0].Equals("linked") || constraintParts[0].Equals("link") || constraintParts[0].Equals("linked-by"))
                    {
                        int sourceNum = 1;
                        var dependency = new Predicate() as IPredicate;
                        if (constraintParts.Count() == 4)
                        {
                            // linked dependencyCondition sourceStepname sinkStepName
                            // 0      1                   2              3
                            dependency = literalMap[constraintParts[1]];
                            sourceNum += 1;
                        }

                        // find the planstep based on the step-variable name
                        var source = fabVarStepMap[constraintParts[sourceNum]];
                        var sink = fabVarStepMap[constraintParts[sourceNum + 1]];
                        var cl = new CausalLink<IPlanStep>(dependency, source, sink);
                        links.Add(cl);
                    }

                    if (constraintParts[0].Equals("precondition") || constraintParts[0].Equals("precond"))
                    {
                        // indicates a precondition
                        var predicate = ProcessPredicateString(constraintParts);
                        //preconditions
                        Preconditions.Add(predicate);

                    }

                    if (constraintParts[0].Equals("effect") || constraintParts[0].Equals("eff"))
                    {
                        if (constraintParts[1].Equals("effects"))
                        {

                        }
                        else
                        {
                            // indicates an effect
                            var predicate = ProcessPredicateString(constraintParts);
                            //preconditions
                            Effects.Add(predicate);
                        }

                    }

                    if (constraintParts[0].Equals("ignoreDidNotStart"))
                    {
                        ignoreDidNotStart.Add(constraintParts[1]);
                    }
                    if (constraintParts[0].Equals("ignoreDidNotEnd"))
                    {
                        ignoreDidNotEnd.Add(constraintParts[1]);
                    }
                       
                }
            }
            /// next we have to filter the schemas based on actual candidates. for steps that's groundActions, and for cams that's gameObjects with the camAttributeStruct
            /// then, we filter based on global constraints
        }

        public TimelineDecomposition NonGroundTimelineDecomposition()
        {
            var root = new Operator(new Predicate(gameObject.name, Terms, true), Preconditions, Effects) as IOperator;

            PartialDecomp = new TimelineDecomposition(root, new List<IPredicate>(), cntgs, dcntgs, stepConstraints, dstepConstraints, SubSteps, DSubSteps, orderings, dorderings, links, dlinks, fabVarStepMap);

            // Instantiate Nonequality constraints
            PartialDecomp.NonEqualities = new List<List<ITerm>>();
            foreach (var nonequality in NonEqualities)
            {
                PartialDecomp.NonEqualities.Add(new List<ITerm>() { Terms[nonequality.first], Terms[nonequality.second] });
            }

            return PartialDecomp;
        }

        public void Filter()
        {
            var root = new Operator(new Predicate(gameObject.name, Terms, true), Preconditions, Effects) as IOperator;
            
            PartialDecomp = new TimelineDecomposition(root, new List<IPredicate>(), cntgs, dcntgs, stepConstraints, dstepConstraints, SubSteps, DSubSteps, orderings, dorderings, links, dlinks, fabVarStepMap);

            // Instantiate Nonequality constraints
            PartialDecomp.NonEqualities = new List<List<ITerm>>();
            foreach (var nonequality in NonEqualities)
            {
                PartialDecomp.NonEqualities.Add(new List<ITerm>() { Terms[nonequality.first], Terms[nonequality.second] });
            }

            GroundDecomps = TimelineDecompositionHelper.Compose(0, PartialDecomp);
            NumGroundDecomps = GroundDecomps.Count();
            // foreach cndt, create a child gameobject and create a playable director and a control track. for each action in 
        }

        public static PlanStep GetPlanStepFromVariableName<T>(string variableName, List<ClipSchema<T>> clips, Dictionary<ClipSchema<T>, PlanStep> schemaMap)
        {
            var step = new PlanStep();
            
            foreach(var schema in clips)
            {
                if (schema.display.Equals(variableName))
                {
                    return schemaMap[schema];
                }
            }

            return step;
        }


        public void Read()
        {
            Terms = new List<ITerm>();

            fabulaClips = new List<ClipSchema<FabulaAsset>>();
            discourseClips = new List<ClipSchema<DiscourseAsset>>();
            globalConstraints = new List<ClipSchema<ConstraintAsset>>();

            // get params by perceiving playable director.
            for (int i = 0; i < objectParameterTypes.Count; i++)
            {
                var newTerm = new Term(i.ToString());
                if (TermNames.Count == 0)
                {
                    newTerm = new Term(i.ToString() + "_unboundTerm");

                }
                else if (TermNames.Count == objectParameterTypes.Count)
                {
                    newTerm = new Term(TermNames[i]);
                }
                newTerm.Type = objectParameterTypes[i].name;
                Terms.Add(newTerm as ITerm);
            }

            var timelineasset = playableDirector.playableAsset as TimelineAsset;

            foreach (var track in timelineasset.GetRootTracks())
            {
                if (track.name.Equals("ConstraintTrack")){
                    foreach (var clip in track.GetClips())
                    {
                        var cschema = new ClipSchema<ConstraintAsset>(clip.start, clip.duration, clip.displayName, clip.asset as ConstraintAsset);
                        globalConstraints.Add(cschema);
                    }
                }
                if (track.name.Equals("FabulaTrack"))
                {
                    foreach (var clip in track.GetClips())
                    {
                        var cschema = new ClipSchema<FabulaAsset>(clip.start, clip.duration, clip.displayName, clip.asset as FabulaAsset);
                       
                        fabulaClips.Add(cschema);
                    }
                }
                if (track.name.Equals("DiscourseTrack"))
                {
                    foreach (var clip in track.GetClips())
                    {
                        var cschema = new ClipSchema<DiscourseAsset>(clip.start, clip.duration, clip.displayName, clip.asset as DiscourseAsset);
                        discourseClips.Add(cschema);
                        UpdateActionSegmentsWithPercentages(cschema);
                    }
                }
            }
        }


        public void InstantiateTest(int whichTestItem)
        {
            var whichDecomp = GroundDecomps[whichTestItem];
            // how do you execute?
            Debug.Log(whichDecomp.ToString());
            Debug.Log("Fabula:");
            foreach (var substep in whichDecomp.SubSteps)
            {
                Debug.Log(substep);
            }
            Debug.Log("Fabula Orderings:");
            foreach(var ord in whichDecomp.SubOrderings)
            {
                Debug.Log(ord.ToString());
            }
            Debug.Log("Fabula Links:");
            foreach(var link in whichDecomp.SubLinks)
            {
                Debug.Log(link.ToString());
            }
            Debug.Log("Discourse:");
            foreach(var substep in whichDecomp.discourseSubSteps)
            {
                Debug.Log(substep.ToString());
            }
            Debug.Log("Disc Orderings:");
            foreach (var ord in whichDecomp.discOrderings)
            {
                Debug.Log(ord.ToString());
            }
        }

        public PlanStep ReadStepVariable(ClipSchema<FabulaAsset> schema)
        {
            PlanStep ps;
            FabulaAsset fabAsset = schema.asset;

            var terms = new List<ITerm>();
            var preconditions = new List<IPredicate>();
            var effects = new List<IPredicate>();

            string schemaName = "";

            var unityactionschema = fabAsset.schema;
            if (unityactionschema != null)
            {
                schemaName = unityactionschema.name;
                var actionOperator = GameObject.Find(schemaName).GetComponent<UnityActionOperator>();
                for (int i = 0; i < actionOperator.MutableParameters.Count; i++)
                {
                    var newTerm = new Term(i.ToString())
                    {
                        Type = actionOperator.MutableParameters[i].name
                    };

                    terms.Add(newTerm as ITerm);
                }
            }

           

            foreach(var constraint in fabAsset.Constraints)
            {
                var constraintParts = constraint.Split(' ');
                var instruction = constraintParts[0];
                if (instruction.Equals("terms"))
                {
                    for (int j = 1; j < constraintParts.Count(); j++)
                    {
                        var instructArg = constraintParts[j];
                        var newTerm = new Term(instructArg) as ITerm;
                        if (terms.Count >= j)
                        {
                            terms[j - 1].Variable = instructArg;
                        }
                        //else if (terms.Count > j-1)
                        //{
                        //    // and not bound
                        //    terms.Add(newTerm);
                        //}
                        // if there already is a term at this index, need to reference with variable
                        //if (terms.Count > j - 1)
                        //{
                        //    var existingTerm = terms[j - 1];
                        //    newTerm.Variable = existingTerm.Variable;
                        //    newTerm.Type = existingTerm.Type;
                        //    terms[j - 1] = newTerm;
                        //}
                        //else
                        //{
                        //    terms.Add(newTerm);
                        //}

                    }
                    //foreach(var instructArg in constraintParts.Skip(1))
                    //{
                    //    terms.Add(new Term(instructArg, true) as ITerm);
                    //}
                }
                if (instruction.Equals("term"))
                {
                    var argPos = Int32.Parse(constraintParts[1]);
                    //if (argPos >= terms.Count - 1)
                    //{
                    //    // if the arg position is not the next item in term list, then 
                    //    while (argPos != terms.Count)
                    //    {
                    //        // add placeholder terms
                    //        terms.Add(new Term(terms.Count.ToString()) as ITerm);
                    //    }
                    //    terms.Add(new Term(constraintParts[2], true) as ITerm);
                    //}
                    //else if(argPos < terms.Count)
                    //{
                     terms[argPos] =  new Term(constraintParts[2]) as ITerm;
                    //}

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
                    if (constraintParts[1].Equals("schema"))
                    {
                        // propagate to schema
                        schema.AddConstraint(constraint);
                    }
                    // TODO: assemble list of negative constraints and propagate to filtering
                }

                //if (instruction.Equals("set"))
                //{
                //    // set who?
                //    var whodat = constraintParts[1];

                //    // do what?
                //    var dowat = constraintParts[2];

                //    if (dowat.Equals("idle"))
                //    {
                //        // how do we represent 
                //    }
                //}

            }

            var root = new Operator(new Predicate(schemaName,terms,true), preconditions, effects);

            ps = new PlanStep(root as IOperator);
            return ps;
        }

        public CamPlanStep ReadCamStepVariable(ClipSchema<DiscourseAsset> discClip)
        {
            /* Input: the ClipSchema<DiscourseAsset> 
             * Output: A camera plan step (type CamPlanStep) representing a single durative camera action/shot
             * 
             * CamPlanStep references 
             *      Camera Feature Vector
             *      A target location and orientation to reference as the line of action
             *      A set of action segments
             */

            // The Terms of the camera plan step are just its scale, hangle, vangle, and the location of the target.
            var termList = new List<ITerm>()
            {
                new Term(discClip.asset.camSchema.scale.ToString(), true) as ITerm,
                new Term(discClip.asset.camSchema.hangle.ToString(), true) as ITerm,
                new Term(discClip.asset.camSchema.vangle.ToString(), true) as ITerm,
                new Term(discClip.asset.camSchema.targetLocation.ToString(), true) as ITerm
            };

            // Create the CameraPlanStep
            CamPlanStep cps;
            var root = new Operator(new Predicate("", termList, true), new List<IPredicate>(), new List<IPredicate>());
            cps = new CamPlanStep(root as IOperator);

            ////////////////////////////////////////
            // Assign updated target Schemata
            ////////////////////////////////////////

            cps.TargetDetails = discClip.asset.targetSchema;

            ////////////////////
            // Assign CamSchema, will be used to filter valid camera objects (CamAttributesStruct)
            ////////////////////

            cps.CamDetails = discClip.asset.camSchema;

            ////////////////////
            // Assign directive
            ////////////////////

            //cps.directive = discClip.asset.camDirective;
            //if (cps.directive == BoltFreezer.Camera.CameraEnums.CamDirective.None)
            //{
            //    cps.directive = BoltFreezer.Camera.CameraEnums.CamDirective.Pan;
            //}

            // include here, other constraints that can be tacked on, like horizontal and vertical damping, amount of lead time, etc. Can we just display the entire cinemachine camera body?

            foreach (var constraint in discClip.asset.Constraints)
            {
                var constraintParts = constraint.Split(' ');

                // special constraints for different kinds of targets? Here, would create a new target and perhaps merge 2 targets?
                if (constraintParts[0].Equals("2-shot"))
                {
                    // "2-shot agent(anchor), agent(focus)"
                    // "2-shot locat0 locat1"

                    var anchor = constraintParts[1];

                    if (!this.TermNames.Contains(anchor))
                    {
                        throw new System.Exception("constraint terms must be labeled variables");
                    }

                    var focus = constraintParts[2];

                    if (!this.TermNames.Contains(focus))
                    {
                        throw new System.Exception("constraint terms must be labeled variables");
                    }

                    /////////////////////////////
                    //// Orient Towards (var) ///
                    /////////////////////////////

                    // orientTowards, setting this variable so that orient is calculated with respect to the direction towards this element
                    //cps.TargetDetails.orientTowards = focus;

                    /////////////////////////////
                    //// Target Var Name (var) //
                    /////////////////////////////

                    // agentOfFocus is the agent, at the orientTowards location, that the camera should focus on.
                    var targetActionSeg = cps.TargetDetails.ActionSegs[cps.TargetDetails.actionSegOfFocus];

                    // Use a special symbol to denote that 2 things are specified here. can split on this special character during execution.
                    targetActionSeg.targetVarName = focus + "  " + anchor;

                    /////////////////////////////
                    //// Group Target Directive /
                    /////////////////////////////

                    // It ought to be set to this already; but in case it is not...
                    //cps.directive = CamDirective.GroupAim;
                }
            }

            return cps;
            
        }

        public void UpdateActionSegmentsWithPercentages(ClipSchema<DiscourseAsset> discClip)
        {
            Tuple<double, double> percents;
            ClipSchema<FabulaAsset> lastClip = null;
            List<int> placesToInsertFreeSpaceActionSeg = new List<int>();
            for (int i =0; i < discClip.asset.targetSchema.ActionSegs.Count; i++) 
            //foreach (var actionSeg in discClip.asset.targetSchema.ActionSegs)
            {
                var actionSeg = discClip.asset.targetSchema.ActionSegs[i];
                if (actionSeg.actionVarName == "")
                {
                    // this actionSeg is just free space.
                    continue;
                }

                bool notFound = true;
                ClipSchema<FabulaAsset> fabulaClip = null;
                foreach (var clip in fabulaClips)
                {
                    // find the fabula clip associated and read in the percent values of the segment. (Later, these can be rounded?)
                    if (clip.display.Equals(actionSeg.actionVarName))
                    {
                        // This is the clip we were looking for
                        fabulaClip = clip;

                        // What's the intersection between the discourse clip and the fabula clip?
                        var intersect = IntervalIntersection(discClip, clip);
                        percents = PercentIntersect(intersect, clip);
                        actionSeg.startPercent = percents.First;
                        actionSeg.endPercent = percents.Second;

                        // We've found this so we can stop searching for clips
                        notFound = false;
                        break;
                    }
                }
                if (notFound)
                {
                    Debug.Log("couldn't find referenced fabula action in clip step: " + discClip.asset.name);
                    // then we didn't find the clip referenced.
                    throw new System.Exception();
                }

                if (lastClip != null)
                {
                    // compare last clip to fabula clip. If they are consecutive, then there is no free space.
                    if (lastClip.start + lastClip.duration < (fabulaClip.start - 0.06))
                    {
                        // there is free space.
                        placesToInsertFreeSpaceActionSeg.Add(i);
                        
                    }
                }
                lastClip = fabulaClip;
            }

            int numberOfInsertions = 0;
            foreach(var index in placesToInsertFreeSpaceActionSeg)
            {
                discClip.asset.targetSchema.ActionSegs.Insert(index + numberOfInsertions++, new ActionSeg());
            }
        }

        public Tuple<double, double> PercentIntersect(Tuple<double,double> intervalX, ClipSchema<FabulaAsset> fa)
        {
            double percentStart = 0;
            double percentEnd = 1;
            if (intervalX.First >= fa.start)
            {
                // The percentage of intervalX.First into fa.duration
                percentStart = (intervalX.First - fa.start) / fa.duration;
            }
            
            if (intervalX.Second < fa.start + fa.duration)
            {
                
                // The percentage of fa.Second into fa.duration
                percentEnd = (1 - (fa.start + fa.duration - intervalX.Second) / fa.duration);
            }

            return new Tuple<double, double>(Math.Round(percentStart, 2), Math.Round(percentEnd, 2));
        }

        public Tuple<double, double> IntervalIntersection(ClipSchema<DiscourseAsset> da, ClipSchema<FabulaAsset> fa)
        {
            double start = 0;
            double end = 0;

            // do they overlap at all?
            if (da.start + da.duration < fa.start)
                throw new System.Exception();

            if (fa.start + fa.duration < da.start)
                throw new System.Exception();

            // the interval is the last one to start and the first one to end.
            if (da.start < fa.start)
                start = fa.start;
            else
                start = da.start;

            if (da.start + da.duration <= fa.start + fa.duration)
                return new Tuple<double, double>(start, da.start + da.duration);
            else
                return new Tuple<double, double>(start, fa.start + fa.duration);

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
                predTerms.Add(new Term(instructArg) as ITerm);
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

        [SerializeField]
        public List<string> constraints;

        public ClipSchema(double startTime, double duration, string display, T clipAsset){
            asset = clipAsset;
            start =  startTime;
            this.duration = duration;
            this.display = display;
            constraints = new List<string>();
        }

        public void AddConstraint(string constraint)
        {
            constraints.Add(constraint);
        }

    }
}