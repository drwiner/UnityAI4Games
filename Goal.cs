using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal :MonoBehaviour{

    private GameObject previousGoal;
    public GameObject goalObject;
    private Transform goal;

    public Material non_goal_material;
    public Material goal_material;

    private bool changed_goal = false;

    public void setGoal(GameObject new_goal_object)
    {
        goalObject = new_goal_object;
    }

    public Transform getGoal()
    {
        return goal;
    }

    void Start()
    {
        previousGoal = goalObject;
        goal = goalObject.transform;
        goalObject.GetComponent<Renderer>().material = goal_material;
        //goalObject = (GameObject)goal.parent;
    }

    void Update()
    {
        if (previousGoal != goalObject)
        {
            changed_goal = true;
            goal = goalObject.transform;
        }

        if (changed_goal)
        {
            previousGoal.GetComponent<Renderer>().material = non_goal_material;
            //previousGoal.AddComponent<Material>(non_goal_material);
            goalObject.GetComponent<Renderer>().material = goal_material;
            //goalObject.AddComponent<Material>(goal_material);
            changed_goal = false;
            previousGoal = goalObject;
        }
    }
}
