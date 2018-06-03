using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanningNamespace
{
    [Serializable]
    public class Schedule<T> : Graph<T>
    {

        public Schedule() : base()
        {

        }
        public Schedule(HashSet<T> nodes, HashSet<Tuple<T, T>> edges) : base(nodes, edges, new Dictionary<T, HashSet<T>>())
        {
        }

        public Schedule(HashSet<T> nodes, HashSet<Tuple<T, T>> edges, HashSet<Tuple<T, T>> cntgs) : base(nodes, edges, new Dictionary<T, HashSet<T>>())
        {
        }

        public Schedule(HashSet<Tuple<T, T>> cntgs) : base(new HashSet<T>(), cntgs, new Dictionary<T, HashSet<T>>())
        {
        }

        public Schedule(List<Tuple<T, T>> cntgs) : base(new HashSet<T>(), new HashSet<Tuple<T, T>>(cntgs), new Dictionary<T, HashSet<T>>())
        {
        }


        public bool HasFault(Graph<T> orderings)
        {
            /* PYTHON CODE: 
             * 
             * def cntg_consistent(self):
            cntg_edges = [edge for edge in self.edges if edge.label == "cntg"]
            sources = []
            sinks = []
            # cntg must be s -> t -> u and cannot exist another s -> s' or t' -> u
            for edge in cntg_edges:
                if edge.sink in sinks:
                    return False
                if edge.source in sources:
                    return False
                sources.append(edge.source)
                sinks.append(edge.sink)
                # cannot be an ordering between cntg edge e.g. s --cntg--> t and s < u < t
                ordering_edges = [ord for ord in self.edges if ord.source == edge.source and ord.sink != edge.sink and ord.label == "<"]
                for ord in ordering_edges:
                    if self.isPath(ord.sink, edge.sink):
                        return False
                ordering_edges = [ord for ord in self.edges if ord.sink == edge.sink and ord.source != edge.source and ord.label == "<"]
                for ord in ordering_edges:
                    if self.isPath(edge.source, ord.source):
                        return False

            return True
            */
            List<T> sources = new List<T>();
            List<T> sinks = new List<T>();
            foreach (var edge in edges)
            {
                // base cases
                if (sinks.Contains(edge.Second))
                {
                    return true;
                }
                if (sources.Contains(edge.First))
                {
                    return true;
                }

                sources.Add(edge.First);
                sinks.Add(edge.Second);

                foreach (var ordering in orderings.edges)
                {
                    if (ordering.First.Equals(edge.First) && !ordering.Second.Equals(edge.Second))
                    {
                        // There cannot be a path from the ordering to the tail of the cntg edge
                        if (IsPath(ordering.Second, edge.Second))
                        {
                            return true;
                        }
                    }

                    if (ordering.Second.Equals(edge.Second) && !ordering.First.Equals(edge.First))
                    {
                        // There cannot be a path from the head of cntg edge to the head of the ordering
                        if (IsPath(edge.First, ordering.First))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}

