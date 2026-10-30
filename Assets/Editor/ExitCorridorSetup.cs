using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Keeps the exterior wall, its foundation, floor edge and collision in sync.
public static class ExitCorridorSetup
{
    private const string ScenePath = "Assets/Scenes/Laboratory.unity";
    private const string WallTiles = "Assets/Environment/WallAssets/Tiles/";

    public static string Widen()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open Laboratory in Edit mode.");

        Transform root = GameObject.Find("Refined Laboratory").transform;
        Tilemap floor = Map(root, "01 Floor");
        Tilemap tops = Map(root, "05 Wall tops");
        Tilemap foundation = Map(root, "Exterior foundation");
        Tilemap collision = Map(root, "06 Wall collision");

        TileBase normalFloor = AssetDatabase.LoadAssetAtPath<TileBase>(
            "Assets/Environment/LabMilk/Tiles/Floors_00.asset");
        TileBase wallTop = AssetDatabase.LoadAssetAtPath<TileBase>(
            "Assets/Environment/WallAssets/WallAutoBrush47.asset");
        TileBase solid = AssetDatabase.LoadAssetAtPath<TileBase>(
            "Assets/Environment/LabMilk/Tiles/WallTops_00.asset");
        if (normalFloor == null || wallTop == null || solid == null)
            throw new InvalidOperationException("Corridor tiles are missing.");

        // The old outer edge pinched the walkable strip down to one cell.
        // Move only that edge two cells east; keep the room wall at x=62.
        for (int y = 9; y <= 30; y++)
        {
            for (int x = 64; x <= 65; x++)
            {
                Vector3Int oldEdge = Cell(x, y);
                tops.SetTile(oldEdge, null);
                foundation.SetTile(oldEdge, null);
                collision.SetTile(oldEdge, null);
            }
            tops.SetTile(Cell(66, y), wallTop);
            collision.SetTile(Cell(66, y), solid);
        }

        // Join the new vertical edge to the old top and bottom borders.
        foreach (int x in new[] { 65, 66 })
        {
            foreach (int y in new[] { 8, 31 })
            {
                tops.SetTile(Cell(x, y), wallTop);
                collision.SetTile(Cell(x, y), solid);
            }
        }

        for (int y = 8; y <= 31; y++)
        for (int x = 63; x <= 66; x++)
            if (!floor.HasTile(Cell(x, y))) floor.SetTile(Cell(x, y), normalFloor);

        // Reuse the project's sliced edge tiles so no full floor tile sticks
        // out from beneath the thinner exterior wall artwork.
        for (int y = 8; y <= 31; y++)
        for (int x = 63; x <= 66; x++)
        {
            Vector3Int p = Cell(x, y);
            int edge = (floor.HasTile(p + Vector3Int.up) ? 0 : 1)
                     | (floor.HasTile(p + Vector3Int.right) ? 0 : 2)
                     | (floor.HasTile(p + Vector3Int.down) ? 0 : 4)
                     | (floor.HasTile(p + Vector3Int.left) ? 0 : 8);
            TileBase tile = edge == 0 ? normalFloor : AssetDatabase.LoadAssetAtPath<TileBase>(
                WallTiles + "FloorBoundary_" + edge.ToString("D2") + ".asset");
            if (tile == null) throw new InvalidOperationException("Missing floor edge " + edge);
            floor.SetTile(p, tile);
        }

        for (int y = 8; y <= 32; y++)
        for (int x = 63; x <= 66; x++)
        {
            Vector3Int p = Cell(x, y);
            if (!tops.HasTile(p))
            {
                foundation.SetTile(p, null);
                continue;
            }
            int mask = (tops.HasTile(p + Vector3Int.up) ? 1 : 0)
                     | (tops.HasTile(p + Vector3Int.right) ? 2 : 0)
                     | (tops.HasTile(p + Vector3Int.down) ? 4 : 0)
                     | (tops.HasTile(p + Vector3Int.left) ? 8 : 0);
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(
                WallTiles + "WallBases_" + mask.ToString("D2") + ".asset");
            if (tile == null) throw new InvalidOperationException("Missing wall base " + mask);
            foundation.SetTile(p, tile);
        }

        floor.RefreshAllTiles();
        tops.RefreshAllTiles();
        foundation.RefreshAllTiles();
        collision.RefreshAllTiles();
        collision.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        collision.GetComponent<CompositeCollider2D>().GenerateGeometry();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Right corridor widened to three cells, y=10..30";
    }

    private static Tilemap Map(Transform root, string name) =>
        root.Find(name).GetComponent<Tilemap>();

    private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);
}
