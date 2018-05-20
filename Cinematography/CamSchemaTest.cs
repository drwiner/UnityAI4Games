using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraNamespace;
using PlanningNamespace;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using BoltFreezer.PlanTools;
using BoltFreezer.Interfaces;
using System.Linq;
using System;

[ExecuteInEditMode]
public class CamSchemaTest : MonoBehaviour {

    public CamSchema camSchema;
    public GameObject camObject;
    public bool checkConsistency = false;

    public GameObject action;
    private UnityActionOperator unityAction;
    public List<GameObject> terms;
    public bool executeTest = false;

    public ExecutePlan fabExecutePlanScript;

    public bool resetPlayableDirectors = false;


    // Update is called once per frame
    void Update()
    {
        if (unityAction == null)
        {
            unityAction = action.GetComponent<UnityActionOperator>();
        }

        if (checkConsistency)
        {
            checkConsistency = false;
            var camAtt = camObject.GetComponent<CamAttributesStruct>();
            if (camSchema.IsConsistent(camAtt))
            {
                Debug.Log("Consistent");
                
            }
            else
            {
                Debug.Log("Inconsistent");
            }
        }

        if (executeTest)
        {
            executeTest = false;
            ExecuteTest();
        }

        if (resetPlayableDirectors)
        {
            resetPlayableDirectors = false;
            fabExecutePlanScript.ResetExternally();
            //fabExecutePlanScript = null;
        }
    }

    public void ExecuteTest()
    {
        // first, set all gameobjects not listed to false. 
        var actorHost = GameObject.FindGameObjectWithTag("ActorHost");

        var irrelevantActorStorage = new List<GameObject>();
        for(int i = 0; i < actorHost.transform.childCount; i++)
        {
            var ithChild = actorHost.transform.GetChild(i);
            if (!terms.Contains(ithChild.gameObject))
            {
                irrelevantActorStorage.Add(ithChild.gameObject);
                ithChild.gameObject.SetActive(false);
            }
        }

        // set starting position of relevant objects
        foreach(var precondition in unityAction.MutablePreconditions)
        {
            Debug.Log(precondition);
            var predicate = PreconditionToPredicate(precondition);
            if (!predicate.Sign)
            {
                continue;
            }

            if (predicate.Name.Equals("at"))
            {
                // arg 0 is item name
                // arg 1 is location name
                var locationPosition = GameObject.Find(predicate.Terms[1].Constant).transform.position;
                var item = GameObject.Find(predicate.Terms[0].Constant);
                var newPosition = new Vector3(locationPosition.x, item.transform.position.y, locationPosition.z);
                item.transform.position = newPosition;
            }

        }

        var discoursePlayableDirector = GameObject.FindGameObjectWithTag("DiscourseTimeline").GetComponent<PlayableDirector>();

        var fabExecutePlanObject = GameObject.FindGameObjectWithTag("ExecuteTimeline");
        fabExecutePlanScript = fabExecutePlanObject.GetComponent<ExecutePlan>();

        fabExecutePlanScript.InstantiateExternally();
        InstantiateFabulaTimeline(fabExecutePlanScript);
        fabExecutePlanScript.ExecuteExternally();

        //var fabulaPlayableDirector = fabExecutePlanObject.GetComponent<PlayableDirector>();

    }

    public void InstantiateFabulaTimeline(ExecutePlan executePlanScript)
    {

        double startTime = 0;
        double accumulatedTime = 0;
        char[] charsToTrim = { '(', ')' };
        var CIList = new List<ClipInfo>();

        // Follow Unity Instructions
        var instructions = unityAction.UnityInstructions;

        accumulatedTime = 0;
        foreach (var instruction in instructions)
        {
            var thisCI = executePlanScript.ProcessInstruction(action, instruction, terms, startTime + accumulatedTime, 1);
            CIList.Add(thisCI);
            accumulatedTime += 1;
        }
        startTime = startTime + accumulatedTime;

    }


    public Predicate PreconditionToPredicate(string precondition)
    {
        var signage = true;
        var stringArray = precondition.Split(' ');
        if (stringArray[0].Equals("not"))
        {
            signage = false;
            stringArray = stringArray.Skip(1).ToArray();
        }
        var predName = stringArray[0];
        var theseTerms = new List<ITerm>();
        foreach (var item in stringArray.Skip(1))
        {
            var go = terms[Int32.Parse(item)];
            var newObj = new Term(item, go.name, go.tag);
            //terms.Add(new Term(item, true) as ITerm);
            theseTerms.Add(newObj as ITerm);
        }
        var newPredicate = new Predicate(predName, theseTerms, signage);
        return newPredicate;
    }

}
