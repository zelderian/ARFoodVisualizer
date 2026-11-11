using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// ЛР3, завдання 3: обертання об'єкта жестом двох пальців.
/// Вішається на XR Origin.
/// Обидва пальці рухаються в ОДНОМУ напрямку -> обертання
/// (у протилежних напрямках - це pinch, його обробляє PinchScaleController).
/// Рух по горизонталі -> обертання навколо вертикальної осі (Y).
/// Рух по вертикалі   -> обертання навколо горизонтальної осі (X).
/// </summary>
public class RotateGestureController : MonoBehaviour
{
    [Tooltip("Скрипт розміщення - звідси беремо розміщений об'єкт")]
    public PlaceOrMoveObject placer;

    public Camera arCamera;              // Камера AR (для горизонтальної осі)
    public float rotationSpeed = 0.2f;   // Швидкість обертання

    [Tooltip("Дозволити обертання навколо горизонтальної осі")]
    public bool rotateHorizontalAxis = true;

    private GameObject selectedObject;   // Об'єкт для обертання

    private void OnEnable()  => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Awake()
    {
        if (placer == null) placer = GetComponent<PlaceOrMoveObject>();
        if (arCamera == null) arCamera = Camera.main;
    }

    private void Update()
    {
        // Перевіряємо, що на екрані рівно 2 пальці
        if (Touch.activeTouches.Count != 2)
            return;

        selectedObject = placer != null ? placer.SpawnedObject : null;
        if (selectedObject == null)
            return;

        Touch touch1 = Touch.activeTouches[0];
        Touch touch2 = Touch.activeTouches[1];

        // Перевіряємо, чи обидва дотики рухаються
        if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
        {
            // Якщо вже йде pinch - не обертаємо
            if (TwoFingerGesture.Current == TwoFingerGesture.Mode.Pinch)
                return;

            // Зміщення пальців за кадр
            Vector2 delta1 = touch1.delta;
            Vector2 delta2 = touch2.delta;

            // Пальці рухаються в одному напрямку (кут між ними < ~25°)
            if (Vector2.Dot(delta1.normalized, delta2.normalized) > 0.9f)
            {
                TwoFingerGesture.Current = TwoFingerGesture.Mode.Rotate;

                // Середнє зміщення по осях X та Y
                float averageDeltaX = (delta1.x + delta2.x) / 2;
                float averageDeltaY = (delta1.y + delta2.y) / 2;

                if (Mathf.Abs(averageDeltaX) >= Mathf.Abs(averageDeltaY))
                {
                    // Горизонтальний рух -> навколо вертикальної осі Y
                    selectedObject.transform.Rotate(0, -averageDeltaX * rotationSpeed, 0, Space.World);
                }
                else if (rotateHorizontalAxis)
                {
                    // Вертикальний рух -> навколо горизонтальної осі X (вісь "вправо" камери)
                    selectedObject.transform.Rotate(arCamera.transform.right, averageDeltaY * rotationSpeed, Space.World);
                }
            }
        }
    }
}
