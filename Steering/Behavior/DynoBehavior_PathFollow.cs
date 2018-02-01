using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoalNamespace;
using GraphNamespace;

namespace SteeringNamespace
{

    public class DynoBehavior_PathFollow : MonoBehaviour
    {
        private Stack<TileNode> currentPath = new Stack<TileNode>();
        private Vector3 currentGoal;
        private Vector3 prevGoal;
        private PathGoal goalComponent;

        public TileGraph tg;
        private TileNode currentTile;
        private TileNode nextTile;
        private TileNode currentGoalTile;
        private TileNode prevGoalTile;

        private Kinematic char_RigidBody;
        private KinematicSteering ks;
        private DynoSteering ds;

        private KinematicSteeringOutput kso;
        private DynoSeek_PathFollow seek;
        private DynoArrive_PathFollow arrive;
        private DynoAlign_PathFollow align;

        private DynoSteering ds_force;
        private DynoSteering ds_torque;

        void Start()
        {
            char_RigidBody = GetComponent<Kinematic>();
            goalComponent = GetComponent<PathGoal>();
            arrive = GetComponent<DynoArrive_PathFollow>();
            align = GetComponent<DynoAlign_PathFollow>();
            seek = GetComponent<DynoSeek_PathFollow>();
        }

        void Update()
        {
            currentGoalTile = goalComponent.getGoalTile();
            currentTile = QuantizeLocalize.Quantize(transform.position, tg);
            if (prevGoalTile != currentGoalTile)
            {
                foreach (TileNode tn in currentPath)
                {
                    if (tn.isEqual(currentTile))
                        continue;
                    tn.setOffMaterial();
                }

                
                currentPath = PathFind.Dijkstra(tg, currentTile, currentGoalTile);
                prevGoalTile = currentGoalTile;
                // light up all tiles
                foreach (TileNode tn in currentPath)
                {
                    if (tn.isEqual(currentGoalTile))
                        continue;
                    tn.setPlanMaterial();
                }

                currentGoal = QuantizeLocalize.Localize(currentPath.Pop());
            }


            // determine how to set force
            if (currentPath.Count > 0)
            {
                ds_force = seek.getSteering(currentGoal);
                // pop when seek says we've made it into range and seek the next target
                if (seek.changeGoal)
                {
                    nextTile = currentPath.Pop();
                    nextTile.setNextMaterial();
                    currentGoal = QuantizeLocalize.Localize(nextTile);
                    if (currentPath.Count > 0)
                        ds_force = seek.getSteering(currentGoal);
                    else
                        ds_force = arrive.getSteering(currentGoal);
                }
            }
            else if (currentPath.Count == 0)
            {
                ds_force = arrive.getSteering(currentGoal);
            }


            ds_torque = align.getSteering(currentGoal);

            ds = new DynoSteering();
            ds.force = ds_force.force;
            ds.torque = ds_torque.torque;

            kso = char_RigidBody.updateSteering(ds, Time.deltaTime);
            transform.position = new Vector3(kso.position.x, transform.position.y, kso.position.z);
            transform.rotation = Quaternion.Euler(0f, kso.orientation * Mathf.Rad2Deg, 0f);


        }
    }
}