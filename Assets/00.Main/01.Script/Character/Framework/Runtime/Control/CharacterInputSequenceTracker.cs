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

        /// <summary>마지막 확인 이후 새로 들어온 입력이 몇 번인지 반환하고 기준을 갱신한다.
        /// WasPressed와 달리 한 번에 여러 번 눌려도 누락 없이 센다. 처음 보는 대상/입력은
        /// 이전 기록을 새 입력으로 착각하지 않도록 0을 반환하고 기준만 잡는다.</summary>
        public int ConsumeNewPresses(int targetKey, CharacterInputType input, int sequence)
        {
            Key key = new Key(targetKey, input);
            if (!lastSequences.TryGetValue(key, out int previous))
            {
                lastSequences.Add(key, sequence);
                return 0;
            }

            lastSequences[key] = sequence;
            return sequence > previous ? sequence - previous : 0;
        }

        /// <summary>지금까지의 입력은 이미 본 것으로 치도록 기준을 현재 순번으로 맞춘다.
        /// 관찰을 새로 시작하기 직전에 호출해서, 그 전에 눌렀던 입력이 세어지지 않게 한다.</summary>
        public void SyncBaseline(int targetKey, CharacterInputType input, int sequence)
        {
            lastSequences[new Key(targetKey, input)] = sequence;
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
