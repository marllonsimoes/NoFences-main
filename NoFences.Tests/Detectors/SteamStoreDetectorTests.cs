using System;
using System.Collections.Generic;
using FluentAssertions;
using NoFences.Core.Util;
using NoFencesDataLayer.Services;
using Xunit;

namespace NoFences.Tests.Detectors
{
    public class SteamStoreDetectorTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            var detector = new SteamStoreDetector();

            detector.Should().NotBeNull();
            detector.PlatformName.Should().Be("Steam");
        }

        [Fact]
        public void PlatformName_ShouldReturnSteam()
        {
            var detector = new SteamStoreDetector();

            var platformName = detector.PlatformName;

            platformName.Should().Be("Steam");
        }

        [Fact]
        public void GetInstalledGames_ShouldReturnList()
        {
            var detector = new SteamStoreDetector();

            var games = detector.GetInstalledGames();

            games.Should().NotBeNull();
            games.Should().BeOfType<List<GameInfo>>();
        }

        [Fact]
        public void GetInstalledGames_ShouldHandleNoSteamInstallation()
        {
            var detector = new SteamStoreDetector();

            Action act = () => detector.GetInstalledGames();

            act.Should().NotThrow();
        }

        [Fact]
        public void GetInstalledGames_WhenSteamInstalled_GamesHaveSteamPlatform()
        {
            var detector = new SteamStoreDetector();

            var games = detector.GetInstalledGames();

            games.Should().OnlyContain(g => g.Platform == "Steam");
        }

        [Fact]
        public void GetInstalledGames_WhenSteamInstalled_GamesHaveValidAppIds()
        {
            var detector = new SteamStoreDetector();

            var games = detector.GetInstalledGames();

            if (games.Count > 0)
            {
                games.Should().OnlyContain(g => !string.IsNullOrEmpty(g.GameId));
                foreach (var game in games)
                {
                    int parsedId;
                    int.TryParse(game.GameId, out parsedId).Should().BeTrue("GameId should be numeric Steam AppID");
                }
            }
        }

        [Fact]
        public void IsInstalled_ShouldNotThrow()
        {
            var detector = new SteamStoreDetector();

            Action act = () => detector.IsInstalled();

            act.Should().NotThrow();
        }

        [Fact]
        public void IsInstalled_ReturnsConsistentResult()
        {
            var detector = new SteamStoreDetector();

            bool firstCall = detector.IsInstalled();
            bool secondCall = detector.IsInstalled();

            firstCall.Should().Be(secondCall, "IsInstalled should return same result across calls");
        }

        [Fact]
        public void GetInstallPath_ShouldNotThrow()
        {
            var detector = new SteamStoreDetector();

            Action act = () => detector.GetInstallPath();

            act.Should().NotThrow();
        }

        [Fact]
        public void GetInstallPath_WhenInstalled_ReturnsValidPath()
        {
            var detector = new SteamStoreDetector();

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
            var detector = new SteamStoreDetector();

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
        public void FindOrCreateGameShortcut_WithInvalidAppId_ReturnsNull()
        {
            var detector = new SteamStoreDetector();
            string tempDir = System.IO.Path.GetTempPath();

            var result = detector.FindOrCreateGameShortcut("invalid", "Test Game", tempDir);

            result.Should().BeNull("Invalid AppID should return null");
        }

        [Fact]
        public void FindOrCreateGameShortcut_WithValidAppId_CreatesShortcut()
        {
            var detector = new SteamStoreDetector();
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var result = detector.FindOrCreateGameShortcut("440", "Team Fortress 2", tempDir);

                if (result != null)
                {
                    result.Should().EndWith(".url");
                    System.IO.File.Exists(result).Should().BeTrue();
                }
            }
            finally
            {
                if (System.IO.Directory.Exists(tempDir))
                {
                    System.IO.Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void FindOrCreateGameShortcut_ExistingShortcut_ReturnsExistingPath()
        {
            var detector = new SteamStoreDetector();
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var firstResult = detector.FindOrCreateGameShortcut("440", "Test Game", tempDir);
                var secondResult = detector.FindOrCreateGameShortcut("440", "Test Game", tempDir);

                if (firstResult != null && secondResult != null)
                {
                    firstResult.Should().Be(secondResult, "Should return same path for existing shortcut");
                }
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
