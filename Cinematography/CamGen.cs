using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine.Timeline;
using Cinemachine;
using Cinematography;
using PlanningNamespace;
using GraphNamespace;
using System;

namespace CameraNamespace {

    [ExecuteInEditMode]
    public class CamGen : MonoBehaviour {

        // Needs access to problem and actions
        private UnityProblemCompiler problemStates;
        public List<UnityActionOperator> actions;
        private List<GameObject> actors;
        private TileGraph tileMap;
        private List<FramingType> FrameTypeList;

        public bool initiated = false;
        public bool assembleCameras = false;
        public bool filterExisting = false;
        public int numCams;
        public bool refresh;
        public bool deleteChildren = false;
        public bool activateAllCams = false;

        private List<GameObject> cameraList;

        public List<GameObject> CameraList
        {
            get { return cameraList; }
        }

        // Use this for initialization
        void Start()
        {
            Initiate();
        }

        public void Initiate()
        {
            if (CinematographyAttributes.lensFovData != null && (cameraList != null && cameraList.Count > 0))
            {
                initiated = true;
                return;
            }
            ProCamsLensDataTable.Instance.LoadData();
            CinematographyAttributes.lensFovData = ProCamsLensDataTable.Instance.GetFilmFormat("35mm 16:9 Aperture (1.78:1)").GetLensKitData(0)._fovDataset;
            CinematographyAttributes.standardNoise = Instantiate(Resources.Load("Handheld_tele_mild", typeof(NoiseSettings))) as NoiseSettings;

            //FrameTypeList = new List<FramingType>() { FramingType.ExtremeLong, FramingType.Full, FramingType.Waist, FramingType.ExtremeCloseUp };

            // problem information may not be useful here
            problemStates = GameObject.FindGameObjectWithTag("Problem").GetComponent<UnityProblemCompiler>();

            // locations are the basic anchor for camera positioning
            tileMap = GameObject.FindGameObjectWithTag("Locations").GetComponent<TileGraph>();

            // actors
            var actorHost = GameObject.FindGameObjectWithTag("ActorHost");
            actors = new List<GameObject>();
            for (int i = 0; i < actorHost.transform.childCount; i++)
            {
                actors.Add(actorHost.transform.GetChild(i).gameObject);
            }

            initiated = true;
            Debug.Log("Initiated");
        }

        // Update is called once per frame
        void Update() {
            if (numCams == 0 && refresh)
            {
                // get all inactive cameras
                for(int i =0; i < transform.childCount; i++)
                {
                    var item = transform.GetChild(i).gameObject;
                    cameraList.Add(item);
                }
                refresh = false;
            }

            if (!initiated)
            {
                Initiate();
            }

            // If in execution, assemble cameras
            if (assembleCameras)
            {
                assembleCameras = false;
                Assemble();
            }

            if (cameraList != null)
                numCams = cameraList.Count;

            if (filterExisting)
            {
                filterExisting = false;
                FilterUnclearShots();
            }

            if (deleteChildren)
            {
                deleteChildren = false;
                cameraList = new List<GameObject>();
                while (transform.childCount > 0)
                {
                    GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
                }

            }

            if (activateAllCams)
            {
                activateAllCams = false;
                foreach( var item in cameraList)
                {
                    item.SetActive(true);
                }
            }
        }

