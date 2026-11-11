using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Вішається на XR Origin (або будь-який об'єкт з посиланням на ARPlaneManager).
/// Метод TogglePlanes() призначається в On Click() кнопки.
/// Детекція площин НЕ вимикається - ховається лише візуалізація,
/// тому raycast і розміщення об'єкта продовжують працювати.
/// </summary>
public class PlaneVisibilityToggle : MonoBehaviour
{
    [SerializeField] private ARPlaneManager planeManager;
    [Tooltip("Необов'язково: текст на кнопці")]
    [SerializeField] private TMP_Text buttonLabel;

    private bool planesVisible = true;

    private void Awake()
    {
        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();
    }

    private void Start()
    {
        UpdateLabel();
    }

    public void TogglePlanes()
    {
        planesVisible = !planesVisible;
        ApplyVisibility();
        UpdateLabel();
    }

    private void Update()
    {
        if (!planesVisible)
            ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        foreach (ARPlane plane in planeManager.trackables)
        {
            if (plane.gameObject.activeSelf != planesVisible)
                plane.gameObject.SetActive(planesVisible);
        }
    }

    private void UpdateLabel()
    {
        if (buttonLabel != null)
            buttonLabel.text = planesVisible ? "Сховати площини" : "Показати площини";
    }
}
