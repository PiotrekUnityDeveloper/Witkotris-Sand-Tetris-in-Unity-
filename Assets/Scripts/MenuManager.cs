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
    // Start is called before the first frame update
    void Start()
    {
        
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
    public List<Sprite> fancysixteensprites = new List<Sprite>();
    //4
    public List<Sprite> defaultfoursprites = new List<Sprite>();
    public List<Sprite> fancyfoursprites = new List<Sprite>();
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
        else if (selectedTileStyle == "fancy16")
        {
            SettingsSaver.tiles = new List<Sprite>(this.fancysixteensprites);
            SettingsSaver.chunkSize = 16;
        }
        else if (selectedTileStyle == "default4")
        {
            SettingsSaver.tiles = new List<Sprite>(this.defaultfoursprites);
            SettingsSaver.chunkSize = 4;
        }
        else if (selectedTileStyle == "fancy4")
        {
            SettingsSaver.tiles = new List<Sprite>(this.fancyfoursprites);
            SettingsSaver.chunkSize = 4;
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
            else if (witkotrisDifficulty == WitkotrisDifficulty.MEDIUM || witkotrisDifficulty == WitkotrisDifficulty.HARD)
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
                // TODO ADD SOME ELEMENTS MATE

                difficultyInfoText.text = "Tiles go faster, 5 elements";
            }
            /// TODO ADD CUSTOM GAMEMODE

            this.colors.Clear();
            this.colors.Add(new Color(0.2877358f, 0.5475029f, 1, 1)); // blue
            this.colors.Add(new Color(1, 0.3254717f, 0.3254717f, 1)); // red
            this.colors.Add(new Color(0.9761904f, 1, 0, 1)); // yellow
            this.colors.Add(new Color(0, 0.745283f, 0.1789982f, 1)); // green
        }

        SettingsSaver.elements = this.elementTypes;
        //SettingsSaver.tiles = this.defaulteightsprites;
        SettingsSaver.colors = this.colors;
        SettingsSaver.chunkSize = this.chunkSize;
    }

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