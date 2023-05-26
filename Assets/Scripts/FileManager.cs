using System;
using System.IO;
using UnityEngine;


// TODO: Write a Binary file manager version: https://www.red-gate.com/simple-talk/development/dotnet-development/saving-game-data-with-unity/
public static class FileManager
{
    public static bool WriteToFile(string a_FileName, string a_FileContents)
    {
        var fullPath = Path.Combine(Application.persistentDataPath, a_FileName);

        try
        {
            File.WriteAllText(fullPath, a_FileContents);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to {fullPath} with exception {e}");
            return false;
        }
    }

    public static bool LoadFromFile(string a_FileName, out string result)
    {
        var fullPath = Path.Combine(Application.persistentDataPath, a_FileName);

        try
        {
            result = File.ReadAllText(fullPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read from {fullPath} with exception {e}");
            result = "";
            return false;
        }
    }

    public static bool DeleteFile(string a_FileName)
    {
        var fullPath = Path.Combine(Application.persistentDataPath, a_FileName);

        try
        {
            File.Delete(fullPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete file from {fullPath} with exception {e}");
            return false;
        }
    }
}