        public void Assemble()
        {
            // in preparation, de-enable all actors
            ToggleActorsVisible(false);

            cameraList = new List<GameObject>();
            // Gist: for each location, (and each segment duration for each navigation action),

            /// FramingParameters framing_data = FramingParameters.FramingTable[FramingType.ExtremeCloseUp];
            /// cva.m_LookAt = target_go.transform;

            foreach (var loc in tileMap.Nodes)
            {
                foreach (FramingType frame in Enum.GetValues(typeof(FramingType)))
                {
                    if (frame.Equals(FramingType.None))
                    {
                        continue;
                    }
                    foreach (Orient orient in Enum.GetValues(typeof(Orient)))
                    {
                        if (orient.Equals(Orient.None))
                        {
                            continue;
                        }
                        foreach (Vangle vangle in Enum.GetValues(typeof(Vangle)))
                        {
                            if (vangle.Equals(Vangle.None))
                            {
                                continue;
                            }
                            foreach (Hangle hangle in Enum.GetValues(typeof(Hangle)))
                            {
                                if (hangle.Equals(Hangle.None))
                                {
                                    continue;
                                }
                                // Create Camera
                                var Cam = CreateCamera(loc, frame, orient, hangle, vangle);
                                cameraList.Add(Cam);
                                Cam.transform.parent = this.transform;
                                Cam.SetActive(false);
                                // Query whether there exists a clear shot
                                //if (IsValidShot(Cam.transform.position, actors[0].gameObject))
                                //{
                                //    CameraList.Add(Cam);
                                //    Cam.transform.parent = this.transform;
                                //}
                                //else
                                //{
                                //    GameObject.DestroyImmediate(Cam);
                                //}

                            }
                        }
                    }
                }
            }

            ToggleActorsVisible(true);
            Debug.Log("Assembled Cameras");
        }

        public void ToggleActorsVisible(bool whichWay)
        {
            foreach (var actor in actors)
            {
                actor.gameObject.SetActive(whichWay);
            }
        }

        public GameObject CreateCamera(TileNode loc, FramingType scale, Orient orient, Hangle hangle, Vangle vangle)
        {
            //Debug.Log(scale);
            GameObject camHost = new GameObject();
            //camHost.transform.position = loc.transform.position;
            var cva = camHost.AddComponent<CinemachineVirtualCamera>();
            var cbod = camHost.AddComponent<CinemachineCameraBody>();
            var cc = cva.AddCinemachineComponent<CinemachineComposer>();
            var cbmcp = cva.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var camattributes = camHost.AddComponent<CamAttributesStruct>();
            camattributes.Set(scale, loc.transform.gameObject.name, orient, hangle, vangle);

            // composer parameters TODO: tweak via separate gameobject component structure
            cc.m_HorizontalDamping = 10;
            cc.m_VerticalDamping = 10;
            cc.m_LookaheadTime = 0.2f;
            cc.m_DeadZoneWidth = 0.25f;
            cc.m_SoftZoneWidth = 0.5f;

            var framing_data = FramingParameters.FramingTable[scale];
            // FStop
            cbod.IndexOfFStop = CinematographyAttributes.fStops[framing_data.DefaultFStop];
            // Lens
            cbod.IndexOfLens = CinematographyAttributes.lenses[framing_data.DefaultFocalLength];

            // set at planning time.
            //cbod.FocusTransform = target_go.transform;

            // create small amount of noise
            cbmcp = cva.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cbmcp.m_NoiseProfile = CinematographyAttributes.standardNoise;
            cbmcp.m_AmplitudeGain = 0.5f;
            cbmcp.m_FrequencyGain = 1f;

            // worldDirectionOf Camera relative to location transform
            var camTransformDirection = DegToVector3(camattributes.OrientInt + camattributes.HangleInt);

            // calculate where to put camera
            var fakeTarget = CreateFakeTarget(actors[0].gameObject, loc.transform);
            var camDist = CinematographyAttributes.CalcCameraDistance(fakeTarget, scale);
            GameObject.DestroyImmediate(fakeTarget);
            cbod.FocusDistance = camDist;

            // Calculate Camera Position
            camHost.transform.position = loc.transform.position + camTransformDirection * camDist;
            var height = CinematographyAttributes.SolveForY(loc.transform.position, camHost.transform.position, 0.5f, camattributes.VangleInt);
            camHost.transform.position = new Vector3(camHost.transform.position.x, height, camHost.transform.position.z);

            // Gives starting orientation of camera. At planning time, a "lookAt" parameter is set to specific target.
            camHost.transform.rotation.SetLookRotation(loc.transform.position);

            // Set Name of camera object
            camHost.name = string.Format("{0}.{1}.{2}.{3}.{4}", loc.name, scale, camattributes.targetOrientation, camattributes.hangle, camattributes.vangle);
            return camHost;

        }

