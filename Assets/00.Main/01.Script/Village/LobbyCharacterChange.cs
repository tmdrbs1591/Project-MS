using ProjectMS.CharacterSystem;
using UnityEngine;

public class LobbyCharacterChange : MonoBehaviour
{
    static public LobbyCharacterChange Instance { get; private set; }
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

    public void ChangeCharacterBody(int index)
    {
        Transform body = characterPrefabs[index].transform.Find(bodyRootName);
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
        }
    }
}
