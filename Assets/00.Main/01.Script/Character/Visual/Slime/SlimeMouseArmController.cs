using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 화면 중앙을 기준으로 마우스가 왼쪽에 있는지 오른쪽에 있는지 판단해서 슬라임 팔 전체를 좌우 반전한다.
///
/// [역할]
///   - 마우스가 화면 중앙보다 왼쪽이면 Arm Root를 좌우 반전한다.
///   - 마우스가 화면 중앙보다 오른쪽이면 Arm Root를 기본 방향으로 되돌린다.
///   - Fire Point에서 마우스 월드 좌표까지의 발사 방향을 계산한다.
///
/// [필요한 것]
///   - Player 루트에 SlimeCharacter와 함께 붙인다.
///   - Arm Root에는 팔 전체를 묶은 부모 오브젝트를 연결한다.
///   - Fire Point에는 발사체가 생성될 위치를 연결한다.
/// </summary>
public class SlimeMouseArmController : MonoBehaviour
{
    [Header("Arm")]
    [SerializeField] private Transform armRoot;
    [SerializeField] private Transform firePoint;

    [Header("Aim")]
    [SerializeField] private float rotationOffset;

    private Vector3 armRootBaseScale;
    private int armDirection = 1;

    public Transform FirePoint => firePoint;
    public int ArmDirection => armDirection;

    private void Awake()
    {
        if (armRoot != null)
            armRootBaseScale = armRoot.localScale;
    }

    private void Update()
    {
        UpdateArmAimByMousePosition();
    }

    public Vector2 GetAimDirection(Camera targetCamera, Transform fallbackPoint)
    {
        Transform spawnPoint = firePoint != null ? firePoint : fallbackPoint;

        if (spawnPoint == null)
            return armDirection > 0 ? Vector2.right : Vector2.left;

        Vector3 mouseWorldPosition = GetMouseWorldPosition(targetCamera, spawnPoint.position.z);
        Vector2 direction = mouseWorldPosition - spawnPoint.position;

        if (direction.sqrMagnitude < 0.001f)
            return armDirection > 0 ? Vector2.right : Vector2.left;

        return direction.normalized;
    }

    private void UpdateArmAimByMousePosition()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Camera targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Vector3 characterScreenPosition = targetCamera.WorldToScreenPoint(transform.position);
        bool mouseOnLeftSide = mouse.position.ReadValue().x < characterScreenPosition.x;

        armDirection = mouseOnLeftSide ? -1 : 1;

        ApplyArmDirection();
    }

    private void ApplyArmDirection()
    {
        if (armRoot == null)
            return;

        armRoot.localScale = new Vector3(
            Mathf.Abs(armRootBaseScale.x) * armDirection,
            armRootBaseScale.y,
            armRootBaseScale.z
        );

        Vector3 mouseWorldPosition = GetMouseWorldPosition(Camera.main, armRoot.position.z);
        Vector2 direction = mouseWorldPosition - armRoot.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (armDirection < 0)
            angle -= 180f;

        armRoot.localRotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }

    private Vector3 GetMouseWorldPosition(Camera targetCamera, float z)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null || Mouse.current == null)
            return transform.position;

        Vector3 screenPosition = Mouse.current.position.ReadValue();
        screenPosition.z = Mathf.Abs(targetCamera.transform.position.z - z);

        return targetCamera.ScreenToWorldPoint(screenPosition);
    }
}