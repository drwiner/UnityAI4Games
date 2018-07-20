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
using Cinemachine;
using BoltFreezer.Camera;

[ExecuteInEditMode]
public class CamSchemaTest : MonoBehaviour {

    //public CamSchema camSchema;
    //public GameObject camObject;
    //public bool checkConsistency = false;

    //public GameObject action;
    //private UnityActionOperator unityAction;
    //public List<GameObject> terms;

    //public GameObject action2;
    //private UnityActionOperator unityAction2;
    //public List<GameObject> terms2;

    //public bool executeTest = false;

    //public UnityPlanExecutor fabExecutePlanScript;

    //public bool resetPlayableDirectors = false;


    // Update is called once per frame
    void Update()
    {
        //if (unityAction == null)
        //{
        //    unityAction = action.GetComponent<UnityActionOperator>();
        //}
        //if (unityAction2 == null)
        //{
        //    unityAction2 = action2.GetComponent<UnityActionOperator>();
        //}

        //if (checkConsistency)
        //{
        //    checkConsistency = false;
        //    var camAtt = camObject.GetComponent<CamAttributesStruct>();
        //    if (camSchema.IsConsistent(camAtt.AsSchema()))
        //    {
        //        Debug.Log("Consistent");
                
        //    }
        //    else
        //    {
        //        Debug.Log("Inconsistent");
        //    }
        //}

        //if (executeTest)
        //{
        //    executeTest = false;
        //    ExecuteTest();
        //}

        //if (resetPlayableDirectors)
        //{
        //    resetPlayableDirectors = false;
        //    //fabExecutePlanScript.ResetExternally();
        //    discExecutePlanScript.ResetExternally();
        //}
    }

    //public void ExecuteTest()
    //{
    //    // first, set all gameobjects not listed to false. 
    //    var actorHost = GameObject.FindGameObjectWithTag("ActorHost");

    //    var irrelevantActorStorage = new List<GameObject>();
    //    for(int i = 0; i < actorHost.transform.childCount; i++)
    //    {
    //        var ithChild = actorHost.transform.GetChild(i);
    //        if (!terms.Contains(ithChild.gameObject) && !terms2.Contains(ithChild.gameObject))
    //        {
    //            irrelevantActorStorage.Add(ithChild.gameObject);
    //            ithChild.gameObject.SetActive(false);
    //        }
    //    }

    //    // set starting position of relevant objects
    //    SetInitial(unityAction);

    //    var discExecutePlanObject = GameObject.FindGameObjectWithTag("DiscourseTimeline");
    //    discExecutePlanScript = discExecutePlanObject.GetComponent<CamPlan>();

    //    var fabExecutePlanObject = GameObject.FindGameObjectWithTag("ExecuteTimeline");
    //    fabExecutePlanScript = fabExecutePlanObject.GetComponent<UnityPlanExecutor>();

    //    // Instantiate Timeline Components
    //    //fabExecutePlanScript.InstantiateExternally();
    //    //// Create Action
    //    //InstantiateFabulaTimeline(fabExecutePlanObject.GetComponent<PlayableDirector>());
    //    //// Start Execution
    //    //fabExecutePlanScript.ExecuteExternally();

    //    //// Test if camera shot is suitable for a transition to a next action.
    //    //discExecutePlanScript.InitiateExternally();
    //    //// reference cinemachine virtual cam
    //    //var cvc = camObject.GetComponent<CinemachineVirtualCamera>();
    //    //cvc.m_LookAt = terms[0].transform;
    //    //camObject.GetComponent<CinemachineCameraBody>().FocusTransform = terms[0].transform;
    //    //// create clip starting at 0 and lasting for arbitrary time
    //    //discExecutePlanScript.AddClip(cvc, fabExecutePlanObject.GetComponent<PlayableDirector>(), 0, 0, 10, camObject.name);
    //    //// Start Execution
    //    //discExecutePlanScript.ExecuteExternally();
    //}

    //public void SetInitial(UnityActionOperator ua)
    //{
    //    // set starting position of relevant objects
    //    foreach (var precondition in ua.MutablePreconditions)
    //    {
    //        Debug.Log(precondition);
    //        var predicate = PreconditionToPredicate(precondition);
    //        if (!predicate.Sign)
    //        {
    //            continue;
    //        }

    //        if (predicate.Name.Equals("at"))
    //        {
    //            // arg 0 is item name
    //            // arg 1 is location name
    //            var locationPosition = GameObject.Find(predicate.Terms[1].Constant).transform.position;
    //            var item = GameObject.Find(predicate.Terms[0].Constant);
    //            var newPosition = new Vector3(locationPosition.x, item.transform.position.y, locationPosition.z);
    //            item.transform.position = newPosition;
    //        }

    //    }
    //}

    //// Sets up fabula timeline
    //public void InstantiateFabulaTimeline(PlayableDirector pd)
    //{
    //    double startTime = 0;
    //    startTime = ProcessInstructionsForUnityAction(pd, action, unityAction, terms, startTime);
    //    ProcessInstructionsForUnityAction(pd, action2, unityAction2, terms2, startTime);
    //}

    //public static double ProcessInstructionsForUnityAction(PlayableDirector pd, GameObject a, UnityActionOperator ua, List<GameObject> actionTerms, double startTime)
    //{
    //    var instructions = ua.UnityInstructions;

    //    double accumulatedTime = 0;
    //    foreach (var instruction in instructions)
    //    {
    //        //var thisCI = UnityPlanExecutor.ProcessInstruction(pd, a, instruction, actionTerms, startTime + accumulatedTime, 1);
    //        accumulatedTime += 1;
    //    }
    //    startTime = startTime + accumulatedTime;
    //    return startTime;
    //}


    //public Predicate PreconditionToPredicate(string precondition)
    //{
    //    var signage = true;
    //    var stringArray = precondition.Split(' ');
    //    if (stringArray[0].Equals("not"))
    //    {
    //        signage = false;
    //        stringArray = stringArray.Skip(1).ToArray();
    //    }
    //    var predName = stringArray[0];
    //    var theseTerms = new List<ITerm>();
    //    foreach (var item in stringArray.Skip(1))
    //    {
    //        var go = terms[Int32.Parse(item)];
    //        var newObj = new Term(item, go.name, go.tag);
    //        //terms.Add(new Term(item, true) as ITerm);
    //        theseTerms.Add(newObj as ITerm);
    //    }
    //    var newPredicate = new Predicate(predName, theseTerms, signage);
    //    return newPredicate;
    //}

}
