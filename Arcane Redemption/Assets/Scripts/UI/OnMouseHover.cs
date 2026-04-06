using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnMouseHover : MonoBehaviour, IPointerEnterHandler
{
    private GameObject mainCanvas;
    private InventoryUI inventoryUI;
    private MainMenu mainMenu;

    private void Start()
    {
        mainCanvas = GameObject.FindWithTag("MainCanvas");
        inventoryUI = mainCanvas.GetComponent<InventoryUI>();
        mainMenu = mainCanvas.GetComponent<MainMenu>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventoryUI != null) {
            inventoryUI.PlayHoverSounds();
        }
        if (mainMenu != null) {
            mainMenu.PlayHoverSounds();
        }
    }

}
