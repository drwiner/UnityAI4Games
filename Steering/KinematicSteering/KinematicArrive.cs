using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicArrive : MonoBehaviour {

    private Goal goalObject;
    private Transform goal;
    private SteeringParams sp;
    private Kinematic charKinematic;
    public float radius_of_satisfaction = 0.5f;
    public float time_to_target = 0.25f;

	// Use this for initialization
	void Start () {
        goalObject = GetComponent<Goal>();
        sp = GetComponent<SteeringParams>();
        charKinematic = GetComponent<Kinematic>();
	}

    // Update is called once per frame
    public Vector3 updateSteering()
    {
        goal = goalObject.getGoal();
        //steering = new Steering();
        Vector3 new_velc = goal.position - this.transform.position;

        if (new_velc.magnitude < radius_of_satisfaction) {
            new_velc = new Vector3(0f, 0f, 0f);
            return new_velc;
        }

        new_velc = new_velc / time_to_target;
        
        // clip speed
        if (new_velc.magnitude > sp.MAXSPEED)
        {
            new_velc.Normalize();
            new_velc = new_velc * sp.MAXSPEED;
        }

        return new_velc;
    }
}
