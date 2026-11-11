using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Вішається на XR Origin (поруч з ARRaycastManager).
/// Перший дотик на площину -> Instantiate об'єкта.
/// Наступні дотики -> переміщення вже створеного об'єкта.
/// Використовує новий Input System (Touchscreen / Mouse в редакторі).
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class PlaceOrMoveObject : MonoBehaviour
{
    [Tooltip("Префаб моделі з Asset Store")]
    [SerializeField] private GameObject objectPrefab;

    private ARRaycastManager raycastManager;
    private GameObject spawnedObject;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    private void Update()
    {
        if (!TryGetPressPosition(out Vector2 touchPosition))
            return;

        if (IsOverUI(touchPosition))
            return;

        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {

                spawnedObject = Instantiate(objectPrefab, hitPose.position, hitPose.rotation);
            }
            else
            {

                spawnedObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            }
        }
    }

    /// <summary>
    /// Позиція початку дотику (телефон) або кліку мишею (редактор).
    /// </summary>
    private bool TryGetPressPosition(out Vector2 position)
    {
        position = default;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
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