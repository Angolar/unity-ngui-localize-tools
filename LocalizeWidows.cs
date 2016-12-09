using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class LocalizeInfoData
{
    public string CheckIsHaveValue(string value)
    {
        if (values == null || values.Count <1) return "";
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] == value) return values[0];
        }
        return "";
    }

    public List<string> Strvalues
    {
        get { return values; }
    }

    public LocalizeInfoData(List<string> localValues )
    {
        values = localValues;
    }
    private List<string> values; 
}


public class LocalizePathItem
{
    public LocalizePathItem(string targetPath)
    {
        path = targetPath;
    }

    private string path;
    private bool isInSubFolder;
    public bool IsDelete
    {
        get { return string.IsNullOrEmpty(path); }
    }


    /// <summary>
    /// 收集子对象内的对象
    /// </summary>
    /// <returns></returns>
    public string[] GetCollectObjects()
    {
        if (string.IsNullOrEmpty(path)) return null;
        var collects = Directory.GetFiles(path, "*.prefab", isInSubFolder ? SearchOption.AllDirectories :SearchOption.TopDirectoryOnly);

        return collects;
    }



    public void Show()
    {
        EditorGUILayout.LabelField("path: " +path);
        isInSubFolder = EditorGUILayout.Toggle("exclude son folder ：" ,isInSubFolder);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("modify" ))
        {
            string newPath =path;
            newPath = EditorUtility.OpenFolderPanel("change folder path", newPath, path);
 
           if (!string.IsNullOrEmpty(newPath))
            {
                path = newPath;
            }
        }
        if (GUILayout.Button("-"))
        {
            path = "";
        }
        EditorGUILayout.EndHorizontal();
    }
}

public class LocalizeWidows : EditorWindow
{
    /// <summary>
    /// CSV文件名
    /// </summary>
    private const string localizefilePos = @"Assets\Resources\Localization.csv";
    private const string curSelectLanguateSaveKey = "LocalizeLanguage";
    private const string selectLanguateTitle = "Select Language";


    private const string keyStartKey = "Test_{0}";
    private int curIndex;

    /// <summary>
    /// 最近设置的语言
    /// </summary>
    private string Curlanguage;

    private int languageSelectIndex = 0;
    private string[] curChoiceLanguages;

    private List<LocalizeInfoData> curStoreInfoDatas = new List<LocalizeInfoData>();
    private List<LocalizePathItem> curPaths = new List<LocalizePathItem>();

    private List<string> curHasBuildPrefabs = new List<string>();

    private CSVReader curCsvReader;


    /// <summary>
    /// 是否生成了新的KEY
    /// </summary>
    private bool isGenNewKey = false;


    [MenuItem("Custom/localize/ShowlocalizeWindow")]
    private static void ShowCustomWindow()
    {
        var windowsItem = EditorWindow.GetWindow<LocalizeWidows>();
        if (windowsItem != null)
        {
            windowsItem.InitWindow();
            windowsItem.LoadCSVFile();
        }
    }

    private void InitWindow()
    {
        curStoreInfoDatas.Clear();
        curHasBuildPrefabs.Clear();
 
        Curlanguage = EditorPrefs.GetString(curSelectLanguateSaveKey);
    }

    private void LoadCSVFile()
    {
        curCsvReader = new CSVReader();
        curCsvReader.Load(localizefilePos);

        _InitLanguage();
        _InitCSVDatas();
    }

    private void _InitLanguage()
    {
        if (curCsvReader.GetRowCount() > 1)
        {
            List<string> firstRow = new List<string>(curCsvReader.CurRow);

            _RecordItem(curCsvReader.CurRow);

            //移掉"key"
            firstRow.RemoveAt(0);
            curChoiceLanguages = firstRow.ToArray();
        }
    }

    private void _InitCSVDatas()
    {
        if (curCsvReader == null || curChoiceLanguages.Length == 0) return;

        curCsvReader.Row = 1;
        curIndex = 0;

        if (string.IsNullOrEmpty(Curlanguage))
        {
            Curlanguage = curChoiceLanguages[0];
        }

        languageSelectIndex = Array.FindIndex(curChoiceLanguages, m => m == Curlanguage);

        while (curCsvReader.MoveNext())
        {
            curIndex++;
            var stringItem = curCsvReader.CurRow;
            if (languageSelectIndex >= stringItem.Count)
            {
                Debug.LogError(string.Format(" read row {0} has error ,the language index is{1}" +
                                             " ,but the string length is{2}",
                    curCsvReader.Row, languageSelectIndex, stringItem.Count));
                continue;
            }

            _RecordItem(stringItem);
        }
    }

