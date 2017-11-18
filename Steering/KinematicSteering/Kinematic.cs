using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kinematic : MonoBehaviour {

    private SteeringParams sp;
    private Vector3 position;
    private float rotation;
    private float orientation;
    private Vector3 velc;
    private float height;
    private KinematicSteeringOutput steering;
    //private Steering seeking;
    //private Steering arriving;

    //private KinematicSeek seek;
    //private KinematicArrive arrive;
    //public Vector3 force;
    //public float torque;

    // Use this for initialization
    void Start () {
        sp = GetComponent<SteeringParams>();

        position = this.transform.position;
        velc = new Vector3(0f, 0f, 0f);
        rotation = 0f;
        orientation = 0f;
    }

	// Update is called once per frame
	public KinematicSteeringOutput updateSteering(KinematicSteering ks, DynoSteering ds) {

        steering = new KinematicSteeringOutput();

        // make Updates
        position += ks.velc * Time.deltaTime;
        orientation += ks.rotation * Time.deltaTime;

        velc += ds.force * Time.deltaTime;
        orientation += ds.torque * Time.deltaTime;

        steering.position = velc;
        steering.orientation = orientation;

        return steering;
	}

    //private Steering Steer()
    //{
    //    seek_steering = seek.updateSteering();
    //    arrive_steering = arrive.updateSteering();
    //    if (arrive_steering.force == new Vector3(0f, 0f, 0f))
    //    {
    //        return arrive_steering;
    //    }
    //    else {
    //        seek_steering.force
    //    }
        
    //}

    public void setOrientation(float new_value)
    {
        orientation = new_value;
    }

    public void setRotation(float new_rotation)
    {
        rotation = new_rotation;
    }

    public float getNewOrientation(Vector3 new_force)
    {
        new_force.Normalize();
        if (new_force.magnitude > 0f) {
            return Mathf.Atan2(-velc.z, velc.x);
        } else
        {
            return orientation;
        }
    }

    public static float mapToRange(float radians)
    {
        float targetRadians = radians;
        while (targetRadians <= -Mathf.PI)
        {
            targetRadians += Mathf.PI * 2;
        }
        while (targetRadians >= Mathf.PI)
        {
            targetRadians -= Mathf.PI * 2;
        }
        return targetRadians;
    }

    public static float randomBinomial()
    {
        return (float)(Random.Range(0f, 1f) - Random.Range(0f, 1f));
    }
}
