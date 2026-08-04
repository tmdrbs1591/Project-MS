using System.Collections.Generic;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// Project-MS-specific integration kept separate from the reusable character framework.
    /// </summary>
    public abstract partial class CharacterBase
    {
        public static readonly List<CharacterBase> All = new List<CharacterBase>();

        public static CharacterBase LocalPlayer
        {
            get
            {
                foreach (CharacterBase character in All)
                {
                    if (character != null && character.IsLocalPlayer)
                        return character;
                }

                return null;
            }
        }

        private static bool lobbyControlLocked;

        private static bool IsProjectInputLocked => lobbyControlLocked;

        private static bool IsProjectGameplayLocked =>
            MatchManager.Instance != null && MatchManager.Instance.Phase != MatchPhase.Fighting;

        public static void SetLobbyControlLocked(bool locked)
        {
            lobbyControlLocked = locked;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetProjectIntegrationStatics()
        {
            lobbyControlLocked = false;
            All.Clear();
        }

        private void RegisterProjectIntegration()
        {
            if (!All.Contains(this))
                All.Add(this);
        }

        private void BindProjectHud()
        {
            if (IsLocalPlayer)
                CooldownHUD.Instance?.Bind(this);
        }

        private void UnregisterProjectIntegration()
        {
            All.Remove(this);

            if (IsLocalPlayer)
                CooldownHUD.Instance?.Unbind();
        }
    }
}
