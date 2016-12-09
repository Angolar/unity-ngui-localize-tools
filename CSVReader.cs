using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;


/// <summary>
/// CSV格式读取器.行列均由1开始
/// </summary>
public class CSVReader
{
    private List<List<string>> mContentList = new List<List<string>>();
    private string mOutputContent = "";
    private int currentRow = 1;

    public string FileName;

    
    public int Row
    {
        get { return currentRow; }
        set { currentRow = value; }
    }

    /// <summary>
    /// 向下移动指定行
    /// </summary>
    /// <param name="count"></param>
    public void MoveRow(int count)
    {
        currentRow += count;
    }

    /// <summary>
    /// 移至下一行
    /// </summary>
    /// <returns></returns>
    public bool MoveNext()
    {
        return ++currentRow <= mContentList.Count;
    }

    public IEnumerable GetCurRow()
    {
        return mContentList[currentRow - 1];
    }

	/// <summary>
	/// 当前指向的行中的原始数据
	/// </summary>
	public List<string> CurRow
	{
		get { return mContentList[currentRow - 1]; }
	}

    public void Append(String fileName, string newInfo)
    {
        StreamWriter sw = null;

        FileInfo t = new FileInfo(fileName);
        if (!t.Exists)
        {
            sw = t.CreateText();
        }
        else
        {
            sw = t.AppendText();
        }
        sw.WriteLine(newInfo);
        sw.Close();
        sw.Dispose();
    }

    public void Save(String fileName)
    {
        try
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Create);
            StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
            writer.Write(mOutputContent);
            writer.Close();
            fileStream.Close();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void Load(String fileName)
    {
        try
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader reader = new StreamReader(fs, Encoding.UTF8);
            string content = reader.ReadToEnd();
            LoadContent(content);
            reader.Close();
            fs.Close();
            FileName = fileName;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void LoadContent(String content)
    {
        StringReader sr = new StringReader(content);

        string line = null;

        while ((line = sr.ReadLine()) != null)
        {
            line = line.Trim(' ', '\t');
            if (line == "") continue;

            List<string> items = new List<string>();
            items.AddRange(line.Split('|'));
            for (int i = 0; i < items.Count; i++) items[i].Trim(' ', '\t');

            mContentList.Add(items);
        }

        sr.Close();
    }

    public int GetInt(int row, int col)
    {
        int i = 0;

        string content = mContentList[row-1][col-1];
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }
        
        //LogError(row , col);
        try
        {
            i = int.Parse(content);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Int Parse error at row = " + row + " col = " + col);
        }
        return i;
    }

    public int GetInt(int col)
    {
        return GetInt(currentRow, col);
    }

    public float GetFloat(int row, int col)
    {
        float f = 0f;
        //LogError(row, col);
        try
        {
            f = float.Parse(mContentList[row - 1][col - 1]);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Float Parse error at row = " + row + " col = " + col);
        }
        return f;
    }

    public float GetFloat(int col)
    {
        return GetFloat(currentRow, col);
    }

    public bool GetBoolean(int row, int col)
    {
        //LogError(row , col);
        bool ret = false;
        string str = GetString(row, col).ToLower();
        try
        {
            ret = bool.Parse(str);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Bool Parse error at row = " + row + " col = " + col);
        }
        return ret;
    }

    public bool GetBoolean(int col)
    {
        return GetBoolean(currentRow, col);
    }

    public string GetString(int row, int col)
    {
        LogError(row, col);
        string temp = mContentList[row - 1][col - 1];
        temp = temp.Replace('#', '|').Replace('@', '\n');
        return temp;
    }

    public string GetString(int col)
    {
        return GetString(currentRow, col);
    }

    public Vector3 GetVector3(int row, int col)
    {
        LogError(row, col);
        Vector3 v = Vector3.zero;
        string str = mContentList[row - 1][col - 1];
        string[] strs = str.Split(',');
        v.x = float.Parse(strs[0]);
        v.y = float.Parse(strs[1]);
        v.z = float.Parse(strs[2]);
        return v;
    }

    public Vector3 GetVector3(int col)
    {
        return GetVector3(currentRow, col);
    }

    public List<string> GetColList(int col)
    {
        List<string> cols = new List<string>();
        for (int i = 1; i < GetRowCount() + 1; i++)
        {
            cols.Add(GetString(i, col));
        }
        return cols;
    }

    void LogError(int row, int col)
    {
        if (row - 1 >= mContentList.Count)
            Debug.LogError("read data error at row " + row + " col " + col);
        else
        {
            if (col - 1 >= mContentList[row - 1].Count)
                Debug.LogError("read data error at row " + row + " col " + col);
        }
    }

#if UNITY_EDITOR
    public void SetInt(int col, int value)
    {
        mContentList[currentRow - 1][col - 1] = value.ToString();
    }

    public void SetFloat(int col, float value)
    {
        mContentList[currentRow - 1][col - 1] = value.ToString();
    }

    public void SetBoolean(int col, bool value)
    {
        mContentList[currentRow - 1][col - 1] = value ? "1" : "0";
    }

    public void SetString(int col, string value)
    {
        mContentList[currentRow - 1][col - 1] = value;
    }

    public void SetVector3(int col, Vector3 value)
    {
        string str = "";
        str = value.x.ToString() + "," + value.y.ToString() + "," + value.z.ToString();
        mContentList[currentRow - 1][col] = str;
    }

    public void SaveByModify(string fileName)
    {
        mOutputContent = "";
        List<string> row = null;
        for (int i = 0; i < mContentList.Count; i++)
        {
            if (i > 0)
                EndRow();
            row = mContentList[i];
            for (int j = 0; j < row.Count; j++)
            {
                AddColumn(row[j]);
            }
        }

        Save(fileName);
    }

    public void DeleteRow(int row)
    {
        mContentList.RemoveAt(row - 1);
    }
#endif
    public int GetRowCount()
    {
        return mContentList.Count;
    }

    public int GetColumnCount(int row)
    {
        return mContentList[row - 1].Count;
    }

    /// <summary>
    /// 添加一个输出单元
    /// </summary>
    /// <param name="content"></param>
    /// <param name="endLine">是否终止一行</param>
    public void AddData<T>(T content, bool endLine = false)
    {
        AddColumn(content.ToString(), false);
        if (endLine)
        {
            EndRow();
        }
    }

    public void AddColumn(string content, bool autoReplace = true)
    {
        if (mOutputContent.Length > 0 && mOutputContent[mOutputContent.Length - 1] != '\n')
        {
            mOutputContent += "|";
        }
        if (string.IsNullOrEmpty(content))
        {
            content += "";
        }

        if (autoReplace)
        {
            mOutputContent += content.Replace('|', '#').Replace('\n', '@');
        }
        else
        {
            mOutputContent += content;
        }
    }

    public void ExpandColumn(string content)
    {
        if (mOutputContent.Length > 0 && mOutputContent[mOutputContent.Length - 1] != '\n')
        {
            mOutputContent += "|";
        }
        mOutputContent += content;
    }

    public void AddColumn(int content)
    {
        AddColumn(content.ToString());
    }

    public void AddColumn(float content)
    {
        AddColumn(content.ToString());
    }

    public void AddColumn(bool content)
    {
        string str = content ? "true" : "false";
        AddColumn(str);
    }

    public void AddColumn(Vector3 content)
    {
        string str = "";
        str = content.x.ToString() + "," + content.y.ToString() + "," + content.z.ToString();
        AddColumn(str);
    }

    public void EndRow()
    {
        mOutputContent += "\n";
    }

    public void AddNullRow()
    {
        mOutputContent += "|\n";
    }

}
