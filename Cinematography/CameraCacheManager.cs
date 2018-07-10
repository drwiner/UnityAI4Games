using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BoltFreezer.Utilities;
using Cinematography;
using System.IO;
using BoltFreezer.FileIO;
using BoltFreezer.Camera;
using GraphNamespace;

namespace CameraNamespace
{
    public static class CameraCacheManager 
    {
        public static List<CamSchema> CachedCams;

        public static string Path = @"D:\Documents\Frostbow\Cached\Cams\";

        public static void CacheCam(CamSchema cas)
        {
            CachedCams.Add(cas);
        }



        public static void CacheCam(GameObject cam)
        {
            CacheCam(cam.GetComponent<CamAttributesStruct>().AsSchema());
        }

        public static bool CacheCams(List<GameObject> cams, string fileName)
        {
            CachedCams = new List<CamSchema>();
            foreach (var cam in cams)
            {
                CacheCam(cam);
            }
            return SerializeCams(fileName);
        }

        

        public static bool DecacheCams(string fileName)
        {
            try
            {
                CachedCams = BinarySerializer.DeSerializeObject<List<CamSchema>>(Path + fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Dictionary<Edge, Dictionary<double, List<CamSchema>>> CalculateNavCamDictFromCache()
        {
            var navCamDict = new Dictionary<Edge, Dictionary<double, List<CamSchema>>>();
            var LocationGraph = GameObject.FindGameObjectWithTag("Locations").GetComponent<TileGraph>();
            var tileNodeNames = LocationGraph.Nodes.Select(node => node.name).ToList();

            foreach(var eachEdge in LocationGraph.Edges)
            {
                navCamDict[eachEdge] = new Dictionary<double, List<CamSchema>>();
                navCamDict[eachEdge][0] = new List<CamSchema>();
                navCamDict[eachEdge][1] = new List<CamSchema>();
            }

            foreach (var camSchema in CachedCams)
            {
                var nameOfLocation = camSchema.targetLocation;
                
                // if it's just the name of a location, ignore
                if (tileNodeNames.Contains(nameOfLocation))
                {
                    // Then this is good for the 0th position of all edges that originate here or end here.
                    foreach(var thisEdge in LocationGraph.Edges)
                    {
                        if (thisEdge.S.name.Equals(nameOfLocation))
                        {
                            // If it's the start of an edge:
                            navCamDict[thisEdge][0].Add(camSchema);
                        }
                        else if (thisEdge.T.name.Equals(nameOfLocation))
                        {
                            navCamDict[thisEdge][1].Add(camSchema);
                        }
                    }
                    continue;
                }

                var edgeName = nameOfLocation.Split('_')[0];
                String[] substrings = edgeName.Split('-');
                var xNode = substrings[0];
                var xPos = GameObject.Find(xNode).transform.position;
                var yNode = substrings[1];
                var yPos = GameObject.Find(yNode).transform.position;
                var intermediateLoc = GameObject.Find(nameOfLocation).transform.position;
                var percentIntoEdge = Vector3.Distance(xPos, intermediateLoc) / Vector3.Distance(xPos, yPos);
                var edge = LocationGraph.FindRelevantEdge(xNode, yNode);
                if (!navCamDict[edge].ContainsKey(percentIntoEdge))
                {
                    navCamDict[edge][percentIntoEdge] = new List<CamSchema>();
                }
                navCamDict[edge][percentIntoEdge].Add(camSchema);
            }

            return navCamDict;
        }

        public static bool SerializeCams(string fileName)
        {
            try
            {
                Directory.CreateDirectory(Path);
               // BinarySerializer.SerializeObject<Tuple<Trans, CamSchema>>(Path + fileName, CachedCams[3545]);
                BinarySerializer.SerializeObject<List<CamSchema>>(Path + fileName, CachedCams);
                return true;
            }
            catch
            { 
                return false;
            }
        }

    }
}
