using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public readonly struct OwnedEntitySpawnRequest
    {
        public OwnedEntitySpawnRequest(
            Vector2 position,
            Quaternion rotation,
            OwnedEntityGroupId group,
            int maxCount = 1,
            OwnedEntityOverflowPolicy overflowPolicy = OwnedEntityOverflowPolicy.RejectNew,
            OwnedEntityOwnerExitPolicy ownerExitPolicy = OwnedEntityOwnerExitPolicy.Destroy,
            Vector2 initialVelocity = default)
        {
            Position = position;
            Rotation = rotation;
            InitialVelocity = initialVelocity;
            Group = group;
            MaxCount = maxCount;
            OverflowPolicy = overflowPolicy;
            OwnerExitPolicy = ownerExitPolicy;
        }

        public Vector2 Position { get; }
        public Quaternion Rotation { get; }
        public Vector2 InitialVelocity { get; }
        public OwnedEntityGroupId Group { get; }
        public int MaxCount { get; }
        public OwnedEntityOverflowPolicy OverflowPolicy { get; }
        public OwnedEntityOwnerExitPolicy OwnerExitPolicy { get; }

        public bool HasValidCount => OverflowPolicy == OwnedEntityOverflowPolicy.Unlimited || MaxCount > 0;
    }
}
