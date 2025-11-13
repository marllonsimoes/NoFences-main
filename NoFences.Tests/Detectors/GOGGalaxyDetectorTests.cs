using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NoFences.Core.Util;
using NoFencesDataLayer.Services;
using Xunit;

namespace NoFences.Tests.Detectors
{
    public class GOGGalaxyDetectorTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            var detector = new GOGGalaxyDetector();

            detector.Should().NotBeNull();
            detector.PlatformName.Should().Be("GOG Galaxy");
        }

        [Fact]
        public void PlatformName_ShouldReturnGOGGalaxy()
        {
            var detector = new GOGGalaxyDetector();

            var platformName = detector.PlatformName;

            platformName.Should().Be("GOG Galaxy");
        }

        [Fact]
        public void GetInstalledGames_ShouldReturnList()
        {
            var detector = new GOGGalaxyDetector();

            var games = detector.GetInstalledGames();

            games.Should().NotBeNull();
            games.Should().BeOfType<List<GameInfo>>();
        }

        [Fact]
        public void GetInstalledGames_ShouldHandleNoGOGInstallation()
        {
            var detector = new GOGGalaxyDetector();

            Action act = () => detector.GetInstalledGames();

            act.Should().NotThrow();
        }

        [Fact]
        public void GetInstalledGames_WhenGOGInstalled_GamesHaveGOGPlatform()
        {
            var detector = new GOGGalaxyDetector();

            var games = detector.GetInstalledGames();

            games.Should().OnlyContain(g => g.Platform == "GOG Galaxy");
        }

        [Fact]
        public void GetInstalledGames_WhenGOGInstalled_GamesHaveValidGameIds()
        {
            var detector = new GOGGalaxyDetector();

            var games = detector.GetInstalledGames();

            if (games.Count > 0)
            {
                games.Should().OnlyContain(g => !string.IsNullOrEmpty(g.GameId));
            }
        }

        [Fact]
        public void GetInstalledGames_RemovesDuplicates()
        {
            var detector = new GOGGalaxyDetector();

            var games = detector.GetInstalledGames();

            var gameIds = games.Select(g => g.GameId).ToList();
            var uniqueGameIds = gameIds.Distinct().ToList();

            gameIds.Count.Should().Be(uniqueGameIds.Count, "Should not contain duplicate GameIds");
        }

        [Fact]
        public void IsInstalled_ShouldNotThrow()
        {
            var detector = new GOGGalaxyDetector();

            Action act = () => detector.IsInstalled();

            act.Should().NotThrow();
        }

        [Fact]
        public void IsInstalled_ReturnsConsistentResult()
        {
            var detector = new GOGGalaxyDetector();

            bool firstCall = detector.IsInstalled();
            bool secondCall = detector.IsInstalled();

            firstCall.Should().Be(secondCall, "IsInstalled should return same result across calls");
        }

        [Fact]
        public void GetInstallPath_ShouldNotThrow()
        {
            var detector = new GOGGalaxyDetector();

            Action act = () => detector.GetInstallPath();

            act.Should().NotThrow();
        }

        [Fact]
        public void GetInstallPath_WhenInstalled_ReturnsValidPath()
        {
            var detector = new GOGGalaxyDetector();

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
            var detector = new GOGGalaxyDetector();

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
            var detector = new GOGGalaxyDetector();
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Action act = () => detector.FindOrCreateGameShortcut("1234567890", "Test GOG Game", tempDir);

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
