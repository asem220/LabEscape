using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class InputReview
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Keyboard keyboard;
    static Mouse mouse;
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Keys(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        await Task.Delay(120);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        await Task.Delay(80);
    }
    public static async Task<object> Main()
    {
        Check(Application.isPlaying,"Requires Play mode");
        var originalSettings=InputSystem.settings;
        var originalFlags=originalSettings.hideFlags;
        originalSettings.hideFlags|=HideFlags.DontUnloadUnusedAsset;
        var settings=UnityEngine.Object.Instantiate(originalSettings);
        settings.hideFlags|=HideFlags.DontUnloadUnusedAsset;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=settings;
        var originalKeyboard=Keyboard.current;var originalMouse=Mouse.current;
        keyboard=InputSystem.AddDevice<Keyboard>("Review Keyboard");mouse=InputSystem.AddDevice<Mouse>("Review Mouse");
        keyboard.MakeCurrent();mouse.MakeCurrent();
        var passed=new List<string>();
        try
        {
        if(SceneManager.GetActiveScene().name!="MainMenu")
        {
            var operation=SceneManager.LoadSceneAsync("MainMenu");
            while(!operation.isDone)await Task.Delay(25);
            await Task.Delay(100);
        }
        Application.runInBackground=true;
        var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        menu.ShowSettings(true);await Keys(Key.Escape);
        Check(!Field<GameObject>(menu,"settingsPanel").activeSelf,"Escape did not close menu settings");
        passed.Add("Menu Escape");
        await Keys(Key.Enter);
        Check(SceneManager.GetActiveScene().name=="Laboratory","Enter did not activate Play");
        await Task.Delay(100);
        var player=GameObject.Find("Player");var body=player.GetComponent<Rigidbody2D>();
        var start=body.position;
        await Keys(Key.W);float straight=Vector2.Distance(start,body.position);
        Check(straight>.15f&&straight<1.5f,"W movement failed");
        var beforeDiagonal=body.position;
        await Keys(Key.W,Key.D);float diagonal=Vector2.Distance(beforeDiagonal,body.position);
        Check(diagonal>.15f&&diagonal<1.5f&&diagonal<straight*1.5f,"Diagonal movement is too fast");
        passed.Add("WASD movement");
        var energy=player.GetComponent<PlayerEnergy>();energy.UseTelekinesis(1);
        var source=UnityEngine.Object.FindObjectsByType<FoodPickup>(FindObjectsInactive.Include).First(r=>r.name.StartsWith("FoodRation_"));
        var food=UnityEngine.Object.Instantiate(source.gameObject);food.name="Review Food";food.transform.position=player.transform.position;food.SetActive(true);
        await Keys(Key.E);
        Check(energy.Magic==100,"E did not eat newly activated food");
        passed.Add("Eat key");

        var boxSource=UnityEngine.Object.FindObjectsByType<TelekinesisObject>(FindObjectsInactive.Include).First(b=>b.name=="TelekinesisBox");
        var box=UnityEngine.Object.Instantiate(boxSource.gameObject);box.name="Review Box";box.transform.position=player.transform.position+Vector3.right;box.SetActive(true);
        await Task.Delay(80);
        var target=box.GetComponent<TelekinesisObject>();
        // Restrict this test object's actions to the temporary devices, so moving
        // the real cursor in another app cannot override the test pointer.
        Field<InputAction>(target,"pointInput").ApplyBindingOverride(mouse.position.path);
        Field<InputAction>(target,"interactInput").ApplyBindingOverride(keyboard.eKey.path);
        Field<InputAction>(target,"throwInput").ApplyBindingOverride(mouse.leftButton.path);
        var screen=Camera.main.WorldToScreenPoint(box.GetComponent<BoxCollider2D>().bounds.center);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y)});
        await Task.Delay(80);
        var pointAction=Field<InputAction>(target,"pointInput");
        var pointer=pointAction.ReadValue<Vector2>();
        var cursor=Camera.main.ScreenToWorldPoint(new Vector3(pointer.x,pointer.y,box.transform.position.z-Camera.main.transform.position.z));
        int presses=0;Field<InputAction>(target,"interactInput").performed+=_=>presses++;
        var pointerInfo="point="+pointer+" expected="+screen+" device="+pointAction.activeControl?.device?.name+" overlaps="+box.GetComponent<BoxCollider2D>().OverlapPoint(cursor);
        await Keys(Key.E);
        Check(Field<bool>(target,"held"),"E did not pick up box: "+pointerInfo+" presses="+presses);
        Check(energy.Magic<100,"Holding box did not consume magic");
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y),buttons=1});
        await Task.Delay(180);
        Check(Field<float>(target,"charge")>0,"Throw did not charge");
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y)});
        await Task.Delay(80);
        Check(!Field<bool>(target,"held")&&box.GetComponent<Rigidbody2D>().linearVelocity.magnitude>1,"Mouse release did not throw box");
        UnityEngine.Object.Destroy(box);passed.Add("Pickup and throw");

        await Keys(Key.Escape);await Task.Delay(100);
        Check(Time.timeScale==0,"Escape did not pause game");
        var pausedPosition=body.position;await Keys(Key.D);
        Check(Vector2.Distance(body.position,pausedPosition)<.01f,"Player moved during pause");
        var gui=UnityEngine.Object.FindAnyObjectByType<GameInterface>();gui.ShowSettings(true);
        await Keys(Key.Escape);
        Check(Time.timeScale==0&&!Field<GameObject>(gui,"settingsPanel").activeSelf,"Escape did not go back from pause settings");
        await Keys(Key.Escape);
        Check(Time.timeScale==1,"Escape did not resume game");
        passed.Add("Pause keys");
        body.position=start;body.linearVelocity=Vector2.zero;
        var report=new {passed,straightDistance=straight,diagonalDistance=diagonal,success=true};
        File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Input.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
        }
        catch(Exception error)
        {
            File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Input.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {passed,error=error.Message},Newtonsoft.Json.Formatting.Indented));
            throw;
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            originalKeyboard?.MakeCurrent();originalMouse?.MakeCurrent();
            if(originalSettings!=null){InputSystem.settings=originalSettings;originalSettings.hideFlags=originalFlags;}
            UnityEngine.Object.Destroy(settings);
        }
    }
}
