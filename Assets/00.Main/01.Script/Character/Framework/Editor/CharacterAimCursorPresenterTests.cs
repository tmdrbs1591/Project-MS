#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Tests
{
    public sealed class CharacterAimCursorPresenterTests
    {
        [Test]
        public void Apply_ShowsAndOffsetsLocalCrosshair()
        {
            CharacterAimCursorPresenter presenter = new CharacterAimCursorPresenter();
            try
            {
                presenter.Apply(
                    new Vector2(40f, -20f),
                    hasCrosshairOverride: true,
                    crosshairVisible: true,
                    hasCursorOverride: false,
                    systemCursorVisible: true);

                Assert.That(presenter.IsCrosshairVisible, Is.True);
                Assert.That(presenter.ScreenOffset, Is.EqualTo(new Vector2(40f, -20f)));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void Apply_RemovesCrosshairWhenOverrideEnds()
        {
            CharacterAimCursorPresenter presenter = new CharacterAimCursorPresenter();
            try
            {
                presenter.Apply(Vector2.zero, true, true, false, true);
                presenter.Apply(Vector2.zero, false, false, false, true);

                Assert.That(presenter.IsCrosshairVisible, Is.False);
            }
            finally
            {
                presenter.Dispose();
            }
        }
    }
}
#endif
