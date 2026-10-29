using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.SceneManagement;

public static class PaletteReview
{
    const string Walls="Assets/Environment/WallAssets";
    const string Doors="Assets/Environment/DoorAssets";
    static bool Same(Color32 a,Color32 b)=>a.a==b.a&&(a.a==0||a.Equals(b));
    public static object Main()
    {
        int cells=0;
        foreach(var path in new[]{Walls+"/WallFaces.png",Walls+"/Pillars.png",Doors+"/DoorTiles.png"})
            foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                if(sprite.rect.width!=32||sprite.rect.height!=32||sprite.pivot!=new Vector2(16,16)||sprite.pixelsPerUnit!=32)
                    throw new Exception("Non-grid sprite: "+sprite.name);
                cells++;
            }
        var parts=AssetDatabase.LoadAllAssetsAtPath(Walls+"/Pillars.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        var whole=AssetDatabase.LoadAllAssetsAtPath(Walls+"/WholePillars.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        int reconstructed=0;
        for(int variant=0;variant<4;variant++)for(int y=0;y<64;y++)for(int x=0;x<32;x++)
        {
            var part=parts[variant+(y<32?4:0)];var r=part.rect;var original=whole[variant].rect;
            if(!Same(part.texture.GetPixel((int)r.x+x,(int)r.y+y%32),whole[variant].texture.GetPixel((int)original.x+x,(int)original.y+y)))
                throw new Exception("Pillar parts do not reconstruct the original");
            reconstructed++;
        }
        var root=GameObject.Find("Refined Laboratory");var map=root.transform.Find("04 Vertical wall faces").GetComponent<Tilemap>();
        int paired=0;
        var caps=root.transform.Find("05 Wall tops").GetComponent<Tilemap>();
        foreach(var p in map.cellBounds.allPositionsWithin)
        {
            var tile=map.GetTile<Tile>(p);if(tile==null)continue;
            int part=int.Parse(tile.name.Substring("WallFaces_".Length));if(part>=4)continue;
            var bottom=map.GetTile<Tile>(p+Vector3Int.down);
            if(bottom==null||bottom.name!="WallFaces_"+(part+4).ToString("D2"))throw new Exception("Unpaired wall at "+p);
            foreach(var direction in new[]{Vector3Int.left,Vector3Int.right})
                if(caps.HasTile(p+direction)&&caps.HasTile(p+direction+Vector3Int.down)&&!map.HasTile(p+direction))
                    throw new Exception("Face does not reach corner at "+p);
            paired++;
        }
        int faceChecks=0;
        var face=AssetDatabase.LoadAllAssetsAtPath(Walls+"/WallFaces.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        for(int row=0;row<2;row++)foreach(int a in new[]{0,1})foreach(int b in new[]{0,2})for(int y=0;y<32;y++)
        {
            var ar=face[a+row*4].rect;var br=face[b+row*4].rect;
            if(!Same(face[0].texture.GetPixel((int)ar.x+31,(int)ar.y+y),face[0].texture.GetPixel((int)br.x,(int)br.y+y)))throw new Exception("Face seam error");
            faceChecks++;
        }
        var frames=AssetDatabase.LoadAllAssetsAtPath(Doors+"/Doors.png").OfType<Sprite>().ToArray();
        var green=frames.Single(s=>s.name=="Closed Green");var open=frames.Single(s=>s.name=="Open Green");
        int doorChecks=0;
        foreach(var frame in frames.Where(s=>s.name.StartsWith("Opening")||s.name=="Open Green"))
            for(int y=0;y<60;y++)for(int x=28;x<=131;x++)if(x<32||x>127||y<4||y>51)
            {
                if(!Same(frame.texture.GetPixel((int)frame.rect.x+x,(int)frame.rect.y+y),green.texture.GetPixel((int)green.rect.x+x,(int)green.rect.y+y)))throw new Exception("Door frame moves");
                doorChecks++;
            }
        for(int y=4;y<=51;y++)for(int x=32;x<=127;x++)
            if(open.texture.GetPixel((int)open.rect.x+x,(int)open.rect.y+y).a!=0)throw new Exception("Open door blocks the passage visually");
        foreach(var palettePath in new[]{Walls+"/Palettes/Laboratory Walls.prefab",Doors+"/Palettes/Door States.prefab"})
        {
            if(!AssetDatabase.LoadAllAssetsAtPath(palettePath).OfType<GridPalette>().Any())throw new Exception("Native palette data missing");
            var palette=PrefabUtility.LoadPrefabContents(palettePath);
            try
            {
                var grid=palette.GetComponent<Grid>();var paletteMap=palette.GetComponentInChildren<Tilemap>();
                if(grid.cellSize.x!=1||grid.cellSize.y!=1||paletteMap.tileAnchor!=new Vector3(.5f,.5f,0))throw new Exception("Palette cell alignment wrong");
                foreach(var p in paletteMap.cellBounds.allPositionsWithin)
                {
                    var tile=paletteMap.GetTile<Tile>(p);if(tile==null)continue;
                    if(tile.sprite.rect.width!=32||tile.sprite.rect.height!=32||tile.sprite.pivot!=new Vector2(16,16))
                        throw new Exception("Palette contains an oversized tile: "+tile.name);
                }
            }
            finally {PrefabUtility.UnloadPrefabContents(palette);}
        }
        var report=new {gridSprites=cells,reconstructedPillarPixels=reconstructed,pairedWalls=paired,faceSeamChecks=faceChecks,doorFrameChecks=doorChecks,doorFrames=frames.Length,cornerGaps=0,errors=0};
        File.WriteAllText(Path.Combine(Application.dataPath,"..","TestResults","Palette.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }

    public static object Preview()
    {
        if(EditorApplication.isPlaying)throw new Exception("Preview requires Edit mode");
        var original=SceneManager.GetActiveScene();
        var review=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(review);
        var root=new GameObject("Palette Review",typeof(Grid));root.transform.position=new Vector3(90,0,0);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/Facility/FacilityUnlit.mat");
        Func<string,int,Tilemap> create=(name,order)=>{
            var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(root.transform,false);
            var renderer=go.GetComponent<TilemapRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;renderer.mode=TilemapRenderer.Mode.Individual;
            return go.GetComponent<Tilemap>();
        };
        var floor=create("Floor",-100);var tops=create("Tops",0);var faces=create("Faces",-1);var pillars=create("Pillars",0);
        var floorTile=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Environment/LabMilk/Tiles/Floors_00.asset");
        var brush=AssetDatabase.LoadAssetAtPath<HospitalWallTile>(Walls+"/WallAutoBrush47.asset");
        for(int y=0;y<14;y++)for(int x=0;x<19;x++)floor.SetTile(new Vector3Int(x,y,0),floorTile);
        for(int x=1;x<=8;x++)
        {
            tops.SetTile(new Vector3Int(x,11,0),brush);
            int variant=x==1?1:x==8?2:0;
            faces.SetTile(new Vector3Int(x,11,0),AssetDatabase.LoadAssetAtPath<Tile>(Walls+"/Tiles/WallFaces_"+variant.ToString("D2")+".asset"));
            faces.SetTile(new Vector3Int(x,10,0),AssetDatabase.LoadAssetAtPath<Tile>(Walls+"/Tiles/WallFaces_"+(variant+4).ToString("D2")+".asset"));
        }
        for(int y=7;y<=10;y++){tops.SetTile(new Vector3Int(1,y,0),brush);tops.SetTile(new Vector3Int(8,y,0),brush);}
        for(int i=0;i<4;i++)
        {
            pillars.SetTile(new Vector3Int(1+i*2,5,0),AssetDatabase.LoadAssetAtPath<Tile>(Walls+"/Tiles/Pillars_"+i.ToString("D2")+".asset"));
            pillars.SetTile(new Vector3Int(1+i*2,4,0),AssetDatabase.LoadAssetAtPath<Tile>(Walls+"/Tiles/Pillars_"+(i+4).ToString("D2")+".asset"));
        }
        string[] states={"Closed Red","Closed Green","Open Green"};
        for(int i=0;i<3;i++)
        {
            int y=10-i*4;
            foreach(int x in new[]{10,11,15,16,17})tops.SetTile(new Vector3Int(x,y,0),brush);
            var door=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Doors+"/Prefabs/"+states[i]+".prefab"));
            door.transform.SetParent(root.transform,false);door.transform.localPosition=new Vector3(13.5f,y,-.1f);
        }
        var cameraObject=new GameObject("PaletteReviewCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
        camera.CopyFrom(Camera.main);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=7.5f;
        camera.transform.position=new Vector3(99.5f,7,-10);camera.backgroundColor=new Color32(15,24,30,255);camera.clearFlags=CameraClearFlags.SolidColor;
        SceneManager.SetActiveScene(original);
        return new {scene=review.handle,camera=camera.name};
    }
    public static object ClosePreview()
    {
        var camera=GameObject.Find("PaletteReviewCamera");
        if(camera!=null)EditorSceneManager.CloseScene(camera.scene,true);
        return "Temporary review scene closed";
    }
}
