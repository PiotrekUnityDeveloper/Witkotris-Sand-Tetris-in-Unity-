using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataIndex : MonoBehaviour
{
    public List<ColorDef> colorDefinitions = new List<ColorDef>();
    public List<TileShapeDef> tileShapeDefinitions = new List<TileShapeDef>();
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