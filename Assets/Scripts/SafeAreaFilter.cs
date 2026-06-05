// SafeAreaFitter.cs
using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
    }
}