using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphNamespace
{

    public class Edge
    {
        public TileNode s { get; set; }
        public TileNode t { get; set; }
        public float weight { get; set; }

        public Edge(TileNode source, TileNode sink, float starting_weight)
        {
            s = source;
            t = sink;
            weight = starting_weight;
        }

        public bool isEqual(Edge other)
        {
            if (other.s.isEqual(s) && other.t.isEqual(t))
            {
                return true;
            }
            if (other.t.isEqual(s) && other.s.isEqual(t))
            {
                return true;
            }
            return false;
        }

        public bool hasNode(TileNode other)
        {
            if (other.isEqual(s))
                return true;
            if (other.isEqual(t))
                return true;
            return false;
        }

        public TileNode getNeighbor(TileNode other)
        {
            if (other.isEqual(s))
                return t;
            if (other.isEqual(t))
                return s;
            throw new System.ArgumentException("other node " + other.ToString() + " is not in this edge");
        }

        public override string ToString()
        {
            return s.ToString() + " --" + weight.ToString() + "-- " + t.ToString();
        }
    }

}
