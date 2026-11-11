using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// ЛР3, завдання 2: масштабування жестом "щипка" (pinch-to-zoom).
/// Вішається на XR Origin. Масштабує розміщений об'єкт.
/// </summary>
public class PinchScaleController : MonoBehaviour
{
    [Tooltip("Скрипт розміщення - звідси беремо розміщений об'єкт")]
    public PlaceOrMoveObject placer;

    public float minScale = 0.05f;
    public float maxScale = 1.5f;

    [Tooltip("На скільки пікселів має змінитись відстань між пальцями, щоб жест вважався pinch")]
    public float pinchThreshold = 15f;

    private GameObject selectedObject;  // Об'єкт, який будемо масштабувати
    private float initialDistance;      // Початкова відстань між пальцями
    private Vector3 initialScale;       // Початковий масштаб об'єкта

    private void OnEnable()  => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Awake()
    {
        if (placer == null) placer = GetComponent<PlaceOrMoveObject>();
    }

    private void Update()
    {
        // Перевіряємо, що на екрані рівно 2 пальці
        if (Touch.activeTouches.Count != 2)
        {
            TwoFingerGesture.Current = TwoFingerGesture.Mode.None;
            return;
        }

        selectedObject = placer != null ? placer.SpawnedObject : null;
        if (selectedObject == null)
            return;

        Touch touchZero = Touch.activeTouches[0];
        Touch touchOne = Touch.activeTouches[1];

        // Один з пальців щойно торкнувся - запам'ятовуємо початкові дані
        if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
        {
            initialDistance = Vector2.Distance(touchZero.screenPosition, touchOne.screenPosition);
            initialScale = selectedObject.transform.localScale;
            TwoFingerGesture.Current = TwoFingerGesture.Mode.None;
            return;
        }

        // Під час руху пальців змінюємо масштаб
        if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
        {
            // Якщо вже йде обертання - не масштабуємо
            if (TwoFingerGesture.Current == TwoFingerGesture.Mode.Rotate)
                return;

            // Поточна відстань між пальцями
            float currentDistance = Vector2.Distance(touchZero.screenPosition, touchOne.screenPosition);

            if (Mathf.Approximately(initialDistance, 0))
                return; // уникаємо ділення на нуль

            // Жест ще не визначено: вважаємо його pinch, лише коли відстань
            // помітно змінилась (при обертанні пальці йдуть паралельно
            // і відстань майже не змінюється)
            if (TwoFingerGesture.Current == TwoFingerGesture.Mode.None)
            {
                if (Mathf.Abs(currentDistance - initialDistance) < pinchThreshold)
                    return;
                TwoFingerGesture.Current = TwoFingerGesture.Mode.Pinch;
            }

            // Обчислюємо фактор масштабування
            float scaleFactor = currentDistance / initialDistance;

            // Застосовуємо масштабування до об'єкта (з обмеженням min/max)
            Vector3 newScale = initialScale * scaleFactor;
            float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);
            selectedObject.transform.localScale = newScale * (clamped / newScale.x);
        }
    }
}
