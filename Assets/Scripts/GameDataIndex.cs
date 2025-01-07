using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataIndex : MonoBehaviour
{
    //game settings
    public bool checkColorsOnly = true;

    //selected types and stuff
    public List<ColorDef> colorDefinitions = new List<ColorDef>();
    public List<TileShapeDef> tileShapeDefinitions = new List<TileShapeDef>();
    public List<string> elementTypes = new List<string>();
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