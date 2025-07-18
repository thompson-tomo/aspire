// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureResourcePreparerTests
{
    [Theory]
    [InlineData(DistributedApplicationOperation.Publish)]
    [InlineData(DistributedApplicationOperation.Run)]
    public async Task ThrowsExceptionsIfRoleAssignmentUnsupported(DistributedApplicationOperation operation)
    {
        using var builder = TestDistributedApplicationBuilder.Create(operation);

        var storage = builder.AddAzureStorage("storage");

        builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataReader);

        var app = builder.Build();

        if (operation == DistributedApplicationOperation.Publish)
        {
            var ex = Assert.Throws<InvalidOperationException>(app.Start);
            Assert.Contains("role assignments", ex.Message);
        }
        else
        {
            await app.StartAsync();
            // no exception is thrown in Run mode
        }
    }

    [Theory]
    [InlineData(true, DistributedApplicationOperation.Run)]
    [InlineData(false, DistributedApplicationOperation.Run)]
    [InlineData(true, DistributedApplicationOperation.Publish)]
    [InlineData(false, DistributedApplicationOperation.Publish)]
    public async Task AppliesDefaultRoleAssignmentsInRunModeIfReferenced(bool addContainerAppsInfra, DistributedApplicationOperation operation)
    {
        using var builder = TestDistributedApplicationBuilder.Create(operation);
        if (addContainerAppsInfra)
        {
            builder.AddAzureContainerAppEnvironment("env");
        }

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobService("blobs");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(blobs);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.True(storage.Resource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultAssignments));

        if (!addContainerAppsInfra || operation == DistributedApplicationOperation.Run)
        {
            // when AzureContainerAppsInfrastructure is not added, we always apply the default role assignments to a new 'storage-roles' resource.
            // The same applies when in RunMode and we are provisioning Azure resources for F5 local development.
            var storageRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "storage-roles");

            var storageRolesManifest = await GetManifestWithBicep(storageRoles, skipPreparer: true);
            await Verify(storageRolesManifest.BicepText, extension: "bicep");

        }
        else
        {
            // in PublishMode when AzureContainerAppsInfrastructure is added, the DefaultRoleAssignmentsAnnotation
            // is copied to referencing resources' RoleAssignmentAnnotation.

            Assert.True(api.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var apiRoleAssignments));
            Assert.Equal(storage.Resource, apiRoleAssignments.Target);
            Assert.Equal(defaultAssignments.Roles, apiRoleAssignments.Roles);
        }
    }

    [Theory]
    [InlineData(DistributedApplicationOperation.Run)]
    [InlineData(DistributedApplicationOperation.Publish)]
    public async Task AppliesRoleAssignmentsInRunMode(DistributedApplicationOperation operation)
    {
        using var builder = TestDistributedApplicationBuilder.Create(operation);
        builder.AddAzureContainerAppEnvironment("env");

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobService("blobs");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDelegator, StorageBuiltInRole.StorageBlobDataReader)
            .WithReference(blobs);

        var api2 = builder.AddProject<Project>("api2", launchProfileName: null)
            .WithRoleAssignments(storage, StorageBuiltInRole.StorageBlobDataContributor)
            .WithReference(blobs);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        if (operation == DistributedApplicationOperation.Run)
        {
            // in RunMode, we apply the role assignments to a new 'storage-roles' resource, so the provisioned resource
            // adds these role assignments for F5 local development.
            var storageRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "storage-roles");

            var storageRolesManifest = await GetManifestWithBicep(storageRoles, skipPreparer: true);
            await Verify(storageRolesManifest.BicepText, extension: "bicep");

        }
        else
        {
            // in PublishMode, the role assignments are copied to the referencing resources' RoleAssignmentAnnotation.
            Assert.True(api.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var apiRoleAssignments));
            Assert.Equal(storage.Resource, apiRoleAssignments.Target);
            Assert.Collection(apiRoleAssignments.Roles,
                role => Assert.Equal(StorageBuiltInRole.StorageBlobDelegator.ToString(), role.Id),
                role => Assert.Equal(StorageBuiltInRole.StorageBlobDataReader.ToString(), role.Id));

            Assert.True(api2.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var api2RoleAssignments));
            Assert.Equal(storage.Resource, api2RoleAssignments.Target);
            Assert.Single(api2RoleAssignments.Roles,
                role => role.Id == StorageBuiltInRole.StorageBlobDataContributor.ToString());
        }
    }

    [Fact]
    public async Task DoesNotApplyRoleAssignmentsInRunModeForEmulators()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        builder.AddAzureContainerAppEnvironment("env");

        builder.AddBicepTemplateString("foo", "");

        var dbsrv = builder.AddAzureSqlServer("dbsrv").RunAsContainer();
        var db = dbsrv.AddDatabase("db");

        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithReference(db);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, default);

        // in RunMode, we skip applying the role assignments to a new 'dbsrv-roles' resource, since the storage is running as emulator.
        Assert.DoesNotContain(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "dbsrv-roles");
    }

    [Fact]
    public async Task FindsAzureReferencesFromArguments()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        builder.AddAzureContainerAppEnvironment("env");

        var storage = builder.AddAzureStorage("storage");
        var blobs = storage.AddBlobService("blobs");

        // the project doesn't WithReference or WithRoleAssignments, so it should get the default role assignments
        var api = builder.AddProject<Project>("api", launchProfileName: null)
            .WithArgs(context =>
            {
                context.Args.Add("--azure-blobs");
                context.Args.Add(blobs.Resource.ConnectionStringExpression);
            });

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.True(storage.Resource.TryGetLastAnnotation<DefaultRoleAssignmentsAnnotation>(out var defaultAssignments));

        Assert.True(api.Resource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var apiRoleAssignments));
        Assert.Equal(storage.Resource, apiRoleAssignments.Target);
        Assert.Equal(defaultAssignments.Roles, apiRoleAssignments.Roles);
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }
}
