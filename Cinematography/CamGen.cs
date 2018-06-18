using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine.Timeline;
using Cinemachine;
using Cinematography;
using PlanningNamespace;
using GraphNamespace;
using System;
using BoltFreezer.Camera.CameraEnums;
using BoltFreezer.Utilities;
using BoltFreezer.Camera;
using BoltFreezer.PlanTools;
using BoltFreezer.Interfaces;

namespace CameraNamespace {

    [ExecuteInEditMode]
    public class CamGen : MonoBehaviour
    {

        // Needs access to problem and actions
        public Dictionary<string, List<Tuple<Trans, CamSchema>>> locationCamDictionary;
        public Dictionary<Edge, Dictionary<double, List<CamSchema>>> navCamDictionary;
        private UnityProblemCompiler problemStates;
        public List<UnityActionOperator> actions;
        private List<GameObject> actors;
        private TileGraph tileMap;

        public bool initiated = false;
        public bool assembleCameras = false;
        public bool recheckCamerasForVisibility = false;
        public int numCams;
        public bool refresh;
        public bool deleteChildren = false;
        public bool activateAllCams = false;
        public string cacheFileName = "raceTest(1)";
        public bool cacheCams = false;
        public bool decacheCams = false;

        private List<GameObject> cameraList;

        public List<GameObject> CameraList
        {
            get { return cameraList; }
        }

