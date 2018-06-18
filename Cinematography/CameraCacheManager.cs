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
