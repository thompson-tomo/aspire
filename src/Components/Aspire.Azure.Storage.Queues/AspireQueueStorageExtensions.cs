// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Queues;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Specialized;
using HealthChecks.Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="QueueServiceClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// Enables retries, corresponding health check, logging and telemetry.
/// </summary>
public static class AspireQueueStorageExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Storage:Queues";

    /// <summary>
    /// Registers <see cref="QueueServiceClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.</exception>
    public static void AddAzureQueueServiceClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new StorageQueuesComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="QueueServiceClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.</exception>
    [Obsolete("Use AddAzureQueueServiceClient instead.")]
    public static void AddAzureQueueClient(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AddAzureQueueServiceClient(builder, connectionName, configureSettings, configureClientBuilder);
    }

    /// <summary>
    /// Registers <see cref="QueueServiceClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.</exception>
    public static void AddKeyedAzureQueueServiceClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new StorageQueuesComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    /// <summary>
    /// Registers <see cref="QueueServiceClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureStorageQueuesSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.</exception>
    [Obsolete("Use AddKeyedAzureQueueServiceClient instead.")]
    public static void AddKeyedAzureQueueClient(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageQueuesSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueServiceClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        
        AddKeyedAzureQueueServiceClient(builder, name, configureSettings, configureClientBuilder);
    }

    /// <summary>
    ///  Registers <see cref="QueueClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    ///  Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">
    ///  An optional method that can be used for customizing the <see cref="AzureStorageQueueSettings"/>.
    ///  It's invoked after the settings are read from the configuration.
    /// </param>
    /// <param name="configureClientBuilder">
    ///  An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.
    /// </param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">
    ///  Neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.
    ///  - or -
    ///  <see cref="AzureStorageQueueSettings.QueueName"/> is not provided in the configuration section.
    /// </exception>
    public static void AddAzureQueue(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureStorageQueueSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        new StorageQueueComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    ///  Registers <see cref="QueueClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    ///  Enables retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">
    ///  The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve
    ///  the connection string from the ConnectionStrings configuration section.
    /// </param>
    /// <param name="configureSettings">
    ///  An optional method that can be used for customizing the <see cref="AzureStorageQueueSettings"/>.
    ///  It's invoked after the settings are read from the configuration.
    /// </param>
    /// <param name="configureClientBuilder">
    ///  An optional method that can be used for customizing the <see cref="IAzureClientBuilder{TClient, TOptions}"/>.
    /// </param>
    /// <remarks>Reads the configuration from "Aspire:Azure:Storage:Queues:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">
    ///  Neither <see cref="AzureStorageQueuesSettings.ConnectionString"/> nor <see cref="AzureStorageQueuesSettings.ServiceUri"/> is provided.
    ///  - or -
    ///  <see cref="AzureStorageQueueSettings.QueueName"/> is not provided in the configuration section.
    /// </exception>
    public static void AddKeyedAzureQueue(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureStorageQueueSettings>? configureSettings = null,
        Action<IAzureClientBuilder<QueueClient, QueueClientOptions>>? configureClientBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        new StorageQueueComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    private sealed class StorageQueuesComponent : AzureComponent<AzureStorageQueuesSettings, QueueServiceClient, QueueClientOptions>
    {
        protected override IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureStorageQueuesSettings settings, string connectionName,
            string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<QueueServiceClient, QueueClientOptions>((options, cred) =>
            {
                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && settings.ServiceUri is null)
                {
                    throw new InvalidOperationException($"A QueueServiceClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
                }

                return !string.IsNullOrEmpty(connectionString)
                    ? new QueueServiceClient(connectionString, options)
                    : cred is not null
                        ? new QueueServiceClient(settings.ServiceUri, cred, options)
                        : new QueueServiceClient(settings.ServiceUri, options);
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<QueueServiceClient, QueueClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureStorageQueuesSettings settings, IConfiguration configuration)
        {
            configuration.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(QueueServiceClient client, AzureStorageQueuesSettings settings)
            => new AzureQueueStorageHealthCheck(client, new AzureQueueStorageHealthCheckOptions());

        protected override bool GetHealthCheckEnabled(AzureStorageQueuesSettings settings)
            => !settings.DisableHealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageQueuesSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureStorageQueuesSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureStorageQueuesSettings settings)
            => !settings.DisableTracing;
    }

    private sealed partial class StorageQueueComponent : AzureComponent<AzureStorageQueueSettings, QueueClient, QueueClientOptions>
    {
        protected override IAzureClientBuilder<QueueClient, QueueClientOptions> AddClient(
            AzureClientFactoryBuilder azureFactoryBuilder, AzureStorageQueueSettings settings, string connectionName, string configurationSectionName)
        {
            return ((IAzureClientFactoryBuilderWithCredential)azureFactoryBuilder).RegisterClientFactory<QueueClient, QueueClientOptions>((options, cred) =>
            {
                if (string.IsNullOrEmpty(settings.QueueName))
                {
                    throw new InvalidOperationException($"The connection string '{connectionName}' does not exist or is missing the queue name.");
                }

                var connectionString = settings.ConnectionString;
                if (string.IsNullOrEmpty(connectionString) && settings.ServiceUri is null)
                {
                    throw new InvalidOperationException($"A QueueClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'ConnectionString' or 'ServiceUri' in the '{configurationSectionName}' configuration section.");
                }

                var queueServiceClient = !string.IsNullOrEmpty(connectionString) ? new QueueServiceClient(connectionString, options) :
                    cred is not null ? new QueueServiceClient(settings.ServiceUri, cred, options) :
                    new QueueServiceClient(settings.ServiceUri, options);

                var client = queueServiceClient.GetQueueClient(settings.QueueName);
                return client;
            }, requiresCredential: false);
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<QueueClient, QueueClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureStorageQueueSettings settings, IConfiguration configuration)
        {
            configuration.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(QueueClient client, AzureStorageQueueSettings settings)
            => new AzureQueueStorageHealthCheck(client.GetParentQueueServiceClient(), new AzureQueueStorageHealthCheckOptions { QueueName = client.Name });

        protected override bool GetHealthCheckEnabled(AzureStorageQueueSettings settings)
            => !settings.DisableHealthChecks;

        protected override TokenCredential? GetTokenCredential(AzureStorageQueueSettings settings)
            => settings.Credential;

        protected override bool GetMetricsEnabled(AzureStorageQueueSettings settings)
            => false;

        protected override bool GetTracingEnabled(AzureStorageQueueSettings settings)
            => !settings.DisableTracing;
    }
}
