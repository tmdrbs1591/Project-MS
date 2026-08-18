using System;
using Fusion;

namespace ProjectMS.CharacterSystem
{
    [Serializable]
    public struct OwnedEntityGroupId : INetworkStruct, IEquatable<OwnedEntityGroupId>
    {
        public OwnedEntityGroupId(int value)
        {
            Value = value;
        }

        public int Value;
        public bool IsValid => Value > 0;

        public bool Equals(OwnedEntityGroupId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is OwnedEntityGroupId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public static bool operator ==(OwnedEntityGroupId left, OwnedEntityGroupId right) => left.Equals(right);
        public static bool operator !=(OwnedEntityGroupId left, OwnedEntityGroupId right) => !left.Equals(right);
    }
}
