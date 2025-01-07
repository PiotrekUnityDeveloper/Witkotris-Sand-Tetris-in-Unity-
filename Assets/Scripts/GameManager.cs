using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public GameDataIndex gameDataIndex;
    public SandSimulation simulation;
    public List<Color> tileColors = new List<Color>();
    public List<Sprite> tileSprites = new List<Sprite>();
    public List<string> elementTypes = new List<string>();

    // used for clearline checks
    public List<Color[]> dataColors = new List<Color[]>();

    
    public Transform tileSpawnpoint;

    // Start is called before the first frame update
    void Start()
    {
        InitializeTiles();
        StartCoroutine(DelayGameStart());
    }

    public void InitializeTiles()
    {
        foreach(ColorDef colordef in gameDataIndex.colorDefinitions)
        {
            this.tileColors.Add(colordef.color);
        }

        foreach(TileShapeDef shapedef in gameDataIndex.tileShapeDefinitions)
        {
            this.tileSprites.Add(shapedef.tileSprite);
        }

        foreach(string etype in gameDataIndex.elementTypes)
        {
            this.elementTypes.Add(etype);
        }


        //assuming each tilesprite has the same pallete of colors (if not, use a double loop)
        if(tileSprites.Count > 0)
        {
            foreach (Color color in this.tileColors)
            {
                dataColors.Add(GetTileColorBlending(tileSprites[0], color).ToArray());
            }
        }

        SandSimulation.Instance.checkColorsOnly = gameDataIndex.checkColorsOnly;
        SandSimulation.Instance.colorsToCheck = dataColors;
        SandSimulation.Instance.typesToCheck = elementTypes;
    }

    public void StartGame()
    {
        if(tileSpawnpoint != null)
        {
            simulation.CreateSimObject(tileSpawnpoint.position, GetRandomTileSprite(), GetRandomTileColor(), GetRandomElementType());
        }else { Debug.Log("tileSpawnpoint is null, please assign it so tiles have a place to spawn..."); }
    }

    public IEnumerator DelayGameStart()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        StartGame();
    }

    // Update is called once per frame
    void Update()
    {
        // ROTATE TILE
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Rotate();
        }

        // HORIZONTAL MOVEMENT
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveRight();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveLeft();
        }

        if(Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.LeftArrow))
        {
            StopMove();
        }

        // FAST FORWARD
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            FastForward();
        }

        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            StopFastForward();
        }
    }

    public void MoveLeft()
    {
        simulation.MoveTileLeft();
    }

    public void MoveRight()
    {
        simulation.MoveTileRight();
    }

    public void StopMove()
    {
        simulation.StopTileMovement();
    }

    public void Rotate()
    {
        simulation.RotateSimObject();
    }

    public void FastForward()
    {
        simulation.FastForwardFall();
    }

    public void StopFastForward()
    {
        simulation.StopFastFall();
    }

    [HideInInspector] public Coroutine lineChecker = null;

    public void TriggerNextTile()
    {
        StartCoroutine(DelayNextTile());

        if(lineChecker == null)
        {
            lineChecker = SandSimulation.Instance.StartCoroutine(SandSimulation.Instance.CheckForClearLineLoop());
        }
    }

    public IEnumerator DelayNextTile()
    {
        yield return new WaitForSeconds(0.4f);
        simulation.CreateSimObject(tileSpawnpoint.position, GetRandomTileSprite(), GetRandomTileColor(), GetRandomElementType());
    }

    public Color GetRandomTileColor()
    {
        return tileColors[Random.Range(0, tileColors.Count)];
    }

    public Sprite GetRandomTileSprite()
    {
        return tileSprites[Random.Range(0, tileSprites.Count)];
    }

    public string GetRandomElementType()
    {
        return elementTypes[Random.Range(0, elementTypes.Count)];
    }

    public List<Color> GetTileColorBlending(Sprite tileSprite, Color tileColor)
    {
        HashSet<Color> colors = new HashSet<Color>();

        // Get the sprite texture
        Texture2D texture = tileSprite.texture;

        // Calculate the rectangle that contains the sprite within the texture
        Rect spriteRect = tileSprite.textureRect;
        int startX = (int)spriteRect.x;
        int startY = (int)spriteRect.y;
        int width = (int)spriteRect.width;
        int height = (int)spriteRect.height;

        // Loop through each pixel in the sprite
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Get the color of the current pixel
                Color pixelColor = texture.GetPixel(startX + x, startY + y);

                // Calculate brightness as a simple average of RGB components
                float brightness = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;

                // Blend the color based on brightness and tileColor
                Color finalColor = new Color(
                    tileColor.r * brightness,
                    tileColor.g * brightness,
                    tileColor.b * brightness,
                    1f
                );

                colors.Add(finalColor);
            }
        }

        return colors.ToList();
    }
}

/*
public class TileBlockData
{
    public Sprite tileSprite;
    public Color tileBaseColor;
    public Color[] blendColors = null;

    public List<Color> GetTileColorBlending()
    {
        if (blendColors != null)
        {
            return blendColors.ToList();
        }
        else
        {
            HashSet<Color> colors = new HashSet<Color>();

            // Get the sprite texture
            Texture2D texture = tileSprite.texture;

            // Calculate the rectangle that contains the sprite within the texture
            Rect spriteRect = tileSprite.textureRect;
            int startX = (int)spriteRect.x;
            int startY = (int)spriteRect.y;
            int width = (int)spriteRect.width;
            int height = (int)spriteRect.height;

            // Loop through each pixel in the sprite
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Get the color of the current pixel
                    Color pixelColor = texture.GetPixel(startX + x, startY + y);

                    // Calculate brightness as a simple average of RGB components
                    float brightness = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;

                    // Blend the color based on brightness and tileColor
                    Color finalColor = new Color(
                        pixelColor.r * brightness,
                        pixelColor.g * brightness,
                        pixelColor.b * brightness,
                        1f
                    );

                    colors.Add(finalColor);
                }
            }

            blendColors = colors.ToArray();
            return blendColors.ToList();
        }
    }
}*/