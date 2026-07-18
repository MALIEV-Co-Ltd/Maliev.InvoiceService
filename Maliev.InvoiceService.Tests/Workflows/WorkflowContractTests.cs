// <copyright file="WorkflowContractTests.cs" company="Maliev Company Limited">
// Copyright (c) Maliev Company Limited. All rights reserved.
// </copyright>

namespace Maliev.InvoiceService.Tests.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Protects validation from image publication and GitOps side effects.
    /// </summary>
    public sealed class WorkflowContractTests
    {
        private static readonly string RepositoryRoot = FindRepositoryRoot();
        private static readonly string WorkflowsRoot = Path.Combine(RepositoryRoot, ".github", "workflows");

        /// <summary>
        /// Pull requests must always receive a read-only validation result.
        /// </summary>
        [Fact]
        public void PullRequestValidation_RunsWithoutPathFiltersOrCredentials()
        {
            var workflow = ReadWorkflow("pr-validation.yml");

            Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("paths:", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("paths-ignore:", workflow, StringComparison.Ordinal);
            Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
            Assert.Contains("uses: ./.github/workflows/_validate.yml", workflow, StringComparison.Ordinal);
            AssertCredentialFree(workflow);
        }

        /// <summary>
        /// Branch and release-tag events validate but cannot publish or promote.
        /// </summary>
        [Theory]
        [InlineData("ci-main.yml", "main")]
        [InlineData("ci-develop.yml", "develop")]
        [InlineData("ci-staging.yml", "release/v*")]
        public void BranchAndTagWorkflows_ValidateWithoutDeploymentSideEffects(string fileName, string trigger)
        {
            var workflow = ReadWorkflow(fileName);

            Assert.Contains(trigger, workflow, StringComparison.Ordinal);
            Assert.Contains("uses: ./.github/workflows/_validate.yml", workflow, StringComparison.Ordinal);
            AssertCredentialFree(workflow);
        }

        /// <summary>
        /// The reusable validation boundary must use immutable actions and no write permissions.
        /// </summary>
        [Fact]
        public void ReusableValidation_IsImmutableCredentialFreeAndStable()
        {
            var workflow = ReadWorkflow("_validate.yml");

            Assert.Contains("workflow_call:", workflow, StringComparison.Ordinal);
            Assert.Contains("name: validate", workflow, StringComparison.Ordinal);
            Assert.Contains("permissions:", workflow, StringComparison.Ordinal);
            Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
            Assert.Contains("persist-credentials: false", workflow, StringComparison.Ordinal);
            Assert.Contains("actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0", workflow, StringComparison.Ordinal);
            Assert.Contains("actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68", workflow, StringComparison.Ordinal);
            Assert.Contains("dotnet build", workflow, StringComparison.Ordinal);
            Assert.Contains("dotnet test", workflow, StringComparison.Ordinal);
            AssertCredentialFree(workflow);
        }

        /// <summary>
        /// No checked-in workflow may regain a credentialed publication or GitOps path.
        /// </summary>
        [Fact]
        public void RepositoryWorkflows_ContainNoPublicationPromotionOrUntrustedTrigger()
        {
            var workflows = Directory.GetFiles(WorkflowsRoot, "*.yml", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(WorkflowsRoot, "*.yaml", SearchOption.TopDirectoryOnly));

            foreach (var path in workflows)
            {
                var workflow = File.ReadAllText(path);
                Assert.DoesNotContain("pull_request_target", workflow, StringComparison.OrdinalIgnoreCase);
                AssertCredentialFree(workflow);
            }
        }

        /// <summary>
        /// Operators must be told that validation does not imply release authorization.
        /// </summary>
        [Fact]
        public void OperatingDocumentation_RecordsOwnerReviewReleaseGate()
        {
            var readme = File.ReadAllText(Path.Combine(RepositoryRoot, "README.md"));

            Assert.Contains("Validation and release boundary", readme, StringComparison.Ordinal);
            Assert.Contains("No workflow in this repository publishes", readme, StringComparison.Ordinal);
            Assert.Contains("Aspire owner review", readme, StringComparison.Ordinal);
        }

        private static void AssertCredentialFree(string workflow)
        {
            var forbidden = new[]
            {
                "secrets.",
                "secrets[",
                "id-token: write",
                "credentials_json",
                "google-github-actions/auth",
                "gcloud auth",
                "docker push",
                "maliev-gitops",
                "GITOPS_PAT",
                "kustomize edit",
                "gh pr create",
            };

            foreach (var value in forbidden)
            {
                Assert.DoesNotContain(value, workflow, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string ReadWorkflow(string fileName)
        {
            var path = Path.Combine(WorkflowsRoot, fileName);
            Assert.True(File.Exists(path), $"Required workflow is missing: {fileName}");
            return File.ReadAllText(path);
        }

        private static string FindRepositoryRoot()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "Maliev.InvoiceService.sln")))
                {
                    return current.FullName;
                }
            }

            throw new DirectoryNotFoundException("Could not locate the InvoiceService repository root.");
        }
    }
}
