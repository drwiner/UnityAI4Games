using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphNamespace
{
    class QuantizeLocalize
    {
        private static Vector3 offset = new Vector3(0.6f, 0, -0.6f);

        // A potential shortcut method which only looks at adjacent nodes to some previous node for quantization
        public static TileNode QuantizeWithPreviousNode(Vector3 position, TileGraph tg, TileNode last_node)
        {
            float best_dist = 1000f;
            TileNode best_node = null;
            List<Edge> adj_edges = tg.GetAdjacentEdges(last_node) as List<Edge>;
            foreach (Edge adj_edge in adj_edges)
            {
                TileNode other_node = adj_edge.GetNeighbor(last_node);
                float dist = Mathf.Abs(Vector3.Distance(Localize(other_node), position));
                if (dist < best_dist)
                {
                    best_node = other_node;

                }
            }
            return best_node;
        } 

        public static TileNode Quantize(Vector3 position, TileGraph tg)
        {
            float best_dist = 1000f;
            TileNode best_node = null;
            foreach (TileNode tn in tg.Nodes)
            {
                float dist = Mathf.Abs(Vector3.Distance(Localize(tn),position));

                if (dist < best_dist)
                {
                    best_node = tn;
                    best_dist = dist;
                }
            }
            return best_node;

        }

        public static Vector3 Localize(TileNode tn)
        {
            return tn.transform.position + offset;
        }
    }

}

