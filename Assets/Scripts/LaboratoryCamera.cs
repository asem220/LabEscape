using UnityEngine;

// Камера следует за игроком и не показывает пустоту за краями комплекса.
public class LaboratoryCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 levelMin = Vector2.zero;
    [SerializeField] private Vector2 levelMax = new Vector2(88f, 65f);
    private Camera view;

    private void Awake() => view = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (target == null) return;
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * view.aspect;
        float x = Mathf.Clamp(target.position.x, levelMin.x + halfWidth, levelMax.x - halfWidth);
        float y = Mathf.Clamp(target.position.y, levelMin.y + halfHeight, levelMax.y - halfHeight);
        transform.position = new Vector3(x, y, -10f);
    }
}
