using UnityEngine;
using UnityEngine.UI;
using ProjectMS.CharacterSystem;

public class ShowCharacterSelectIconUI : MonoBehaviour
{
    [SerializeField] private Sprite[] characterIconImages;
    [SerializeField] private Image p1CharacterImage;
    [SerializeField] private Image p2CharacterImage;

    void ShowSelectedCharacterIcon()
    {
        MatchManager match = MatchManager.Instance;
        if (match == null || p1CharacterImage == null || p2CharacterImage == null) return;

        foreach (CharacterBase character in CharacterBase.All)
        {
            if (character == null || character.Object == null)
                continue;

            bool isP1 = match.IsPlayer1(character.MatchPlayer);
            string displayName = (character.Definition != null) ? character.Definition.DisplayName : string.Empty;

            if (isP1)
            {
                ChangeCharacterImage(displayName, p1CharacterImage);

            }
            else
            {
                ChangeCharacterImage(displayName, p2CharacterImage);
            }
        }
    }
    void ChangeCharacterImage(string displayName, Image playerImage)
    {
        switch (displayName)
        {
            case "거너":
                playerImage.sprite = characterIconImages[0];
                break;
            case "스파크":
                playerImage.sprite = characterIconImages[1];
                break;
            case "체이서":
                playerImage.sprite = characterIconImages[2];
                break;
            case "팝업":
                playerImage.sprite = characterIconImages[3];
                break;
            case "지퍼":
                playerImage.sprite = characterIconImages[4];
                break;
        }
    }
    void Update()
    {
        ShowSelectedCharacterIcon();
    }

}
