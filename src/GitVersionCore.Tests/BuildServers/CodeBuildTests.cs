using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GitVersion;
using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;
using GitVersion.Common;
using GitVersion.OutputVariables;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public sealed class CodeBuildTests : TestBase
    {
        private IEnvironment environment;

        [SetUp]
        public void SetUp()
        {
            environment = new TestEnvironment();
        }

        [Test]
        public void CorrectlyIdentifiesCodeBuildPresence()
        {
            environment.SetEnvironmentVariable(CodeBuild.HeadRefEnvironmentName, "a value");
            var cb = new CodeBuild(environment);
            cb.CanApplyToCurrentContext().ShouldBe(true);
        }

        [Test]
        public void PicksUpBranchNameFromEnvironment()
        {
            environment.SetEnvironmentVariable(CodeBuild.HeadRefEnvironmentName, "refs/heads/master");
            var cb = new CodeBuild(environment);
            cb.GetCurrentBranch(false).ShouldBe("refs/heads/master");
        }

        [Test]
        public void WriteAllVariablesToTheTextWriter()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var f = Path.Combine(assemblyLocation, "codebuild_this_file_should_be_deleted.properties");

            try
            {
                AssertVariablesAreWrittenToFile(f);
            }
            finally
            {
                File.Delete(f);
            }
        }

        private void AssertVariablesAreWrittenToFile(string f)
        {
            var writes = new List<string>();
            var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "beta1",
                BuildMetaData = "5"
            };

            semanticVersion.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");
            semanticVersion.BuildMetaData.Sha = "commitSha";

            var config = new TestEffectiveConfiguration();

            var variables = VariableProvider.GetVariablesFor(semanticVersion, config, false);

            var j = new CodeBuild(environment, f);

            j.WriteIntegration(writes.Add, variables);

            writes[1].ShouldBe("1.2.3-beta.1+5");

            File.Exists(f).ShouldBe(true);

            var props = File.ReadAllText(f);

            props.ShouldContain("GitVersion_Major=1");
            props.ShouldContain("GitVersion_Minor=2");
        }
    }
}
