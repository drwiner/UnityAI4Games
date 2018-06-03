using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is the "el presidente" control script for the entire cinemICE system architecture
/// cinemICE : Unity Cinemachine Interactive Cinematography Engine
/// </summary>
[ExecuteInEditMode]
public class CinemICE : MonoBehaviour {

    public bool runEverything = false;

    public bool assembleObjectsAndLocations = false;
    public bool assembleUnityOperators = false;
    public bool assemblePlayableTimelines = false;
    public bool compilePlanningProblem = false;

    public bool compilePrimitiveSteps= false;
    public bool compileCameras = false;

    public bool compileUnityTimelineDecompositions = false;
    public bool composeCompositeSteps = false;

    public bool runPlanner = false;
    public bool executePlan = false;
	// Use this for initialization
	//void Start () {
		
	//}
	
	// Update is called once per frame
	void Update () {
	    if (runEverything)
        {
            runEverything = false;
        }
	}
}
