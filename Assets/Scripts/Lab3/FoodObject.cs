using System.Collections;
using UnityEngine;

/// <summary>
/// Вішається на префаб страви (разом з BoxCollider).
/// 1) Позначає об'єкт, з яким можна взаємодіяти жестами
///    (Physics.Raycast знаходить колайдер -> шукаємо цей компонент).
/// 2) Містить анімацію: об'єкт обертається навколо своєї осі
///    і підстрибує. Запускається / зупиняється потрійним тапом.
/// </summary>
public class FoodObject : MonoBehaviour
{
    [Tooltip("Швидкість обертання анімації, градусів за секунду")]
    public float spinSpeed = 120f;

    [Tooltip("Висота підстрибування (частка від розміру об'єкта)")]
    public float bounceHeight = 0.15f;

    [Tooltip("Швидкість підстрибування")]
    public float bounceSpeed = 4f;

    public bool IsAnimating => animationRoutine != null;

    private Coroutine animationRoutine;
    private Transform[] children;
    private Vector3[] childStartPositions;

    private void Awake()
    {
        // Запам'ятовуємо початкові позиції дочірніх мешів (LOD0..LOD2),
        // підстрибують саме вони, а корінь залишається на площині.
        children = new Transform[transform.childCount];
        childStartPositions = new Vector3[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
            childStartPositions[i] = children[i].localPosition;
        }
    }

    /// <summary>Викликається потрійним тапом: старт або стоп анімації.</summary>
    public void ToggleAnimation()
    {
        if (IsAnimating)
            StopAnimation();
        else
            animationRoutine = StartCoroutine(AnimationRoutine());
    }

    public void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        // Повертаємо дочірні об'єкти на місце
        for (int i = 0; i < children.Length; i++)
            children[i].localPosition = childStartPositions[i];
    }

    private IEnumerator AnimationRoutine()
    {
        float height = GetLocalSize() * bounceHeight;
        float time = 0f;

        while (true)
        {
            // Обертання навколо вертикальної осі світу
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            // Підстрибування (|sin| дає "м'ячикові" стрибки)
            time += Time.deltaTime;
            float offset = Mathf.Abs(Mathf.Sin(time * bounceSpeed)) * height;
            for (int i = 0; i < children.Length; i++)
                children[i].localPosition = childStartPositions[i] + Vector3.up * offset;

            yield return null; // чекаємо наступний кадр
        }
    }

    private float GetLocalSize()
    {
        var box = GetComponent<BoxCollider>();
        return box != null ? Mathf.Max(box.size.x, box.size.z) : 1f;
    }

    /// <summary>
    /// Допоміжний метод: чи є під точкою екрана об'єкт FoodObject.
    /// </summary>
    public static FoodObject FindAtScreenPoint(Camera camera, Vector2 screenPosition)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider.GetComponentInParent<FoodObject>();
        return null;
    }
}
