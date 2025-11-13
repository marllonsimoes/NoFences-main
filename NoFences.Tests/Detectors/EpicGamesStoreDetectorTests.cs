using System;
using System.Collections.Generic;
using FluentAssertions;
using NoFences.Core.Util;
using NoFencesDataLayer.Services;
using Xunit;

namespace NoFences.Tests.Detectors
{
    public class EpicGamesStoreDetectorTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            var detector = new EpicGamesStoreDetector();

            detector.Should().NotBeNull();
            detector.PlatformName.Should().Be("Epic Games Store");
        }

        [Fact]
        public void PlatformName_ShouldReturnEpicGames()
        {
            var detector = new EpicGamesStoreDetector();

            var platformName = detector.PlatformName;

            platformName.Should().Be("Epic Games Store");
        }

        [Fact]
        public void GetInstalledGames_ShouldReturnList()
        {
            var detector = new EpicGamesStoreDetector();

            var games = detector.GetInstalledGames();

            games.Should().NotBeNull();
            games.Should().BeOfType<List<GameInfo>>();
        }

        [Fact]
        public void GetInstalledGames_ShouldHandleNoEpicInstallation()
        {
            var detector = new EpicGamesStoreDetector();

            Action act = () => detector.GetInstalledGames();

            act.Should().NotThrow();
        }

        [Fact]
        public void GetInstalledGames_WhenEpicInstalled_GamesHaveEpicPlatform()
        {
            var detector = new EpicGamesStoreDetector();

            var games = detector.GetInstalledGames();

            games.Should().OnlyContain(g => g.Platform == "Epic Games Store");
        }

        [Fact]
        public void GetInstalledGames_WhenEpicInstalled_GamesHaveValidGameIds()
        {
            var detector = new EpicGamesStoreDetector();

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
            var detector = new EpicGamesStoreDetector();

            Action act = () => detector.IsInstalled();

            act.Should().NotThrow();
        }

        [Fact]
        public void IsInstalled_ReturnsConsistentResult()
        {
            var detector = new EpicGamesStoreDetector();

            bool firstCall = detector.IsInstalled();
            bool secondCall = detector.IsInstalled();

            firstCall.Should().Be(secondCall, "IsInstalled should return same result across calls");
        }

        [Fact]
        public void GetInstallPath_ShouldNotThrow()
        {
            var detector = new EpicGamesStoreDetector();

            Action act = () => detector.GetInstallPath();

            act.Should().NotThrow();
        }

        [Fact]
        public void GetInstallPath_WhenInstalled_ReturnsValidPath()
        {
            var detector = new EpicGamesStoreDetector();

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
            var detector = new EpicGamesStoreDetector();

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
            var detector = new EpicGamesStoreDetector();
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Action act = () => detector.FindOrCreateGameShortcut("test-game-id", "Test Epic Game", tempDir);

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
