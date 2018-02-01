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
        public float adjacent_distance = 1.3f;

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

        public IEnumerable<Edge> getAdjacentEdges(TileNode tn)
        {
            return edges.Where(e => e.hasNode(tn));
        }

        //private 

        private bool isAdjacent(Vector3 a, Vector3 b)
        {
            float zdist = Mathf.Abs(a.z - b.z);
            float xdist = Mathf.Abs(a.x - b.x);
            if (zdist < adjacent_distance && xdist < adjacent_distance)
            {
                return true;
            }
            return false;
        }

    }

}
