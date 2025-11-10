using FluentAssertions;
using NoFences.Behaviors;
using System;
using System.Windows.Forms;
using Xunit;

namespace NoFences.Tests.Behaviors
{
    public class FenceMinifyBehaviorTests : IDisposable
    {
        private readonly Panel testControl;
        private FenceMinifyBehavior behavior;
        private const int TestTitleHeight = 35;
        private const int TestInitialHeight = 300;

        public FenceMinifyBehaviorTests()
        {
            testControl = new Panel { Width = 300, Height = TestInitialHeight };
        }

        public void Dispose()
        {
            testControl?.Dispose();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            // Assert
            behavior.Should().NotBeNull();
            behavior.IsMinified.Should().BeFalse();
            behavior.PreviousHeight.Should().Be(TestInitialHeight);
        }

        [Fact]
        public void Constructor_WithNullControl_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceMinifyBehavior(
                null,
                () => TestTitleHeight,
                () => true);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("containerControl");
        }

        [Fact]
        public void Constructor_WithNullGetTitleHeight_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceMinifyBehavior(
                testControl,
                null,
                () => true);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("getTitleHeight");
        }

        [Fact]
        public void Constructor_WithNullCanMinifyCheck_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("canMinifyCheck");
        }

        [Fact]
        public void TryMinify_WhenCanMinify_ShouldCollapseToTitleHeight()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true); // Can minify

            // Act
            bool result = behavior.TryMinify();

            // Assert
            result.Should().BeTrue("minify operation should succeed");
            behavior.IsMinified.Should().BeTrue();
            testControl.Height.Should().Be(TestTitleHeight);
            behavior.PreviousHeight.Should().Be(TestInitialHeight);
        }

        [Fact]
        public void TryMinify_WhenCannotMinify_ShouldReturnFalse()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => false); // Cannot minify

            // Act
            bool result = behavior.TryMinify();

            // Assert
            result.Should().BeFalse("minify should be prevented");
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight, "height should not change");
        }

        [Fact]
        public void TryMinify_WhenAlreadyMinified_ShouldReturnFalse()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            behavior.TryMinify(); // First minify

            // Act
            bool result = behavior.TryMinify(); // Try again

            // Assert
            result.Should().BeFalse("already minified");
            behavior.IsMinified.Should().BeTrue();
            testControl.Height.Should().Be(TestTitleHeight);
        }

        [Fact]
        public void TryMinify_ShouldRaiseStateChangedEvent()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            MinifyStateChangedEventArgs receivedArgs = null;
            behavior.StateChanged += (s, e) => receivedArgs = e;

            // Act
            behavior.TryMinify();

            // Assert
            receivedArgs.Should().NotBeNull();
            receivedArgs.State.Should().Be(MinifyState.Minified);
            receivedArgs.IsMinified.Should().BeTrue();
            receivedArgs.OldHeight.Should().Be(TestInitialHeight);
            receivedArgs.NewHeight.Should().Be(TestTitleHeight);
        }

        [Fact]
        public void TryExpand_WhenMinified_ShouldRestorePreviousHeight()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            behavior.TryMinify(); // First minify

            // Act
            bool result = behavior.TryExpand();

            // Assert
            result.Should().BeTrue("expand should succeed");
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight);
        }

        [Fact]
        public void TryExpand_WhenNotMinified_ShouldReturnFalse()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            // Act (try to expand without minifying first)
            bool result = behavior.TryExpand();

            // Assert
            result.Should().BeFalse("nothing to expand");
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight);
        }

        [Fact]
        public void TryExpand_ShouldRaiseStateChangedEvent()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            behavior.TryMinify();

            MinifyStateChangedEventArgs receivedArgs = null;
            behavior.StateChanged += (s, e) => receivedArgs = e;

            // Act
            behavior.TryExpand();

            // Assert
            receivedArgs.Should().NotBeNull();
            receivedArgs.State.Should().Be(MinifyState.Expanded);
            receivedArgs.IsMinified.Should().BeFalse();
            receivedArgs.OldHeight.Should().Be(TestTitleHeight);
            receivedArgs.NewHeight.Should().Be(TestInitialHeight);
        }

        [Fact]
        public void Toggle_WhenExpanded_ShouldMinify()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            // Act
            bool result = behavior.Toggle();

            // Assert
            result.Should().BeTrue();
            behavior.IsMinified.Should().BeTrue();
            testControl.Height.Should().Be(TestTitleHeight);
        }

        [Fact]
        public void Toggle_WhenMinified_ShouldExpand()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            behavior.TryMinify(); // First minify

            // Act
            bool result = behavior.Toggle();

            // Assert
            result.Should().BeTrue();
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight);
        }

        [Fact]
        public void ForceExpand_WhenMinified_ShouldExpandEvenIfCanMinifyIsFalse()
        {
            // Arrange
            bool canMinify = true;
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => canMinify);

            behavior.TryMinify();
            canMinify = false; // Disable minify

            // Act
            behavior.ForceExpand();

            // Assert
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight);
        }

        [Fact]
        public void ForceExpand_ShouldRaiseStateChangedEventWithForcedExpandState()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            behavior.TryMinify();

            MinifyStateChangedEventArgs receivedArgs = null;
            behavior.StateChanged += (s, e) => receivedArgs = e;

            // Act
            behavior.ForceExpand();

            // Assert
            receivedArgs.Should().NotBeNull();
            receivedArgs.State.Should().Be(MinifyState.ForcedExpand);
            receivedArgs.IsMinified.Should().BeFalse();
        }

        [Fact]
        public void ForceExpand_WhenNotMinified_ShouldDoNothing()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            int eventCount = 0;
            behavior.StateChanged += (s, e) => eventCount++;

            // Act
            behavior.ForceExpand(); // Not minified

            // Assert
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight);
            eventCount.Should().Be(0, "no event should be raised");
        }

        [Fact]
        public void UpdatePreviousHeight_WhenExpanded_ShouldUpdateStoredHeight()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            const int newHeight = 450;

            // Act
            testControl.Height = newHeight;
            behavior.UpdatePreviousHeight(newHeight);

            // Assert
            behavior.PreviousHeight.Should().Be(newHeight);

            // Now minify and expand to verify it restores new height
            behavior.TryMinify();
            behavior.TryExpand();
            testControl.Height.Should().Be(newHeight);
        }

        [Fact]
        public void UpdatePreviousHeight_WhenMinified_ShouldNotUpdateStoredHeight()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            behavior.TryMinify();

            const int newHeight = 450;

            // Act
            behavior.UpdatePreviousHeight(newHeight);

            // Assert
            behavior.PreviousHeight.Should().Be(TestInitialHeight,
                "stored height should not change while minified");
        }

        [Fact]
        public void GetSaveHeight_WhenExpanded_ShouldReturnCurrentHeight()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            testControl.Height = 400;

            // Act
            int saveHeight = behavior.GetSaveHeight();

            // Assert
            saveHeight.Should().Be(400);
        }

        [Fact]
        public void GetSaveHeight_WhenMinified_ShouldReturnPreviousHeight()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            behavior.TryMinify();

            // Act
            int saveHeight = behavior.GetSaveHeight();

            // Assert
            saveHeight.Should().Be(TestInitialHeight,
                "should return full height, not title height");
            testControl.Height.Should().Be(TestTitleHeight,
                "control should still be minified");
        }

        [Fact]
        public void MinifyExpandCycle_ShouldMaintainCorrectState()
        {
            // Arrange
            behavior = new FenceMinifyBehavior(
                testControl,
                () => TestTitleHeight,
                () => true);

            // Act & Assert - Full cycle
            behavior.TryMinify();
            behavior.IsMinified.Should().BeTrue();
            testControl.Height.Should().Be(TestTitleHeight);

            behavior.TryExpand();
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight);

            // Second cycle
            behavior.TryMinify();
            behavior.IsMinified.Should().BeTrue();
            testControl.Height.Should().Be(TestTitleHeight);

            behavior.TryExpand();
            behavior.IsMinified.Should().BeFalse();
            testControl.Height.Should().Be(TestInitialHeight);
        }
    }
}
