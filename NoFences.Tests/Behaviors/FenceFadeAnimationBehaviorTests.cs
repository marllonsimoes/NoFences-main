using FluentAssertions;
using NoFences.Behaviors;
using NoFences.Core.Model;
using System;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace NoFences.Tests.Behaviors
{
    public class FenceFadeAnimationBehaviorTests : IDisposable
    {
        private readonly Control testControl;
        private readonly FenceInfo testFenceInfo;
        private FenceFadeAnimationBehavior behavior;

        public FenceFadeAnimationBehaviorTests()
        {
            testControl = new Panel { Width = 300, Height = 300 };
            testFenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = "Test Fence",
                EnableFadeEffect = true
            };
        }

        public void Dispose()
        {
            behavior?.Dispose();
            testControl?.Dispose();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true);

            // Assert
            behavior.Should().NotBeNull();
            behavior.CurrentOpacity.Should().Be(1.0);
            behavior.IsFadedOut.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithNullControl_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceFadeAnimationBehavior(
                null,
                () => testFenceInfo,
                () => true);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("containerControl");
        }

        [Fact]
        public void Constructor_WithNullGetFenceInfo_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceFadeAnimationBehavior(
                testControl,
                null,
                () => true);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("getFenceInfo");
        }

        [Fact]
        public void Constructor_WithNullHasContentCheck_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("hasContentCheck");
        }

        [Fact]
        public void FadeOut_WithContent_ShouldTriggerAnimation()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true); // Has content

            bool opacityChanged = false;
            behavior.OpacityChanged += (s, opacity) => opacityChanged = true;

            // Act
            behavior.FadeOut();

            // Allow animation to start and process Windows Forms timer messages
            for (int i = 0; i < 5; i++)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }

            // Assert
            behavior.IsFadedOut.Should().BeTrue();
            opacityChanged.Should().BeTrue("opacity changed event should fire during animation");
        }

        [Fact]
        public void FadeOut_WithoutContent_ShouldNotFade()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => false); // No content

            bool opacityChanged = false;
            behavior.OpacityChanged += (s, opacity) => opacityChanged = true;

            // Act
            behavior.FadeOut();

            // Allow animation time
            Thread.Sleep(50);

            // Assert
            behavior.IsFadedOut.Should().BeFalse("should not fade without content");
            opacityChanged.Should().BeFalse("opacity should not change without content");
        }

        [Fact]
        public void FadeIn_WithContent_ShouldRestoreOpacity()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true);

            // First fade out
            behavior.FadeOut();

            // Process Windows Forms timer messages for fade out
            for (int i = 0; i < 10; i++)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }

            double opacityAfterFadeOut = behavior.CurrentOpacity;

            // Act
            behavior.FadeIn();

            // Process Windows Forms timer messages for fade in
            for (int i = 0; i < 10; i++)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }

            // Assert
            behavior.IsFadedOut.Should().BeFalse();
            behavior.CurrentOpacity.Should().BeGreaterThan(opacityAfterFadeOut,
                "opacity should increase during fade in");
        }

        [Fact]
        public void SetMinifiedOpacity_ShouldSetTo30Percent()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true);

            bool opacityChanged = false;
            double receivedOpacity = 0;
            behavior.OpacityChanged += (s, opacity) =>
            {
                opacityChanged = true;
                receivedOpacity = opacity;
            };

            // Act
            behavior.SetMinifiedOpacity();

            // Assert
            behavior.IsFadedOut.Should().BeTrue();
            behavior.CurrentOpacity.Should().Be(0.3);
            opacityChanged.Should().BeTrue();
            receivedOpacity.Should().Be(0.3);
        }

        [Fact]
        public void ResetOpacity_ShouldRestoreFullOpacity()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true);

            // Set to minified first
            behavior.SetMinifiedOpacity();

            bool opacityChanged = false;
            double receivedOpacity = 0;
            behavior.OpacityChanged += (s, opacity) =>
            {
                opacityChanged = true;
                receivedOpacity = opacity;
            };

            // Act
            behavior.ResetOpacity();

            // Assert
            behavior.IsFadedOut.Should().BeFalse();
            behavior.CurrentOpacity.Should().Be(1.0);
            opacityChanged.Should().BeTrue();
            receivedOpacity.Should().Be(1.0);
        }

        [Fact]
        public void Start_ShouldBeginMouseTracking()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true);

            // Act
            behavior.Start();

            // Assert
            // If Start() doesn't throw and behavior continues to work, it's initialized
            behavior.Should().NotBeNull();

            // Cleanup
            behavior.Stop();
        }

        [Fact]
        public void Stop_ShouldCleanupTimers()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true);

            behavior.Start();

            // Act
            behavior.Stop();

            // Assert
            // After stop, starting a fade should still work (no disposed timer errors)
            Action act = () => behavior.FadeOut();
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            behavior = new FenceFadeAnimationBehavior(
                testControl,
                () => testFenceInfo,
                () => true);

            behavior.Start();

            // Act
            behavior.Dispose();

            // Assert
            // No exception should be thrown
            // (This test verifies proper cleanup, not post-dispose behavior)
            behavior.Should().NotBeNull();
        }
    }
}
