using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SettingsSaver : MonoBehaviour
{

    public static WitkotrisGamemode gamemode;
    public static WitkotrisDifficulty difficulty;

    //properties
    public static int defaultSpeed = 2;
    public static int fastForwardSpeed = 6;
    public static int horizontalSpeed = 2;

    public static bool checkColorsOnly = true;

    public static List<Sprite> tiles = new List<Sprite>();
    public static List<Color> colors = new List<Color>();
    public static List<string> elements = new List<string>();

    public static int chunkSize = 8;

    //gamemode specific
    public static int highscoretime; // in seconds
    public static int clearAmount;
}
