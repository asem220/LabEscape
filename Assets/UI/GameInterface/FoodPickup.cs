using UnityEngine;

// Паёк съедается по E рядом с игроком, только если ресурсы не полны.
public class FoodPickup : MonoBehaviour
{
    [SerializeField] private PlayerEnergy player;
    [SerializeField] private float eatDistance = 1.1f;
    [SerializeField] private float foodRestored = 35f;
    [SerializeField] private float magicRestored = 45f;
    public bool IsNearby => player != null && Vector2.Distance(transform.position, player.transform.position) <= eatDistance;

    public bool TryEat()
    {
        if (Time.timeScale == 0f || !IsNearby) return false;
        if (player.Eat(foodRestored, magicRestored))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}
