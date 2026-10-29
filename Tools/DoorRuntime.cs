using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public static class DoorRuntime
{
    static async Task Wait(Func<bool> condition,string message)
    {
        var until=DateTime.UtcNow.AddSeconds(3);
        while(!condition()){if(DateTime.UtcNow>until)throw new Exception(message);await Task.Delay(25);}
    }
    public static async Task<object> Main()
    {
        if(!Application.isPlaying)throw new Exception("Requires Play mode");
        bool background=Application.runInBackground;Application.runInBackground=true;
        var player=GameObject.Find("Player");var body=player.GetComponent<Rigidbody2D>();var movement=player.GetComponent<PlayerMovement>();var playerCollider=player.GetComponent<Collider2D>();
        var originalPosition=body.position;bool originalMovement=movement.enabled;float originalTime=Time.timeScale;
        var exit=UnityEngine.Object.FindAnyObjectByType<LabExit>()?.GetComponent<Collider2D>();bool exitEnabled=exit!=null&&exit.enabled;
        var doors=GameObject.Find("Refined Laboratory").transform.Find("Doors").GetComponentsInChildren<LabDoor>();
        int startFrame=Time.frameCount,crossings=0;bool pauseChecked=false;var passed=new List<string>();
        movement.enabled=false;Time.timeScale=1;if(exit!=null)exit.enabled=false;
        var playerOffset=(Vector2)playerCollider.bounds.center-body.position;
        try
        {
            foreach(var door in doors)
            {
                var barrier=door.GetComponent<BoxCollider2D>();var visual=door.GetComponent<SpriteRenderer>();bool vertical=barrier.size.y>barrier.size.x;
                foreach(float sign in new[]{-1f,1f})
                {
                    body.linearVelocity=Vector2.zero;body.position=new Vector2(35,30);Physics2D.SyncTransforms();
                    await Wait(()=>!door.IsOpen&&visual.sprite.name=="Closed Green",door.name+" did not close");
                    Vector2 direction=(vertical?Vector2.right:Vector2.up)*sign;
                    float extent=(vertical?barrier.size.x:barrier.size.y)*.5f;
                    var local=barrier.offset+direction*(extent+.9f);
                    var desiredCenter=(Vector2)door.transform.TransformPoint(local);
                    if(!pauseChecked)
                    {
                        Time.timeScale=0;body.position=desiredCenter-playerOffset;Physics2D.SyncTransforms();await Task.Delay(150);
                        if(door.IsOpen||visual.sprite.name!="Closed Green")throw new Exception("Door moved during pause");
                        Time.timeScale=1;pauseChecked=true;
                    }
                    else {body.position=desiredCenter-playerOffset;Physics2D.SyncTransforms();}
                    await Wait(()=>door.IsOpen&&visual.sprite.name=="Open Green",door.name+" did not open from both sides");
                    var start=body.position;body.linearVelocity=-direction*4f;await Task.Delay(700);body.linearVelocity=Vector2.zero;
                    float distance=Vector2.Dot(body.position-start,-direction);
                    if(distance<2.1f)throw new Exception(door.name+" blocked crossing: "+distance);
                    crossings++;
                }
                passed.Add(door.name);
            }
            if(Time.frameCount<=startFrame)throw new Exception("Game loop did not advance");
            var report=new {doors=passed.Count,crossings,openedBothSides=true,closedAfterLeaving=true,pauseChecked,framesAdvanced=Time.frameCount-startFrame,passed,errors=0};
            File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","DoorRuntime.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            body.linearVelocity=Vector2.zero;body.position=originalPosition;movement.enabled=originalMovement;
            if(exit!=null)exit.enabled=exitEnabled;Time.timeScale=originalTime;Application.runInBackground=background;
        }
    }
}
