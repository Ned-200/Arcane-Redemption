using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class IntroCutscene : NPC_Character
{

    [Header("UI")]
    [SerializeField] private GameObject CutsceneImageObject;
    private Image cutsceneImage;
    public Sprite[] images;
    [SerializeField] private MainMenu mainMenu;

    protected override void Start()
    {
        cutsceneImage = CutsceneImageObject.GetComponent<Image>();
    }

    public void StartIntro()
    {
        gameObject.SetActive(true);
        cutsceneImage = CutsceneImageObject.GetComponent<Image>();

        textComponent.text = string.Empty;
        Debug.Log("Cutscene Begin");
        textComponent.text = string.Empty;
        index = 0;
        cutsceneImage.sprite = images[index];
        StartCoroutine(TypeLine());
        DialogueBox.SetActive(true);
        CutsceneImageObject.SetActive(true);
        NPC_Speaking = true;
    }

    protected override void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
            cutsceneImage.sprite = images[index];
        } else
        {
            // End introduction
            Debug.Log("Introduction End");

            mainMenu.PlayGame();
        }
    }

}
