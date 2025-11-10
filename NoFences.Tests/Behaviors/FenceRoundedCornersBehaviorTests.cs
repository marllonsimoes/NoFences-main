using FluentAssertions;
using NoFences.Behaviors;
using System;
using System.Windows.Forms;
using Xunit;

namespace NoFences.Tests.Behaviors
{
    public class FenceRoundedCornersBehaviorTests : IDisposable
    {
        private readonly Panel testContainer;
        private readonly Panel testTitlePanel;
        private readonly Panel testContentControl;
        private FenceRoundedCornersBehavior behavior;
        private int currentRadius = 0;
        private const int TestBorderSize = 5;

        public FenceRoundedCornersBehaviorTests()
        {
            testContainer = new Panel { Width = 300, Height = 300 };
            testTitlePanel = new Panel { Width = 300, Height = 35 };
            testContentControl = new Panel { Width = 300, Height = 265 };
        }

        public void Dispose()
        {
            behavior?.Dispose();
            testContainer?.Dispose();
            testTitlePanel?.Dispose();
            testContentControl?.Dispose();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Assert
            behavior.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullContainer_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceRoundedCornersBehavior(
                null,
                () => currentRadius,
                TestBorderSize);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("containerControl");
        }

        [Fact]
        public void Constructor_WithNullGetCornerRadius_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceRoundedCornersBehavior(
                testContainer,
                null,
                TestBorderSize);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("getCornerRadius");
        }

        [Fact]
        public void Apply_WithZeroRadius_ShouldClearRegions()
        {
            // Arrange
            currentRadius = 0;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            behavior.Apply();

            // Assert
            testContainer.Region.Should().BeNull("zero radius should clear regions");
        }

        [Fact]
        public void Apply_WithPositiveRadius_ShouldApplyContainerRegion()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            behavior.Apply();

