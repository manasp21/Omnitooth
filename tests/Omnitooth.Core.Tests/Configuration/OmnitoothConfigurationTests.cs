using FluentAssertions;
using Omnitooth.Core.Configuration;
using Xunit;

namespace Omnitooth.Core.Tests.Configuration;

/// <summary>
/// Tests for the OmnitoothConfiguration model.
/// </summary>
public class OmnitoothConfigurationTests
{
    [Fact]
    public void OmnitoothConfiguration_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var config = new OmnitoothConfiguration();

        // Assert
        config.Bluetooth.Should().NotBeNull();
        config.Input.Should().NotBeNull();
        config.HID.Should().NotBeNull();
        config.Security.Should().NotBeNull();
        config.Performance.Should().NotBeNull();
        config.UI.Should().NotBeNull();
    }

    [Fact]
    public void BluetoothConfiguration_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new BluetoothConfiguration();

        // Assert
        config.DeviceName.Should().Be("Omnitooth HID");
        config.ServiceUuid.Should().Be("00001812-0000-1000-8000-00805f9b34fb");
        config.AutoReconnect.Should().BeTrue();
        config.ConnectionTimeoutMs.Should().Be(30000);
        config.AdvertisingIntervalMs.Should().Be(100);
    }

    [Fact]
    public void InputConfiguration_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new InputConfiguration();

        // Assert
        config.EnableGameInput.Should().BeTrue();
        config.FallbackToRawInput.Should().BeTrue();
        config.KeyboardBufferSize.Should().Be(6);
        config.MouseSensitivity.Should().Be(1.0);
        config.DeadZoneThreshold.Should().Be(0.1);
        config.InputRateLimitMs.Should().Be(1);
    }

    [Fact]
    public void HidConfiguration_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new HidConfiguration();

        // Assert
        config.ReportRateHz.Should().Be(1000);
        config.EnableBatching.Should().BeTrue();
        config.BatchSizeLimit.Should().Be(10);
        config.CompressionEnabled.Should().BeFalse();
    }

    [Fact]
    public void SecurityConfiguration_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new SecurityConfiguration();

        // Assert
        config.RequireAuthentication.Should().BeTrue();
        config.RequireEncryption.Should().BeTrue();
        config.AllowedDevices.Should().BeEmpty();
        config.BlockedDevices.Should().BeEmpty();
    }

    [Fact]
    public void UIConfiguration_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new UIConfiguration();

        // Assert
        config.StartMinimized.Should().BeFalse();
        config.MinimizeToTray.Should().BeTrue();
        config.ShowNotifications.Should().BeTrue();
        config.Theme.Should().Be("System");
        config.AutoStart.Should().BeFalse();
    }
}