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

    [Header("교체 연출")]
    [Tooltip("캐릭터가 바뀐 걸 보여줄 때 캐릭터 위치에 터뜨릴 이펙트. 캐릭터별로 따로 쓰고 싶으면 swapVfxByCharacter에 넣는다.")]
    [SerializeField] private GameObject swapVfx;
    [Tooltip("캐릭터 번호 순서. 비어 있는 칸은 swapVfx를 쓴다.")]
    [SerializeField] private GameObject[] swapVfxByCharacter;
    [SerializeField] private float swapVfxScale = 1f;
    [SerializeField] private Vector3 swapVfxOffset = new Vector3(0f, 0.3f, 0f);
    [SerializeField] private AudioClip swapSound;
    [Range(0f, 1f)] [SerializeField] private float swapVolume = 0.7f;
    [Min(0.01f)] [SerializeField] private float popDuration = 0.4f;

    /// <summary>지금 로비 캐릭터의 정의(이름/그림). 로비 HUD가 읽는다.</summary>
    public CharacterDefinition CurrentDefinition { get; private set; }

    private GameObject currentBodyRoot;
    private int currentIndex;
    private Coroutine swapRoutine;
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

        currentIndex = index;
        CharacterBase sourceBase = characterPrefabs[index].GetComponentInChildren<CharacterBase>(true);
        CurrentDefinition = sourceBase != null ? sourceBase.Definition : null;
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
            lobbyCharacterController.SetAutoHop(CurrentDefinition == null || CurrentDefinition.AutoHop);
        }
    }

    /// <summary>캐릭터가 바뀐 걸 보여주는 연출: delay 뒤에 이펙트가 터지고 새 캐릭터가 뿅 하고 커지며 나타난다.
    /// 캐릭터 선택 창이 닫혀 카메라가 돌아올 때 CharacterSelectUI가 부른다(창이 열려 있는 동안엔
    /// 카메라가 건물 쪽을 보고 있어서 캐릭터가 화면 밖이다).</summary>
    public void PlaySwapEffect(float delay)
    {
        if (swapRoutine != null)
            StopCoroutine(swapRoutine);
        swapRoutine = StartCoroutine(SwapEffectRoutine(delay));
    }

    private System.Collections.IEnumerator SwapEffectRoutine(float delay)
    {
        // 기다리는 동안엔 숨겨둬야 "뿅" 하고 나타난다.
        Transform body = currentBodyRoot != null ? currentBodyRoot.transform : null;
        Vector3 baseScale = body != null ? body.localScale : Vector3.one;
        if (body != null)
            body.localScale = Vector3.zero;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        GameObject vfxPrefab = swapVfxByCharacter != null && currentIndex < swapVfxByCharacter.Length && swapVfxByCharacter[currentIndex] != null
            ? swapVfxByCharacter[currentIndex]
            : swapVfx;
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.position + swapVfxOffset, Quaternion.identity);
            vfx.transform.localScale *= swapVfxScale;
            Destroy(vfx, 3f);
        }
        SoundManager.Instance?.PlaySfx(swapSound, swapVolume);

        float elapsed = 0f;
        while (elapsed < popDuration && body != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            // 살짝 넘쳤다가 돌아오는 튕김
            const float c1 = 2.2f;
            float x = t - 1f;
            float s = 1f + (c1 + 1f) * x * x * x + c1 * x * x;
            body.localScale = baseScale * s;
            yield return null;
        }

        if (body != null)
            body.localScale = baseScale;
        swapRoutine = null;
    }
}
