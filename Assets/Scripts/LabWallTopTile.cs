using UnityEngine;
using UnityEngine.Tilemaps;

// Верх стены: соседние клетки автоматически выбирают прямой участок, угол или стык.
[CreateAssetMenu(menuName="Tiles/Laboratory Wall Top")]
public class LabWallTopTile : TileBase
{
    [SerializeField] private Sprite[] shapes = new Sprite[16];
    private static readonly Vector3Int[] Directions={Vector3Int.up,Vector3Int.right,Vector3Int.down,Vector3Int.left};
    public override void GetTileData(Vector3Int position,ITilemap tilemap,ref TileData data)
    {
        int mask=0;
        for(int i=0;i<4;i++)if(tilemap.GetTile(position+Directions[i]) is LabWallTopTile)mask|=1<<i;
        data.sprite=shapes[mask];data.color=Color.white;data.transform=Matrix4x4.identity;
        data.flags=TileFlags.LockTransform;data.colliderType=Tile.ColliderType.Grid;
    }
    public override void RefreshTile(Vector3Int position,ITilemap tilemap)
    {
        tilemap.RefreshTile(position);
        foreach(var direction in Directions)tilemap.RefreshTile(position+direction);
    }
}
