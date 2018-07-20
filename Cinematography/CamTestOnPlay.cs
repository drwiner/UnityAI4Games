using CameraNamespace;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamTestOnPlay : MonoBehaviour {

    public float shotDuration = 3f;

    private float prevTime;
    private int camIndex;

    public int numCams;
    private CamGen camGen;
    public GameObject Target;

    //public List<GameObject> CameraList
    //{
    //    get { return camGen.CameraList; }
    //}

    //void Awake()
    //{
    //    camGen = GetComponent<CamGen>();
    //    if (CameraList == null)
    //    {
    //        camGen.Initiate();
    //        camGen.Assemble();
    //    }
    //    numCams = CameraList.Count;
    //}

    // Use this for initialization
    void Start () {
        //CameraList = new List<GameObject>();
        //for (int i = 0; i < camGen.transform.childCount; i++)
        //{
        //   CameraList.Add(camGen.transform.GetChild(i).gameObject);
        //}

        camIndex = -1;
        prevTime = Time.time;
	}
	
	// Update is called once per frame
	//void Update () {
 //       if (CameraList.Count > 0)
 //       {
 //           if (Time.time - prevTime > shotDuration)
 //           {
 //               if (camIndex > -1)
 //                   CameraList[camIndex].SetActive(false);
 //               camIndex++;
 //               if (camIndex >= CameraList.Count)
 //               {
 //                   camIndex = 0;
 //               }

 //               CameraList[camIndex].GetComponent<CinemachineVirtualCamera>().m_LookAt = Target.transform;

 //               //CameraList[camIndex].GetComponent<CinemachineCameraBody>().FocusTransform = Target.transform;
 //               CameraList[camIndex].SetActive(true);
 //               prevTime = Time.time;
 //               Debug.Log(CameraList[camIndex].name);
 //           }
 //       }
 //       else
 //       {
 //           numCams = CameraList.Count;
 //       }
		
	//}
}
