using ProjectMS.CharacterSystem;
using UnityEngine;

public class LobbyCharacterChange : MonoBehaviour
{
    static public LobbyCharacterChange Instance { get; private set; }

    /// <summary>선택한 캐릭터 인덱스 저장 키. CharacterChangeUi / CharacterSelectUI /
    /// PlayerSpawner가 같은 키를 쓴다(기본값 0 = 거너).</summary>
    public const string SelectedCharacterIndexKey = "SelectedCharacterIndex";

    [Header("로비 캐릭터 프리팹 목록")]
    [SerializeField] private GameObject[] characterPrefabs;

    [Header("로비 캐릭터 변경 설정")]
    [SerializeField] private string bodyRootName = "Body";
    private GameObject currentBodyRoot;
    private LobbyCharacterController lobbyCharacterController;

    private void Awake()
    {
        Instance = this;
        lobbyCharacterController = GetComponent<LobbyCharacterController>();
        currentBodyRoot = transform.Find(bodyRootName).gameObject;
    }

    private void Start()
    {
        // 로비에 들어올 때마다 마지막에 고른 캐릭터로 맞춘다. 저장된 게 없으면(=첫 실행)
        // 0번 거너. 인게임 PlayerSpawner도 같은 키/기본값을 보므로 로비와 인게임이 항상 같다.
        int index = PlayerPrefs.GetInt(SelectedCharacterIndexKey, 0);
        if (index < 0 || characterPrefabs == null || index >= characterPrefabs.Length)
            index = 0;

        PlayerPrefs.SetInt(SelectedCharacterIndexKey, index);
        ChangeCharacterBody(index);
    }

    public void ChangeCharacterBody(int index)
    {
        Transform body = null;
        if (characterPrefabs[index].transform.Find(bodyRootName))
        {
            body = characterPrefabs[index].transform.Find(bodyRootName);
        }
        else if (characterPrefabs[index].transform.Find("VisualRoot"))
        {
            body = characterPrefabs[index].transform.Find("VisualRoot");
        }

        if (body == null) return;

        if (currentBodyRoot != null) Destroy(currentBodyRoot);

        currentBodyRoot = Instantiate(body.gameObject, transform);
        currentBodyRoot.transform.localPosition = body.localPosition;
        currentBodyRoot.transform.localRotation = body.localRotation;
        currentBodyRoot.transform.localScale = body.localScale;

        if (lobbyCharacterController != null)
        {
            var visualController = currentBodyRoot.GetComponentInChildren<CharacterVisualController>();
            lobbyCharacterController.SetVisual(visualController);

            // 걸을 때 통통 튀는지(AutoHop)는 캐릭터마다 다르다. 로비는 CharacterBase를 안 쓰고
            // Body만 떼어오기 때문에, 원본 프리팹의 CharacterDefinition을 직접 읽어서 맞춰준다.
            var characterBase = characterPrefabs[index].GetComponentInChildren<CharacterBase>(true);
            CharacterDefinition definition = characterBase != null ? characterBase.Definition : null;
            lobbyCharacterController.SetAutoHop(definition == null || definition.AutoHop);
        }
    }
}
