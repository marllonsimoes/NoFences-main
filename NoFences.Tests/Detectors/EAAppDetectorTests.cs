using System;
using System.Collections.Generic;
using FluentAssertions;
using NoFences.Core.Util;
using NoFencesDataLayer.Services;
using Xunit;

namespace NoFences.Tests.Detectors
{
    public class EAAppDetectorTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            var detector = new EAAppDetector();

            detector.Should().NotBeNull();
            detector.PlatformName.Should().Be("EA App");
        }

        [Fact]
        public void PlatformName_ShouldReturnEAApp()
        {
            var detector = new EAAppDetector();

            var platformName = detector.PlatformName;

            platformName.Should().Be("EA App");
        }

        [Fact]
        public void GetInstalledGames_ShouldReturnList()
        {
            var detector = new EAAppDetector();

            var games = detector.GetInstalledGames();

            games.Should().NotBeNull();
            games.Should().BeOfType<List<GameInfo>>();
        }

        [Fact]
        public void GetInstalledGames_ShouldHandleNoEAInstallation()
        {
            var detector = new EAAppDetector();

            Action act = () => detector.GetInstalledGames();

            act.Should().NotThrow();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GetInstalledGames_WhenEAInstalled_GamesHaveEAPlatform()
        {
            var detector = new EAAppDetector();

            var games = detector.GetInstalledGames();

            games.Should().OnlyContain(g => g.Platform == "EA App");
        }

        [Fact]
        public void GetInstalledGames_WhenEAInstalled_GamesHaveValidGameIds()
        {
            var detector = new EAAppDetector();

            var games = detector.GetInstalledGames();

            if (games.Count > 0)
            {
                games.Should().OnlyContain(g => !string.IsNullOrEmpty(g.GameId));
                games.Should().OnlyContain(g => !string.IsNullOrEmpty(g.Name));
            }
        }

        [Fact]
        public void IsInstalled_ShouldNotThrow()
        {
            var detector = new EAAppDetector();

            Action act = () => detector.IsInstalled();

            act.Should().NotThrow();
        }

        [Fact]
        public void IsInstalled_ReturnsConsistentResult()
        {
            var detector = new EAAppDetector();

            bool firstCall = detector.IsInstalled();
            bool secondCall = detector.IsInstalled();

            firstCall.Should().Be(secondCall, "IsInstalled should return same result across calls");
        }

        [Fact]
        public void GetInstallPath_ShouldNotThrow()
        {
            var detector = new EAAppDetector();

            Action act = () => detector.GetInstallPath();

            act.Should().NotThrow();
        }

        [Fact]
        public void GetInstallPath_WhenInstalled_ReturnsValidPath()
        {
            var detector = new EAAppDetector();

            var path = detector.GetInstallPath();

            if (path != null)
            {
                path.Should().NotBeEmpty();
                System.IO.Directory.Exists(path).Should().BeTrue("Install path should exist");
            }
        }

        [Fact]
        public void GetInstallPath_ConsistentWithIsInstalled()
        {
            var detector = new EAAppDetector();

            bool isInstalled = detector.IsInstalled();
            var installPath = detector.GetInstallPath();

            if (isInstalled)
            {
                installPath.Should().NotBeNullOrEmpty("If IsInstalled is true, GetInstallPath should return a path");
            }
            else
            {
                installPath.Should().BeNullOrEmpty("If IsInstalled is false, GetInstallPath should return null");
            }
        }

        [Fact]
        public void FindOrCreateGameShortcut_WithValidGameId_ShouldHandleGracefully()
        {
            var detector = new EAAppDetector();
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Action act = () => detector.FindOrCreateGameShortcut("test-content-id", "Test EA Game", tempDir);

                act.Should().NotThrow();
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    System.IO.Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
