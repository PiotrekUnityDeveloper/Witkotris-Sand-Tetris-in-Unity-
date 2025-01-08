using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        gamemodeListAnimator.SetTrigger("show");
        gamemodeDescriptionAnimator.SetTrigger("hide");
        StartCoroutine(PauseUIAnimations());
    }

    public void CloseGamemodeList()
    {
        if (uiPaused) return;

        gamemodeListAnimator.SetTrigger("hide");
        gamemodeDescriptionAnimator.SetTrigger("show");
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
                break;
            case "HIGHSCORE":
                highscoreDescription.SetActive(true);
                break;
            case "CLEAR":
                clearDescription.SetActive(true);
                break;
            case "CHAOS":
                chaosDescription.SetActive(true);
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
}

public enum WitkotrisGamemode
{
    DEFAULT,
    HIGHSCORE,
    CLEAR,
    CHAOS,
}