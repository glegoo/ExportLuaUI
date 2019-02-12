/*
lua UI 脚本导出工具 by glegoo
 */
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ExportLuaUIGroups : Editor
{
    delegate bool Filter(Component component);

    private static string[] ignoreList = new string[] { "static", "item" };

    [MenuItem("Assets/Export Lua UI Script(Group)")]
    private static void ExportSelect()
    {
        Debug.Log("Export Group UI");
        if (Selection.objects != null)
        {
            UnityEngine.Object[] prefabs = Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel);
            if (prefabs != null)
            {
                string outPath = Application.dataPath + "/../../../../../TulongQPProject/Assets/Tlqp/Lua/View";
                string savePath = EditorUtility.SaveFolderPanel("选择导出位置", Path.GetFullPath(outPath), "");
                Debug.Log("Save Path: " + savePath);
                if (!string.IsNullOrEmpty(savePath))
                {
                    foreach (UnityEngine.Object obj in prefabs)
                    {
                        ExportLua(obj as GameObject, savePath);
                    }
                }
            }
        }
    }

    private static void ExportLua(GameObject prefab, string savePath)
    {
        Dictionary<string, Dictionary<Transform, List<Component>>> map = null;
        foreach (Transform child in prefab.transform)
        {
            Debug.Log("Child Name: " + child.name);
            if (child.name.Contains("Group"))
            {
                string groupName = child.name.Replace("Group", "");
                Debug.Log("Group: " + groupName);

                Dictionary<Transform, List<Component>> subMap = GetComponentMap(child.gameObject, groupName);
                if (subMap != null)
                {
                    if (map == null)
                    {
                        map = new Dictionary<string, Dictionary<Transform, List<Component>>>();
                    }
                    map.Add(groupName, GetComponentMap(child.gameObject, groupName));
                }
            }
        }

        if (map != null)
        {
            WriteLuaFile(map, savePath, prefab.name);
        }
    }

    private static Dictionary<Transform, List<Component>> GetComponentMap(GameObject prefab, string groupName)
    {

        Component[] components = GetFiltered<Component>(prefab, (component) =>
        {
            if (component.transform.parent == null)
                return false;

            // 如果是忽略节点的子节点 略过
            Transform trans = component.transform;
            while (trans.parent != null)
            {
                trans = trans.parent;
                if (Array.IndexOf(ignoreList, trans.name) > -1)
                {
                    return false;
                }
            }

            if (component is UILabel && component.name.Contains("lab"))
                return true;

            if (component is UISprite && component.name.Contains("spr"))
                return true;

            if (component is BoxCollider && component.name.Contains("btn"))
                return true;

            if (component is UIGrid && component.name.Contains("grid"))
                return true;

            return false;
        });

        if (components != null)
        {
            Dictionary<Transform, List<Component>> map = new Dictionary<Transform, List<Component>>();
            foreach (Component item in components)
            {
                Transform parent = item.transform.parent;
                if (!map.ContainsKey(parent))
                {
                    map.Add(parent, new List<Component>());
                }
                map[parent].Add(item);
            }
            return map;
            // WriteLuaFile(map, savePath, prefab.name);
        }
        return null;
    }

    private static void WriteLuaFile(Dictionary<string, Dictionary<Transform, List<Component>>> map, string savePath, string name)
    {
        savePath = string.Format("{0}/{1}.lua", savePath, name);
        Debug.Log("SavePath: " + savePath);
        if (File.Exists(savePath))
        {
            int option = EditorUtility.DisplayDialogComplex("文件已存在",
                savePath + "已存在, 是否替换?",
                "替换",
                "取消",
                "保留两者");

            switch (option)
            {
                case 0:
                    File.Delete(savePath);
                    break;

                case 1:
                    return;

                case 2:
                    savePath = savePath.Replace(".lua", "_auto.lua");
                    if (File.Exists(savePath))
                        File.Delete(savePath);
                    break;

                default:
                    Debug.LogError("Unrecognized option.");
                    break;
            }
        }

        FileStream fs = new FileStream(savePath, FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);
        //开始写入
        sw.WriteLine("--[[Created by lua UI script exporter.]]");
        sw.WriteLine(string.Format("{0} = {{}}", name));

        sw.WriteLine();
        sw.WriteLine(string.Format("local this = {0}", name));
        sw.WriteLine();
        sw.WriteLine("local m_transform");
        sw.WriteLine("local m_luaBehaviour");


        // 局部变量
        sw.WriteLine();
        foreach (KeyValuePair<string, Dictionary<Transform, List<Component>>> item in map)
        {
            sw.WriteLine(string.Format("local {0} = {{", item.Key));
            foreach (KeyValuePair<Transform, List<Component>> subMap in item.Value)
            {
                foreach (Component component in subMap.Value)
                {
                    sw.WriteLine(string.Format("\t{0} = nil,", component.name));
                }
            }
            sw.WriteLine(string.Format("}}"));
            sw.WriteLine();
        }

        // Awake方法
        sw.WriteLine();
        sw.WriteLine("function this.Awake(obj)");
        sw.WriteLine("\tm_luaBehaviour = obj:GetComponent('LuaBehaviour')");
        sw.WriteLine("\tm_transform = obj.transform");
        foreach (KeyValuePair<string, Dictionary<Transform, List<Component>>> item in map)
        {
            foreach (KeyValuePair<Transform, List<Component>> pair in item.Value)
            {
                sw.WriteLine(string.Format("\tlocal {0} = m_transform:Find('{1}')", pair.Key.name, GetNodePath(pair.Key)));
                foreach (Component component in pair.Value)
                {
                    if (component is UIWidget || component is UIGrid)
                    {
                        sw.WriteLine(string.Format("\t{3}.{0} = {1}:Find('{0}'):GetComponent('{2}')", component.name, pair.Key.name, component.GetType(), item.Key));
                    }
                    else if (component is BoxCollider)
                    {
                        sw.WriteLine(string.Format("\t{2}.{0} = {1}:Find('{0}').gameObject", component.name, pair.Key.name, item.Key));
                        sw.WriteLine(string.Format("\tm_luaBehaviour:AddClick({2}.{0}, this.{2}{1}Clicked);", component.name, component.name.Replace("btn", ""), item.Key));
                    }
                }
            }
        }
        sw.WriteLine("end");

        sw.WriteLine();
        sw.WriteLine("function this.Start()");
        sw.WriteLine("\t");
        sw.WriteLine("end");

        foreach (KeyValuePair<string, Dictionary<Transform, List<Component>>> item in map)
        {
            foreach (KeyValuePair<Transform, List<Component>> pair in item.Value)
            {
                foreach (Component component in pair.Value)
                {
                    if (component is BoxCollider)
                    {
                        sw.WriteLine();
                        sw.WriteLine("function this.{1}{0}Clicked()", component.name.Replace("btn", ""), item.Key);
                        sw.WriteLine("\t");
                        sw.WriteLine("end");
                    }
                }
            }
        }

        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();
    }

    private static T[] GetFiltered<T>(GameObject prefab, Filter filter) where T : Component
    {
        T[] components = prefab.GetComponentsInChildren<T>(true);
        if (components == null)
            return null;

        List<T> result = new List<T>(components);
        result = result.FindAll((component) =>
        {
            return filter(component);
        });
        return result.ToArray();
    }

    private static string GetNodePath(Transform trans)
    {
        string result = trans.name;
        while (trans.parent.parent != null)
        {
            result = trans.parent.name + "/" + result;
            trans = trans.parent;
        }
        return result;
    }
}