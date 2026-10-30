using UnityEngine;

// Two small trigger rectangles follow the L-shaped corridor by the exit.
public class CombatArea : MonoBehaviour
{
    [SerializeField] private BoxCollider2D[] regions;

    public bool Contains(Vector2 position)
    {
        if (regions == null) return false;
        foreach (BoxCollider2D region in regions)
            if (region != null && region.OverlapPoint(position)) return true;
        return false;
    }
}