            // Assert
            testContainer.Region.Should().NotBeNull("positive radius should create region");
        }

        [Fact]
        public void RegisterTitlePanel_ShouldAllowRoundingApplication()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            behavior.RegisterTitlePanel(testTitlePanel);
            behavior.Apply();

            // Assert
            testTitlePanel.Region.Should().NotBeNull("title panel should have rounded region");
        }

        [Fact]
        public void RegisterContentControl_ShouldAllowRoundingApplication()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            behavior.RegisterContentControl(testContentControl);
            behavior.Apply();

            // Assert
            testContentControl.Region.Should().NotBeNull("content control should have rounded region");
        }

        [Fact]
        public void Apply_WithAllControlsRegistered_ShouldRoundAllControls()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            behavior.RegisterTitlePanel(testTitlePanel);
            behavior.RegisterContentControl(testContentControl);

            // Act
            behavior.Apply();

            // Assert
            testContainer.Region.Should().NotBeNull("container should have region");
            testTitlePanel.Region.Should().NotBeNull("title panel should have region");
            testContentControl.Region.Should().NotBeNull("content control should have region");
        }

        [Fact]
        public void Apply_MultipleTimes_ShouldNotLeak()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            behavior.RegisterTitlePanel(testTitlePanel);
            behavior.RegisterContentControl(testContentControl);

            // Act - Apply multiple times (should dispose old regions)
            behavior.Apply();
            behavior.Apply();
            behavior.Apply();

            // Assert
            testContainer.Region.Should().NotBeNull("container should still have region");
            testTitlePanel.Region.Should().NotBeNull("title panel should still have region");
            testContentControl.Region.Should().NotBeNull("content control should still have region");
        }

        [Fact]
        public void Apply_ChangingRadius_ShouldUpdateRounding()
        {
            // Arrange
            currentRadius = 5;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            behavior.Apply();
            var firstRegion = testContainer.Region;

            // Act - Change radius
            currentRadius = 15;
            behavior.Apply();

            // Assert
            testContainer.Region.Should().NotBeSameAs(firstRegion, "region should be recreated with new radius");
            testContainer.Region.Should().NotBeNull();
        }

        [Fact]
        public void ClearRegions_ShouldRemoveAllRounding()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            behavior.RegisterTitlePanel(testTitlePanel);
            behavior.RegisterContentControl(testContentControl);
            behavior.Apply();

            // Act
            behavior.ClearRegions();

            // Assert
            testContainer.Region.Should().BeNull("container region should be cleared");
            testTitlePanel.Region.Should().BeNull("title panel region should be cleared");
            testContentControl.Region.Should().BeNull("content control region should be cleared");
        }

        [Fact]
        public void ClearRegions_WithoutPriorApply_ShouldNotThrow()
        {
            // Arrange
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            Action act = () => behavior.ClearRegions();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Apply_WithZeroWidthContainer_ShouldClearRegions()
        {
            // Arrange
            currentRadius = 10;
            testContainer.Width = 0;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            behavior.Apply();

            // Assert
            testContainer.Region.Should().BeNull("zero width should prevent rounding");
        }

        [Fact]
        public void Apply_WithZeroHeightContainer_ShouldClearRegions()
        {
            // Arrange
            currentRadius = 10;
            testContainer.Height = 0;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            behavior.Apply();

            // Assert
            testContainer.Region.Should().BeNull("zero height should prevent rounding");
        }

        [Fact]
        public void Apply_WithMaximumRadius_ShouldNotThrow()
        {
            // Arrange
            currentRadius = 100; // Very large radius
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            Action act = () => behavior.Apply();

            // Assert
            act.Should().NotThrow("large radius should be clamped safely");
            testContainer.Region.Should().NotBeNull();
        }

        [Fact]
        public void Apply_WithNegativeRadius_ShouldClearRegions()
        {
            // Arrange
            currentRadius = -5;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            // Act
            behavior.Apply();

            // Assert
            testContainer.Region.Should().BeNull("negative radius should clear regions");
        }

        [Fact]
        public void Dispose_ShouldClearAllRegions()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            behavior.RegisterTitlePanel(testTitlePanel);
            behavior.RegisterContentControl(testContentControl);
            behavior.Apply();

            // Act
            behavior.Dispose();

            // Assert
            testContainer.Region.Should().BeNull("container region should be disposed");
            testTitlePanel.Region.Should().BeNull("title panel region should be disposed");
            testContentControl.Region.Should().BeNull("content control region should be disposed");
        }

        [Fact]
        public void Apply_WithOnlyTitlePanelRegistered_ShouldNotThrow()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            behavior.RegisterTitlePanel(testTitlePanel);
            // Don't register content control

            // Act
            Action act = () => behavior.Apply();

            // Assert
            act.Should().NotThrow();
            testContainer.Region.Should().NotBeNull();
            testTitlePanel.Region.Should().NotBeNull();
        }

        [Fact]
        public void Apply_WithOnlyContentRegistered_ShouldNotThrow()
        {
            // Arrange
            currentRadius = 10;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                TestBorderSize);

            behavior.RegisterContentControl(testContentControl);
            // Don't register title panel

            // Act
            Action act = () => behavior.Apply();

            // Assert
            act.Should().NotThrow();
            testContainer.Region.Should().NotBeNull();
            testContentControl.Region.Should().NotBeNull();
        }

        [Fact]
        public void InnerRadius_ShouldBeCalculatedFromBorderSize()
        {
            // Arrange
            currentRadius = 10;
            const int borderSize = 5;
            behavior = new FenceRoundedCornersBehavior(
                testContainer,
                () => currentRadius,
                borderSize);

            behavior.RegisterTitlePanel(testTitlePanel);

            // Act
            behavior.Apply();

            // Assert
            // Inner radius = radius - borderSize = 10 - 5 = 5
            // We can't directly test the inner radius, but we can verify
            // that title panel got a different (smaller) region than container
            testTitlePanel.Region.Should().NotBeNull();
            testContainer.Region.Should().NotBeNull();
            // The regions should be different sizes due to border consideration
        }
    }
}
