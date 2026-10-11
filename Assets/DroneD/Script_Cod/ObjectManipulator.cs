using UnityEngine;

public class ObjectManipulator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 1.2f;

    [Header("Pinch Zoom Settings")]
    public float zoomSpeed = 0.015f;
    public float buttonZoomStep = 0.15f;

    [Header("Zoom Multipliers")]
    public float minScaleMultiplier = 1.0f;
    public float maxScaleMultiplier = 2.5f;

    [Header("Control States")]
    [Tooltip("قفل يدوي عبر زر RotateLock_Button")]
    public bool isManualLocked = false; 

    private bool isCanvasOpen = false;
    private Vector3 previousMousePosition;
    
    // التخزين للوضع الأصلي (للإعادة عند الضغط على زر Reset)
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    
    private float minScale;
    private float maxScale;

    void Start()
    {
        // حفظ الوضعية والقياسات الاصلية للدرون عند بدء التشغيل
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;

        minScale = initialScale.x * minScaleMultiplier;
        maxScale = initialScale.x * maxScaleMultiplier;
    }

    void Update()
    {
        // يتوقف التدوير والزوم إذا كان هناك قفل يدوي من الزر أو إذا كان الكانفس مفتوحاً
        if (isManualLocked || isCanvasOpen) return;

        #if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            previousMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - previousMousePosition;
            
            // تدوير متوازي وسلس حول محور العالم لضمان عدم حدوث ميلان عشوائي
            transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
            transform.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
            
            previousMousePosition = Input.mousePosition;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            ApplyZoom(scroll * 0.5f);
        }

        #else
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
                transform.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            ApplyZoom(difference * zoomSpeed);
        }
        #endif
    }

    public void ApplyZoom(float increment)
    {
        float newScaleX = Mathf.Clamp(transform.localScale.x + increment, minScale, maxScale);
        float scaleFactor = newScaleX / initialScale.x;
        transform.localScale = initialScale * scaleFactor;
    }

    public void ZoomIn() => ApplyZoom(buttonZoomStep);
    public void ZoomOut() => ApplyZoom(-buttonZoomStep);

    // دالة إعادة الدرون لوضعه الأصلي تماماً (الموقع، الدوران، والحجم)
    public void ResetToOriginalTransform()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;
    }

    // دالة خاصة بشرط فتح/إغلاق الكانفس
    public void SetCanvasOpenState(bool isOpen)
    {
        isCanvasOpen = isOpen;
    }

    // دالة ربط زر RotateLock_Button الجانبي
    public void ToggleManualLock()
    {
        isManualLocked = !isManualLocked;
    }
}