using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public TMP_Text highscoretext;

    // Start is called before the first frame update
    void Start()
    {
        highscoretext.text = PlayerPrefs.GetInt("highscore", 0).ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public WitkotrisGamemode selectedGamemode = WitkotrisGamemode.DEFAULT;

    public bool uiPaused = false;

    public Animator gamemodeButtonAnimator;
    public Animator gamemodeListAnimator;
    public Animator gamemodeDescriptionAnimator;
    public Animator touchButtonsAnimator;

    public TMP_Text gamemodeButtonText;

    public GameObject defaultDescription;
    public GameObject highscoreDescription;
    public GameObject clearDescription;
    public GameObject chaosDescription;

    public void OpenGamemodeList()
    {
        if(uiPaused) return;

        gamemodeListAnimator.gameObject.SetActive(true);
        gamemodeButtonAnimator.SetTrigger("hide");
        gamemodeListAnimator.SetTrigger("show");
        gamemodeDescriptionAnimator.SetTrigger("hide");
        StartCoroutine(PauseUIAnimations());
    }

    public void CloseGamemodeList()
    {
        if (uiPaused) return;

        gamemodeListAnimator.SetTrigger("hide");
        gamemodeDescriptionAnimator.SetTrigger("show");
        gamemodeButtonAnimator.SetTrigger("show");
        StartCoroutine(PauseUIAnimations());
        StartCoroutine(DisableUIObjectWithDelay(gamemodeListAnimator.gameObject));
    }

    public GameObject settingsWindow;

    public void ToggleSettings()
    {
        settingsWindow.SetActive(!settingsWindow.activeInHierarchy);
        Menu.SetActive(!Menu.activeInHierarchy);

        if (settingsWindow.activeInHierarchy)
        {
            customMenu.SetActive(false);
        }
        else
        {
            customMenu.SetActive(false);
        }
    }

    public void SelectGamemode(GameObject gmButton)
    {
        if (uiPaused) return;

        gmButton.transform.SetAsFirstSibling();
        CloseGamemodeList();
    }

    public void SetGamemode(string gamemode)
    {
        gamemodeButtonText.text = gamemode.ToUpper();
        selectedGamemode = (WitkotrisGamemode)Enum.Parse(typeof(WitkotrisGamemode), gamemode.ToUpper());

        defaultDescription.SetActive(false);
        highscoreDescription.SetActive(false);
        clearDescription.SetActive(false);
        chaosDescription.SetActive(false);

        switch (gamemode.ToUpper())
        {
            case "DEFAULT":
                defaultDescription.SetActive(true);
                witkotrisGamemode = WitkotrisGamemode.DEFAULT;
                break;
            case "HIGHSCORE":
                highscoreDescription.SetActive(true);
                witkotrisGamemode = WitkotrisGamemode.HIGHSCORE;
                break;
            case "CLEAR":
                clearDescription.SetActive(true);
                witkotrisGamemode = WitkotrisGamemode.CLEAR;
                break;
            case "CHAOS":
                chaosDescription.SetActive(true);
                witkotrisGamemode = WitkotrisGamemode.CHAOS;
                break;
        }
    }

    public IEnumerator PauseUIAnimations(float duration = 0.1f)
    {
        uiPaused = true;
        yield return new WaitForSeconds(duration);
        uiPaused = false;
    }

    public IEnumerator DisableUIObjectWithDelay(GameObject uiObject, float duration = 0.1f)
    {
        yield return new WaitForSeconds(duration);
        uiObject.SetActive(false);
    }

    public Image defaultImage;
    public Image highscoreImage;
    public Image clearImage;
    public Image chaosImage;

    //def
    public Sprite defaultEasyImage;
    public Sprite defaultMediumImage;
    public Sprite defaultHardImage;

    //high
    public Sprite highscoreEasyImage;
    public Sprite highscoreMediumImage;
    public Sprite highscoreHardImage;

    //clear
    public Sprite clearEasyImage;
    public Sprite clearMediumImage;
    public Sprite clearHardImage;

    //chaos
    public Sprite chaosEasyImage;
    public Sprite chaosMediumImage;
    public Sprite chaosHardImage;

    //other
    public Sprite difficultyButtonOn;
    public Sprite difficultyButtonOff;

    public Image difficultyImageEasy;
    public Image difficultyImageMedium;
    public Image difficultyImageHard;

    public TMP_Text difficultyTextEasy;
    public TMP_Text difficultyTextMedium;
    public TMP_Text difficultyTextHard;
    
    public void SetDifficulty(string difficuly)
    {
        difficultyImageEasy.sprite = difficultyButtonOff;
        difficultyImageMedium.sprite = difficultyButtonOff;
        difficultyImageHard.sprite = difficultyButtonOff;

        difficultyTextEasy.color = Color.white;
        difficultyTextMedium.color = Color.white;
        difficultyTextHard.color = Color.white;

        if (difficuly.ToLower() == "easy")
        {
            defaultImage.sprite = defaultEasyImage;
            highscoreImage.sprite = highscoreEasyImage;
            clearImage.sprite = clearEasyImage;
            chaosImage.sprite = chaosEasyImage;

            witkotrisDifficulty = WitkotrisDifficulty.EASY;

            difficultyImageEasy.sprite = difficultyButtonOn;
            difficultyTextEasy.color = Color.red;
        }
        else if (difficuly.ToLower() == "medium")
        {
            defaultImage.sprite = defaultMediumImage;
            highscoreImage.sprite = highscoreMediumImage;
            clearImage.sprite = clearMediumImage;
            chaosImage.sprite = chaosMediumImage;

            witkotrisDifficulty = WitkotrisDifficulty.MEDIUM;

            difficultyImageMedium.sprite = difficultyButtonOn;
            difficultyTextMedium.color = Color.red;
        } else if (difficuly.ToLower() == "hard")
        {
            defaultImage.sprite = defaultHardImage;
            highscoreImage.sprite = highscoreHardImage;
            clearImage.sprite = clearHardImage;
            chaosImage.sprite = chaosHardImage;

            witkotrisDifficulty = WitkotrisDifficulty.HARD;

            difficultyImageHard.sprite = difficultyButtonOn;
            difficultyTextHard.color = Color.red;
        }
    }

    public void SetTilePattern(string style)
    {
        this.selectedTileStyle = style;
    }

    public GameObject Menu;
    public GameObject tileselect;
    public void GoToMenu()
    {
        tileselect.SetActive(false);
        Menu.SetActive(true);
        customMenu.SetActive(false);
    }

    public void GoToTilePatternSelect()
    {
        tileselect.SetActive(true);
        Menu.SetActive(false);
        customMenu.SetActive(false);
    }

    private WitkotrisDifficulty witkotrisDifficulty;
    private WitkotrisGamemode witkotrisGamemode;

    public List<string> elementTypes = new List<string>();
    public List<Color> colors = new List<Color>();
    public string selectedTileStyle = "default8";
    //8
    public List<Sprite> defaulteightsprites = new List<Sprite>();
    public List<Sprite> fancyeightsprites = new List<Sprite>();
    //16
    public List<Sprite> defaultsixteensprites = new List<Sprite>();
    //4
    public List<Sprite> defaultfoursprites = new List<Sprite>();
    public List<Sprite> defaulttwosprites = new List<Sprite>();
    public List<Sprite> default32sprites = new List<Sprite>();

    public List<Sprite> extraShapes = new List<Sprite>();

    public int chunkSize = 8;

    public TMP_Text difficultyInfoText;

    public void SaveSettings()
    {
        SettingsSaver.gamemode = witkotrisGamemode;
        SettingsSaver.difficulty = witkotrisDifficulty;

        SettingsSaver.tiles.Clear();
        SettingsSaver.colors.Clear();
        SettingsSaver.elements.Clear();

        //default in case no
        SettingsSaver.tiles = new List<Sprite>(this.defaulteightsprites);
        SettingsSaver.chunkSize = 8;

        if (selectedTileStyle == "default8")
        {
            SettingsSaver.tiles = new List<Sprite>(this.defaulteightsprites);
            SettingsSaver.chunkSize = 8;
        }else if (selectedTileStyle == "fancy8")
        {
            SettingsSaver.tiles = new List<Sprite>(this.fancyeightsprites);
            SettingsSaver.chunkSize = 8;
        }
        else if (selectedTileStyle == "default16")
        {
            SettingsSaver.tiles = new List<Sprite>(this.defaultsixteensprites);
            SettingsSaver.chunkSize = 16;
        }
        else if (selectedTileStyle == "default2")
        {
            SettingsSaver.tiles = new List<Sprite>(this.defaulttwosprites);
            SettingsSaver.chunkSize = 2;
        }
        else if (selectedTileStyle == "default4")
        {
            SettingsSaver.tiles = new List<Sprite>(this.defaultfoursprites);
            SettingsSaver.chunkSize = 4;
        }
        else if (selectedTileStyle == "default32")
        {
            SettingsSaver.tiles = new List<Sprite>(this.default32sprites);
            SettingsSaver.chunkSize = 32;
        }

        if (witkotrisGamemode == WitkotrisGamemode.DEFAULT)
        {
            if(witkotrisDifficulty == WitkotrisDifficulty.EASY)
            {
                SettingsSaver.defaultSpeed = 1;
                SettingsSaver.fastForwardSpeed = 3;
                SettingsSaver.horizontalSpeed = 1;

                difficultyInfoText.text = "Just Relax, tiles go slowly";
            }
            else if (witkotrisDifficulty == WitkotrisDifficulty.MEDIUM)
            {
                SettingsSaver.defaultSpeed = 2;
                SettingsSaver.fastForwardSpeed = 6;
                SettingsSaver.horizontalSpeed = 2;

                difficultyInfoText.text = "Tiles go slightly faster";
            } else if (witkotrisDifficulty == WitkotrisDifficulty.HARD)
            {
                SettingsSaver.defaultSpeed = 3;
                SettingsSaver.fastForwardSpeed = 8;
                SettingsSaver.horizontalSpeed = 2;

                difficultyInfoText.text = "Tiles go very fast";
            }

            this.elementTypes.Clear();
            this.elementTypes.Add("sawdust");

            SettingsSaver.checkColorsOnly = true;

            this.colors.Clear();
            this.colors.Add(new Color(0.2877358f, 0.5475029f, 1, 1)); // blue
            this.colors.Add(new Color(1, 0.3254717f, 0.3254717f, 1)); // red
            this.colors.Add(new Color(0.9761904f, 1, 0, 1)); // yellow
            this.colors.Add(new Color(0, 0.745283f, 0.1789982f, 1)); // green
        }
        else if(witkotrisGamemode == WitkotrisGamemode.HIGHSCORE)
        {
            if (witkotrisDifficulty == WitkotrisDifficulty.EASY)
            {
                SettingsSaver.defaultSpeed = 1;
                SettingsSaver.fastForwardSpeed = 3;
                SettingsSaver.horizontalSpeed = 1;
                SettingsSaver.highscoretime = 180;

                difficultyInfoText.text = "Game time: 3 minutes";
            }
            else if (witkotrisDifficulty == WitkotrisDifficulty.MEDIUM)
            {
                SettingsSaver.defaultSpeed = 2;
                SettingsSaver.fastForwardSpeed = 6;
                SettingsSaver.horizontalSpeed = 2;
                SettingsSaver.highscoretime = 120;

                difficultyInfoText.text = "You have 2 minutes, and tiles go faster";
            }
            else if (witkotrisDifficulty == WitkotrisDifficulty.HARD)
            {
                SettingsSaver.defaultSpeed = 3;
                SettingsSaver.fastForwardSpeed = 8;
                SettingsSaver.horizontalSpeed = 2;
                SettingsSaver.highscoretime = 60;

                difficultyInfoText.text = "Tiles go very fast. 1 minute";
            }

            this.elementTypes.Clear();
            this.elementTypes.Add("sawdust");

            SettingsSaver.checkColorsOnly = true;

            this.colors.Clear();
            this.colors.Add(new Color(0.2877358f, 0.5475029f, 1, 1)); // blue
            this.colors.Add(new Color(1, 0.3254717f, 0.3254717f, 1)); // red
            this.colors.Add(new Color(0.9761904f, 1, 0, 1)); // yellow
            this.colors.Add(new Color(0, 0.745283f, 0.1789982f, 1)); // green
        }
        else if(witkotrisGamemode == WitkotrisGamemode.CLEAR)
        {
            if (witkotrisDifficulty == WitkotrisDifficulty.EASY)
            {
                SettingsSaver.defaultSpeed = 1;
                SettingsSaver.fastForwardSpeed = 3;
                SettingsSaver.horizontalSpeed = 1;
                SettingsSaver.clearAmount = 20;

                difficultyInfoText.text = "You need to clear 20 lines";
            }
            else if (witkotrisDifficulty == WitkotrisDifficulty.MEDIUM)
            {
                SettingsSaver.defaultSpeed = 2;
                SettingsSaver.fastForwardSpeed = 6;
                SettingsSaver.horizontalSpeed = 2;
                SettingsSaver.clearAmount = 40;

                difficultyInfoText.text = "You need to clear 40 lines. Tiles go faster";
            }
            else if (witkotrisDifficulty == WitkotrisDifficulty.HARD)
            {
                SettingsSaver.defaultSpeed = 2;
                SettingsSaver.fastForwardSpeed = 7;
                SettingsSaver.horizontalSpeed = 2;
                SettingsSaver.clearAmount = 60;

                difficultyInfoText.text = "You need to clear 60 lines";
            }

            this.elementTypes.Clear();
            this.elementTypes.Add("sawdust");

            SettingsSaver.checkColorsOnly = true;

            this.colors.Clear();
            this.colors.Add(new Color(0.2877358f, 0.5475029f, 1, 1)); // blue
            this.colors.Add(new Color(1, 0.3254717f, 0.3254717f, 1)); // red
            this.colors.Add(new Color(0.9761904f, 1, 0, 1)); // yellow
            this.colors.Add(new Color(0, 0.745283f, 0.1789982f, 1)); // green
        }
        else if(witkotrisGamemode == WitkotrisGamemode.CHAOS)
        {
            if (witkotrisDifficulty == WitkotrisDifficulty.EASY)
            {
                SettingsSaver.defaultSpeed = 1;
                SettingsSaver.fastForwardSpeed = 3;
                SettingsSaver.horizontalSpeed = 1;

                SettingsSaver.checkColorsOnly = true;

                this.elementTypes.Clear();
                this.elementTypes.Add("sand");
                this.elementTypes.Add("water");
                this.elementTypes.Add("sawdust");
                this.elementTypes.Add("flour");

                difficultyInfoText.text = "Slow speed. 4 types of elements";
            }
            else if (witkotrisDifficulty == WitkotrisDifficulty.MEDIUM)
            {
                SettingsSaver.defaultSpeed = 2;
                SettingsSaver.fastForwardSpeed = 6;
                SettingsSaver.horizontalSpeed = 2;

                SettingsSaver.checkColorsOnly = true;

                this.elementTypes.Clear();
                this.elementTypes.Add("sand");
                this.elementTypes.Add("water");
                this.elementTypes.Add("sawdust");
                this.elementTypes.Add("flour");
                this.elementTypes.Add("bricks");
                // TODO ADD SOME ELEMENTS MATE

                SettingsSaver.tiles.AddRange(extraShapes);

                //SettingsSaver.checkColorsOnly = false;

                difficultyInfoText.text = "Tiles go faster, 5 elements, and a few extra shapes";
            }
            else if (witkotrisDifficulty == WitkotrisDifficulty.HARD)
            {
                SettingsSaver.defaultSpeed = 2;
                SettingsSaver.fastForwardSpeed = 6;
                SettingsSaver.horizontalSpeed = 2;

                //SettingsSaver.checkColorsOnly = true;

                this.elementTypes.Clear();
                this.elementTypes.Add("sand");
                this.elementTypes.Add("water");
                this.elementTypes.Add("sawdust");
                this.elementTypes.Add("flour");
                this.elementTypes.Add("bricks");
                // TODO ADD SOME ELEMENTS MATE

                SettingsSaver.tiles.AddRange(extraShapes);

                SettingsSaver.checkColorsOnly = false;

                difficultyInfoText.text = "Tiles go even faster, 5 elements, extra shapes, and only the same element types can clear a line";
            }
            /// TODO ADD CUSTOM GAMEMODE

            this.colors.Clear();
            this.colors.Add(new Color(0.2877358f, 0.5475029f, 1, 1)); // blue
            this.colors.Add(new Color(1, 0.3254717f, 0.3254717f, 1)); // red
            this.colors.Add(new Color(0.9761904f, 1, 0, 1)); // yellow
            this.colors.Add(new Color(0, 0.745283f, 0.1789982f, 1)); // green
        }else if(witkotrisGamemode == WitkotrisGamemode.CUSTOM)
        {
            SettingsSaver.horizontalSpeed = (int)horizSpeed.value;
            SettingsSaver.defaultSpeed = (int)fallSpeed.value;
            SettingsSaver.fastForwardSpeed = (int)ffSpeed.value;

            if (usepowder.isOn)
            {
                SettingsSaver.elements.Add("sand");
            }

            if (uselightpowder.isOn)
            {
                SettingsSaver.elements.Add("sawdust");
            }

            if (usedust.isOn)
            {
                SettingsSaver.elements.Add("flour");
            }

            if (useliquid.isOn)
            {
                SettingsSaver.elements.Add("water");
            }

            if (usesolid.isOn)
            {
                SettingsSaver.elements.Add("bricks");
            }

            //colors

            this.colors.Clear();
            
            

            if (useyellow.isOn)
            {
                this.colors.Add(new Color(0.9761904f, 1, 0, 1)); // yellow
            }

            if (usegreen.isOn)
            {
                this.colors.Add(new Color(0, 0.745283f, 0.1789982f, 1)); // green
            }

            if (usered.isOn)
            {
                this.colors.Add(new Color(1, 0.3254717f, 0.3254717f, 1)); // red
            }

            if (useblue.isOn)
            {
                this.colors.Add(new Color(0.2877358f, 0.5475029f, 1, 1)); // blue
            }

            if (useorange.isOn)
            {
                this.colors.Add(orange);
            }

            if (usewhite.isOn)
            {
                this.colors.Add(Color.white);
            }

            if (usegray .isOn)
            {
                this.colors.Add(gray);
            }

            if (usepink.isOn)
            {
                this.colors.Add(pink);
            }

            if(colors.Count <= 0)
            {
                this.colors.Add(Color.black);
            }

            if (useextraShapes.isOn)
            {
                SettingsSaver.tiles.AddRange(extraShapes);
            }

            if (impossile.isOn)
            {
                SettingsSaver.checkColorsOnly = true;
            }
            else
            {
                SettingsSaver.checkColorsOnly = false;
            }
        }

        SettingsSaver.elements = this.elementTypes;
        //SettingsSaver.tiles = this.defaulteightsprites;
        SettingsSaver.colors = this.colors;
        SettingsSaver.chunkSize = this.chunkSize;
    }

    public Toggle impossile;

    public GameObject customMenu;
    public void CustomGamemodeToggle()
    {
        customMenu.SetActive(!customMenu.activeInHierarchy);

        if (customMenu.activeInHierarchy)
        {
            settingsWindow.SetActive(false);
            Menu.SetActive(false);
        }
        else
        {
            settingsWindow.SetActive(false);
            Menu.SetActive(true);
        }
    }

    public void SetAsCustom()
    {
        this.selectedGamemode = WitkotrisGamemode.CUSTOM;
        gamemodeButtonText.text = "CUSTOM";
    }

    public void ShowCustom()
    {
        if (customMenu.activeInHierarchy)
        {
            settingsWindow.SetActive(false);
            Menu.SetActive(true);
            customMenu.SetActive(false);
        }
        else
        {
            settingsWindow.SetActive(false);
            Menu.SetActive(false);
            customMenu.SetActive(true);
        }
    }

    public Toggle usepowder;
    public Toggle uselightpowder;
    public Toggle usedust;
    public Toggle useliquid;
    public Toggle usesolid;

    public Toggle useextraShapes;

    public Toggle useyellow;
    public Toggle usegreen;
    public Toggle useblue;
    public Toggle usered;
    public Toggle usepink;
    public Toggle usegray;
    public Toggle usewhite;
    public Toggle useorange;

    public Color orange;
    public Color gray;
    public Color pink;

    public Slider horizSpeed;
    public Slider fallSpeed;
    public Slider ffSpeed;

    public Animator menuAnimator;
    public Animator mobileControlsAnimator;
    public void StartGame()
    {
        SaveSettings();

        menuAnimator.SetTrigger("hide");
        mobileControlsAnimator.SetTrigger("start");
        StartCoroutine(DelayStart());
    }

    public PowderDrawer powderDrawer;

    private IEnumerator DelayStart()
    {
        powderDrawer.StopAndClear();
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadSceneAsync("PlaySand");
    }

}

public enum WitkotrisGamemode
{
    DEFAULT,
    HIGHSCORE,
    CLEAR,
    CHAOS,
    CUSTOM,
}

public enum WitkotrisDifficulty
{
    EASY,
    MEDIUM,
    HARD,
}