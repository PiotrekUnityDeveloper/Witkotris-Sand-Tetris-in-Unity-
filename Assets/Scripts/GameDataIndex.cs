using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataIndex : MonoBehaviour
{
    public GameManager gameManager;

    private void Awake()
    {
        this.gamemode = SettingsSaver.gamemode;
        this.difficulty = SettingsSaver.difficulty;
        this.checkColorsOnly = SettingsSaver.checkColorsOnly;

        colorDefinitions.Clear();
        tileShapeDefinitions.Clear();
        elementTypes.Clear();
        
        foreach(Color c in SettingsSaver.colors)
        {
            this.colorDefinitions.Add(new ColorDef { color = c });
        }

        foreach(Sprite s in SettingsSaver.tiles)
        {
            this.tileShapeDefinitions.Add(new TileShapeDef { tileSprite = s });
        }

        foreach(string str in SettingsSaver.elements)
        {
            this.elementTypes.Add(str);
        }

        horizontalMovement = SettingsSaver.horizontalSpeed;
        defFallSpeed = SettingsSaver.defaultSpeed;
        fastFallSpeed = SettingsSaver.fastForwardSpeed;

        clearAmount = SettingsSaver.clearAmount;
        highscoreTime = SettingsSaver.highscoretime;

        chunkSize = SettingsSaver.chunkSize;

        //gameManager.enabled = true;
        gameManager.Wakeup();
    }

    private void OnEnable()
    {
        
    }

    public WitkotrisGamemode gamemode;
    public WitkotrisDifficulty difficulty;

    //game settings
    public bool checkColorsOnly = true;

    //movement
    public int horizontalMovement;
    public int defFallSpeed;
    public int fastFallSpeed;

    public int chunkSize = 8;

    //selected types and stuff
    public List<ColorDef> colorDefinitions = new List<ColorDef>();
    public List<TileShapeDef> tileShapeDefinitions = new List<TileShapeDef>();
    public List<string> elementTypes = new List<string>();

    //gamemode specific
    public int clearAmount;
    public int highscoreTime;
}

[System.Serializable]
public class ColorDef
{
    public string colorName;
    public Color color;
}

[System.Serializable]
public class TileShapeDef
{
    public string tileName;
    public Sprite tileSprite;
}