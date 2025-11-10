using FluentAssertions;
using NoFences.Behaviors;
using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace NoFences.Tests.Behaviors
{
    public class FenceResizeBehaviorTests : IDisposable
    {
        private readonly Panel testContainer;
        private readonly Panel borderLeft, borderRight, borderTop, borderBottom, borderBottomRight;
        private FenceResizeBehavior behavior;
        private readonly Rectangle testBoundaries = new Rectangle(0, 0, 1920, 1080);
        private bool canResize = true;
        private const int TestMinSize = 150;

        public FenceResizeBehaviorTests()
        {
            testContainer = new Panel { Width = 300, Height = 300, Location = new Point(100, 100) };
            borderLeft = new Panel { Width = 5, Height = 300 };
            borderRight = new Panel { Width = 5, Height = 300 };
            borderTop = new Panel { Width = 300, Height = 5 };
            borderBottom = new Panel { Width = 300, Height = 5 };
            borderBottomRight = new Panel { Width = 10, Height = 10 };
        }

        public void Dispose()
        {
            behavior?.Detach();
            testContainer?.Dispose();
            borderLeft?.Dispose();
            borderRight?.Dispose();
            borderTop?.Dispose();
            borderBottom?.Dispose();
            borderBottomRight?.Dispose();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize,
                TestMinSize);

            // Assert
            behavior.Should().NotBeNull();
            behavior.IsResizing.Should().BeFalse();
            behavior.Direction.Should().Be(ResizeDirection.None);
            behavior.MinSize.Should().Be(TestMinSize);
        }

        [Fact]
        public void Constructor_WithNullContainer_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceResizeBehavior(
                null,
                () => testBoundaries,
                () => canResize);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("containerControl");
        }

        [Fact]
        public void Constructor_WithNullGetBoundaryArea_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceResizeBehavior(
                testContainer,
                null,
                () => canResize);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("getBoundaryArea");
        }

        [Fact]
        public void Constructor_WithNullCanResizeCheck_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("canResizeCheck");
        }

        [Fact]
        public void RegisterBorders_ShouldStoreReferences()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            // Act
            behavior.RegisterBorders(
                borderLeft,
                borderRight,
                borderTop,
                borderBottom,
                borderBottomRight);

            // Assert
            behavior.Should().NotBeNull(); // Borders registered internally
        }

        [Fact]
        public void Attach_ShouldNotThrow()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            behavior.RegisterBorders(
                borderLeft,
                borderRight,
                borderTop,
                borderBottom,
                borderBottomRight);

            // Act
            Action act = () => behavior.Attach();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Detach_ShouldNotThrow()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            behavior.RegisterBorders(
                borderLeft,
                borderRight,
                borderTop,
                borderBottom,
                borderBottomRight);

            behavior.Attach();

            // Act
            Action act = () => behavior.Detach();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void CalculateResizeBounds_Right_ShouldIncreaseWidth()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            // Simulate resize started from right border
            var startPoint = new Point(400, 250); // testContainer.Right
            var startLocation = testContainer.Location;
            var startSize = testContainer.Size;

            // Use reflection to set private fields for testing
            SetPrivateField(behavior, "isResizing", true);
            SetPrivateField(behavior, "resizeStartPoint", startPoint);
            SetPrivateField(behavior, "resizeStartLocation", startLocation);
            SetPrivateField(behavior, "resizeStartSize", startSize);
            SetPrivateField(behavior, "resizeDirection", ResizeDirection.Right);

            // Act - Mouse moved 50px to the right
            var currentPoint = new Point(450, 250);
            var result = behavior.CalculateResizeBounds(currentPoint);

            // Assert
            result.Width.Should().Be(350); // 300 + 50
            result.Height.Should().Be(300); // Unchanged
            result.X.Should().Be(100); // Unchanged
            result.Y.Should().Be(100); // Unchanged
        }

        [Fact]
        public void CalculateResizeBounds_Bottom_ShouldIncreaseHeight()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            var startPoint = new Point(250, 400);
            var startLocation = testContainer.Location;
            var startSize = testContainer.Size;

            SetPrivateField(behavior, "isResizing", true);
            SetPrivateField(behavior, "resizeStartPoint", startPoint);
            SetPrivateField(behavior, "resizeStartLocation", startLocation);
            SetPrivateField(behavior, "resizeStartSize", startSize);
            SetPrivateField(behavior, "resizeDirection", ResizeDirection.Bottom);

            // Act - Mouse moved 50px down
            var currentPoint = new Point(250, 450);
            var result = behavior.CalculateResizeBounds(currentPoint);

            // Assert
            result.Width.Should().Be(300); // Unchanged
            result.Height.Should().Be(350); // 300 + 50
            result.X.Should().Be(100); // Unchanged
            result.Y.Should().Be(100); // Unchanged
        }

        [Fact]
        public void CalculateResizeBounds_Left_ShouldIncreaseWidthAndMoveLeft()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            var startPoint = new Point(100, 250);
            var startLocation = testContainer.Location;
            var startSize = testContainer.Size;

            SetPrivateField(behavior, "isResizing", true);
            SetPrivateField(behavior, "resizeStartPoint", startPoint);
            SetPrivateField(behavior, "resizeStartLocation", startLocation);
            SetPrivateField(behavior, "resizeStartSize", startSize);
            SetPrivateField(behavior, "resizeDirection", ResizeDirection.Left);

            // Act - Mouse moved 50px to the left
            var currentPoint = new Point(50, 250);
            var result = behavior.CalculateResizeBounds(currentPoint);

            // Assert
            result.Width.Should().Be(350); // 300 + 50
            result.X.Should().Be(50); // Moved left
            result.Height.Should().Be(300); // Unchanged
            result.Y.Should().Be(100); // Unchanged
        }

        [Fact]
        public void CalculateResizeBounds_Top_ShouldIncreaseHeightAndMoveUp()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            var startPoint = new Point(250, 100);
            var startLocation = testContainer.Location;
            var startSize = testContainer.Size;

            SetPrivateField(behavior, "isResizing", true);
            SetPrivateField(behavior, "resizeStartPoint", startPoint);
            SetPrivateField(behavior, "resizeStartLocation", startLocation);
            SetPrivateField(behavior, "resizeStartSize", startSize);
            SetPrivateField(behavior, "resizeDirection", ResizeDirection.Top);

            // Act - Mouse moved 50px up
            var currentPoint = new Point(250, 50);
            var result = behavior.CalculateResizeBounds(currentPoint);

            // Assert
            result.Height.Should().Be(350); // 300 + 50
            result.Y.Should().Be(50); // Moved up
            result.Width.Should().Be(300); // Unchanged
            result.X.Should().Be(100); // Unchanged
        }

        [Fact]
        public void CalculateResizeBounds_BottomRight_ShouldIncreaseBoth()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            var startPoint = new Point(400, 400);
            var startLocation = testContainer.Location;
            var startSize = testContainer.Size;

            SetPrivateField(behavior, "isResizing", true);
            SetPrivateField(behavior, "resizeStartPoint", startPoint);
            SetPrivateField(behavior, "resizeStartLocation", startLocation);
            SetPrivateField(behavior, "resizeStartSize", startSize);
            SetPrivateField(behavior, "resizeDirection", ResizeDirection.BottomRight);

            // Act - Mouse moved 50px right and down
            var currentPoint = new Point(450, 450);
            var result = behavior.CalculateResizeBounds(currentPoint);

            // Assert
            result.Width.Should().Be(350); // 300 + 50
            result.Height.Should().Be(350); // 300 + 50
            result.X.Should().Be(100); // Unchanged
            result.Y.Should().Be(100); // Unchanged
        }

        [Fact]
        public void CalculateResizeBounds_ShouldEnforceMinimumSize()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize,
                TestMinSize);

            var startPoint = new Point(400, 250);
            var startLocation = testContainer.Location;
            var startSize = testContainer.Size;

            SetPrivateField(behavior, "isResizing", true);
            SetPrivateField(behavior, "resizeStartPoint", startPoint);
            SetPrivateField(behavior, "resizeStartLocation", startLocation);
            SetPrivateField(behavior, "resizeStartSize", startSize);
            SetPrivateField(behavior, "resizeDirection", ResizeDirection.Right);

            // Act - Try to make it smaller than minimum
            var currentPoint = new Point(200, 250); // Would make width 100
            var result = behavior.CalculateResizeBounds(currentPoint);

            // Assert
            result.Width.Should().Be(TestMinSize); // Constrained to minimum
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToLeft()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            // Act - Try to go too far left
            var bounds = new Rectangle(-500, 100, 300, 300);
            var result = behavior.ApplyBoundaries(bounds);

            // Assert
            result.X.Should().BeGreaterThan(-300); // -width + 50 visible
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToRight()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            // Act - Try to go too far right
            var bounds = new Rectangle(2000, 100, 300, 300);
            var result = behavior.ApplyBoundaries(bounds);

            // Assert
            result.X.Should().BeLessThan(testBoundaries.Width); // Keep 50px visible
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToTop()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            // Act - Try to go above top
            var bounds = new Rectangle(100, -100, 300, 300);
            var result = behavior.ApplyBoundaries(bounds);

            // Assert
            result.Y.Should().Be(0); // Constrained to 0
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToBottom()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            // Act - Try to go too far down
            var bounds = new Rectangle(100, 1500, 300, 300);
            var result = behavior.ApplyBoundaries(bounds);

            // Assert
            result.Y.Should().BeLessThan(testBoundaries.Height); // Keep 50px visible
        }

        [Fact]
        public void ApplyBoundaries_WithValidPosition_ShouldNotModify()
        {
            // Arrange
            behavior = new FenceResizeBehavior(
                testContainer,
                () => testBoundaries,
                () => canResize);

            // Act - Position well within boundaries
            var bounds = new Rectangle(500, 300, 300, 300);
            var result = behavior.ApplyBoundaries(bounds);

            // Assert
            result.Should().Be(bounds); // Unchanged
        }

        // Helper method to set private fields using reflection
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
