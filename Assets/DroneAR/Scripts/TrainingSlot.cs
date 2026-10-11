using UnityEngine;

public enum SlotCheck { Empty, Correct, WrongPlace, WrongDirection }

/// <summary>
/// A spot on the drone where one picture should be dropped (for example "Front-left motor").
/// Make it an empty child object of the drone, and move it to the real place of the part.
/// </summary>
public class TrainingSlot : MonoBehaviour
{
    public PartType expectedType;
    public Spin expectedSpin = Spin.None;   // CW / CCW for motor and propeller spots
    public string slotLabel = "";           // e.g. "Front-left motor"
    public Vector2 iconOffset = Vector2.zero; // move the picture on the screen (pixels). Useful for stacked parts
    public float gizmoSize = 0.02f;         // only for the Scene view (small colored circle)

    public PartCard Occupant { get; private set; }

    public SlotCheck Check()
    {
        if (Occupant == null || Occupant.data == null) return SlotCheck.Empty;
        if (Occupant.data.type != expectedType) return SlotCheck.WrongPlace;
        if (expectedSpin != Spin.None && Occupant.spin != expectedSpin) return SlotCheck.WrongDirection;
        return SlotCheck.Correct;
    }

    public bool IsCorrect
    {
        get { return Check() == SlotCheck.Correct; }
    }

    public void Place(PartCard card)
    {
        if (Occupant != null && Occupant != card)
            Occupant.ReturnToTray();   // kicks the old picture out (it calls Clear on this spot)

        Occupant = card;
        card.currentSlot = this;
    }

    public void Clear()
    {
        Occupant = null;
    }

    /// <summary>Where this spot is on the phone screen. False if it is behind the camera.</summary>
    public bool TryGetScreenPos(Camera cam, out Vector2 pos)
    {
        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        pos = new Vector2(sp.x, sp.y) + iconOffset;
        return sp.z > 0f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = expectedSpin == Spin.CW ? Color.cyan : (expectedSpin == Spin.CCW ? Color.yellow : Color.white);
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
    }
}