        public void FilterUnclearShots()
        {
            List<GameObject> _cameraList = new List<GameObject>();
            foreach(var cam in cameraList)
            {
                var camAttributes = cam.GetComponent<CamAttributesStruct>();
                var fakeTarget = CreateFakeTarget(actors[0].gameObject, GameObject.Find(camAttributes.targetLocation).transform);
                if (IsValidShot(cam.transform.position, fakeTarget))
                {
                    _cameraList.Add(cam);
                }
                GameObject.DestroyImmediate(fakeTarget);
                // For debugging:
                    //fakeTarget.name = string.Format("TargetFor {0}", cam.name);
            }
            cameraList = _cameraList;
            Debug.Log(string.Format("Filtered Cameras: {0}", cameraList.Count));
        }

        public GameObject CreateFakeTarget(GameObject cndtTarget, Transform loc)
        {
            var fakeTarget = GameObject.Instantiate(cndtTarget);
            fakeTarget.SetActive(true);
            fakeTarget.transform.position = new Vector3(loc.position.x, fakeTarget.transform.position.y, loc.position.z);
            return fakeTarget;
        }

        public bool IsValidShot(Vector3 camPosition, GameObject target)
        {
            target.SetActive(true);

            var targetPosition = target.transform.position;
            var camDist = Vector3.Distance(camPosition, targetPosition);
            var vectorDirection = (targetPosition - camPosition);

            // First, just check if we can hit the target at all.
            if (Physics.Raycast(camPosition, vectorDirection, camDist))
            {
                target.SetActive(false);
                return false;
            }

            // Next, check if we can hit every vertex on bounding box collider
            var vertexArray = GetColliderVertexPositions(target);
            int unseen = 0;
            for (int i = 0; i < 8; i++)
            {
                var vectorBetweenPointAndPosition = vertexArray[i] - camPosition;
                if (Physics.Raycast(camPosition, vectorBetweenPointAndPosition, Vector3.Distance(camPosition, vertexArray[i])))
                {
                    unseen++;
                    if (unseen >= 6)
                    {
                        target.SetActive(false);
                        return false;
                    }
                }
            }

            return true;
            
        }

        public static Vector3[] GetColliderVertexPositions(GameObject obj) {
            var vertices = new Vector3[8];
            var thisMatrix = obj.transform.localToWorldMatrix;
            var storedRotation = obj.transform.rotation;
            obj.transform.rotation = Quaternion.identity;
            
            var extents = obj.GetComponent<BoxCollider>().bounds.extents;
            vertices[0] = thisMatrix.MultiplyPoint3x4(extents);
            vertices[1] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, extents.y, extents.z));
            vertices[2] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, extents.y, -extents.z));
            vertices[3] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, extents.y, -extents.z));
            vertices[4] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, -extents.y, extents.z));
            vertices[5] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, -extents.y, extents.z));
            vertices[6] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, -extents.y, -extents.z));
            vertices[7] = thisMatrix.MultiplyPoint3x4(-extents);
   
            obj.transform.rotation = storedRotation;
            return vertices;
        }

    public static float MapToRange(float radians)
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

        public static Vector3 DegToVector3(float degs)
        {
            float rads = MapToRange(degs * Mathf.Deg2Rad);
            return new Vector3(Mathf.Cos(rads), 0f, Mathf.Sin(rads));
        }

        public static float FindNearPlane(Vector3 origin, Vector3 direction, float dist)
        {
            var n = 1;
            while (true)
            {
                n++;
                if (!Physics.Raycast(origin, -direction, dist - n))
                {
                    break;
                }
            }
            return n;
        }
    }
}