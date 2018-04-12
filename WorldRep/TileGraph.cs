﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace GraphNamespace
{
    [ExecuteInEditMode]
    [Serializable]
    public class TileGraph : MonoBehaviour
    {
        [SerializeField]
        public List<TileNode> Nodes;

        [SerializeField]
        public List<Edge> Edges;

        [SerializeField]
        public float adjacent_distance = .6f;

        void Start()
        {
            CalculateEdges();
        }

        public void Update()
        {
            CalculateEdges();
        }

        public void CalculateEdges()
        {
            Nodes = new List<TileNode>();
            Edges = new List<Edge>();

            for (int i = 0; i < transform.childCount; i++)
            {

                TileNode tn = transform.GetChild(i).GetComponent<TileNode>();
                Nodes.Add(tn);

                for (int j = 0; j < transform.childCount; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    TileNode other_node = transform.GetChild(j).GetComponent<TileNode>();

                    if (IsAdjacent(tn.transform.position, other_node.transform.position))
                    {
                        Edge e = new Edge(tn, other_node, 1f);
                        if (!Edges.Any(other_edge => other_edge.IsEqual(e)))
                        {
                            Edges.Add(e);
                            //Debug.Log(e);
                        }
                    }

                }
            }
        }

        public IEnumerable<Edge> GetAdjacentEdges(TileNode tn)
        {
            return Edges.Where(e => e.HasNode(tn));
        }

        //private 

        private bool IsAdjacent(Vector3 a, Vector3 b)
        {
            float zdist = Mathf.Abs(a.z - b.z);
            float xdist = Mathf.Abs(a.x - b.x);
            if (zdist < adjacent_distance && xdist < 2*adjacent_distance)
            {
                return true;
            }
            if (xdist < adjacent_distance && zdist < 2*adjacent_distance)
            {
                return true;
            }
            return false;
        }

    }

}
