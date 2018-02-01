﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge {
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

    public override string ToString()
    {
        return s.ToString() + " --" + weight.ToString() + "-- " + t.ToString();
    }
}
