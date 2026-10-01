using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Lrw.Script._Core._SaveSystem
{
    public static class SaveSystem
    {
        public static string SaveDirectory => Application.persistentDataPath;
        private const string Ext = ".save";
        
        public static string GetPath<T>(string name)
        {
            return Path.Combine(SaveDirectory, $"[{typeof(T).FullName}]{name}{Ext}");
        }
        
        public static bool Exists(string path) => File.Exists(path);
        
        public static void Save<T>(string name, T data)
        {
            File.WriteAllText(GetPath<T>(name),JsonUtility.ToJson(data));
        }

        public static string LoadText(string path)
        {
            if(!Exists(path)) return null;
            return File.ReadAllText(path);
        }
        
        public static T Load<T>(string name, T defaultValue)
        {
            string path = GetPath<T>(name);
            string readData = LoadText(path);
            
            if(string.IsNullOrEmpty(readData)) return defaultValue;
            
            try
            {
                return JsonUtility.FromJson<T>(readData);
            }
            catch
            {
                FDebug.LogError("Failed to load save file: " + path);
            }
            
            return defaultValue;
        }

        
        public static void Delete<T>(string name)
        {
            string path = GetPath<T>(name);
            if(Exists(path)) File.Delete(path);
        }

        #region MyRegion

        public static string[] GetFilePaths()
            => Directory.GetFiles(SaveDirectory, "*" + Ext);

        #endregion

    }
}