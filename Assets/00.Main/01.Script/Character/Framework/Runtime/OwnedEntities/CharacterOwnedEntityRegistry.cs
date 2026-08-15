using System.Collections.Generic;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 한 캐릭터가 생성한 소유 오브젝트를 그룹과 생성 순서로 관리한다.
    /// 네트워크 생성/제거는 CharacterBase와 CharacterOwnedEntity가 담당한다.
    /// </summary>
    public sealed class CharacterOwnedEntityRegistry
    {
        private readonly Dictionary<OwnedEntityGroupId, List<CharacterOwnedEntity>> groups =
            new Dictionary<OwnedEntityGroupId, List<CharacterOwnedEntity>>();

        private int nextCreationSequence;

        public int ReserveCreationSequence()
        {
            nextCreationSequence++;
            return nextCreationSequence;
        }

        public bool TrySelectOverflowEntity(
            in OwnedEntitySpawnRequest request,
            out CharacterOwnedEntity replacement,
            out OwnedEntitySpawnFailureReason failureReason)
        {
            replacement = null;
            failureReason = OwnedEntitySpawnFailureReason.None;

            if (!request.Group.IsValid)
            {
                failureReason = OwnedEntitySpawnFailureReason.InvalidGroup;
                return false;
            }

            if (!request.HasValidCount)
            {
                failureReason = OwnedEntitySpawnFailureReason.InvalidCount;
                return false;
            }

            if (request.OwnerExitPolicy == OwnedEntityOwnerExitPolicy.TransferStateAuthority)
            {
                failureReason = OwnedEntitySpawnFailureReason.UnsupportedPolicy;
                return false;
            }

            if (request.OverflowPolicy == OwnedEntityOverflowPolicy.Unlimited)
                return true;

            List<CharacterOwnedEntity> entities = GetMutableGroup(request.Group);
            RemoveInvalid(entities);
            if (entities.Count < request.MaxCount)
                return true;

            switch (request.OverflowPolicy)
            {
                case OwnedEntityOverflowPolicy.DestroyOldest:
                    replacement = FindBySequence(entities, findOldest: true);
                    return replacement != null;
                case OwnedEntityOverflowPolicy.DestroyNewest:
                    replacement = FindBySequence(entities, findOldest: false);
                    return replacement != null;
                default:
                    failureReason = OwnedEntitySpawnFailureReason.CountLimitReached;
                    return false;
            }
        }

        public bool Register(CharacterOwnedEntity entity)
        {
            if (!IsValid(entity) || !entity.Group.IsValid)
                return false;

            List<CharacterOwnedEntity> entities = GetMutableGroup(entity.Group);
            RemoveInvalid(entities);
            if (entities.Contains(entity))
                return false;

            entities.Add(entity);
            return true;
        }

        public bool Unregister(CharacterOwnedEntity entity)
        {
            if (entity == null || !groups.TryGetValue(entity.Group, out List<CharacterOwnedEntity> entities))
                return false;

            bool removed = entities.Remove(entity);
            if (entities.Count == 0)
                groups.Remove(entity.Group);
            return removed;
        }

        public bool Contains(CharacterOwnedEntity entity)
        {
            return entity != null &&
                   groups.TryGetValue(entity.Group, out List<CharacterOwnedEntity> entities) &&
                   entities.Contains(entity);
        }

        public IReadOnlyList<T> Get<T>(OwnedEntityGroupId group) where T : CharacterOwnedEntity
        {
            List<T> result = new List<T>();
            if (!groups.TryGetValue(group, out List<CharacterOwnedEntity> entities))
                return result.AsReadOnly();

            RemoveInvalid(entities);
            foreach (CharacterOwnedEntity entity in entities)
            {
                if (entity is T typed)
                    result.Add(typed);
            }

            return result.AsReadOnly();
        }

        public IReadOnlyList<CharacterOwnedEntity> GetAll()
        {
            List<CharacterOwnedEntity> result = new List<CharacterOwnedEntity>();
            foreach (List<CharacterOwnedEntity> entities in groups.Values)
            {
                RemoveInvalid(entities);
                result.AddRange(entities);
            }

            return result.AsReadOnly();
        }

        public void Prune()
        {
            List<OwnedEntityGroupId> emptyGroups = new List<OwnedEntityGroupId>();
            foreach (KeyValuePair<OwnedEntityGroupId, List<CharacterOwnedEntity>> pair in groups)
            {
                RemoveInvalid(pair.Value);
                if (pair.Value.Count == 0)
                    emptyGroups.Add(pair.Key);
            }

            foreach (OwnedEntityGroupId group in emptyGroups)
                groups.Remove(group);
        }

        public void Clear()
        {
            groups.Clear();
        }

        private List<CharacterOwnedEntity> GetMutableGroup(OwnedEntityGroupId group)
        {
            if (!groups.TryGetValue(group, out List<CharacterOwnedEntity> entities))
            {
                entities = new List<CharacterOwnedEntity>();
                groups.Add(group, entities);
            }

            return entities;
        }

        private static CharacterOwnedEntity FindBySequence(
            List<CharacterOwnedEntity> entities,
            bool findOldest)
        {
            CharacterOwnedEntity selected = null;
            foreach (CharacterOwnedEntity entity in entities)
            {
                if (!IsValid(entity))
                    continue;

                if (selected == null ||
                    (findOldest && entity.CreationSequence < selected.CreationSequence) ||
                    (!findOldest && entity.CreationSequence > selected.CreationSequence))
                {
                    selected = entity;
                }
            }

            return selected;
        }

        private static void RemoveInvalid(List<CharacterOwnedEntity> entities)
        {
            entities.RemoveAll(entity => !IsValid(entity));
        }

        private static bool IsValid(CharacterOwnedEntity entity)
        {
            return entity != null && entity.Object != null && entity.Object.IsValid && !entity.IsDestroying;
        }
    }
}
