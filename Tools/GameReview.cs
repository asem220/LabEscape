using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class GameReview
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static async Task Wait(Func<bool> ready,string message,int milliseconds=8000)
    {
        var end=DateTime.UtcNow.AddMilliseconds(milliseconds);
        while(!ready()){if(DateTime.UtcNow>=end)throw new Exception(message);await Task.Delay(25);}
    }
    static async Task Load(string name)
    {
        var operation=SceneManager.LoadSceneAsync(name);
        await Wait(()=>operation.isDone,"Scene load timed out: "+name);
        await Task.Delay(100);
    }

    public static async Task<object> Main()
    {
        Check(Application.isPlaying,"Requires Play mode");
        Application.runInBackground=true;
        var viewType=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
        UnityEditor.EditorWindow.GetWindow(viewType).Focus();
        int firstFrame=Time.frameCount;
        float volume=PlayerPrefs.GetFloat("MasterVolume",.8f);
        bool hadVolume=PlayerPrefs.HasKey("MasterVolume");
        var rounds=new List<object>();
        await Load("MainMenu");
        try
        {
            for(int round=1;round<=3;round++)
            {
                var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                Check(menu!=null,"Main menu missing");
                var play=Field<Button>(menu,"playButton");
                Check(play.interactable,"Play button stays disabled after returning");
                menu.ShowSettings(true);
                Check(Field<GameObject>(menu,"settingsPanel").activeSelf,"Menu settings did not open");
                menu.SetVolume(.37f);
                Check(Mathf.Abs(AudioListener.volume-.37f)<.001f,"Menu volume did not apply");
                menu.ShowSettings(false);
                Check(EventSystem.current.currentSelectedGameObject==play.gameObject,"Menu selection incorrect");
                play.onClick.Invoke();play.onClick.Invoke();
                await Wait(()=>SceneManager.GetActiveScene().name=="Laboratory","Play did not load Laboratory");
                await Task.Delay(100);
                var gui=UnityEngine.Object.FindAnyObjectByType<GameInterface>();
                var energy=UnityEngine.Object.FindAnyObjectByType<PlayerEnergy>();
                Check(gui!=null&&energy!=null,"Gameplay components missing");
                Check(Time.timeScale==1,"Game started paused");
                Check(Mathf.Abs(AudioListener.volume-.37f)<.001f,"Volume lost when entering game");
                Check(energy.Magic==100,"Magic did not reset for a new game");
                energy.UseTelekinesis(1);
                float usedMagic=energy.Magic;
                await Task.Delay(100);
                Check(Mathf.Abs(energy.Magic-usedMagic)<.001f,"Magic regenerated without food");
                int pausedTextures=0;
                for(int pause=0;pause<3;pause++)
                {
                    gui.Pause();
                    await Wait(()=>!Field<bool>(gui,"capturing"),"Pause screenshot capture stalled");
                    Check(Time.timeScale==0,"Pause did not stop game time");
                    Check(Field<GameObject>(gui,"pauseOverlay").activeSelf,"Pause overlay hidden");
                    Check(Field<RawImage>(gui,"blurredBackground").texture!=null,"Pause blur texture missing");
                    float food=energy.Food,magic=energy.Magic;
                    await Task.Delay(100);
                    Check(energy.Food==food&&energy.Magic==magic,"Resources changed while paused");
                    Check(!FoodPickup.Active.Any(r=>r.TryEat()),"Food could be consumed while paused");
                    gui.ShowSettings(true);
                    Check(Field<GameObject>(gui,"settingsPanel").activeSelf,"Pause settings did not open");
                    gui.ShowSettings(false);gui.Resume();
                    Check(Time.timeScale==1,"Resume did not restore game time");
                    Check(Field<RenderTexture>(gui,"pauseTexture")==null,"Pause texture leaked after resume");
                    pausedTextures++;
                }
                // Every new play session must have one active input/UI owner.
                Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"Duplicate EventSystems after scene reload");
                if(round==3)
                {
                    var player=GameObject.Find("Player").GetComponent<Rigidbody2D>();
                    player.position=new Vector2(58.5f,-.5f);player.linearVelocity=Vector2.zero;
                    Physics2D.SyncTransforms();
                    await Wait(()=>SceneManager.GetActiveScene().name=="MainMenu","Exit trigger did not return to menu");
                    await Task.Delay(100);
                }
                else
                {
                    gui.Pause();await Wait(()=>!Field<bool>(gui,"capturing"),"Final pause stalled");
                    gui.MainMenu();gui.MainMenu();
                    await Wait(()=>SceneManager.GetActiveScene().name=="MainMenu","Return to menu failed");
                    await Task.Delay(100);
                }
                Check(Time.timeScale==1,"Time stayed paused after leaving gameplay");
                rounds.Add(new {round,play=true,pauseCycles=pausedTextures,settings=true,resources=true,returnToMenu=true,exit=round==3});
            }
            Check(Time.frameCount>firstFrame,"Game frames did not advance");
            var report=new {rounds,framesAdvanced=Time.frameCount-firstFrame,success=true};
            File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Gameplay.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            Time.timeScale=1;
            AudioListener.volume=volume;
            if(hadVolume)PlayerPrefs.SetFloat("MasterVolume",volume);else PlayerPrefs.DeleteKey("MasterVolume");
            PlayerPrefs.Save();
        }
    }
}
