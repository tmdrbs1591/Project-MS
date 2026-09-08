using System;
using System.Collections.Generic;

namespace ProjectMS.CharacterSystem
{
    /// <summary>대상별 매핑 입력 순번을 기억해 새 입력만 구분한다.</summary>
    public sealed class CharacterInputSequenceTracker
    {
        private readonly Dictionary<Key, int> lastSequences = new Dictionary<Key, int>();

        public bool WasPressed(int targetKey, CharacterInputType input, int sequence)
        {
            Key key = new Key(targetKey, input);
            if (!lastSequences.TryGetValue(key, out int previous))
            {
                lastSequences.Add(key, sequence);
                return false;
            }

            if (previous == sequence)
                return false;

            lastSequences[key] = sequence;
            if (sequence < previous)
                return false;

            return true;
        }

        public void Clear()
        {
            lastSequences.Clear();
        }

        private readonly struct Key : IEquatable<Key>
        {
            private readonly int targetKey;
            private readonly CharacterInputType input;

            public Key(int targetKey, CharacterInputType input)
            {
                this.targetKey = targetKey;
                this.input = input;
            }

            public bool Equals(Key other)
            {
                return targetKey == other.targetKey && input == other.input;
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (targetKey * 397) ^ (int)input;
                }
            }
        }
    }
}
