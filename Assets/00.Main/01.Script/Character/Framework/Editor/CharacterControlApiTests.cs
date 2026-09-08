#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Tests
{
    public sealed class CharacterControlApiTests
    {
        [Test]
        public void InputSequenceTracker_IgnoresHistoryThenReportsOnlyChanges()
        {
            CharacterInputSequenceTracker tracker = new CharacterInputSequenceTracker();

            Assert.That(tracker.WasPressed(10, CharacterInputType.Jump, 4), Is.False);
            Assert.That(tracker.WasPressed(10, CharacterInputType.Jump, 4), Is.False);
            Assert.That(tracker.WasPressed(10, CharacterInputType.Jump, 5), Is.True);
            Assert.That(tracker.WasPressed(10, CharacterInputType.Jump, 5), Is.False);
            Assert.That(tracker.WasPressed(20, CharacterInputType.Jump, 5), Is.False);
        }

        [Test]
        public void InputSequenceTracker_DoesNotReportSequenceResetAsInput()
        {
            CharacterInputSequenceTracker tracker = new CharacterInputSequenceTracker();

            Assert.That(tracker.WasPressed(10, CharacterInputType.SkillQ, 4), Is.False);
            Assert.That(tracker.WasPressed(10, CharacterInputType.SkillQ, 5), Is.True);
            Assert.That(tracker.WasPressed(10, CharacterInputType.SkillQ, 0), Is.False);
            Assert.That(tracker.WasPressed(10, CharacterInputType.SkillQ, 1), Is.True);
        }

        [TestCase("ApplyControlSeal", typeof(CharacterBase), typeof(CharacterControlType), typeof(float))]
        [TestCase("ApplyAimInversion", typeof(CharacterBase), typeof(float))]
        [TestCase("ApplyAimAngleOffset", typeof(CharacterBase), typeof(float), typeof(float))]
        [TestCase("SetCrosshairOffset", typeof(CharacterBase), typeof(Vector2), typeof(float))]
        [TestCase("SetCrosshairVisible", typeof(CharacterBase), typeof(bool), typeof(float))]
        [TestCase("SetSystemCursorVisible", typeof(CharacterBase), typeof(bool), typeof(float))]
        [TestCase("ReplaceCursorWithCrosshair", typeof(CharacterBase), typeof(float))]
        [TestCase("WasInputPressed", typeof(CharacterBase), typeof(CharacterInputType))]
        [TestCase("IsInputHeld", typeof(CharacterBase), typeof(CharacterInputType))]
        [TestCase("GetObservedMoveDirection", typeof(CharacterBase))]
        public void CharacterBase_ProvidesSimpleControlApi(string name, params Type[] parameters)
        {
            MethodInfo method = typeof(CharacterBase).GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                parameters,
                null);

            Assert.That(method, Is.Not.Null, $"CharacterBase.{name} API가 필요합니다.");
        }
    }
}
#endif
