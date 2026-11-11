using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Вішається на текстовий об'єкт (TextMeshPro) на Canvas.
/// Підписується на ARSession.stateChanged і виводить поточний стан сесії.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ARSessionStateDisplay : MonoBehaviour
{
    private TMP_Text stateText;

    private void Awake()
    {
        stateText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        ARSession.stateChanged += OnStateChanged;
        UpdateText(ARSession.state);
    }

    private void OnDisable()
    {
        ARSession.stateChanged -= OnStateChanged;
    }

    private void OnStateChanged(ARSessionStateChangedEventArgs args)
    {
        UpdateText(args.state);
    }

    private void UpdateText(ARSessionState state)
    {
        string description;
        switch (state)
        {
            case ARSessionState.None:               description = "Не ініціалізовано"; break;
            case ARSessionState.Unsupported:        description = "AR не підтримується на цьому пристрої"; break;
            case ARSessionState.CheckingAvailability: description = "Перевірка доступності AR..."; break;
            case ARSessionState.NeedsInstall:       description = "Потрібно встановити AR-сервіси"; break;
            case ARSessionState.Installing:         description = "Встановлення AR-сервісів..."; break;
            case ARSessionState.Ready:              description = "Готово до запуску"; break;
            case ARSessionState.SessionInitializing: description = "Ініціалізація сесії, рухайте телефон"; break;
            case ARSessionState.SessionTracking:    description = "Відстеження працює"; break;
            default:                                description = state.ToString(); break;
        }

        stateText.text = $"Стан AR: {state}\n{description}";
    }
}
