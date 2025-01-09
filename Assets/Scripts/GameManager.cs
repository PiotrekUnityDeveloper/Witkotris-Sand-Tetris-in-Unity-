using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public WitkotrisGamemode gamemode;
    public WitkotrisDifficulty difficulty;

    //gamemode specific
    public int clearAmount = 20;
    public int highScoreTime = 180;

    //public GameDataIndex gameDataIndex;
    public SandSimulation simulation;
    public List<Color> tileColors = new List<Color>();
    public List<Sprite> tileSprites = new List<Sprite>();
    public List<string> elementTypes = new List<string>();

    // used for clearline checks
    public List<Color[]> dataColors = new List<Color[]>();

    public bool useLocalData = true;
    public Transform tileSpawnpoint;

    // Start is called before the first frame update
    void Start()
    {
        Wakeup();
    }

    private void Awake()
    {
        //SandSimulation.Instance.chunkSize = gameDataIndex.chunkSize;
    }

    public void Wakeup()
    {
        InitializeTiles();
        StartCoroutine(DelayGameStart());
    }

    public void InitializeTiles()
    {
        this.tileColors = SettingsSaver.colors;
        this.tileSprites = SettingsSaver.tiles;
        this.elementTypes = SettingsSaver.elements;


        //assuming each tilesprite has the same pallete of colors (if not, use a double loop)
        if (tileSprites.Count > 0)
        {
            foreach (Color color in this.tileColors)
            {
                dataColors.Add(GetTileColorBlending(tileSprites[0], color).ToArray());
            }
        }

        SandSimulation.Instance.checkColorsOnly = SettingsSaver.checkColorsOnly;
        SandSimulation.Instance.colorsToCheck = dataColors;
        SandSimulation.Instance.typesToCheck = elementTypes;


        SandSimulation.Instance.defaultFallSpeed = SettingsSaver.defaultSpeed;
        SandSimulation.Instance.fastForwardSpeed = SettingsSaver.fastForwardSpeed;
        SandSimulation.Instance.horizontalSpeed = SettingsSaver.horizontalSpeed;
        gamemode = SettingsSaver.gamemode;
        difficulty = SettingsSaver.difficulty;

        clearAmount = SettingsSaver.clearAmount;
        highScoreTime = SettingsSaver.highscoretime;
    }

    public TMP_Text extraText;

    public void StartGame()
    {
        if(tileSpawnpoint != null)
        {
            simulation.CreateSimObject(tileSpawnpoint.position, GetRandomTileSprite(), GetRandomTileColor(), GetRandomElementType());
        }else { Debug.Log("tileSpawnpoint is null, please assign it so tiles have a place to spawn..."); }

        if(gamemode == WitkotrisGamemode.HIGHSCORE)
        {
            timeCount = SettingsSaver.highscoretime;
            extraText.text = timeCount.ToString() + " seconds left";
            StartCoroutine(countdownTime());
        }
        else if(gamemode == WitkotrisGamemode.CLEAR)
        {
            clearCount = SettingsSaver.clearAmount;
            extraText.text = SettingsSaver.clearAmount + "/" + clearCount + " clears left";
        }
    }

    public void CountClear()
    {
        if(gamemode == WitkotrisGamemode.CLEAR)
        {
            clearCount -= 1;

            if (clearCount <= 0)
            {
                Gameover();
            }
        }
    }

    private int clearCount;
    private int timeCount;
    public IEnumerator countdownTime()
    {
        yield return new WaitForSeconds(1f);
        timeCount -= 1;
        extraText.text = timeCount.ToString();

        if(timeCount <= 0)
        {
            Gameover();
        }
        else
        {
            StartCoroutine(countdownTime());
        }
    }

    public IEnumerator DelayGameStart()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        StartGame();
    }

    // Update is called once per frame
    void Update()
    {

        //if (isGameOver) return;


        // ROTATE TILE
        if (Input.GetKeyDown(KeyCode.UpArrow) && !isGameOver)
        {
            Rotate();
        }

        // HORIZONTAL MOVEMENT
        if (Input.GetKeyDown(KeyCode.RightArrow) && !isGameOver)
        {
            MoveRight();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) && !isGameOver)
        {
            MoveLeft();
        }

        if(Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.LeftArrow))
        {
            if(!isGameOver) StopMove();
        }

        // FAST FORWARD
        if (Input.GetKeyDown(KeyCode.DownArrow) && !isGameOver)
        {
            FastForward();
        }

        if (Input.GetKeyUp(KeyCode.DownArrow) && !isGameOver)
        {
            StopFastForward();
        }
    }

    public void MoveLeft()
    {
        if (!isGameOver) { simulation.MoveTileLeft(); } else { BackToMenu(); }
    }

    public void MoveRight()
    {
        if (!isGameOver) { simulation.MoveTileRight(); }
    }

    public void StopMove()
    {
        if(isGameOver) return;
        simulation.StopTileMovement();
    }

    public void Rotate()
    {
        if (!isGameOver) { simulation.RotateSimObject(); } else { ResetGame(); } 
    }

    public void FastForward()
    {
        if (isGameOver) return;
        simulation.FastForwardFall();
    }

    public void StopFastForward()
    {
        if (isGameOver) return;
        simulation.StopFastFall();
    }

    [HideInInspector] public Coroutine lineChecker = null;

    public void TriggerNextTile()
    {
        if(isGameOver) return;

        StartCoroutine(DelayNextTile());

        if(lineChecker == null)
        {
            lineChecker = SandSimulation.Instance.StartCoroutine(SandSimulation.Instance.CheckForClearLineLoop());
        }
    }

    public TMP_Text totalScoreText;
    public int totalScore = 0;
    public void AddScore(int scoreNum)
    {
        totalScore += scoreNum;
        totalScoreText.text = totalScore.ToString();

        if(totalScore > PlayerPrefs.GetInt("highscore", 0))
        {
            PlayerPrefs.SetInt("highscore", totalScore);
        }
    }

    

    public bool isGameOver = false;
    public GameObject gameoverobject;

    public void Gameover()
    {
        gameoverobject.SetActive(true);
        SandSimulation.Instance.TriggerGameOver();
        this.isGameOver = true;
    }

    public void ResetGame()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        SceneManager.LoadSceneAsync("SandMenu");
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
        //return tileSprites[Random.Range(0, tileSprites.Count)];
        return SettingsSaver.tiles[Random.Range(0, SettingsSaver.tiles.Count)];
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