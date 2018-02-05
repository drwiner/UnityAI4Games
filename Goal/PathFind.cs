using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;
using GraphNamespace;

namespace GoalNamespace {

    class NodeInfo
    {
        public float dist = Mathf.Infinity;
        public float heuristic = 0f;
        public TileNode parent = null;
    }

    public static class PathFind {

        public static Stack<TileNode> Path { get; set; }
        private static SimplePriorityQueue<TileNode, float> frontier;
        private static Dictionary<TileNode, NodeInfo> node_dict;

        

        private static Dictionary<TileNode, NodeInfo> DijkstraInitialDictLoad(TileNode start, TileGraph tg)
        {
            node_dict = new Dictionary<TileNode, NodeInfo>();
            foreach (TileNode tn in tg.nodes)
            {
                NodeInfo ni = new NodeInfo();
                if (start.isEqual(tn))
                    ni.dist = 0f;

                node_dict[tn] = ni;
            }

            return node_dict;
        }

        public static Stack<TileNode> Dijkstra(TileGraph tg, TileNode start, TileNode end)
        {
            frontier = new SimplePriorityQueue<TileNode, float>();
            frontier.Enqueue(start, 0f);
            node_dict = DijkstraInitialDictLoad(start, tg);
            List<TileNode> Expanded = new List<TileNode>();

            TileNode v;
            TileNode other;
            float edge_weight;
            float dist_to_node;
            float cost_so_far;

            while (frontier.Count > 0)
            {
                v = frontier.Dequeue();
                cost_so_far = node_dict[v].dist;
                Expanded.Add(v);

                //List<Edge> experiment = tg.getAdjacentEdges(v) as List<Edge>;
                foreach (Edge adj_edge in tg.getAdjacentEdges(v))
                {
                    other = adj_edge.getNeighbor(v);
                    edge_weight = adj_edge.weight;
                    dist_to_node = node_dict[other].dist;
                    if (cost_so_far + edge_weight < dist_to_node)
                    {
                        node_dict[other].dist = cost_so_far + edge_weight;
                        node_dict[other].parent = v;
                    }

                    if (!Expanded.Any(node => node.isEqual(other)) && !frontier.Any(node => node.isEqual(other)))
                        frontier.Enqueue(other, node_dict[other].dist);
                }
            }

            Path = NodeDictToPath(node_dict, start, end);

            return Path;
        }

        private static Stack<TileNode> NodeDictToPath(Dictionary<TileNode, NodeInfo> node_dict, TileNode start, TileNode end)
        {
            Stack<TileNode> path = new Stack<TileNode>();
            bool found_start = false;
            TileNode currentEnd = end;
            while (!found_start)
            {
                path.Push(currentEnd);
                currentEnd = node_dict[currentEnd].parent;
                if (start.isEqual(currentEnd))
                {
                    found_start = true;
                }
            }
            return path;
        }

        public static Stack<TileNode> Astar(TileGraph tg, TileNode start, TileNode end)
        {
            frontier = new SimplePriorityQueue<TileNode, float>();
            frontier.Enqueue(start, 0f);
            node_dict = new Dictionary<TileNode, NodeInfo>();
            List<TileNode> Expanded = new List<TileNode>();

            while (frontier.Count > 0)
            {

            }

            return Path;
        }

    }

}
