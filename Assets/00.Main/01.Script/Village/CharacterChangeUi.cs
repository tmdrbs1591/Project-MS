using UnityEngine;

public class CharacterChangeUi : MonoBehaviour
{
    [SerializeField] int characterIndex;


    public void OnClick()
    {
        PlayerPrefs.SetInt("SelectedCharacterIndex", characterIndex);
        PlayerPrefs.Save();
    }
}