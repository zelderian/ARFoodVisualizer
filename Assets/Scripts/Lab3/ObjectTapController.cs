using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// ЛР3, завдання 4 і 5: реакція на тапи по об'єкту.
/// Вішається на XR Origin.
/// Подвійний тап по об'єкту -> змінюється текстура (наступна з масиву).
/// Потрійний тап по об'єкту -> запускається / зупиняється анімація.
///
/// Розпізнавання через таймер: рахуємо короткі тапи по об'єкту; якщо
/// за multiTapDelay секунд новий тап не прийшов - серія завершена і
/// дивимось, скільки було тапів. Завдяки цьому потрійний тап
/// не спрацьовує спочатку як подвійний.
/// </summary>
public class ObjectTapController : MonoBehaviour
{
    public Camera arCamera;  // Камера AR

    [Tooltip("Текстури, які по черзі накладаються на об'єкт подвійним тапом")]
    public Texture[] textures;

    [Tooltip("Максимальна пауза між тапами в серії, сек")]
    public float multiTapDelay = 0.35f;

    [Tooltip("Максимальна тривалість одного тапу, сек")]
    public float maxTapDuration = 0.3f;

    [Tooltip("Максимальне зміщення пальця за тап, пікселів")]
    public float maxTapMovement = 30f;

    private int tapCount;           // Скільки тапів у поточній серії
    private float lastTapTime;      // Час останнього тапу
    private FoodObject tappedObject;
    private int textureIndex;

    private float touchStartTime;
    private Vector2 touchStartPosition;
    private int maxFingers;

    private void OnEnable()  => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Awake()
    {
        if (arCamera == null) arCamera = Camera.main;
    }

    private void Update()
    {
        DetectTap();

        // Серія тапів завершилась (пауза довша за multiTapDelay)
        if (tapCount > 0 && Time.time - lastTapTime > multiTapDelay)
        {
            if (tappedObject != null)
            {
                if (tapCount == 2)
                    ChangeTexture(tappedObject);        // подвійний тап
                else if (tapCount >= 3)
                    tappedObject.ToggleAnimation();     // потрійний тап
            }
            tapCount = 0;
        }
    }

    private void DetectTap()
    {
        int count = Touch.activeTouches.Count;
        if (count == 0)
            return;

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
            // Тап = короткий дотик одним пальцем майже без руху
            bool isTap = maxFingers == 1
                         && Time.time - touchStartTime <= maxTapDuration
                         && (touch.screenPosition - touchStartPosition).magnitude <= maxTapMovement;
            if (!isTap)
                return;

            // Чи натиснули саме на об'єкт (Physics.Raycast по колайдеру)
            FoodObject food = FoodObject.FindAtScreenPoint(arCamera, touch.screenPosition);
            if (food == null)
                return;

            if (food != tappedObject)
                tapCount = 0;

            tappedObject = food;
            tapCount++;
            lastTapTime = Time.time;
        }
    }

    /// <summary>Наступна текстура з масиву на всі меші об'єкта (LOD0..LOD2).</summary>
    private void ChangeTexture(FoodObject food)
    {
        if (textures == null || textures.Length == 0)
            return;

        textureIndex = (textureIndex + 1) % textures.Length;

        foreach (Renderer rend in food.GetComponentsInChildren<Renderer>())
        {
            // renderer.material створює копію матеріалу саме для цього об'єкта
            rend.material.mainTexture = textures[textureIndex];
        }
    }
}
