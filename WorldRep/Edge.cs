using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphNamespace
{
    [Serializable]
    public class Edge
    {
        [SerializeField]
        public TileNode S;
        [SerializeField]
        public TileNode T;
        [SerializeField]
        public float Weight;

        public Edge(TileNode source, TileNode sink, float starting_weight)
        {
            S = source;
            T = sink;
            Weight = starting_weight;
        }

        public bool IsEqual(Edge other)
        {
            if (other.S.isEqual(S) && other.T.isEqual(T))
            {
                return true;
            }
            if (other.T.isEqual(S) && other.S.isEqual(T))
            {
                return true;
            }
            return false;
        }

        public bool HasNode(TileNode other)
        {
            if (other.isEqual(S))
                return true;
            if (other.isEqual(T))
                return true;
            return false;
        }

        public TileNode GetNeighbor(TileNode other)
        {
            if (other.isEqual(S))
                return T;
            if (other.isEqual(T))
                return S;
            throw new System.ArgumentException("other node " + other.ToString() + " is not in this edge");
        }

        public override string ToString()
        {
            return S.ToString() + " --" + Weight.ToString() + "-- " + T.ToString();
        }
    }

}
