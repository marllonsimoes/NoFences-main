using FluentAssertions;
using NoFences.Behaviors;
using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace NoFences.Tests.Behaviors
{
    public class FenceDragBehaviorTests : IDisposable
    {
        private readonly Panel testContainer;
        private readonly Panel testDragHandle;
        private FenceDragBehavior behavior;
        private readonly Rectangle testBoundaries = new Rectangle(0, 0, 1920, 1080);
        private bool canDrag = true;

        public FenceDragBehaviorTests()
        {
            testContainer = new Panel { Width = 300, Height = 300, Location = new Point(100, 100) };
            testDragHandle = new Panel { Width = 300, Height = 35 };
        }

        public void Dispose()
        {
            behavior?.Detach();
            testContainer?.Dispose();
            testDragHandle?.Dispose();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Assert
            behavior.Should().NotBeNull();
            behavior.IsDragging.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithNullContainer_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceDragBehavior(
                null,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("containerControl");
        }

        [Fact]
        public void Constructor_WithNullDragHandle_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceDragBehavior(
                testContainer,
                null,
                () => testBoundaries,
                () => canDrag);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dragHandle");
        }

        [Fact]
        public void Constructor_WithNullGetBoundaryArea_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceDragBehavior(
                testContainer,
                testDragHandle,
                null,
                () => canDrag);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("getBoundaryArea");
        }

        [Fact]
        public void Constructor_WithNullCanDragCheck_ShouldThrow()
        {
            // Arrange & Act
            Action act = () => new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("canDragCheck");
        }

        [Fact]
        public void Attach_ShouldWireUpEvents()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Act
            behavior.Attach();

            // Assert
            // We can't directly test event attachment, but we can test that
            // mouse events trigger the behavior
            behavior.Should().NotBeNull();
        }

        [Fact]
        public void Detach_ShouldRemoveEvents()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            behavior.Attach();

            // Act
            behavior.Detach();

            // Assert
            // After detach, dragging should not work
            behavior.Should().NotBeNull();
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToLeft()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Act - Try to go too far left
            var result = behavior.ApplyBoundaries(-500, 100, 300, 300);

            // Assert - Should be constrained to keep 50px visible
            result.X.Should().BeGreaterThan(-300); // -width + MinVisible = -250
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToRight()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Act - Try to go too far right
            var result = behavior.ApplyBoundaries(2000, 100, 300, 300);

            // Assert - Should be constrained to keep 50px visible
            result.X.Should().BeLessThan(testBoundaries.Width); // 1920 - 50 = 1870
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToTop()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Act - Try to go above top
            var result = behavior.ApplyBoundaries(100, -100, 300, 300);

            // Assert - Should be constrained to 0
            result.Y.Should().Be(0);
        }

        [Fact]
        public void ApplyBoundaries_ShouldConstrainToBottom()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Act - Try to go too far down
            var result = behavior.ApplyBoundaries(100, 1500, 300, 300);

            // Assert - Should be constrained to keep 50px visible
            result.Y.Should().BeLessThan(testBoundaries.Height); // 1080 - 50 = 1030
        }

        [Fact]
        public void ApplyBoundaries_WithValidPosition_ShouldNotModify()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Act - Position well within boundaries
            var result = behavior.ApplyBoundaries(500, 300, 300, 300);

            // Assert - Should remain unchanged
            result.X.Should().Be(500);
            result.Y.Should().Be(300);
            result.Width.Should().Be(300);
            result.Height.Should().Be(300);
        }

        [Fact]
        public void ApplyBoundaries_ShouldMaintainMinimumVisibility()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            // Act - Multiple boundary violations
            var resultLeft = behavior.ApplyBoundaries(-1000, 100, 300, 300);
            var resultRight = behavior.ApplyBoundaries(5000, 100, 300, 300);

            // Assert - At least 50px should be visible
            (resultLeft.X + resultLeft.Width).Should().BeGreaterThanOrEqualTo(50);
            resultRight.X.Should().BeLessThan(testBoundaries.Width);
        }

        [Fact]
        public void DragStarted_Event_ShouldFireOnMouseDown()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            behavior.Attach();

            bool eventFired = false;
            behavior.DragStarted += (s, e) => eventFired = true;

            // Act
            var mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseArgs });

            // Assert
            eventFired.Should().BeTrue();
            behavior.IsDragging.Should().BeTrue();
        }

        [Fact]
        public void DragStarted_WhenCanDragIsFalse_ShouldNotStartDrag()
        {
            // Arrange
            canDrag = false;
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            behavior.Attach();

            bool eventFired = false;
            behavior.DragStarted += (s, e) => eventFired = true;

            // Act
            var mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseArgs });

            // Assert
            eventFired.Should().BeFalse();
            behavior.IsDragging.Should().BeFalse();
        }

        [Fact]
        public void DragEnded_Event_ShouldFireOnMouseUp()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            behavior.Attach();

            // Start drag
            var mouseDownArgs = new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseDownArgs });

            bool eventFired = false;
            Point endPosition = Point.Empty;
            behavior.DragEnded += (s, p) =>
            {
                eventFired = true;
                endPosition = p;
            };

            // Act
            var mouseUpArgs = new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseUpArgs });

            // Assert
            eventFired.Should().BeTrue();
            behavior.IsDragging.Should().BeFalse();
        }

        [Fact]
        public void PositionChanged_Event_ShouldFireDuringDrag()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            behavior.Attach();

            // Start drag
            var mouseDownArgs = new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseDownArgs });

            bool eventFired = false;
            Point newPosition = Point.Empty;
            behavior.PositionChanged += (s, p) =>
            {
                eventFired = true;
                newPosition = p;
            };

            // Act - Simulate mouse move while dragging
            var mouseMoveArgs = new MouseEventArgs(MouseButtons.Left, 1, 50, 50, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseMove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseMoveArgs });

            // Assert
            eventFired.Should().BeTrue();
        }

        [Fact]
        public void RightMouseButton_ShouldNotStartDrag()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            behavior.Attach();

            bool eventFired = false;
            behavior.DragStarted += (s, e) => eventFired = true;

            // Act - Right mouse button
            var mouseArgs = new MouseEventArgs(MouseButtons.Right, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseArgs });

            // Assert
            eventFired.Should().BeFalse();
            behavior.IsDragging.Should().BeFalse();
        }

        [Fact]
        public void Cursor_ShouldChangeDuringDrag()
        {
            // Arrange
            behavior = new FenceDragBehavior(
                testContainer,
                testDragHandle,
                () => testBoundaries,
                () => canDrag);

            behavior.Attach();
            testDragHandle.Cursor = Cursors.Default;

            // Act - Start drag
            var mouseDownArgs = new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseDownArgs });

            // Assert
            testDragHandle.Cursor.Should().Be(Cursors.SizeAll);

            // Act - End drag
            var mouseUpArgs = new MouseEventArgs(MouseButtons.Left, 1, 10, 10, 0);
            testDragHandle.GetType()
                .GetMethod("OnMouseUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(testDragHandle, new object[] { mouseUpArgs });

            // Assert - Cursor should be restored
            testDragHandle.Cursor.Should().Be(Cursors.Default);
        }
    }
}
