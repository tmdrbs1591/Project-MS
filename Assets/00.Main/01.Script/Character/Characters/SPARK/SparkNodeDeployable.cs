using Fusion.Addons.Physics;
using ProjectMS.CharacterSystem;
using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

public class SparkNodeDeployable : CharacterDeployable
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private NetworkRigidbody2D netRb;

    public bool IsStopped => isStopped;
    private bool isStopped;

    private Vector2? contactVector;
    private SparkCharacter ownerSpark;

    public void Initialize(SparkCharacter _ownerSpark)
    {
        if (!Object.HasStateAuthority)
            return;

        ownerSpark = _ownerSpark;
    }
    
    // 네트워크 틱 주기에 맞추려고 pendingNormal만 설정하고 실제 위치 변경은 FixedUpdateNetwork에서 한다.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!Object.HasStateAuthority || Runner.IsResimulation)
            return;

        if (collision.contactCount == 0)
            return;
        
        bool isCollisionGround = (groundMask.value & (1 << collision.collider.gameObject.layer)) != 0;
        if (!isCollisionGround)
            return;

        contactVector = collision.GetContact(0).normal;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!Object.HasStateAuthority)
            return;

        if (IsStopped || !contactVector.HasValue)
            return;

        FixNodePosition();
    }

    private void FixNodePosition()
    {
        netRb.Rigidbody.rotation = Vector2.SignedAngle(Vector2.up, contactVector.Value);
        netRb.Rigidbody.bodyType = RigidbodyType2D.Static;

        isStopped = true;
        
        if (ownerSpark != null)
            ownerSpark.OnNodeStopped();
    }
}