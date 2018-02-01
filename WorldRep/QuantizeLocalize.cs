using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphNamespace
{
    class QuantizeLocalize
    {
        private static Vector3 offset = new Vector3(0.75f, 0, 0.75f);

        public static TileNode Quantize(Vector3 position, TileGraph tg)
        {
            float best_dist = 1000f;
            TileNode best_node = null;
            foreach (TileNode tn in tg.nodes)
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

