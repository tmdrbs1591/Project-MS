using UnityEngine;

public class CharacterChangeUi : MonoBehaviour
{
    [SerializeField] int characterIndex;

    /// <summary>이 버튼이 고르는 캐릭터 번호. CharacterSelectUI가 선택 배경/바운스를 붙일 때
    /// 같은 번호를 쓰려고 읽어간다(배열 순서와 캐릭터 번호가 다를 수 있어서).</summary>
    public int CharacterIndex => characterIndex;


    public void OnClick()
    {
        PlayerPrefs.SetInt("SelectedCharacterIndex", characterIndex);
        PlayerPrefs.Save();
        LobbyCharacterChange.Instance.ChangeCharacterBody(characterIndex);
    }
}