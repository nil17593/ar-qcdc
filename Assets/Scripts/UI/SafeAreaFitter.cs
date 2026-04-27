using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect appliedSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeAreaIfNeeded(force: true);
    }

    private void Update()
    {
        ApplySafeAreaIfNeeded(force: false);
    }

    private void ApplySafeAreaIfNeeded(bool force)
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

        if (!force && safeArea == appliedSafeArea && screenSize == lastScreenSize)
        {
            return;
        }

        Vector2 minAnchor = safeArea.position;
        Vector2 maxAnchor = safeArea.position + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        rectTransform.anchorMin = minAnchor;
        rectTransform.anchorMax = maxAnchor;

        appliedSafeArea = safeArea;
        lastScreenSize = screenSize;
    }
}
