using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;
#endif
using System.IO;
 
public class OSXBuildHelper : MonoBehaviour
{
#if UNITY_EDITOR_OSX
    [PostProcessBuild]
    static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        // Read plist
        var plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
 
        // Update value
        PlistElementDict rootDict = plist.root;
        rootDict.SetString("NSCameraUsageDescription", "Used for tracking head position");
 
        // Write plist
        File.WriteAllText(plistPath, plist.WriteToString());
    }
#endif
}