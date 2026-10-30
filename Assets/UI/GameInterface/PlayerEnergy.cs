using UnityEngine;

// Общий запас ресурсов игрока. Баланс меняется в Inspector.
public class PlayerEnergy : MonoBehaviour
{
    [SerializeField] private float maxFood = 100f;
    [SerializeField] private float maxMagic = 100f;
    [SerializeField] private float hungerPerSecond = 0.08f;
    [SerializeField] private float magicPerSecond = 8f;
    [SerializeField] private float foodPerMagic = 0.35f;

    public float Food { get; private set; }
    public float Magic { get; private set; }
    public float FoodFraction => maxFood > 0f ? Mathf.Clamp01(Food / maxFood) : 0f;
    public float MagicFraction => maxMagic > 0f ? Mathf.Clamp01(Magic / maxMagic) : 0f;

    private void Awake()
    {
        Food = maxFood;
        Magic = maxMagic;
    }

    private void Update()
    {
        Food = Mathf.Max(0f, Food - hungerPerSecond * Time.deltaTime);
    }

    public bool UseTelekinesis(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
        float cost = magicPerSecond * seconds;
        if (Magic < cost || Magic <= 0f) return false;
        Magic = Mathf.Max(0f, Magic - cost);
        Food = Mathf.Max(0f, Food - cost * foodPerMagic);
        return true;
    }

    public bool Eat(float foodAmount, float magicAmount)
    {
        if (foodAmount < 0f || magicAmount < 0f || float.IsNaN(foodAmount) || float.IsNaN(magicAmount)
            || float.IsInfinity(foodAmount) || float.IsInfinity(magicAmount)) return false;
        if (Food >= maxFood && Magic >= maxMagic) return false;
        if ((foodAmount <= 0f || Food >= maxFood) && (magicAmount <= 0f || Magic >= maxMagic)) return false;
        Food = Mathf.Min(maxFood, Food + foodAmount);
        Magic = Mathf.Min(maxMagic, Magic + magicAmount);
        return true;
    }

    // After a guard returns the player, keep enough resources to stand up.
    public void RestoreMinimum(float food, float magic)
    {
        Food = Mathf.Min(maxFood, Mathf.Max(Food, food));
        Magic = Mathf.Min(maxMagic, Mathf.Max(Magic, magic));
    }
}
