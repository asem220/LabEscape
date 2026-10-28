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
    public float FoodFraction => Food / maxFood;
    public float MagicFraction => Magic / maxMagic;

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
        float cost = magicPerSecond * seconds;
        if (Magic < cost || Magic <= 0f) return false;
        Magic = Mathf.Max(0f, Magic - cost);
        Food = Mathf.Max(0f, Food - cost * foodPerMagic);
        return true;
    }

    public bool Eat(float foodAmount, float magicAmount)
    {
        if (Food >= maxFood && Magic >= maxMagic) return false;
        Food = Mathf.Min(maxFood, Food + foodAmount);
        Magic = Mathf.Min(maxMagic, Magic + magicAmount);
        return true;
    }
}
