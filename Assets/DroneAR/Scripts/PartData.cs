using UnityEngine;

public enum PartType
{
    Frame, Motor, Propeller, FlightController, ESC, PDB, Battery, Receiver, Transmitter, GPS,
    IMU   // added at the END on purpose, so the saved numbers of the other parts do not change
}

public enum Spin { None, CW, CCW }

/// <summary>
/// Text and video for one kind of part. Create the 10 assets automatically with the menu:
/// Drone AR > Create Part Data Assets
/// </summary>
[CreateAssetMenu(menuName = "Drone AR/Part Data", fileName = "PartData")]
public class PartData : ScriptableObject
{
    public PartType type;
    public string displayName;

    public Sprite icon;                                // picture shown on the Training cards (side tray)
    public Sprite iconCW;                              // optional: picture for the clockwise version (motor / propeller)
    public Sprite iconCCW;                             // optional: picture for the counter-clockwise version

    [TextArea(2, 8)] public string info;               // shown in Info mode
    [TextArea(2, 4)] public string whereItGoes;        // short "where does it go" text
    [TextArea(2, 4)] public string wrongDirectionHelp; // only for motors and propellers

    public string videoUrl;                            // opened by the "Watch video" button
}
