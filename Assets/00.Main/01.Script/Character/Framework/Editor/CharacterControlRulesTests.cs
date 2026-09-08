#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Tests
{
    public sealed class CharacterControlRulesTests
    {
        [Test]
        public void AllActions_DoesNotIncludeMovementOrJump()
        {
            CharacterControlType mask = CharacterControlType.AllActions;

            Assert.That(CharacterControlRules.Contains(mask, CharacterControlType.BasicAttack), Is.True);
            Assert.That(CharacterControlRules.Contains(mask, CharacterControlType.SkillQ), Is.True);
            Assert.That(CharacterControlRules.Contains(mask, CharacterControlType.SkillE), Is.True);
            Assert.That(CharacterControlRules.Contains(mask, CharacterControlType.Dash), Is.True);
            Assert.That(CharacterControlRules.Contains(mask, CharacterControlType.Ultimate), Is.True);
            Assert.That(CharacterControlRules.Contains(mask, CharacterControlType.Movement), Is.False);
            Assert.That(CharacterControlRules.Contains(mask, CharacterControlType.Jump), Is.False);
        }

        [Test]
        public void FilterInput_RemovesOnlySealedControls()
        {
            CharacterInputSnapshot input = new CharacterInputSnapshot
            {
                MoveDirection = 1f,
                JumpPressed = true,
                JumpHeld = true,
                BasicAttackPressed = true,
                SkillQPressed = true,
                SkillEPressed = true,
                DashPressed = true,
                UltimatePressed = true,
                AimWorldPosition = new Vector2(4f, 2f)
            };

            CharacterInputSnapshot result = CharacterControlRules.FilterInput(
                input,
                CharacterControlType.Movement | CharacterControlType.Jump | CharacterControlType.SkillQ);

            Assert.That(result.MoveDirection, Is.Zero);
            Assert.That(result.JumpPressed, Is.False);
            Assert.That(result.JumpHeld, Is.False);
            Assert.That(result.SkillQPressed, Is.False);
            Assert.That(result.BasicAttackPressed, Is.True);
            Assert.That(result.SkillEPressed, Is.True);
            Assert.That(result.DashPressed, Is.True);
            Assert.That(result.UltimatePressed, Is.True);
            Assert.That(result.AimWorldPosition, Is.EqualTo(input.AimWorldPosition));
        }

        [Test]
        public void ShouldReplaceTimedEffect_KeepsLongerRemainingEffect()
        {
            Assert.That(CharacterControlRules.ShouldReplaceTimedEffect(null, 2f), Is.True);
            Assert.That(CharacterControlRules.ShouldReplaceTimedEffect(1f, 2f), Is.True);
            Assert.That(CharacterControlRules.ShouldReplaceTimedEffect(3f, 2f), Is.False);
            Assert.That(CharacterControlRules.ShouldReplaceTimedEffect(2f, 2f), Is.True);
        }

        [Test]
        public void TransformAim_InvertsThenAppliesAngleOffset()
        {
            Vector2 result = CharacterControlRules.TransformAim(
                Vector2.zero,
                new Vector2(2f, 0f),
                invert: true,
                angleOffsetDegrees: 30f);

            Assert.That(result.x, Is.EqualTo(-1.7320508f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(-1f).Within(0.0001f));
        }

        [Test]
        public void TransformAim_PreservesDistanceFromOrigin()
        {
            Vector2 origin = new Vector2(3f, -2f);
            Vector2 aim = new Vector2(6f, 2f);

            Vector2 result = CharacterControlRules.TransformAim(origin, aim, true, -45f);

            Assert.That(Vector2.Distance(origin, result), Is.EqualTo(Vector2.Distance(origin, aim)).Within(0.0001f));
        }
    }
}
#endif
