using UnityEngine;
using UnityEngine.UI;

public class Alert_UI : MonoBehaviour
{
    [Header("References")]
    public Slider alertSlider;

    private void Start()
    {
        if (alertSlider == null)
            alertSlider = GetComponent<Slider>();

        Refresh();

        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.onAlertChanged.AddListener(OnAlertChanged);
    }

    private void OnDestroy()
    {
        if (DungeonAlertSystem.Instance != null)
            DungeonAlertSystem.Instance.onAlertChanged.RemoveListener(OnAlertChanged);
    }

    private void OnAlertChanged(float newValue)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (alertSlider == null || DungeonAlertSystem.Instance == null)
            return;

        alertSlider.minValue = 0f;
        alertSlider.maxValue = DungeonAlertSystem.Instance.maxAlert;
        alertSlider.value = DungeonAlertSystem.Instance.currentAlert;
    }
}