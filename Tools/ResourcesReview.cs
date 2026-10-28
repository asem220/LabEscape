using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ResourcesReview
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Resource(PlayerEnergy energy,string property,float value)=>typeof(PlayerEnergy).GetProperty(property).SetValue(energy,value);
    public static async Task<object> Main()
    {
        if(!Application.isPlaying)throw new Exception("Requires Play mode.");
        var energy=UnityEngine.Object.FindAnyObjectByType<PlayerEnergy>();
        var gui=UnityEngine.Object.FindAnyObjectByType<GameInterface>();
        var failures=new List<string>();var passes=new List<string>();
        Action<string,bool> check=(name,ok)=>{if(ok)passes.Add(name);else failures.Add(name);};
        var food=energy.Food;var magic=energy.Magic;
        try
        {
            Resource(energy,"Food",60);Resource(energy,"Magic",50);
            check("Negative magic duration rejected",!energy.UseTelekinesis(-1)&&energy.Food==60&&energy.Magic==50);
            Resource(energy,"Food",60);Resource(energy,"Magic",50);
            check("Negative food rejected",!energy.Eat(-10,-10)&&energy.Food==60&&energy.Magic==50);
            Resource(energy,"Food",60);Resource(energy,"Magic",50);
            check("Empty food not consumed",!energy.Eat(0,0));
            Resource(energy,"Food",100);Resource(energy,"Magic",100);
            energy.UseTelekinesis(1);
            check("Magic costs food",Mathf.Abs(energy.Magic-92)<.01f&&Mathf.Abs(energy.Food-97.2f)<.01f);
            energy.Eat(35,45);
            check("Food restores magic",energy.Food==100&&energy.Magic==100);
            check("Full resources do not consume food",!energy.Eat(35,45));
            Resource(energy,"Magic",1);
            check("Insufficient magic rejected",!energy.UseTelekinesis(1)&&energy.Magic==1);

            var source=Array.Find(UnityEngine.Object.FindObjectsByType<FoodPickup>(FindObjectsInactive.Include),r=>r.name.StartsWith("FoodRation_"));
            if(source!=null)
            {
                var ration=UnityEngine.Object.Instantiate(source.gameObject);ration.name="Review Food";
                ration.transform.position=energy.transform.position;ration.SetActive(true);
                Resource(energy,"Food",50);Resource(energy,"Magic",50);
                typeof(GameInterface).GetMethod("EatNearbyRation",Private).Invoke(gui,null);
                check("New food works with interact key",!ration.activeSelf);
                UnityEngine.Object.Destroy(ration);
            }
            else failures.Add("Food sample missing");

            var boxSource=Array.Find(UnityEngine.Object.FindObjectsByType<TelekinesisObject>(FindObjectsInactive.Include),b=>b.name=="TelekinesisBox");
            if(boxSource!=null)
            {
                var a=UnityEngine.Object.Instantiate(boxSource.gameObject);a.name="Review Box Left";
                var b=UnityEngine.Object.Instantiate(boxSource.gameObject);b.name="Review Box Right";
                a.SetActive(true);b.SetActive(true);await Task.Delay(50);
                a.SetActive(false);
                var behaviour=b.GetComponent<TelekinesisObject>();
                var cloneField=typeof(TelekinesisObject).GetField("interactInput",Private);
                var input=cloneField!=null?(InputAction)cloneField.GetValue(behaviour):((InputActionReference)typeof(TelekinesisObject).GetField("interactAction",Private).GetValue(behaviour)).action;
                check("Disabling one box keeps other input enabled",input.enabled);
                UnityEngine.Object.Destroy(a);UnityEngine.Object.Destroy(b);
            }
            else failures.Add("Telekinesis sample missing");
        }
        finally {Resource(energy,"Food",food);Resource(energy,"Magic",magic);}
        var report=new {passes,failures};
        File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Resources.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
