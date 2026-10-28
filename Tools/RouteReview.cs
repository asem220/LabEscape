using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Collections.Generic;

public static class RouteReview
{
    public static object Main()
    {
        Physics2D.SyncTransforms();var root=GameObject.Find("Refined Laboratory");var floor=root.transform.Find("01 Floor").GetComponent<Tilemap>();var player=GameObject.Find("Player");
        var passable=new HashSet<Vector2Int>();
        foreach(var p in floor.cellBounds.allPositionsWithin)
        {
            if(!floor.HasTile(p))continue;
            if(!Physics2D.OverlapCapsuleAll(new Vector2(p.x+.5f,p.y),new Vector2(.66f,.82f),CapsuleDirection2D.Vertical,0).Any(c=>!c.isTrigger&&c.gameObject!=player))passable.Add(new Vector2Int(p.x,p.y));
        }
        var queue=new Queue<Vector2Int>();var visited=new HashSet<Vector2Int>();var start=new Vector2Int(10,4);queue.Enqueue(start);visited.Add(start);
        while(queue.Count>0){var p=queue.Dequeue();foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var n=p+d;if(passable.Contains(n)&&visited.Add(n))queue.Enqueue(n);}}
        var goals=new Dictionary<string,Vector2Int>{{"Canteen",new Vector2Int(17,41)},{"Garden",new Vector2Int(45,39)},{"Experimental",new Vector2Int(18,20)},{"Preparation",new Vector2Int(35,20)},{"Control",new Vector2Int(56,20)},{"Ward",new Vector2Int(10,4)},{"Exit",new Vector2Int(58,4)},{"Outside airlock",new Vector2Int(58,-1)}};
        return new {reachable=goals.Where(g=>visited.Contains(g.Value)).Select(g=>g.Key).ToArray(),unreachable=goals.Where(g=>!visited.Contains(g.Value)).Select(g=>g.Key).ToArray(),walkableCells=visited.Count};
    }
}
