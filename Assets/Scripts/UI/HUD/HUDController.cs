using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    public UIDocument hudDocument;
    public PlayerController playerController;
    public Label healthLabel;
    public Label manaLabel;
    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        healthLabel = hudDocument.rootVisualElement.Q<Label>("life");
        manaLabel = hudDocument.rootVisualElement.Q<Label>("mana");
    }

    private void Update()
    {
        UpdateHealth(playerController.health);
        UpdateMana(playerController.mana);
    }

    // Update is called once per frame
    public void UpdateHealth(int health)
    {
        healthLabel.text = "Vida: " + health.ToString();
    }

    public void UpdateMana(int mana)
    {
        manaLabel.text = "Mana: " + mana.ToString();
    }
}
