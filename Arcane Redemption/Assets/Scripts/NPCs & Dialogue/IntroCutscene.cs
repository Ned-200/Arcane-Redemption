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

    protected override void Start()
    {
        cutsceneImage = CutsceneImageObject.GetComponent<Image>();

        textComponent.text = string.Empty;
        Debug.Log("Cutscene Begin");
        StartIntro();
    }

    // void Update()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         if (textComponent.text == lines[index])
    //         {
    //             NextLine();
    //         } else
    //         {
    //             StopAllCoroutines();
    //             textComponent.text = lines[index];
    //         }
    //     }
    // }

    void StartIntro()
    {
        textComponent.text = string.Empty;
        index = 0;
        cutsceneImage.sprite = images[index];
        StartCoroutine(TypeLine());
        DialogueBox.SetActive(true);
        CutsceneImageObject.SetActive(true);
        NPC_Speaking = true;
    }

    // IEnumerator TypeLine()
    // {
    //     foreach (char c in lines[index].ToCharArray())
    //     {
    //         textComponent.text += c;
    //         yield return new WaitForSeconds(textSpeed);
    //     }
    // }

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
            DialogueBox.SetActive(false);
            CutsceneImageObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }

}
