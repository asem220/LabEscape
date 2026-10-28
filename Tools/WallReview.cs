using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;
public static class WallReview
{
    const string Folder="Assets/Environment/WallAssets";
    static bool Same(Color32 a,Color32 b) => a.a==b.a&&(a.a==0||a.Equals(b));
    public static object Main()
    {
        var masks=Enumerable.Range(0,256).Select(HospitalWallTile.NormalizeMask).Distinct().OrderBy(m=>m).ToArray();
        var sprites=AssetDatabase.LoadAllAssetsAtPath(Folder+"/WallTops47.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WallTops47.png");
        var dirs=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left,new Vector2Int(1,1),new Vector2Int(1,-1),new Vector2Int(-1,-1),new Vector2Int(-1,1)};
        int checkedPixels=0,errors=0;
        for(int pattern=0;pattern<512;pattern++)
        {
            Func<int,int,bool> solid=(x,y)=>x>=0&&x<3&&y>=0&&y<3&&(pattern&(1<<(y*3+x)))!=0;
            Func<int,int,Rect> rect=(x,y)=>{
                int mask=0;for(int k=0;k<8;k++)if(solid(x+dirs[k].x,y+dirs[k].y))mask|=1<<k;
                return sprites[Array.IndexOf(masks,HospitalWallTile.NormalizeMask(mask))].rect;
            };
            if(!solid(1,1))continue;
            var a=rect(1,1);
            if(solid(2,1))
            {
                var b=rect(2,1);
                for(int n=0;n<32;n++){checkedPixels++;if(!Same(tex.GetPixel((int)a.x+31,(int)a.y+n),tex.GetPixel((int)b.x,(int)b.y+n)))errors++;}
            }
            if(solid(1,2))
            {
                var b=rect(1,2);
                for(int n=0;n<32;n++){checkedPixels++;if(!Same(tex.GetPixel((int)a.x+n,(int)a.y+31),tex.GetPixel((int)b.x+n,(int)b.y)))errors++;}
            }
        }
        if(errors!=0)throw new Exception("47-tile context seam errors: "+errors);
        var faceSprites=AssetDatabase.LoadAllAssetsAtPath(Folder+"/WallFaces.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        var faceTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WallFaces.png");
        int faceComparisons=0;
        foreach(int a in new[]{0,1})foreach(int b in new[]{0,2})for(int y=0;y<48;y++)
        {
            var ar=faceSprites[a].rect;var br=faceSprites[b].rect;faceComparisons++;
            if(!Same(faceTexture.GetPixel((int)ar.x+31,(int)ar.y+y),faceTexture.GetPixel((int)br.x,(int)br.y+y)))
                throw new Exception("Front wall seam mismatch.");
        }
        return new {variants=sprites.Length,neighbourhoodPatterns=512,checkedPixels,seamErrors=errors,faceComparisons,faceHeight=48,filledCornersSupported=true};
    }
}
