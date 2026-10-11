using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using TMPro;

public class DroneMasterManager : MonoBehaviour
{
    [System.Serializable]
    public class PartData
    {
        [Tooltip("اسم القطعة المكتوب بالضبط في الـ Hierarchy")]
        public string partTagOrName;

        [Tooltip("العنوان الرئيسي الثابت في أعلى الكانفس")]
        public string titleText;

        [TextArea(2, 4)]
        [Tooltip("نص الوصف (Description)")]
        public string descriptionText;

        [TextArea(2, 4)]
        [Tooltip("نص الوظائف الرئيسية (Key Functions)")]
        public string keyFunctionsText;

        [Tooltip("صورة القطعة التوضيحية")]
        public Sprite partSprite;

        [Tooltip("زمن بداية الفيديو بالثواني")]
        public float videoStartTime;

        [Tooltip("مدة مقطع الفيديو الخاص بهذه القطعة بالثواني")]
        public float videoDuration = 10f;
    }

    [Header("UI References")]
    public GameObject worldCanvas;             // كائن الكانفس الرئيسي
    public TextMeshProUGUI nameTMP;            // العنوان الرئيسي فوق
    public TextMeshProUGUI descriptionTMP;     // كائن النص داخل Content
    public Image partImageDisplay;             // كائن الصورة داخل Content
    public VideoPlayer droneVideoPlayer;       // مشغل الفيديو
    public GameObject playVideoButton;         // زر تشغيل الفيديو
    public Button closeCanvasButton;           // زر الإغلاق X

    [Header("Selection Effect (الإشعاع الدائري)")]
    [Tooltip("الكائن أو الـ Prefab الخاص بتأثير الإشعاع الدائري (Particle System) الذي يظهر حول القطعة")]
    public GameObject selectionRingObject;     
    [Tooltip("إزاحة بسيطة لتعديل مركز ظهور الإشعاع فوق القطعة إذا لزم الأمر")]
    public Vector3 ringOffset = Vector3.zero;

    [Header("Text Styling Settings")]
    [Tooltip("لون العناوين الفرعية (Description / Key Functions)")]
    public Color subTitleColor = new Color(0.75f, 0.52f, 0.99f); 

    [Tooltip("لون النص العادي للوصف والوظائف")]
    public Color textColor = new Color(0.89f, 0.91f, 0.94f);     

    [Tooltip("حجم خط العناوين الفرعية")]
    public int subTitleFontSize = 26;

    [Tooltip("حجم خط النص العادي")]
    public int textFontSize = 18;

    [Header("Drone Controls Reference")]
    public ObjectManipulator droneManipulator; 

    [Header("Drone Parts Database")]
    public PartData[] allPartsData;            // قاعدة بيانات أجزاء الدرون

    [Header("Placement Offset")]
    public Vector3 canvasOffset = new Vector3(0.35f, 0.05f, 0f);

    private Camera mainCam;
    private float currentPartEndTime = 0f;     
    private bool isTrackingVideo = false;

    void Start()
    {
        mainCam = Camera.main;

        if (worldCanvas != null)
        {
            Canvas canvasComp = worldCanvas.GetComponent<Canvas>();
            if (canvasComp != null && canvasComp.worldCamera == null)
            {
                canvasComp.worldCamera = mainCam;
            }
            worldCanvas.SetActive(false);
        }

        // إخفاء حلقة التحديد في البداية
        if (selectionRingObject != null)
        {
            selectionRingObject.SetActive(false);
        }

        if (droneVideoPlayer != null)
        {
            droneVideoPlayer.playOnAwake = false;
        }

        if (closeCanvasButton != null)
        {
            closeCanvasButton.onClick.RemoveAllListeners();
            closeCanvasButton.onClick.AddListener(CloseCanvas);
        }
    }

    void Update()
    {
        // 1. التوقف التلقائي للفيديو عند انتهاء زمن القطعة
        if (isTrackingVideo && droneVideoPlayer != null && droneVideoPlayer.isPlaying)
        {
            if (droneVideoPlayer.time >= currentPartEndTime)
            {
                droneVideoPlayer.Pause();
                isTrackingVideo = false;
                if (playVideoButton != null) playVideoButton.SetActive(true); 
            }
        }

        // 2. فحص النقر واللمس لتحديد أجزاء الدرون
        bool isInputDetected = false;
        Vector3 inputPosition = Vector3.zero;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            isInputDetected = true;
            inputPosition = Input.mousePosition;
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;

            isInputDetected = true;
            inputPosition = Input.GetTouch(0).position;
        }

