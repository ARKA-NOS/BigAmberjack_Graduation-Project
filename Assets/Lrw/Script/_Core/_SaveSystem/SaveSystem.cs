using System;
using System.IO;
using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Lrw.Script._Core._SaveSystem
{
    public static class SaveSystem
    {
        private const string Ext = ".save";
        private const string TempExt = ".tmp";

        public static string SaveDirectory => Application.persistentDataPath;
        
        private static string GetPath<T>(string name)
{
    return Path.Combine(SaveDirectory, $"{GetTypeKey(typeof(T))}_{name}{Ext}");
}

        private static string GetTypeKey(Type type)
        {
            if (!type.IsGenericType) return type.Name;

            string baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
            return baseName + "_" + string.Join("_", Array.ConvertAll(type.GetGenericArguments(), GetTypeKey));
        }
        
        public static bool Exists<T>(string name) => File.Exists(GetPath<T>(name));

        public static bool Save<T>(string name, T data)
        {
            string path = GetPath<T>(name);
            string temp = path + TempExt;

            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                FDebug.LogError($"Failed to save file: {path}\n{e}");
                return false;
            }
        }

        public static T Load<T>(string name, T defaultValue = default)
        {
            string path = GetPath<T>(name);
            if (!File.Exists(path)) return defaultValue;

            try
            {
                string json = File.ReadAllText(path);
                return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                FDebug.LogError($"Failed to load save file: {path}\n{e}");
                return defaultValue;
            }
        }

        public static void Delete<T>(string name)
        {
            string path = GetPath<T>(name);
            if (File.Exists(path)) File.Delete(path);
        }

        public static string[] GetFilePaths() => Directory.GetFiles(SaveDirectory, "*" + Ext);
        
    }
}