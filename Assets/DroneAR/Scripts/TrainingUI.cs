using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Training, step 1: press Start, pictures appear on the side, drag them onto the drone.
/// A wrong picture in a spot shows a red mark. Tap the red mark to read the mistake.
/// There is no flight animation yet. The event OnAllCorrect is where the animation plugs in later.
/// One picture is made for every TrainingSlot, so every picture has a correct home.
/// Needs the TextMeshPro package, a Canvas, and an EventSystem.
/// </summary>
public class TrainingUI : MonoBehaviour
{
    [Header("AR")]
    public Camera arCamera;                 // the AR camera (if empty, Camera.main is used)
    public TrainingSlot[] slots;            // all the spots on the drone
    public PartData[] partDatabase;         // the 10 PartData assets

    [Header("Buttons")]
    public Button startButton;              // the Training button at the bottom
    public Button resetButton;              // only visible while training
    public Button exitButton;               // only visible while training

    [Header("Side tray")]
    public GameObject trayPanel;            // the whole side panel
    public RectTransform trayContent;       // the object that has the Grid Layout Group
    public PartCard cardPrefab;

    [Header("Full-screen layers (empty objects under the Canvas, stretched to fill)")]
    public RectTransform slotLayer;         // placed pictures and hints live here
    public RectTransform topLayer;          // the dragged picture and the red marks live here

    [Header("Prefabs")]
    public GameObject slotHintPrefab;       // optional: small circle that shows where a spot is
    public Button errorMarkerPrefab;        // the red mark (a Button)

    [Header("Error panel")]
    public GameObject errorPanel;
    public TMP_Text errorTitle;
    public TMP_Text errorBody;
    public Button closeErrorButton;

    [Header("Popup picture and success message")]
    public Image popupImage;                // an Image inside the error panel (shows the picture)
    public Sprite wrongSprite;              // picture shown with a mistake (COMPropellers)
    public Sprite correctSprite;            // picture shown when everything is right (COMPropellers)
    public string successTitle = "Correct!";
    [TextArea(2, 6)]
    public string successBody = "Well done! Every part is in the right place.\n\nThe propellers on the diagonal spin the same way, so the drone can fly stable.";

    [Header("Optional")]
    public TMP_Text statusText;             // shows "Installed correctly: 3 / 14"

    [Header("Merge with the Info mode (optional)")]
    public ObjectManipulator droneManipulator;      // stops the drone from rotating/zooming while the student drags cards
    public Behaviour[] disableWhileTraining;        // e.g. DroneMasterManager (so tapping the drone does not open the info canvas)
    public GameObject[] hideWhileTraining;          // e.g. the info canvas / side buttons

    [Header("Settings")]
    public Vector2 placedCardSize = new Vector2(90f, 90f);
    public float snapRadiusScreenFraction = 0.08f;   // how close to a spot you must drop (fraction of screen height)
    public Vector2 markerOffset = new Vector2(40f, 40f);

    /// <summary>Fires once when every spot is correct. The flight animation will listen to this later.</summary>
    public event Action OnAllCorrect;

    public bool IsRunning { get; private set; }

    Canvas canvas;
    readonly List<PartCard> cards = new List<PartCard>();
    GameObject[] hints;
    Button[] markers;
    bool wasComplete;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;

        if (startButton != null) startButton.onClick.AddListener(StartTraining);
        if (resetButton != null) resetButton.onClick.AddListener(ResetPlacements);
        if (exitButton != null) exitButton.onClick.AddListener(ExitTraining);
        if (closeErrorButton != null) closeErrorButton.onClick.AddListener(() => errorPanel.SetActive(false));