    private void _RecordItem(List<string> stringItem)
    {
        LocalizeInfoData localizeInfo = new LocalizeInfoData(stringItem);
        curStoreInfoDatas.Add(localizeInfo);
    }


    private void OnGUI()
    {
        DrawUI();
    }

    private void DrawUI()
    {
        EditorGUILayout.BeginVertical();
        _DrawLanguage();

        _DrawPaths();
        _DrawSelectTransform();

        EditorGUILayout.EndVertical();
    }

    private void _DrawPaths()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Select folder Path： ");

        for (int i = 0; i < curPaths.Count; i++)
        {
            if (curPaths[i].IsDelete)
            {
                curPaths.RemoveAt(i);
                if (curPaths.Count == 0) break;

                i--;
                continue;
            }
            curPaths[i].Show();
        }
        EditorGUILayout.EndVertical();
    }

    private void _DrawLanguage()
    {
        string[] languages = curChoiceLanguages;
        if (languages.Length > 0)
        {
            languageSelectIndex = EditorGUILayout.Popup(selectLanguateTitle, languageSelectIndex, languages);
            Curlanguage = languages[languageSelectIndex];
            EditorPrefs.SetString(curSelectLanguateSaveKey, Curlanguage);
        }
    }

 
    private void _DrawSelectTransform()
    {
        EditorGUILayout.BeginVertical();

        _DrawSpace(40);
        if (GUILayout.Button("add path"))
        {
            string folder = "Assets";
            folder = EditorUtility.OpenFolderPanel("add net folder path", folder, folder);
            _AddFolderItem(folder);
        }
        if (curPaths.Count == 0) return;

        _DrawSpace(60);


        if (GUILayout.Button("Confirm change prefab and CSV"))
        {
            isGenNewKey = false;

            //收集所有的路径
            List<string> allPrefabPaths = new List<string>();

            for (int i = 0; i < curPaths.Count; i++)
            {
                var curCollectItems = curPaths[i].GetCollectObjects();
                if (curCollectItems == null) continue;
                allPrefabPaths.AddRange(curCollectItems);
            }

            //搜集Prefab
            List<GameObject> collectPrefabs = _CollectPrefabs(allPrefabPaths);
            if (collectPrefabs == null || collectPrefabs.Count == 0)
            {
                Debug.LogError("the prefabs in this path not changed return out");
                return;
            }

            foreach (var go in collectPrefabs)
            {
                if (go == null) continue;
                _LocalizePrefab(go);
            }

            if (isGenNewKey)
            {
                Debug.LogError("generata new key");
                _SaveCsv();
            }
            else
            {
                Debug.LogError("has not generata newkey");
            }
            Debug.LogError("searth prefab finished");
            AssetDatabase.Refresh();

        }
        EditorGUILayout.EndVertical();
    }

    private void _DrawSpace( int space)
    {
        GUILayout.Space(space);
        GUILayout.Label("----------------------");
    }

    private void _AddFolderItem(string folder)
    {
        if (string.IsNullOrEmpty(folder) || folder == "Assets") return;
        LocalizePathItem pathItem = new LocalizePathItem(folder);
        curPaths.Add(pathItem);
    }

    private int Progress;
    private List<GameObject> _CollectPrefabs(List<string> paths)
    {
        string splitTitle = string.Format("{0}{1}", Application.dataPath, "/");
        Progress = 0;

        List<GameObject> collectObjects = new List<GameObject>();
        foreach (var path in paths)
        {
            Progress++;
            
            //过滤掉已经修改过的prefab
            if(curHasBuildPrefabs.Contains(path))continue;
            curHasBuildPrefabs.Add(path);

            EditorUtility.DisplayProgressBar("search prefab ..", "now progress",  ((float)Progress )/ paths.Count);


            //取相对路径
            string absPath = path.Replace(splitTitle, "Assets\\");
            GameObject go = AssetDatabase.LoadAssetAtPath(absPath, typeof(GameObject)) as GameObject;

            if (go != null)
            {
                collectObjects.Add(go);
            }
        }
        
        EditorUtility.ClearProgressBar();

        return collectObjects;
    }

    /// <summary>
    /// 本地化具体prefab
    /// </summary>
    /// <param name="go"></param>
    private void _LocalizePrefab( GameObject go )
    {
        //取到所有的UIlabel
        UILabel[] alllabels = go.transform.GetComponentsInChildren<UILabel>(true);
        if (alllabels.Length == 0) return;

        //需要排除的label
        List<UILabel> excludeList = new List<UILabel>();
        //得到当前prebab上面的所有脚本
        var compoents = go.transform.GetComponentsInChildren<Component>(true);
        foreach (var component in compoents)
        {
            if (component == null) continue;
            //使用SerializedObject 去找到相关引用。
            //主要是利用Unity提供的迭代API遍历元素去找引用。
            SerializedObject serializeObject = new SerializedObject(component);
            SerializedProperty sp = serializeObject.GetIterator();
            do
            {

                bool isCheckRight = sp != null && sp.propertyType != null;
                if (isCheckRight)
                {
                    //如果该脚本引用了UIlabel则添加进来。
                    if (sp.propertyType == SerializedPropertyType.ObjectReference
                        && sp.objectReferenceValue != null
                        && sp.objectReferenceValue.GetType() == typeof(UILabel))
                    {
                        UILabel excludeLab = sp.objectReferenceValue as UILabel;
                        if (excludeLab != null)
                        {
                            excludeList.Add(excludeLab);
                        }
                    }
                }
            } while (sp.NextVisible(true));
        }


        //筛选出未被引用的label
        var finalLabels = from label in alllabels
                          where
                              !excludeList.Contains(label)
                          select label;


        if (finalLabels.Count() != 0)
        {
         
            GenRecordInfoText(finalLabels.ToArray());
            EditorUtility.SetDirty(go);
        }
    }


    private void _SaveCsv()
    {
        for (int i = 0; i < curStoreInfoDatas.Count; i++)
        {
            for (int j = 0; j < curStoreInfoDatas[i].Strvalues.Count; j++)
            {
                if (string.IsNullOrEmpty(curStoreInfoDatas[i].Strvalues[j]))
                {
                    curCsvReader.AddColumn("-");
                }
                else
                {
                    curCsvReader.AddColumn(curStoreInfoDatas[i].Strvalues[j],false);
                }
            }
            curCsvReader.EndRow();
        }
  
        curCsvReader.Save(localizefilePos);
        Localization.Init(Curlanguage);
        Debug.LogError("Save Success !");
    }


    private void GenRecordInfoText(UILabel[] collectLabels)
    {
        for (int i = 0; i < collectLabels.Length; i++)
        {
            if (collectLabels[i] == null || string.IsNullOrEmpty(collectLabels[i].text)) continue;

            RecordLabelInfo(collectLabels[i]);
        }
    }

    private void RecordLabelInfo(UILabel label)
    {
        UILabel labelItem = label;
        UILocalize localize = labelItem.GetComponent<UILocalize>() ??
                              labelItem.gameObject.AddComponent<UILocalize>();

        string value = labelItem.text.Replace("\n" , @"\\n"); ;
        string key = GetStringKeyInText(value);
        localize.key = key;
    }


    private string GetStringKeyInText(string value)
    {
        string key = _GetKey(value);
        if (string.IsNullOrEmpty(key))
        {
            key = _GenNewKey(value);
        }
        return key;
    }

    private string _GetKey(string value)
    {
        string key ="";
        for (int i = 0; i < curStoreInfoDatas.Count; i++)
        {
            key = curStoreInfoDatas[i].CheckIsHaveValue(value);
            if (!string.IsNullOrEmpty(key))
            {
                break;
            }
        }
        return key;
    }

    private string _GenNewKey(string value)
    {
        isGenNewKey = true;

        string key = string.Format(keyStartKey, curIndex);

        List<string> newStringInfos = new List<string>();
        newStringInfos.Add(key);

        for (int i = 0; i < curChoiceLanguages.Length; i++)
        {
            if (i == languageSelectIndex)
            {
                newStringInfos.Add(value);
            }
            else
            {
                newStringInfos.Add(" ");
            }
        }
        _AddToCsv(newStringInfos);
        return key;
    }


    private void _AddToCsv(List<string> newValue )
    {
        curIndex++;
        _RecordItem(newValue);
    }

}