        // Use this for initialization
        void Start()
        {
            //Initiate();
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
        void Update()
        {
            if (numCams == 0 && refresh)
            {
                // get all inactive cameras
                for (int i = 0; i < transform.childCount; i++)
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

            if (recheckCamerasForVisibility)
            {
                recheckCamerasForVisibility = false;
                cameraList = FilterUnclearShots(cameraList);
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
                foreach (var item in cameraList)
                {
                    item.SetActive(true);
                }
            }

            if (cacheCams)
            {
                cacheCams = false;
                var result = CameraCacheManager.CacheCams(cameraList, cacheFileName);
                if (result)
                {
                    Debug.Log("Cam Cache success");
                }
                else
                {
                    Debug.Log("Cam Cache Failure");
                }
            }

            if (decacheCams)
            {
                decacheCams = false;
                var result = CameraCacheManager.DecacheCams(cacheFileName);
                if (result)
                {
                    Debug.Log("Cam Cache success");
                }
                else
                {
                    Debug.Log("Cam Cache Failure");
                }
                numCams = CameraCacheManager.CachedCams.Count;
            }


        }

        public void Assemble()
        {
            // in preparation, de-enable all actors
            //ToggleActorsVisible(false);

            cameraList = new List<GameObject>();

            /// FramingParameters framing_data = FramingParameters.FramingTable[FramingType.ExtremeCloseUp];
            /// cva.m_LookAt = target_go.transform;

            foreach (var loc in tileMap.Nodes)
            {
                var newList = GenerateCamsPerLocation(loc.gameObject);
                foreach (var item in newList)
                {

                    cameraList.Add(item);
                }
            }
            Debug.Log("Generated Location-based Cams");

            GenerateLocationDictionary();
            Debug.Log("Generated Location-based Dictionary of Cams");

            GenerateNavDictionary();
            Debug.Log("Generated Navigation-based Dictionary of Cams");

            //cameraList = FilterUnclearShots(cameraList);

            //ToggleActorsVisible(true);
            Debug.Log("Assembled Cameras");
        }

        public List<GameObject> GenerateCamsPerLocation(GameObject location)
        {
            List<GameObject> camsPerLocation = new List<GameObject>();
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
                            var Cam = CreateCamera(location, frame, orient, hangle, vangle);
                            if (Cam == null)
                            {
                                continue;
                            }
                            camsPerLocation.Add(Cam);
                            Cam.transform.parent = this.transform;
                            Cam.SetActive(false);
                        }
                    }
                }
            }
            return camsPerLocation;
        }

        public void ToggleActorsVisible(bool whichWay)
        {
            foreach (var actor in actors)
            {
                actor.gameObject.SetActive(whichWay);
            }
        }

        public GameObject CreateCamera(GameObject loc, FramingType scale, Orient orient, Hangle hangle, Vangle vangle)
        {
            //Debug.Log(scale);
            GameObject camHost = new GameObject();
            //camHost.transform.position = loc.transform.position;
            var cva = camHost.AddComponent<CinemachineVirtualCamera>();
            var cbod = camHost.AddComponent<CinemachineCameraBody>();
            var cc = cva.AddCinemachineComponent<CinemachineComposer>();
            var cbmcp = cva.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var camattributes = camHost.AddComponent<CamAttributesStruct>();
            camattributes.Set(scale, loc.name, orient, hangle, vangle);

            // composer parameters TODO: tweak via separate gameobject component structure
            cc.m_HorizontalDamping = 10;
            cc.m_VerticalDamping = 10;
            cc.m_LookaheadTime = 0.2f;
            cc.m_DeadZoneWidth = 0.25f;
            cc.m_DeadZoneHeight = 0.25f;
            cc.m_SoftZoneWidth = 0.5f;
            cc.m_SoftZoneHeight = 0.5f;

            FramingParameters framing_data;
            try
            {
                framing_data = FramingParameters.FramingTable[scale];
            }
            catch
            {
                Debug.Log("here");
                throw new System.Exception();
            }
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

            cbod.FocusDistance = camDist;

            // Calculate Camera Position
            camHost.transform.position = loc.transform.position + camTransformDirection * camDist;
            camHost.transform.position = new Vector3(camHost.transform.position.x, 0.5f, camHost.transform.position.z);
            var height = CinematographyAttributes.SolveForY(loc.transform.position, camHost.transform.position, 0.5f, camattributes.VangleInt);
            camHost.transform.position = new Vector3(camHost.transform.position.x, height, camHost.transform.position.z);

            // Gives starting orientation of camera. At planning time, a "lookAt" parameter is set to specific target.
            camHost.transform.rotation.SetLookRotation(loc.transform.position);

            if (!IsValidShot(camHost.transform.position, fakeTarget))
            {
                GameObject.DestroyImmediate(camHost);
                GameObject.DestroyImmediate(fakeTarget);
                return null;
            }
            GameObject.DestroyImmediate(fakeTarget);

            // Set Name of camera object
            camHost.name = string.Format("{0}.{1}.{2}.{3}.{4}", loc.name, scale, camattributes.targetOrientation, camattributes.hangle, camattributes.vangle);
            return camHost;

        }

        public List<GameObject> FilterUnclearShots(List<GameObject> cams)
        {
            ToggleActorsVisible(false);
            List<GameObject> _cameraList = new List<GameObject>();
            foreach (var cam in cams)
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
            //cameraList = _cameraList;
            ToggleActorsVisible(true);
            Debug.Log(string.Format("Filtered Cameras: {0}", cameraList.Count));

            return _cameraList;
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

        public static Vector3[] GetColliderVertexPositions(GameObject obj)
        {
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

        public void GenerateLocationDictionary()
        {
            // Dictionary mapping locations to a list of cameras
            locationCamDictionary = new Dictionary<string, List<Tuple<Trans, CamSchema>>>();
            foreach (var node in tileMap.Nodes)
            {
                var loc = node.name;
                locationCamDictionary[loc] = new List<Tuple<Trans, CamSchema>>();
                foreach (var cam in CameraList)
                {
                    var camloc = cam.GetComponent<CamAttributesStruct>().targetLocation;
                    if (camloc.Equals(loc))
                    {
                        var newEntry = new Tuple<Trans, CamSchema>(new Trans(cam.transform), cam.GetComponent<CamAttributesStruct>().AsSchema());
                        locationCamDictionary[loc].Add(newEntry);
                    }
                }
            }
        }

        public void GenerateNavDictionary()
        {
            // a dictionary mapping edges to another dictionary mapping intermediate locations to a list of cameras
            navCamDictionary = new Dictionary<Edge, Dictionary<double, List<CamSchema>>>();

            var genericActor = actors[0];
            var generalActorLength = Math.Max(genericActor.GetComponent<BoxCollider>().size.x, genericActor.GetComponent<BoxCollider>().size.z);
            //CreateFakeTarget(GameObject cndtTarget, Transform loc)
            foreach (var edge in tileMap.Edges)
            {
                var intermediateLocationCamDictionary = new Dictionary<double, List<CamSchema>>();
                var edgeDistance = Vector3.Distance(edge.S.transform.position, edge.T.transform.position);
                // int numPartitions = (int)(edgeDistance / generalActorLength);
                int numPartitions = 4;
                var unitLength = edgeDistance / numPartitions;
                var direction = (edge.T.transform.position - edge.S.transform.position).normalized;
                for (int i = 1; i < numPartitions; i++)
                {

                    var newPos = edge.S.transform.position + unitLength * i * direction;
                    newPos = new Vector3(newPos.x, edge.S.transform.position.y, newPos.z);
                    var newIntermediateLocation = new GameObject(edge.S.name + "-" + edge.T.name + "_" + i.ToString());
                    newIntermediateLocation.transform.position = newPos;

                    var percent = (double)(i / numPartitions);
                    intermediateLocationCamDictionary[percent] = new List<CamSchema>();

                    // this will only pass if can reach target.
                    var camList = GenerateCamsPerLocation(newIntermediateLocation);
                    foreach (var item in camList)
                    {
                        cameraList.Add(item);
                        var tupleEntry = item.GetComponent<CamAttributesStruct>().AsSchema();
                        intermediateLocationCamDictionary[percent].Add(tupleEntry);
                    }

                }

                navCamDictionary[edge] = intermediateLocationCamDictionary;
            }
        }

        public List<CamSchema> GetCamsForEdgeAndPercent(Edge edge, double percent)
        {
            var intermediateLocationCamDictionary = navCamDictionary[edge];
            double closestKey = 0;
            double bestDistance = 1000;
            foreach (var key in intermediateLocationCamDictionary.Keys)
            {
                if ((percent - key) < bestDistance)
                {
                    bestDistance = percent - key;
                    closestKey = key;
                }
                else if (key > percent)
                {
                    // we've passed it already
                    break;
                }
            }
            return intermediateLocationCamDictionary[closestKey];
        }

    }
}