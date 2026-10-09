using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class ShowCharacterSelectIconUI : MonoBehaviour
{
    [SerializeField] private Image p1CharacterImage;
    [SerializeField] private Image p2CharacterImage;
    [Min(0f)][SerializeField] private float characterImageSizeMultiplier = 0.67f;


    private void Update()
    {
        ShowSelectedCharacterIcon();
    }

    private void ShowSelectedCharacterIcon()
    {
        MatchManager match = MatchManager.Instance;

        foreach (CharacterBase character in CharacterBase.All)
        {
            if (character == null || character.Object == null)
                continue;

            ChangeCharacterImage(character);
        }
    }

    private void ChangeCharacterImage(CharacterBase character)
    {
        MatchManager match = MatchManager.Instance;

        if (match == null) 
            return;

        bool isP1 = match.IsPlayer1(character.MatchPlayer);
        Sprite displaySprite = (character.Definition != null) ? character.Definition.CharacterMainSprite : null;
        
        Image playerImage = isP1 ? p1CharacterImage : p2CharacterImage;

       if (playerImage != null && displaySprite != null)
        {
            playerImage.sprite = displaySprite;
        }
    }
}