        if (isInputDetected && mainCam != null)
        {
            Ray ray = mainCam.ScreenPointToRay(inputPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.CompareTag("DronePart") || System.Array.Exists(allPartsData, p => p.partTagOrName == hit.transform.name))
                {
                    ShowPartInformation(hit.transform);
                }
            }
        }
    }

    void ShowPartInformation(Transform clickedPart)
    {
        if (allPartsData == null || allPartsData.Length == 0) return;

        PartData matchedData = System.Array.Find(allPartsData, p => p.partTagOrName == clickedPart.name);

        if (matchedData != null)
        {
            if (nameTMP != null) nameTMP.text = matchedData.titleText;

            if (descriptionTMP != null)
            {
                string subTitleHex = ColorUtility.ToHtmlStringRGB(subTitleColor);
                string textHex = ColorUtility.ToHtmlStringRGB(textColor);

                descriptionTMP.text = 
                    $"<size={subTitleFontSize}><color=#{subTitleHex}><b>Description</b></color></size>\n" +
                    $"<size={textFontSize}><color=#{textHex}>{matchedData.descriptionText}</color></size>\n\n" +
                    $"<size={subTitleFontSize}><color=#{subTitleHex}><b>Key Functions</b></color></size>\n" +
                    $"<size={textFontSize}><color=#{textHex}>{matchedData.keyFunctionsText}</color></size>";
            }

            if (partImageDisplay != null) partImageDisplay.sprite = matchedData.partSprite;

            if (droneVideoPlayer != null)
            {
                droneVideoPlayer.Pause();
                droneVideoPlayer.time = matchedData.videoStartTime;
                currentPartEndTime = matchedData.videoStartTime + matchedData.videoDuration; 
                isTrackingVideo = true;
            }

            if (playVideoButton != null) playVideoButton.SetActive(true);

            // تفعيل وتعديل حجم وموقع "الإشعاع الدائري" بناءً على أبعاد القطعة الفعلية (Bounds)
            if (selectionRingObject != null)
            {
                Bounds partBounds = new Bounds(clickedPart.position, Vector3.one * 0.1f);
                
                Collider col = clickedPart.GetComponent<Collider>();
                if (col != null)
                {
                    partBounds = col.bounds;
                }
                else
                {
                    Renderer rend = clickedPart.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        partBounds = rend.bounds;
                    }
                }

                // ضبط مركز حلقة التحديد في منتصف حدود القطعة
                selectionRingObject.transform.position = partBounds.center + ringOffset;

                // تكبير أو تصغير الحلقة تلقائياً بناءً على أقصى بُعد للقطعة
                float maxDimension = Mathf.Max(partBounds.size.x, Mathf.Max(partBounds.size.y, partBounds.size.z));
                selectionRingObject.transform.localScale = Vector3.one * (maxDimension * 1.2f);

                selectionRingObject.SetActive(true);

                // إعادة تشغيل الـ Particle System لضمان عمله من البداية عند كل اختيار
                ParticleSystem ps = selectionRingObject.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }
            }

            // تموضع الكانفس ومواجهته للكاميرا
            if (worldCanvas != null)
            {
                worldCanvas.transform.position = clickedPart.position + mainCam.transform.right * canvasOffset.x + mainCam.transform.up * canvasOffset.y;
                worldCanvas.transform.rotation = Quaternion.LookRotation(worldCanvas.transform.position - mainCam.transform.position);
                worldCanvas.SetActive(true);
            }

            if (droneManipulator != null)
            {
                droneManipulator.SetCanvasOpenState(true);
            }
        }
    }

    public void ToggleVideoPlay()
    {
        if (droneVideoPlayer == null) return;

        if (droneVideoPlayer.isPlaying)
        {
            droneVideoPlayer.Pause();
            if (playVideoButton != null) playVideoButton.SetActive(true);
        }
        else
        {
            if (droneVideoPlayer.time >= currentPartEndTime)
            {
                PartData matchedData = System.Array.Find(allPartsData, p => p.titleText == nameTMP.text);
                if (matchedData != null)
                {
                    droneVideoPlayer.time = matchedData.videoStartTime;
                }
            }

            droneVideoPlayer.Play();
            isTrackingVideo = true;
            if (playVideoButton != null) playVideoButton.SetActive(false);
        }
    }

    public void CloseCanvas()
    {
        if (worldCanvas != null) worldCanvas.SetActive(false);
        
        // إخفاء وتوقيف حلقة التحديد الإشعاعية عند إغلاق النافذة
        if (selectionRingObject != null)
        {
            ParticleSystem ps = selectionRingObject.GetComponent<ParticleSystem>();
            if (ps != null) ps.Stop();
            selectionRingObject.SetActive(false);
        }

        if (droneVideoPlayer != null) droneVideoPlayer.Pause();
        isTrackingVideo = false;

        if (droneManipulator != null)
        {
            droneManipulator.SetCanvasOpenState(false);
        }
    }
}