using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// ЛР3, завдання 1: переміщення об'єкта пальцем.
/// Вішається на XR Origin.
/// Began  -> ScreenPointToRay + Physics.Raycast: чи натиснули на об'єкт.
/// Moved  -> ARRaycastManager.Raycast по площинах, об'єкт їде за пальцем.
/// Ended  -> вибір скидається.
/// </summary>
public class ObjectDragController : MonoBehaviour
{
    public Camera arCamera;                  // Камера AR
    public ARRaycastManager raycastManager;  // ARRaycastManager для визначення площини

    private GameObject selectedObject;       // Об'єкт, який ми будемо переміщувати
    private Vector2 touchPosition;           // Позиція дотику

    // Список для результатів raycast
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void OnEnable()  => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Awake()
    {
        if (arCamera == null) arCamera = Camera.main;
        if (raycastManager == null) raycastManager = GetComponent<ARRaycastManager>();
    }

    private void Update()
    {
        // Переміщення - тільки одним пальцем. Два пальці = pinch / обертання.
        if (Touch.activeTouches.Count != 1)
        {
            selectedObject = null;
            return;
        }

        // Отримуємо позицію дотику
        Touch touch = Touch.activeTouches[0];
        touchPosition = touch.screenPosition;

        if (touch.phase == TouchPhase.Began)
        {
            // Промінь з камери через точку дотику
            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            if (Physics.Raycast(ray, out RaycastHit hitObject))
            {
                // Вибираємо лише наші об'єкти (не площини, у яких теж є колайдер)
                FoodObject food = hitObject.collider.GetComponentInParent<FoodObject>();
                if (food != null)
                    selectedObject = food.gameObject;
            }
        }

        if (touch.phase == TouchPhase.Moved && selectedObject != null)
        {
            // Використовуємо Raycast для визначення нової позиції на площині
            if (raycastManager.Raycast(touchPosition, hits, TrackableType.Planes))
            {
                // Беремо перший результат з raycast і встановлюємо позицію об'єкта
                Pose hitPose = hits[0].pose;
                selectedObject.transform.position = hitPose.position;
            }
        }

        // Якщо дотик завершено - обнулюємо вибраний об'єкт
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            selectedObject = null;
        }
    }
}
