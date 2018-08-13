using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using BoltFreezer.Utilities;

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

        public bool calculateEdgesByDistance;
        public bool makeEdgesByHand = false;

        void Start()
        {
            //CalculateEdges();
            makeEdgesByHand = true;
        }

        public void Update()
        {
            if (makeEdgesByHand)
            {
                makeEdgesByHand = false;
                Edges = new List<Edge>();
                Edges.Add(new Edge(Nodes[0], Nodes[1], 0f));
                Edges.Add(new Edge(Nodes[1], Nodes[2], 0f));
                Edges.Add(new Edge(Nodes[2], Nodes[3], 0f));
                Edges.Add(new Edge(Nodes[1], Nodes[3], 0f));
            }
            else if (calculateEdgesByDistance)
            {
                calculateEdgesByDistance = false;
                CalculateEdges();
            }
        }

        public void CalculateEdges()
        {
            Nodes = new List<TileNode>();
            Edges = new List<Edge>();

            for (int i = 0; i < transform.childCount; i++)
            {
                var go = transform.GetChild(i).gameObject;
                if (!go.name.Last().Equals("A"))
                {
                    go.name = string.Format("{0}{1}", "L", i.ToString());
                }
                TileNode tn = transform.GetChild(i).GetComponent<TileNode>();
                if (!transform.GetChild(i).gameObject.activeSelf)
                {
                    continue;
                }
                Nodes.Add(tn);

                for (int j = 0; j < transform.childCount; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    TileNode other_node = transform.GetChild(j).GetComponent<TileNode>();
                    if (!transform.GetChild(j).gameObject.activeSelf)
                    {
                        continue;
                    }

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

        public Edge FindRelevantEdge(string location1, string location2)
        {
            foreach(var edge in Edges)
            {
                if (edge.S.name.Equals(location1) && edge.T.name.Equals(location2))
                {
                    return edge;
                }
                if (edge.T.name.Equals(location1) && edge.S.name.Equals(location2))
                {
                    return edge;
                }
            }
            return null;
        }

        public Tuple<Edge, int> FindRelevantDirectedEdge(string location1, string location2)
        {
            foreach (var edge in Edges)
            {
                if (edge.S.name.Equals(location1) && edge.T.name.Equals(location2))
                {
                    return new Tuple<Edge,int>(edge, 1);
                }
                if (edge.T.name.Equals(location1) && edge.S.name.Equals(location2))
                {
                    return new Tuple<Edge, int>(edge, -1);
                }
            }
            return null;
        }

    }

}
