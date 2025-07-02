using FluentAssertions;
using Omnitooth.Core.Enums;
using Omnitooth.Core.Models;
using Xunit;

namespace Omnitooth.Core.Tests.Models;

/// <summary>
/// Tests for the KeyboardInput model.
/// </summary>
public class KeyboardInputTests
{
    [Fact]
    public void KeyboardInput_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var input = new KeyboardInput();

        // Assert
        input.InputType.Should().Be(InputType.Keyboard);
        input.VirtualKeyCode.Should().Be(0);
        input.ScanCode.Should().Be(0);
        input.IsPressed.Should().BeFalse();
        input.IsExtended.Should().BeFalse();
        input.Modifiers.Should().Be(KeyboardModifiers.None);
    }

    [Fact]
    public void KeyboardInput_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var input = new KeyboardInput
        {
            Timestamp = timestamp,
            VirtualKeyCode = 65, // 'A' key
            ScanCode = 30,
            IsPressed = true,
            IsExtended = false,
            Modifiers = KeyboardModifiers.LeftCtrl | KeyboardModifiers.LeftShift
        };

        // Assert
        input.Timestamp.Should().Be(timestamp);
        input.VirtualKeyCode.Should().Be(65);
        input.ScanCode.Should().Be(30);
        input.IsPressed.Should().BeTrue();
        input.IsExtended.Should().BeFalse();
        input.Modifiers.Should().Be(KeyboardModifiers.LeftCtrl | KeyboardModifiers.LeftShift);
        input.InputType.Should().Be(InputType.Keyboard);
    }

    [Theory]
    [InlineData(KeyboardModifiers.None, false)]
    [InlineData(KeyboardModifiers.LeftCtrl, true)]
    [InlineData(KeyboardModifiers.LeftShift, true)]
    [InlineData(KeyboardModifiers.LeftCtrl | KeyboardModifiers.LeftShift, true)]
    public void KeyboardModifiers_ShouldWorkCorrectlyWithFlags(KeyboardModifiers modifiers, bool hasModifiers)
    {
        // Arrange
        var input = new KeyboardInput { Modifiers = modifiers };

        // Assert
        if (hasModifiers)
        {
            input.Modifiers.Should().NotBe(KeyboardModifiers.None);
        }
        else
        {
            input.Modifiers.Should().Be(KeyboardModifiers.None);
        }
    }
}