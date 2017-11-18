using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Behavior : MonoBehaviour {

    private Kinematic char_kinematic;
    private KinematicSteering ks;
    private DynoSteering ds;
    private KinematicSteeringOutput kso;
    private KinematicSeek seek;
    private KinematicArrive arrive;

    private KinematicSteering seeking_output;
    private Vector3 arrival_output;

    // Use this for initialization
    void Start () {
        char_kinematic = GetComponent<Kinematic>();
        seek = GetComponent<KinematicSeek>();
        arrive = GetComponent<KinematicArrive>();
    }
	
	// Update is called once per frame
	void Update () {
        //seeking = seek.updateSteering();
        //velc = seeking.force;
        //rotation = seeking.torque;

        //velc = arrive.updateSteering();

        seeking_output = seek.updateSteering();
        arrival_output = arrive.updateSteering();

        ks.velc = arrival_output;

        kso = char_kinematic.updateSteering(ks, ds);

        //rotation = seeking.torque;

        //velc = arrive.updateSteering();

        transform.position = new Vector3(kso.position.x, transform.position.y, ks.position.z);
        transform.rotation = Quaternion.Euler(0f, kso.orientation * Mathf.Rad2Deg, 0f);

        ks = 
        // call Kinematic to get the latest
        

        

        if (velc.magnitude > sp.MAXSPEED)
        {
            velc.Normalize();
            velc = velc * sp.MAXSPEED;
        }
    }
}
