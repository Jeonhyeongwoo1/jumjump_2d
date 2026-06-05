using JumJump.Data;
using JumJump.Event;
using JumJump.Service;
using NUnit.Framework;
using UnityEngine;

namespace JumJump.Tests
{
    public sealed class ScoreServiceTests
    {
        private GameConfigData _configData;

        [SetUp]
        public void SetUp()
        {
            _configData = ScriptableObject.CreateInstance<GameConfigData>();
            PlayerPrefs.DeleteKey(_configData.HighScoreKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(_configData.HighScoreKey);
            Object.DestroyImmediate(_configData);
        }

        [Test]
        public void PlayerLanded_IncrementsScore()
        {
            var eventBus = new EventBus();
            var scoreService = new ScoreService(eventBus, _configData);

            scoreService.Initialize();
            eventBus.Publish(new PlayerLandedEvent(null));

            Assert.AreEqual(1, scoreService.Score);
            Assert.AreEqual(1, scoreService.HighScore);

            scoreService.Dispose();
        }

        [Test]
        public void Restart_ResetsCurrentScoreOnly()
        {
            var eventBus = new EventBus();
            var scoreService = new ScoreService(eventBus, _configData);

            scoreService.Initialize();
            eventBus.Publish(new PlayerLandedEvent(null));
            eventBus.Publish(new RestartRequestedEvent());

            Assert.AreEqual(0, scoreService.Score);
            Assert.AreEqual(1, scoreService.HighScore);

            scoreService.Dispose();
        }
    }
}
