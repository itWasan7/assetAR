using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One picture card. It starts in the side tray. The student drags it onto the drone.
/// It can be dragged again to fix a mistake. Put this on the card prefab (the root needs an Image).
/// </summary>
public class PartCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TMP_Text label;                              // optional: the name text on the card

    [HideInInspector] public PartData data;
    [HideInInspector] public Spin spin = Spin.None;
    [HideInInspector] public TrainingSlot currentSlot;

    TrainingUI ui;
    RectTransform rt;
    CanvasGroup group;
    Transform trayParent;
    int trayIndex;
    bool dragging;

    public void Init(TrainingUI owner, PartData d, Spin s)
    {
        ui = owner;
        data = d;
        spin = s;

        rt = (RectTransform)transform;
        rt.pivot = new Vector2(0.5f, 0.5f);

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        Image img = GetComponent<Image>();
        Sprite pic = d.icon;
        if (s == Spin.CW && d.iconCW != null) pic = d.iconCW;
        else if (s == Spin.CCW && d.iconCCW != null) pic = d.iconCCW;

        if (img != null && pic != null)
        {
            img.sprite = pic;
            img.preserveAspect = true;
        }

        if (label != null)
            label.text = s == Spin.None ? d.displayName : d.displayName + " (" + s + ")";

        trayParent = transform.parent;
        trayIndex = transform.GetSiblingIndex();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (ui == null || !ui.IsRunning) return;

        // picking up a picture that is already on the drone: free its spot first
        if (currentSlot != null)
        {
            TrainingSlot s = currentSlot;
            currentSlot = null;
            s.Clear();
        }

        dragging = true;
        ui.BeginDragCard(this);
        group.blocksRaycasts = false;
        ui.SetScreenPos(rt, e.position);
        ui.Refresh();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        ui.SetScreenPos(rt, e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        group.blocksRaycasts = true;
        ui.DropCard(this, e.position);
    }

    /// <summary>Put the card on a full-screen layer, centered, at the given size.</summary>
    public void MoveToLayer(RectTransform layer, Vector2 size)
    {
        transform.SetParent(layer, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        transform.SetAsLastSibling();
    }

    public void ReturnToTray()
    {
        if (currentSlot != null)
        {
            TrainingSlot s = currentSlot;
            currentSlot = null;
            s.Clear();
        }

        transform.SetParent(trayParent, false);
        transform.SetSiblingIndex(Mathf.Min(trayIndex, trayParent.childCount - 1));
        gameObject.SetActive(true);
        rt.localScale = Vector3.one;

        if (ui != null) ui.Refresh();
    }
}
