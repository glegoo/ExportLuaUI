/*
注意事项:
节点按照驼峰命名,否则无法达到最佳效果. 如: labText, sprIcon, btnCloseWindow;
所有lab开头(大小写敏感, 下同)的UILabel, spr开头的UISprite, btn开头包含碰撞的节点为自动生成代码对象;
by wcheng
 */
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ExportLuaUI : Editor
{
    delegate bool Filter(Component component);

    [MenuItem("Assets/Export Lua UI Script")]
    static void ExportSelect()
    {
        if (Selection.objects != null)
        {
            Object[] prefabs = Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel);
            if (prefabs != null)
            {

                string outPath = Application.dataPath + "/../../../../../TulongQPProject/Assets/Tlqp/Lua/View";
                string savePath = EditorUtility.SaveFolderPanel("选择导出位置", Path.GetFullPath(outPath), "");
                if (!string.IsNullOrEmpty(savePath))
                {
                    foreach (Object obj in prefabs)
                    {
                        ExportLua(obj as GameObject, savePath);
                    }
                }
            }
        }
    }

    static void ExportLua(GameObject prefab, string savePath)
    {

        Component[] components = GetFiltered<Component>(prefab, (component) =>
        {
            if (component.transform.parent == null || component.transform.parent.name == "static")
                return false;

            if (component is UILabel && component.name.Contains("lab"))
                return true;

            if (component is UISprite && component.name.Contains("spr"))
                return true;

            if (component is BoxCollider && component.name.Contains("btn"))
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
            WriteLuaFile(map, savePath, prefab.name);
        }
    }

    static void WriteLuaFile(Dictionary<Transform, List<Component>> map, string savePath, string name)
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
        sw.WriteLine(string.Format("local this = {0}", name));
        sw.WriteLine();
        sw.WriteLine("local m_transform");
        sw.WriteLine("local m_luaBehaviour");

        // 局部变量
        sw.WriteLine();
        foreach (KeyValuePair<Transform, List<Component>> pair in map)
        {
            foreach (Component component in pair.Value)
            {
                sw.WriteLine(string.Format("local m_{0}", component.name));
            }
        }

        // Awake方法
        sw.WriteLine();
        sw.WriteLine("function this.Awake(obj)");
        sw.WriteLine("\tm_luaBehaviour = obj:GetComponent('LuaBehaviour')");
        sw.WriteLine("\tm_transform = obj.transform");
        foreach (KeyValuePair<Transform, List<Component>> pair in map)
        {
            sw.WriteLine("\t");
            sw.WriteLine(string.Format("\tlocal {0} = m_transform:Find('{1}')", pair.Key.name, GetNodePath(pair.Key)));
            foreach (Component component in pair.Value)
            {
                if (component is UIWidget)
                {
                    sw.WriteLine(string.Format("\tm_{0} = {1}:Find('{0}'):GetComponent('{2}')", component.name, pair.Key.name, component.GetType()));
                }
                else if (component is BoxCollider)
                {
                    sw.WriteLine(string.Format("\tm_{0} = {1}:Find('{0}').gameObject", component.name, pair.Key.name));
                    sw.WriteLine(string.Format("\tm_luaBehaviour:AddClick({0}, this.On{1}Clicked);", "m_" + component.name, component.name.Replace("btn", "")));
                }
            }
        }
        sw.WriteLine("end");

        sw.WriteLine();
        sw.WriteLine("function this.Start()");
        sw.WriteLine("\t");
        sw.WriteLine("end");

        foreach (KeyValuePair<Transform, List<Component>> pair in map)
        {
            foreach (Component component in pair.Value)
            {
                if (component is BoxCollider)
                {
                    sw.WriteLine();
                    sw.WriteLine("function this.On{0}Clicked()", component.name.Replace("btn", ""));
                    sw.WriteLine("\t");
                    sw.WriteLine("end");
                }
            }
        }

        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();
    }

    static T[] GetFiltered<T>(GameObject prefab, Filter filter) where T : Component
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

    static string GetNodePath(Transform trans)
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