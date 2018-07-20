using BoltFreezer.FileIO;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Scheduling;
using BoltFreezer.Utilities;
using PlanningNamespace;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CompilationNamespace
{
    public class CacheManager : MonoBehaviour
    {
        public UnityGroundActionFactory UGAF;
        public UnityProblemCompiler UPC;
        public string problemname;
        public bool cacheIt = false;
        public bool decacheIt = false;
        public bool justCacheMapsAndEffort = false;
        
        // Use this for initialization
        public void CacheIt()
        {
            Parser.path = @"D:\documents\frostbow\";
            var FileName = GetFileName();
            var CausalMapFileName = GetCausalMapFileName();
            var ThreatMapFileName = GetThreatMapFileName();
            var EffortMapFileName = GetEffortDictFileName();

            System.IO.Directory.CreateDirectory(FileName);
            System.IO.Directory.CreateDirectory(CausalMapFileName);
            System.IO.Directory.CreateDirectory(ThreatMapFileName);
            System.IO.Directory.CreateDirectory(EffortMapFileName);
            if (!justCacheMapsAndEffort)
            {
                foreach (var op in GroundActionFactory.GroundActions)
                {

                    if (op.Height > 0)
                    {
                        Debug.Log(op.ToString());
                    }

                    if (op.Height > 1)
                    {
                        Debug.Log(op.ToString());
                    }

                    BinarySerializer.SerializeObject(FileName + op.GetHashCode().ToString() + ".CachedOperator", op);
                }
            }

//            CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
 //           CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, UPC.goalPredicateList);
 //           CacheMaps.CacheAddReuseHeuristic(new State(UPC.initialPredicateList) as IState);
   //         UnityGroundActionFactory.PrimaryEffectHack(new State(UPC.initialPredicateList) as IState);

            //BinarySerializer.SerializeObject(CausalMapFileName + ".CachedCausalMap", CacheMaps.CausalTupleMap);
            //BinarySerializer.SerializeObject(ThreatMapFileName + ".CachedThreatMap", CacheMaps.ThreatTupleMap);
            //try
            //{
            //    BinarySerializer.SerializeObject(EffortMapFileName + ".CachedEffortMap", HeuristicMethods.visitedPreds);
            //}
            //catch
            //{

            //}

            //var seeable = HeuristicMethods.visitedPreds;
            //var alreadyVisited = new Dictionary<int, IPredicate>();
            //foreach (var keyvalue in HeuristicMethods.visitedPreds)
            //{
            //    //Debug.Log(keyvalue.Key);
            //    //Debug.Log(keyvalue.Key.GetHashCode().ToString());
            //    if (alreadyVisited.ContainsKey(keyvalue.Key.GetHashCode()))
            //    {
            //        Debug.Log("check it here binch");
            //    }
            //    alreadyVisited[keyvalue.Key.GetHashCode()] = keyvalue.Key;
            //}
            //BinarySerializer.SerializeObject(EffortMapFileName + ".CachedEffortMap", HeuristicMethods.visitedPreds);
        }

        public List<IPlanStep> DecachePlan()
        {
            Parser.path = @"D:\documents\frostbow\";
            var savePath = Parser.GetTopDirectory() + @"Results\" + problemname + @"\Solutions\";
            var plan = BinarySerializer.DeSerializeObject<List<IPlanStep>>(savePath + "PlanSteps");
            return plan;
        }

        public void DecacheSteps()
        {
            Parser.path = @"D:\documents\frostbow\";
            var FileName = GetFileName();
            GroundActionFactory.GroundActions = new List<IOperator>();
            GroundActionFactory.GroundLibrary = new Dictionary<int, IOperator>();

            int maxSeen = 0;
            int maxStepSeen = 0;

            foreach (var file in Directory.GetFiles(Parser.GetTopDirectory() + @"Cached\CachedOperators\UnityBlocksWorld\", problemname + "*.CachedOperator"))
            {
                var op = BinarySerializer.DeSerializeObject<IOperator>(file);
                GroundActionFactory.GroundActions.Add(op);
                GroundActionFactory.GroundLibrary[op.ID] = op;
                Debug.Log(op.ToString());
                
                if (op.ID > maxSeen)
                {
                    maxSeen = op.ID;
                }
                var comp = op as IComposite;
                if (comp == null)
                {
                    continue;
                }
                else
                {
                    foreach (var sub in comp.SubSteps)
                    {
                        if (sub.ID > maxStepSeen)
                        {
                            maxStepSeen = sub.ID;
                        }
                    }
                    if (comp.GoalStep.ID > maxStepSeen)
                    {
                        maxStepSeen = comp.GoalStep.ID;
                    }
                }
            }
            // THIS is so that initial and goal steps created don't get matched with these
            Operator.SetCounterExternally(maxSeen + 1);
            PlanStep.SetCounterExternally(maxStepSeen + 1);
        }

        public void DeCacheIt()
        {
            Parser.path = @"D:\documents\frostbow\";
            BoltFreezer.Utilities.Logger.InitiateTimer();
            BoltFreezer.Utilities.Logger.Active = true;
            BoltFreezer.Utilities.Logger.SetDirectory(@"D:\documents\frostbow\Results\UnityBlocksWorld\");
            DecacheSteps();

          //  UnityPlanningInterface.AddObservedNegativeConditions(UPC);

            var CausalMapFileName = GetCausalMapFileName();
            var ThreatMapFileName = GetThreatMapFileName();
            var EffortMapFileName = GetEffortDictFileName();

            CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
            CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, UPC.goalPredicateList);
            CacheMaps.CacheAddReuseHeuristic(new State(UPC.initialPredicateList) as IState);
            CacheMaps.PrimaryEffectHack(new State(UPC.initialPredicateList) as IState);

            //try
            //{
            //    var cmap = BinarySerializer.DeSerializeObject<TupleMap<IPredicate, List<int>>>(CausalMapFileName + ".CachedCausalMap");
            //    CacheMaps.CausalTupleMap = cmap;

            //    var tcmap = BinarySerializer.DeSerializeObject<TupleMap<IPredicate, List<int>>>(ThreatMapFileName + ".CachedThreatMap");
            //    CacheMaps.ThreatTupleMap = tcmap;

            //    try
            //    {
            //        var emap = BinarySerializer.DeSerializeObject<TupleMap<IPredicate, int>>(EffortMapFileName + ".CachedEffortMap");
            //        HeuristicMethods.visitedPreds = emap;
            //    }
            //    catch
            //    {
            //        CacheMaps.CacheAddReuseHeuristic(new State(UPC.initialPredicateList) as IState);
            //        UnityGroundActionFactory.PrimaryEffectHack(new State(UPC.initialPredicateList) as IState);
            //    }
            //}
            //catch
            //{
            //    CacheMaps.CacheLinks(GroundActionFactory.GroundActions);
            //    CacheMaps.CacheGoalLinks(GroundActionFactory.GroundActions, UPC.goalPredicateList);
            //    CacheMaps.CacheAddReuseHeuristic(new State(UPC.initialPredicateList) as IState);
            //    UnityGroundActionFactory.PrimaryEffectHack(new State(UPC.initialPredicateList) as IState);
            //}

        }

        public string GetFileName()
        {
            return Parser.GetTopDirectory() + @"Cached\CachedOperators\UnityBlocksWorld\" + problemname;
        }

        public string GetCausalMapFileName()
        {
            return Parser.GetTopDirectory() + @"Cached\CausalMaps\UnityBlocksWorld\" + problemname;
        }

        public string GetThreatMapFileName()
        {
            return Parser.GetTopDirectory() + @"Cached\ThreatMaps\UnityBlocksWorld\" + problemname;
        }

        public string GetEffortDictFileName()
        {
            return Parser.GetTopDirectory() + @"Cached\EffortMaps\UnityBlocksWorld\" + problemname;
        }

        // Update is called once per frame
        void Update()
        {
            if (cacheIt)
            {
                cacheIt = false;
                CacheIt();
                //CacheIt();
            }

            if (decacheIt)
            {
                decacheIt = false;
            }
        }
    }

}