using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
public static class DoorReview
{
    const string Folder="Assets/Environment/DoorAssets";
    const string Walls="Assets/Environment/WallAssets";
    struct Opening
    {
        public string name;public Vector3Int cell;public bool vertical;
        public Opening(string name,int x,int y,bool vertical=false){this.name=name;cell=new Vector3Int(x,y,0);this.vertical=vertical;}
        public Vector3Int Direction=>vertical?Vector3Int.up:Vector3Int.right;
        public Vector3 Position=>vertical?new Vector3(cell.x+.5f,cell.y+1.5f,0):new Vector3(cell.x+1.5f,cell.y,0);
    }
    static readonly Opening[] Openings={
        new Opening("Canteen Door",16,33),new Opening("Garden Door",44,33),
        new Opening("Experiment Door",16,12),new Opening("Preparation Door",35,12),new Opening("Control Door",55,12),
        new Opening("West Door",8,18,true),new Opening("Middle Door",27,18,true),new Opening("East Door",49,18,true),
        new Opening("Ward Door",12,8),new Opening("Exit Door",56,8),new Opening("Airlock Door",57,1)
    };
    public static object Main()
    {
        var root=GameObject.Find("Refined Laboratory");var holder=root.transform.Find("Doors");
        var caps=root.transform.Find("05 Wall tops").GetComponent<Tilemap>();
        var profile=AssetDatabase.LoadAssetAtPath<Tile>(Walls+"/Tiles/WallTops_10.asset").sprite;
        var sprites=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Doors.png").OfType<Sprite>().ToArray();
        int contacts=0;
        foreach(var sprite in sprites)
        {
            if(sprite.rect.size!=new Vector2(160,64)||sprite.pivot!=new Vector2(80,32))throw new Exception("Door frame rectangle changed");
            for(int y=0;y<36;y++)foreach(int edge in new[]{28,131})
                if(sprite.texture.GetPixel((int)sprite.rect.x+edge,(int)sprite.rect.y+y).a<.99f)throw new Exception("Transparent jamb edge");
            for(int y=36;y<=59;y++)foreach(int begin in new[]{24,128})for(int x=begin;x<begin+8;x++)
            {
                Color32 expected=profile.texture.GetPixel((int)profile.rect.x+16,(int)profile.rect.y+y-32);
                Color32 actual=sprite.texture.GetPixel((int)sprite.rect.x+x,(int)sprite.rect.y+y);
                if(!expected.Equals(actual))throw new Exception("Door wall contact mismatch");contacts++;
            }
        }
        foreach(var opening in Openings)
        {
            var door=holder.Find(opening.name);if(door==null||Vector3.Distance(door.position,opening.Position)>.0001f)throw new Exception("Door placement wrong: "+opening.name);
            if(door.GetComponent<SpriteRenderer>().sprite.name!="Closed Green")throw new Exception("Wrong initial door state");
            if(Mathf.Abs(Mathf.DeltaAngle(door.eulerAngles.z,0))>.001f||door.localScale!=Vector3.one)throw new Exception("Door orientation or scale wrong");
            var behaviour=door.GetComponent<LabDoor>();if(behaviour==null)throw new Exception("Door cannot open");
            var framesProperty=new SerializedObject(behaviour).FindProperty("frames");
            if(framesProperty.arraySize!=5)throw new Exception("Missing opening frames");
            for(int i=0;i<5;i++)if(framesProperty.GetArrayElementAtIndex(i).objectReferenceValue==null)throw new Exception("Missing frame reference");
            foreach(var end in new[]{opening.cell-opening.Direction,opening.cell+opening.Direction*3})
                if(AssetDatabase.GetAssetPath(caps.GetTile(end))!=Folder+"/"+(opening.vertical?"Vertical Join":"Horizontal Join")+".asset")throw new Exception("Unconnected jamb");
        }
        var sideFrames=AssetDatabase.LoadAllAssetsAtPath(Folder+"/VerticalDoors.png").OfType<Sprite>().ToArray();
        var sideProfile=AssetDatabase.LoadAssetAtPath<Tile>(Walls+"/Tiles/WallTops_05.asset").sprite;
        foreach(var sprite in sideFrames)
        {
            if(sprite.rect.size!=new Vector2(32,128)||sprite.pivot!=new Vector2(16,64))throw new Exception("Side frame size wrong");
            foreach(int start in new[]{8,112})for(int y=start;y<start+8;y++)for(int x=4;x<28;x++)
            {
                Color32 expected=sideProfile.texture.GetPixel((int)sideProfile.rect.x+x,(int)sideProfile.rect.y+16);
                Color32 actual=sprite.texture.GetPixel((int)sprite.rect.x+x,(int)sprite.rect.y+y);
                if(!expected.Equals(actual))throw new Exception("Side wall contact mismatch");contacts++;
            }
        }
        var report=new {doors=Openings.Length,frameContacts=contacts,horizontalFrames=sprites.Length,verticalFrames=sideFrames.Length,errors=0};
        File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Doors.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
