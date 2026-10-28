using System;
using System.Threading.Tasks;
using UnityEngine;

public static class PhysicsReview
{
    public static async Task<object> Main()
    {
        if(!Application.isPlaying)throw new Exception("Requires a running game.");
        var player=GameObject.Find("Player");var movement=player.GetComponent<PlayerMovement>();
        var body=player.GetComponent<Rigidbody2D>();
        var initial=body.position;bool wasEnabled=movement.enabled;
        int firstFrame=Time.frameCount;
        movement.enabled=false;
        Vector2 vertical,horizontal,door;
        try
        {
            body.position=new Vector2(25.5f,23.5f);body.linearVelocity=Vector2.right*3;
            await Task.Delay(1500);vertical=body.position;
            body.linearVelocity=Vector2.zero;
            if(vertical.x>26.75f||vertical.x<26)throw new Exception("Vertical wall did not stop the player: "+vertical);
            body.position=new Vector2(20.5f,10.5f);body.linearVelocity=Vector2.up*3;
            await Task.Delay(1500);horizontal=body.position;
            body.linearVelocity=Vector2.zero;
            if(horizontal.y>12.2f||horizontal.y<11.8f)throw new Exception("Horizontal wall did not stop the player: "+horizontal);
            body.position=new Vector2(16.5f,10.5f);body.linearVelocity=Vector2.up*3;
            await Task.Delay(1500);door=body.position;
            if(door.y<14)throw new Exception("Doorway was blocked: "+door);
            if(Time.frameCount<=firstFrame)throw new Exception("Game frames did not advance.");
            return new {verticalWall=vertical.ToString(),horizontalWall=horizontal.ToString(),openPassage=door.ToString(),framesAdvanced=Time.frameCount-firstFrame};
        }
        finally
        {
            body.linearVelocity=Vector2.zero;body.position=initial;movement.enabled=wasEnabled;
        }
    }
}
