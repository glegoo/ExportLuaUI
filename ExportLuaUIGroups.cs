/*
lua UI 脚本导出工具 by glegoo
 */
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public enum LuaNodeType
{
    COMMON,
    LIST,
    TEMPLATE
}

public class LuaNode
{
    public LuaNode(string name, Transform trans)
    {
        nodeName = name;
        transform = trans;
    }
    // 节点名
    public string nodeName;

    public Transform transform;
    // 子节点
    public List<LuaNode> children;
    // 组件列表
    // public Dictionary<Transform, List<Component>> componentMap;
    public List<Component> components = new List<Component>();
    // 是列表父节点
    public LuaNodeType type;
}

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
        LuaNode node = new LuaNode("", prefab.transform);
        InitLuaNode(node);

        WriteLuaFile(node, savePath, prefab.name);
    }

    private static void InitLuaNode(LuaNode node)
    {
        Debug.Log("初始化LuaNode: " + node.nodeName);
        Transform transform = node.transform;

        // List<Component> validComponentList = new List<Component>();
        GetAllValidTransfrom(node, node.components, transform);
        // Debug.Log("validComponentList Count: " + validComponentList.Count);
        // if (validComponentList.Count > 0)
        // {
        //     Dictionary<Transform, List<Component>> map = new Dictionary<Transform, List<Component>>();
        //     foreach (Component item in validComponentList)
        //     {
        //         Transform parent = item.transform.parent;
        //         if (!map.ContainsKey(parent))
        //         {
        //             map.Add(parent, new List<Component>());
        //         }
        //         map[parent].Add(item);
        //     }
        //     node.componentMap = map;
        // }

        if (node.children != null)
        {
            foreach (LuaNode child in node.children)
            {
                InitLuaNode(child);
            }
        }
    }

    private static void GetAllValidTransfrom(LuaNode node, List<Component> list, Transform transform)
    {
        if (node.type == LuaNodeType.LIST && transform == node.transform)
        {
            if (transform.childCount > 0)
            {
                transform = transform.GetChild(0);
            }
            else
            {
                Debug.LogError("列表节点没有子节点：" + transform.name);
                return;
            }
        }

        if (transform)
        {
            if (transform.name.Contains("Node") && transform != node.transform)
            {
                if (node.children == null)
                {
                    node.children = new List<LuaNode>();
                }
                string nodeName = transform.name.Replace("Node", "");
                Debug.Log(string.Format("{0}找到子节点{1}", node.nodeName, nodeName));
                LuaNode child = new LuaNode(nodeName, transform);
                if (transform.name.Contains("temp"))
                {
                    child.type = LuaNodeType.TEMPLATE;
                }
                node.children.Add(child);
                return;
            }

            if (transform.name.Contains("Array") && transform != node.transform)
            {
                if (node.children == null)
                {
                    node.children = new List<LuaNode>();
                }
                string nodeName = transform.name;
                Debug.Log(string.Format("{0}找到子节点{1}", node.nodeName, nodeName));
                LuaNode child = new LuaNode(nodeName, transform);
                child.type = LuaNodeType.LIST;
                node.children.Add(child);
                return;
            }

            if (transform.name == "static")
            {
                return;
            }

            Component component = null;

            if (transform.GetComponent<UILabel>() != null && transform.name.Contains("lab"))
                component = transform.GetComponent<UILabel>();
            else if (transform.GetComponent<UISprite>() != null && transform.name.Contains("spr"))
                component = transform.GetComponent<UISprite>();
            else if (transform.GetComponent<UITexture>() != null && transform.name.Contains("tex"))
                component = transform.GetComponent<UITexture>();
            else if (transform.GetComponent<BoxCollider>() != null && transform.name.Contains("btn"))
                component = transform.GetComponent<BoxCollider>();
            else if (transform.GetComponent<UIGrid>() != null && transform.name.Contains("grid"))
                component = transform.GetComponent<UIGrid>();

            // Debug.Log("检测节点： " + transform.name + " component: " + component);

            if (component != null)
            {
                list.Add(component);
                Debug.Log("检测节点： " + transform.name + " component: " + component + " list: " + list.Count);
            }

            if (transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    GetAllValidTransfrom(node, list, child);
                }
            }
        }
    }

    private static void WriteLuaFile(LuaNode node, string savePath, string name)
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
        sw.WriteLine();


        // 局部变量
        if (node.children != null)
        {
            foreach (LuaNode child in node.children)
            {
                sw.WriteLine(string.Format("local {0} = {{}}", child.nodeName));
            }
        }

        // Awake方法
        sw.WriteLine();
        sw.WriteLine("function this.Awake(obj)");
        sw.WriteLine("\tm_luaBehaviour = obj:GetComponent('LuaBehaviour')");
        sw.WriteLine("\tm_transform = obj.transform");
        WriteLuaNodeBind(sw, node, "");

        sw.WriteLine("end");

        sw.WriteLine();
        sw.WriteLine("function this.Start()");
        sw.WriteLine("\t");
        sw.WriteLine("end");
        WriteLuaNodeMethod(sw, node, "");

        //清空缓冲区
        sw.Flush();
        //关闭流
        sw.Close();
        fs.Close();
    }

    private static void WriteLuaNodeDefine(StreamWriter sw, LuaNode node, string head)
    {
        // 局部变量
        sw.WriteLine();
        string str = head;
        if (string.IsNullOrEmpty(str))
        {
            str = "local ";
        }
        sw.WriteLine(string.Format("{0}{1} = {{}}", str, node.nodeName));
    }

    private static void WriteLuaNodeBind(StreamWriter sw, LuaNode node, string parent)
    {
        sw.WriteLine();
        if (node.components.Count > 0)
        {
            if (node.type == LuaNodeType.COMMON)
            {
                sw.WriteLine(string.Format("\t{2}{0} = {{}}", node.nodeName, GetNodePath(node.transform), parent));
                sw.WriteLine(string.Format("\t{2}{0}.go = m_transform:Find('{1}').gameObject", node.nodeName, GetNodePath(node.transform), parent));
                foreach (Component component in node.components)
                {
                    // sw.WriteLine(string.Format("\tlocal {0} = m_transform:Find('{1}')", pair.Key.name, GetNodePath(pair.Key)));
                    // foreach (Component component in pair.Value)
                    // {
                    if (component is UIWidget || component is UIGrid)
                    {
                        sw.WriteLine(string.Format("\t{3}{2}.{0} = {3}{2}.go.transform:Find('{4}'):GetComponent('{1}')", component.name, component.GetType(), node.nodeName, parent, GetNodePath(component.transform, node.transform)));
                    }
                    else if (component is BoxCollider)
                    {
                        string str = parent;
                        while (str.Contains("."))
                        {
                            int index = str.IndexOf(".");
                            str = str.Remove(index, 1);
                            if (str.Contains("."))
                            {
                                str = str.Substring(0, index) + str.Substring(index, 1).ToUpper() + str.Substring(index + 1);
                            }
                        }

                        string str2 = node.nodeName;
                        if (!string.IsNullOrEmpty(parent))
                        {
                            str2 = str2.Substring(0, 1).ToUpper() + str2.Substring(1);
                        }
                        str = str + str2 + component.name.Replace("btn", "");

                        sw.WriteLine(string.Format("\t{2}{1}.{0} = {2}{1}.go.transform:Find('{3}').gameObject", component.name, node.nodeName, parent, GetNodePath(component.transform, node.transform)));
                        sw.WriteLine(string.Format("\tm_luaBehaviour:AddClick({3}{2}.{0}, this.{4}Clicked);", component.name, component.name.Replace("btn", ""), node.nodeName, parent, str));
                    }
                    // }
                }
            }
            else if (node.type == LuaNodeType.LIST)
            {
                sw.WriteLine(string.Format("\t{2}{0} = {{}}", node.nodeName, GetNodePath(node.transform), parent));
                sw.WriteLine(string.Format("\tlocal {0} = m_transform:Find('{1}')", node.nodeName, GetNodePath(node.transform)));
                sw.WriteLine(string.Format("\tfor i = 0, {0}.childCount - 1 do", node.nodeName));
                sw.WriteLine(string.Format("\t\tlocal child = {0}:GetChild(i)", node.nodeName));
                if (node.components[0].transform.parent == node.transform)
                {
                    if (node.components.Count == 1)
                    {
                        foreach (Component component in node.components)
                        {
                            if (component is UIWidget || component is UIGrid)
                            {
                                sw.WriteLine(string.Format("\t\t{2}{1}[i + 1] = child:GetComponent('{0}')", component.GetType(), node.nodeName, parent));
                            }
                            else if (component is BoxCollider)
                            {
                                sw.WriteLine(string.Format("\t\t{1}{0}[i + 1] = child.gameObject", node.nodeName, parent));
                                string str = parent;
                                while (str.Contains("."))
                                {
                                    int index = str.IndexOf(".");
                                    str = str.Remove(index, 1);
                                    if (str.Contains("."))
                                    {
                                        str = str.Substring(0, index) + str.Substring(index, 1).ToUpper() + str.Substring(index + 1);
                                    }
                                }

                                string str2 = node.nodeName;
                                if (!string.IsNullOrEmpty(parent))
                                {
                                    str2 = str2.Substring(0, 1).ToUpper() + str2.Substring(1);
                                }
                                str = str + str2 + component.name.Replace("btn", "");
                                sw.WriteLine(string.Format("\t\tm_luaBehaviour:AddClick({2}{1}[i + 1], this.{3}Clicked);", component.name.Replace("btn", ""), node.nodeName, parent, str));
                            }
                        }
                    }
                }
                else
                {
                    sw.WriteLine(string.Format("\t\t{1}{0}[i + 1] = {{}}", node.nodeName, parent));
                    foreach (Component component in node.components)
                    {
                        if (component is UIWidget || component is UIGrid)
                        {
                            sw.WriteLine(string.Format("\t\t{3}{2}[i + 1].{0} = child:Find('{0}'):GetComponent('{1}')", GetNodePath(component.transform, node.transform.GetChild(0)), component.GetType(), node.nodeName, parent));
                        }
                        else if (component is BoxCollider)
                        {
                            sw.WriteLine(string.Format("\t\t{2}{1}[i + 1].{0} = child:Find('{0}').gameObject", GetNodePath(component.transform, node.transform.GetChild(0)), node.nodeName, parent));
                            string str = parent;
                            while (str.Contains("."))
                            {
                                int index = str.IndexOf(".");
                                str = str.Remove(index, 1);
                                if (str.Contains("."))
                                {
                                    str = str.Substring(0, index) + str.Substring(index, 1).ToUpper() + str.Substring(index + 1);
                                }
                            }

                            string str2 = node.nodeName;
                            if (!string.IsNullOrEmpty(parent))
                            {
                                str2 = str2.Substring(0, 1).ToUpper() + str2.Substring(1);
                            }
                            str = str + str2 + component.name.Replace("btn", "");
                            sw.WriteLine(string.Format("\t\tm_luaBehaviour:AddClick({3}{2}[i + 1].{0}, this.{4}Clicked);", component.name, component.name.Replace("btn", ""), node.nodeName, parent, str));
                        }
                    }
                }
                sw.WriteLine("\tend");
            }
            else if (node.type == LuaNodeType.TEMPLATE)
            {
                sw.WriteLine(string.Format("\t{2}{1} = m_transform:Find('{0}').gameObject", GetNodePath(node.transform), node.nodeName, parent));
                return;
            }
        }

        if (node.children != null)
        {
            foreach (LuaNode child in node.children)
            {
                string np = parent + node.nodeName;
                if (!string.IsNullOrEmpty(np)) np = np + '.';
                WriteLuaNodeBind(sw, child, np);
            }
        }
    }

    private static void WriteLuaNodeMethod(StreamWriter sw, LuaNode node, string parent)
    {
        string str = node.nodeName;
        // 如果是第二个词缀首字母大写
        if (!string.IsNullOrEmpty(parent))
        {
            str = str.Substring(0, 1).ToUpper() + str.Substring(1);
        }

        if (node.type == LuaNodeType.COMMON || node.type == LuaNodeType.LIST)
        {
            if (node.components != null)
            {
                foreach (Component component in node.components)
                {
                    if (component is BoxCollider)
                    {
                        sw.WriteLine();
                        sw.WriteLine("function this.{2}{1}{0}Clicked()", component.name.Replace("btn", ""), str, parent);
                        sw.WriteLine("\tlog('{2}{1}{0}Clicked')", component.name.Replace("btn", ""), str, parent);
                        sw.WriteLine("\t");
                        sw.WriteLine("end");
                    }
                }
            }
        }
        else if (node.type == LuaNodeType.TEMPLATE)
        {
            sw.WriteLine();
            sw.WriteLine("function this.{0}NodeUpdate(node, data)", node.nodeName);
            foreach (Component component in node.components)
            {
                if (component is UIWidget || component is UIGrid)
                {
                    sw.WriteLine(string.Format("\tlocal {0} = node:Find('{1}'):GetComponent('{2}')", component.name, GetNodePath(component.transform, node.transform), component.GetType()));
                }
            }
            sw.WriteLine("\t");
            sw.WriteLine("end");
        }

        if (node.children != null)
        {
            foreach (LuaNode child in node.children)
            {
                WriteLuaNodeMethod(sw, child, parent + str);
            }
        }
    }

    private static string GetNodePath(Transform trans, Transform root = null)
    {
        if (root != null && !trans.IsChildOf(root))
        {
            Debug.LogError("获取节点路径非法！ " + root.name + "/" + trans.name);
        }
        if (root == null)
        {
            root = trans.root;
        }
        string result = trans.name;
        try
        {
            while (trans.parent != root)
            {
                result = trans.parent.name + "/" + result;
                trans = trans.parent;
            }
        }
        catch
        {
            Debug.LogError(root.name + "/" + trans.name);
        }
        return result;
    }
}
