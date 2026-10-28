using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildReview
{
    public static object Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode before build.");
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))
            throw new Exception("Windows build support is not installed.");
        string output=Path.Combine(Application.dataPath,"..","Build","LabEscape.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
        });
        var messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray();
        var result=new {result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,bytes=report.summary.totalSize,seconds=report.summary.totalTime.TotalSeconds,messages};
        File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Build.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+string.Join("; ",messages));
        return result;
    }
}