        ApplyRunningState(false);
    }

    // ---------- Helpers ----------

    public Camera UICam
    {
        get
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera;
        }
    }

    Camera WorldCam()
    {
        return arCamera != null ? arCamera : Camera.main;
    }

    /// <summary>Move a UI object so it sits on a screen point (works for any Canvas mode).</summary>
    public void SetScreenPos(RectTransform rt, Vector2 screen)
    {
        RectTransform parent = rt.parent as RectTransform;
        if (parent == null) return;
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, UICam, out local))
            rt.localPosition = local;
    }

    public PartData GetData(PartType t)
    {
        foreach (PartData d in partDatabase)
            if (d != null && d.type == t) return d;
        return null;
    }

    bool CheckSetup()
    {
        bool ok = true;
        if (slots == null || slots.Length == 0) { Debug.LogError("TrainingUI: no slots assigned."); ok = false; }
        if (partDatabase == null || partDatabase.Length == 0) { Debug.LogError("TrainingUI: Part Database is empty. Drag the 10 PartData assets into it."); ok = false; }
        if (cardPrefab == null) { Debug.LogError("TrainingUI: Card Prefab is empty."); ok = false; }
        if (trayContent == null) { Debug.LogError("TrainingUI: Tray Content is empty."); ok = false; }
        if (slotLayer == null) { Debug.LogError("TrainingUI: Slot Layer is empty."); ok = false; }
        if (topLayer == null) { Debug.LogError("TrainingUI: Top Layer is empty."); ok = false; }
        if (errorMarkerPrefab == null) { Debug.LogError("TrainingUI: Error Marker Prefab is empty."); ok = false; }
        if (errorPanel == null || errorTitle == null || errorBody == null) { Debug.LogError("TrainingUI: Error panel fields are empty."); ok = false; }
        if (WorldCam() == null) { Debug.LogError("TrainingUI: no camera. Assign the AR Camera or tag it MainCamera."); ok = false; }
        return ok;
    }

    // ---------- Start / Reset / Exit ----------

    public void StartTraining()
    {
        if (!CheckSetup()) return;

        ClearAll();
        BuildHints();
        BuildCards();
        wasComplete = false;
        ApplyRunningState(true);
        SetOtherModes(true);
        Refresh();
    }

    /// <summary>For an external button (e.g. the colleague's TriningB): first press starts, second press exits.</summary>
    public void ToggleTraining()
    {
        if (IsRunning) ExitTraining();
        else StartTraining();
    }

    public void ResetPlacements()
    {
        foreach (PartCard c in cards)
            if (c != null) c.ReturnToTray();
        if (errorPanel != null) errorPanel.SetActive(false);
        Refresh();
    }

    public void ExitTraining()
    {
        ClearAll();
        ApplyRunningState(false);
        SetOtherModes(false);
    }

    void SetOtherModes(bool training)
    {
        if (droneManipulator != null) droneManipulator.SetCanvasOpenState(training);
        if (disableWhileTraining != null)
            foreach (Behaviour b in disableWhileTraining) if (b != null) b.enabled = !training;
        if (hideWhileTraining != null)
            foreach (GameObject g in hideWhileTraining) if (g != null) g.SetActive(!training);
    }

    void ApplyRunningState(bool running)
    {
        IsRunning = running;
        if (trayPanel != null) trayPanel.SetActive(running);
        if (startButton != null) startButton.gameObject.SetActive(!running);
        if (resetButton != null) resetButton.gameObject.SetActive(running);
        if (exitButton != null) exitButton.gameObject.SetActive(running);
        if (statusText != null) statusText.gameObject.SetActive(running);
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    void ClearAll()
    {
        foreach (PartCard c in cards)
        {
            if (c == null) continue;
            c.transform.SetParent(null, false);   // leave the tray now, so new cards get correct positions in the same frame
            Destroy(c.gameObject);
        }
        cards.Clear();

        if (hints != null)
            foreach (GameObject h in hints)
                if (h != null) Destroy(h);
        if (markers != null)
            foreach (Button m in markers)
                if (m != null) Destroy(m.gameObject);
        hints = null;
        markers = null;

        if (slots != null)
            foreach (TrainingSlot s in slots)
                if (s != null) s.Clear();
    }

    void BuildHints()
    {
        hints = new GameObject[slots.Length];
        markers = new Button[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            if (slotHintPrefab != null)
            {
                hints[i] = Instantiate(slotHintPrefab, slotLayer);
                hints[i].SetActive(false);
            }

            Button m = Instantiate(errorMarkerPrefab, topLayer);
            TrainingSlot slot = slots[i];
            m.onClick.AddListener(() => ShowError(slot));
            m.gameObject.SetActive(false);
            markers[i] = m;
        }
    }

    void BuildCards()
    {
        // one picture per spot, in random order
        List<TrainingSlot> order = new List<TrainingSlot>(slots);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            TrainingSlot tmp = order[i];
            order[i] = order[j];
            order[j] = tmp;
        }

        foreach (TrainingSlot s in order)
        {
            PartData d = GetData(s.expectedType);
            if (d == null)
            {
                Debug.LogWarning("TrainingUI: no PartData for " + s.expectedType + " in Part Database.");
                continue;
            }

            PartCard c = Instantiate(cardPrefab, trayContent);
            c.Init(this, d, s.expectedSpin);
            cards.Add(c);
        }
    }

    // ---------- Dragging ----------

    public void BeginDragCard(PartCard card)
    {
        card.MoveToLayer(topLayer, placedCardSize);
    }

    public void DropCard(PartCard card, Vector2 screenPos)
    {
        Camera cam = WorldCam();
        TrainingSlot best = null;
        float bestDist = Screen.height * snapRadiusScreenFraction;

        if (cam != null)
        {
            foreach (TrainingSlot s in slots)
            {
                Vector2 p;
                if (!s.TryGetScreenPos(cam, out p)) continue;
                float d = Vector2.Distance(p, screenPos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }
        }

        if (best == null)
        {
            card.ReturnToTray();
            return;
        }

        best.Place(card);
        card.MoveToLayer(slotLayer, placedCardSize);
        Refresh();
    }

    // ---------- Keep pictures, hints and red marks on top of the drone ----------

    void LateUpdate()
    {
        if (!IsRunning || hints == null || markers == null) return;
        Camera cam = WorldCam();
        if (cam == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            TrainingSlot s = slots[i];
            Vector2 pos;
            bool visible = s.TryGetScreenPos(cam, out pos);
            bool hasCard = s.Occupant != null;

            if (hints[i] != null)
            {
                hints[i].SetActive(visible && !hasCard);
                if (visible) SetScreenPos((RectTransform)hints[i].transform, pos);
            }

            if (hasCard)
            {
                s.Occupant.gameObject.SetActive(visible);
                if (visible) SetScreenPos((RectTransform)s.Occupant.transform, pos);
            }

            if (markers[i] != null)
            {
                bool wrong = hasCard && s.Check() != SlotCheck.Correct;
                markers[i].gameObject.SetActive(wrong && visible);
                if (wrong && visible) SetScreenPos((RectTransform)markers[i].transform, pos + markerOffset);
            }
        }
    }

    // ---------- Progress ----------

    public void Refresh()
    {
        if (slots == null) return;

        int ok = 0;
        foreach (TrainingSlot s in slots)
            if (s != null && s.IsCorrect) ok++;

        if (statusText != null)
            statusText.text = "Installed correctly: " + ok + " / " + slots.Length;

        bool complete = slots.Length > 0 && ok == slots.Length;
        if (complete && !wasComplete)
        {
            ShowPopup(successTitle, successBody, correctSprite);
            if (OnAllCorrect != null) OnAllCorrect();
        }
        wasComplete = complete;
    }

    // ---------- Red mark messages (simple English for students) ----------

    void ShowError(TrainingSlot s)
    {
        if (s.Occupant == null) return;
        ShowPopup(BuildErrorTitle(s), BuildErrorText(s), wrongSprite);
    }

    void ShowPopup(string title, string body, Sprite picture)
    {
        if (errorPanel == null || errorTitle == null || errorBody == null) return;

        errorTitle.text = title;
        errorBody.text = body;

        if (popupImage != null)
        {
            popupImage.sprite = picture;
            popupImage.preserveAspect = true;
            popupImage.gameObject.SetActive(picture != null);
        }

        errorPanel.SetActive(true);
        errorPanel.transform.SetAsLastSibling();   // always on top of everything
    }

    string BuildErrorTitle(TrainingSlot s)
    {
        return s.Check() == SlotCheck.WrongDirection ? "Wrong spin direction" : "Wrong place";
    }

    string BuildErrorText(TrainingSlot s)
    {
        PartData placed = s.Occupant.data;

        if (s.Check() == SlotCheck.WrongDirection)
        {
            string help = string.IsNullOrEmpty(placed.wrongDirectionHelp)
                ? "This part spins the wrong way."
                : placed.wrongDirectionHelp;
            return help + "\n\nThis spot needs a " + s.expectedSpin + " part (" + s.slotLabel + ")."
                 + "\n\nDrag the picture away and try again.";
        }

        PartData expected = GetData(s.expectedType);
        string expectedName = expected != null ? expected.displayName : s.expectedType.ToString();

        return placed.displayName + " does not go here.\n"
             + "This spot is for the " + expectedName + " (" + s.slotLabel + ").\n\n"
             + "Where the " + placed.displayName + " goes:\n" + placed.whereItGoes
             + "\n\nDrag the picture away and try again.";
    }
}
