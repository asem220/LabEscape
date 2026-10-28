using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneReview
{
    public static object Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play mode before scene review.");
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(original.isDirty)throw new Exception("Save current scene before review.");
        var errors=new List<string>();var scenes=new List<object>();
        foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var scene=path==original.path?original:EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            int missingScripts=0,missingReferences=0,buttons=0;
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                foreach(var component in t.GetComponents<Component>().Where(c=>c!=null))
                {
                    var so=new SerializedObject(component);var property=so.GetIterator();
                    while(property.Next(true))
                        if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==null
                            &&!property.objectReferenceEntityIdValue.Equals(Activator.CreateInstance(property.objectReferenceEntityIdValue.GetType())))
                        {missingReferences++;errors.Add(path+": missing "+component.GetType().Name+"."+property.propertyPath+" on "+t.name);}
                }
                foreach(var button in t.GetComponents<Button>())
                {
                    buttons++;
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                    {
                        var target=button.onClick.GetPersistentTarget(i);var method=button.onClick.GetPersistentMethodName(i);
                        if(target==null||string.IsNullOrEmpty(method)||target.GetType().GetMethod(method)==null)
                            errors.Add(path+": broken button "+t.name+" -> "+method);
                    }
                }
            }
            if(missingScripts>0)errors.Add(path+": missing scripts "+missingScripts);
            scenes.Add(new {path,missingScripts,missingReferences,buttons});
            if(path!=original.path)EditorSceneManager.CloseScene(scene,true);
        }
        var buildScenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        if(buildScenes.Length==0||Path.GetFileNameWithoutExtension(buildScenes[0])!="MainMenu")errors.Add("MainMenu must be the first build scene.");
        foreach(var path in buildScenes)if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)errors.Add("Missing build scene "+path);
        var exits=original.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LabExit>(true)).ToArray();
        if(exits.Length==0)errors.Add("Gameplay has no exit trigger.");
        var activeRations=original.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FoodPickup>(true)).Count(p=>p.isActiveAndEnabled);
        var report=new {scenes,buildScenes,exitTriggers=exits.Length,activeRations,errors};
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Scenes.json"),json);
        return report;
    }
}
