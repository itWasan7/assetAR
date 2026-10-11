using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Drone AR > Create Part Data Assets
/// Creates the 10 PartData assets (simple English text + video links) in Assets/DroneAR/PartData.
/// </summary>
public static class PartDataSeeder
{
    [MenuItem("Drone AR/Create Part Data Assets")]
    static void CreateAll()
    {
        string dir = "Assets/DroneAR/PartData";
        Directory.CreateDirectory(dir);

        Make(dir, PartType.Frame, "Frame",
            "The frame is the skeleton of the drone. Every other part is mounted on it. Most frames use an X shape, with arms going out diagonally from the center. Carbon fiber is popular because it is light and strong.",
            "The base of everything. Build on it first.",
            "",
            "https://www.youtube.com/watch?v=lg-QKhl0uuU");

        Make(dir, PartType.Motor, "Motor",
            "Motors spin the propellers to make lift. KV tells how fast a motor spins for each volt: a higher KV spins faster. A motor named 2207 has a core 22mm wide and 7mm tall.",
            "One at the end of each of the 4 arms. Two spin clockwise and two spin counter-clockwise, in a diagonal pattern.",
            "This motor spins the wrong way for this arm. Two motors must spin clockwise (CW) and two counter-clockwise (CCW), in a diagonal pattern. Swap it with the motor on the diagonal.",
            "https://www.youtube.com/watch?v=jOugJpQfUDU");

        Make(dir, PartType.Propeller, "Propeller",
            "Propellers push air down to lift the drone. The size is written like 5.1x4.6 (length x pitch). Propellers on the diagonal spin the same direction.",
            "On top of each motor. Add them last.",
            "Wrong propeller for this motor. Match the CW or CCW mark on the propeller with the motor spin direction. A wrong propeller makes the drone flip or fail to lift.",
            "");

        Make(dir, PartType.FlightController, "Flight Controller (FC)",
            "The flight controller is the brain of the drone. It reads the sensors (gyro and accelerometer) and tells the ESC how fast each motor should spin, many times every second. Most FPV drones run Betaflight on it.",
            "In the middle of the drone, above the ESC, with its arrow pointing to the front.",
            "",
            "https://www.youtube.com/watch?v=jOugJpQfUDU");

        Make(dir, PartType.ESC, "ESC",
            "The ESC takes battery power and turns it into the signal a motor needs, so each motor spins at the speed the flight controller asks for. Most new builds use one 4-in-1 ESC board under the flight controller.",
            "In the middle stack, under the flight controller.",
            "",
            "https://www.youtube.com/watch?v=TwAmmPxOpTM");

        Make(dir, PartType.PDB, "Power Distribution Board (PDB)",
            "The PDB shares the battery power with all the other parts. In many new builds the 4-in-1 ESC does this job too.",
            "At the bottom of the stack, near the battery connector.",
            "",
            "https://www.youtube.com/watch?v=dMPgc6_lIuA");

        Make(dir, PartType.Battery, "Battery (LiPo)",
            "The LiPo battery is the energy source. The S number is how many cells it has: 4S is 14.8V and 6S is 22.2V. Store it at about 3.8V per cell. Charge it with a balance charger and never leave it charging alone.",
            "Strapped tight on the frame, in the center.",
            "",
            "https://www.youtube.com/watch?v=Vu8KoeY9dxA");

        Make(dir, PartType.Receiver, "Receiver (RX)",
            "The receiver gets signals from your remote and passes them to the flight controller. It must be bound (paired) to your remote. With ELRS, both must use the same major version.",
            "Connected to the flight controller, with the antenna out in the open.",
            "",
            "https://www.youtube.com/watch?v=L1Aqy6jGJec");

        Make(dir, PartType.Transmitter, "Remote (TX)",
            "The remote, also called the transmitter, sends your stick commands by radio to the drone's receiver. Pairing the remote with the receiver is called binding.",
            "In the pilot's hands. Pair it with the drone's receiver.",
            "",
            "");

        Make(dir, PartType.GPS, "GPS",
            "GPS tells the drone where it is. It enables GPS Rescue, which flies the drone back toward you in an emergency. Wire GPS TX to FC RX, and GPS RX to FC TX. Keep it at least 3cm away from the video antenna.",
            "On top of the drone, away from other electronics and carbon fiber.",
            "",
            "https://www.youtube.com/watch?v=fmMAu0bQl1E");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Part data assets created in " + dir);
    }

    static void Make(string dir, PartType type, string displayName, string info, string whereText, string wrongDir, string url)
    {
        PartData d = ScriptableObject.CreateInstance<PartData>();
        d.type = type;
        d.displayName = displayName;
        d.info = info;
        d.whereItGoes = whereText;
        d.wrongDirectionHelp = wrongDir;
        d.videoUrl = url;
        AssetDatabase.CreateAsset(d, dir + "/" + type + ".asset");
    }
}
