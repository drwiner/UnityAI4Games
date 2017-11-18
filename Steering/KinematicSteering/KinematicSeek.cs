using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicSeek : MonoBehaviour {

    private Kinematic characterKinematic;
    private KinematicSteering steering;
    private SteeringParams sp;
    private Goal goalObject;
    private Transform goal;

    // Use this for initialization
    void Start () {
        characterKinematic = GetComponent<Kinematic>();
        goalObject = GetComponent<Goal>();
        sp = GetComponent<SteeringParams>();
    }
	
	// Update is called once per frame
	public KinematicSteering updateSteering () {
        goal = goalObject.getGoal();
        steering = new KinematicSteering();
        steering.velc = goal.position - this.transform.position;
        steering.velc.Normalize();
        steering.velc = steering.velc * sp.MAXSPEED;

        // calculate desired orientation (face where you're going)
        float new_orient = characterKinematic.getNewOrientation(steering.velc);
        
        // Need to set these manually for now
        characterKinematic.setOrientation(new_orient);
        characterKinematic.setRotation(0f);

        return steering;
    }
}
