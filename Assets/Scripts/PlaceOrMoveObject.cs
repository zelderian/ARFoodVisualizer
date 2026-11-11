using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Вішається на XR Origin (поруч з ARRaycastManager).
/// ЛР2: перший тап по площині -> Instantiate об'єкта,
///      наступні тапи по площині -> переміщення вже створеного об'єкта.
/// ЛР3: щоб не конфліктувати з жестами, розміщення спрацьовує лише на
///      КОРОТКИЙ тап ОДНИМ пальцем по площині, НЕ по самому об'єкту
///      (тапи по об'єкту - це подвійний/потрійний тап, перетягування).
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class PlaceOrMoveObject : MonoBehaviour
{
    [Tooltip("Префаб моделі з Asset Store")]
    [SerializeField] private GameObject objectPrefab;

    [Tooltip("Камера AR (для перевірки, чи тап припав на об'єкт)")]
    [SerializeField] private Camera arCamera;

    [Tooltip("Максимальна тривалість тапу, сек")]
    [SerializeField] private float maxTapDuration = 0.3f;

    [Tooltip("Максимальне зміщення пальця за тап, пікселів")]
    [SerializeField] private float maxTapMovement = 30f;

    /// <summary>Розміщений об'єкт (потрібен скриптам масштабування й обертання).</summary>
    public GameObject SpawnedObject => spawnedObject;

    private ARRaycastManager raycastManager;
    private GameObject spawnedObject;

    private float touchStartTime;
    private Vector2 touchStartPosition;
    private int maxFingers;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private void OnEnable()  => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        if (arCamera == null) arCamera = Camera.main;
    }

    private void Update()
    {
        int count = Touch.activeTouches.Count;

        if (count > 0)
        {
            maxFingers = Mathf.Max(maxFingers, count);
            Touch touch = Touch.activeTouches[0];

            if (count == 1 && touch.phase == TouchPhase.Began)
            {
                touchStartTime = Time.time;
                touchStartPosition = touch.screenPosition;
                maxFingers = 1;
            }

            if (count == 1 && touch.phase == TouchPhase.Ended)
            {
                bool isTap = maxFingers == 1
                             && Time.time - touchStartTime <= maxTapDuration
                             && (touch.screenPosition - touchStartPosition).magnitude <= maxTapMovement;
                if (isTap)
                    TryPlace(touch.screenPosition);
            }
            return;
        }

#if UNITY_EDITOR
        // У редакторі - клік мишею
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryPlace(Mouse.current.position.ReadValue());
#endif
    }

    private void TryPlace(Vector2 screenPosition)
    {
        // Не реагуємо на натискання по UI (кнопка, текст)
        if (IsOverUI(screenPosition))
            return;

        // Тап по самому об'єкту - це жест для об'єкта, а не переміщення
        if (FoodObject.FindAtScreenPoint(arCamera, screenPosition) != null)
            return;

        // Raycast з точки дотику; PlaneWithinPolygon = лише всередині виявленої площини
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                // Перше натискання - створюємо об'єкт
                spawnedObject = Instantiate(objectPrefab, hitPose.position, hitPose.rotation);
                EnsureInteractable(spawnedObject);
            }
            else
            {
                // Повторне натискання - переміщуємо існуючий об'єкт
                // (обертання й масштаб, задані жестами, зберігаються)
                spawnedObject.transform.position = hitPose.position;
            }
        }
    }

    /// <summary>
    /// Страховка: якщо на префабі немає колайдера або FoodObject - додаємо їх,
    /// інакше Physics.Raycast не знайде об'єкт.
    /// </summary>
    private static void EnsureInteractable(GameObject obj)
    {
        if (obj.GetComponent<Collider>() == null)
        {
            var box = obj.AddComponent<BoxCollider>();
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            foreach (var r in obj.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(r.bounds);
            box.center = obj.transform.InverseTransformPoint(bounds.center);
            Vector3 s = obj.transform.lossyScale;
            box.size = new Vector3(bounds.size.x / s.x, bounds.size.y / s.y, bounds.size.z / s.z);
        }

        if (obj.GetComponent<FoodObject>() == null)
            obj.AddComponent<FoodObject>();
    }

    private bool IsOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var data = new PointerEventData(EventSystem.current) { position = screenPosition };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
        return uiHits.Count > 0;
    }
}
