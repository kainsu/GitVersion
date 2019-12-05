
using System;
using GitVersion;
using GitVersion.BuildServers;
using GitVersion.Logging;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.BuildServers
{
    using System.Collections.Generic;
    using Environment = System.Environment;

    [TestFixture]
    public class GitHubActionsTests : TestBase
    {
        private IEnvironment environment;
        private ILog log;

        [SetUp]
        public void SetUp()
        {
            log = new NullLog();
            environment = new TestEnvironment();
            environment.SetEnvironmentVariable("GITHUB_ACTION", Guid.NewGuid().ToString());
        }

        [TearDown]
        public void TearDown()
        {
            environment.SetEnvironmentVariable("GITHUB_ACTION", null);
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeTrueWhenEnvironmentVariableIsSet()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log);

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContextShouldBeFalseWhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            environment.SetEnvironmentVariable("GITHUB_ACTION", "");
            var buildServer = new GitHubActions(environment, log);

            // Act
            var result = buildServer.CanApplyToCurrentContext();

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public void GetCurrentBranchShouldGetBranchIfSet()
        {
            // Arrange
            const string expected = "actionsBranch";

            environment.SetEnvironmentVariable("GITHUB_REF", $"refs/heads/{expected}");

            var buildServer = new GitHubActions(environment, log);

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBe(expected);
        }

        [Test]
        public void GetCurrentBranchShouldNotMatchTag()
        {
            // Arrange
            environment.SetEnvironmentVariable("GITHUB_REF", $"refs/tags/v1.0.0");

            var buildServer = new GitHubActions(environment, log);

            // Act
            var result = buildServer.GetCurrentBranch(false);

            // Assert
            result.ShouldBeNull();
        }

        [TestCase("Something", "1.0.0", "::set-env name=GitVersion_Something::1.0.0", Ignore = "Skip")]
        public void GetSetParameterMessage(string key, string value, string expected)
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log);

            // Act
            var result = buildServer.GenerateSetParameterMessage(key, value);

            // Assert
            result.ShouldContain(s => true, 1);
            result.ShouldBeEquivalentTo(new[] { expected });
        }

        [Test]
        public void SkipEmptySetParameterMessage()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log);

            // Act
            var result = buildServer.GenerateSetParameterMessage("Hello", string.Empty);

            // Assert
            result.ShouldBeEquivalentTo(new string[0]);
        }

        [Test]
        [Ignore("Skip")]
        public void ShouldWriteIntegration()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log);

            var vars = new TestableVersionVariables("1.0.0");

            var list = new List<string>();

            // Act
            buildServer.WriteIntegration(s => { list.Add(s); }, vars);

            // Assert
            var expected = new List<string>
            {
                "Executing GenerateSetVersionMessage for 'GitHubActions'.",
                "",
                "Executing GenerateBuildLogOutput for 'GitHubActions' Keys:Major.",
                "::set-env name=GitVersion_Major::1.0.0"
            };

            string.Join(Environment.NewLine, list)
                .ShouldBe(string.Join(Environment.NewLine, expected));
        }

        [Test]
        public void GetEmptyGenerateSetVersionMessage()
        {
            // Arrange
            var buildServer = new GitHubActions(environment, log);
            var vars = new TestableVersionVariables("1.0.0");

            // Act
            var message = buildServer.GenerateSetVersionMessage(vars);

            // Assert
            message.ShouldBeEmpty();
        }
    }
}