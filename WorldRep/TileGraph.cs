using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GraphNamespace
{

    public class TileGraph : MonoBehaviour
    {
        public List<TileNode> nodes { get; set; }
        public List<Edge> edges { get; set; }

        void Start()
        {
            nodes = new List<TileNode>();
            edges = new List<Edge>();

            for (int i = 0; i < transform.childCount; i++)
            {

                TileNode tn = transform.GetChild(i).GetComponent<TileNode>();
                nodes.Add(tn);

                for (int j = 0; j < transform.childCount; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    TileNode other_node = transform.GetChild(j).GetComponent<TileNode>();

                    if (isAdjacent(tn.transform.position, other_node.transform.position))
                    {
                        Edge e = new Edge(tn, other_node, 1f);
                        if (!edges.Any(other_edge => other_edge.isEqual(e)))
                        {
                            edges.Add(e);
                            //Debug.Log(e);
                        }
                    }

                }
            }

        }

        //private 

        private bool isAdjacent(Vector3 a, Vector3 b)
        {
            if (Mathf.Abs(a.x - b.x) < 1.5)
            {
                return true;
            }
            if (Mathf.Abs(a.z - b.z) < 1.5)
            {
                return true;
            }
            return false;
        }

    }

}
