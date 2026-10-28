using UnityEngine;
using UnityEngine.Tilemaps;

// Один тайл-кисть. Углы и края выбираются по соседним клеткам Tilemap.
[CreateAssetMenu(menuName = "Tiles/Hospital Wall")]
public class HospitalWallTile : TileBase
{
    [SerializeField] private Sprite[] variants = new Sprite[256];
    private static readonly Vector3Int[] Neighbours = {
        new Vector3Int(0,1,0), new Vector3Int(1,0,0),
        new Vector3Int(0,-1,0), new Vector3Int(-1,0,0),
        new Vector3Int(1,1,0), new Vector3Int(1,-1,0),
        new Vector3Int(-1,-1,0), new Vector3Int(-1,1,0)
    };

    public static int NormalizeMask(int mask)
    {
        // Диагональ важна лишь тогда, когда присутствуют оба соседних края.
        if ((mask & 3) != 3) mask &= ~16;
        if ((mask & 6) != 6) mask &= ~32;
        if ((mask & 12) != 12) mask &= ~64;
        if ((mask & 9) != 9) mask &= ~128;
        return mask;
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData data)
    {
        int mask = 0;
        for (int i = 0; i < Neighbours.Length; i++)
            if (tilemap.GetTile(position + Neighbours[i]) is HospitalWallTile) mask |= 1 << i;
        data.sprite = variants[NormalizeMask(mask)];
        data.color = Color.white;
        data.transform = Matrix4x4.identity;
        data.flags = TileFlags.LockTransform;
        data.colliderType = Tile.ColliderType.Grid;
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                tilemap.RefreshTile(position + new Vector3Int(x,y,0));
    }
}
