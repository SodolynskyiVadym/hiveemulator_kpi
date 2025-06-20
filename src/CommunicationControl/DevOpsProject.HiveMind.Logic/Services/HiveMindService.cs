﻿using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Clients;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using DevOpsProject.HiveMind.Logic.State;
using System.Text;
using Polly;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class HiveMindService : IHiveMindService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HiveMindHttpClient _httpClient;
        private readonly ILogger<HiveMindService> _logger;
        private readonly HiveCommunicationConfig _communicationConfigurationOptions;
        private Timer _telemetryTimer;

        public HiveMindService(IHttpClientFactory httpClientFactory, HiveMindHttpClient httpClient, ILogger<HiveMindService> logger, IOptionsSnapshot<HiveCommunicationConfig> communicationConfigurationOptions)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _communicationConfigurationOptions = communicationConfigurationOptions.Value;

            if (Environment.GetEnvironmentVariable("RequestSchema") == "https")
            {
                _communicationConfigurationOptions.IsHttpsConnected = true;
                _communicationConfigurationOptions.CommunicationControlIP = Environment.GetEnvironmentVariable("COMMUNICATION_IP") ?? "localhost";
                _communicationConfigurationOptions.HiveIP = Environment.GetEnvironmentVariable("HIVE_IP") ?? "localhost";
                _communicationConfigurationOptions.RequestSchema = "https";
            }
            System.Console.WriteLine(_communicationConfigurationOptions.ToString());
        }

        public async Task ConnectHive()
        {
            var requestSerialize = new HiveConnectRequest
            {
                HiveSchema = _communicationConfigurationOptions.RequestSchema,
                HiveIP = _communicationConfigurationOptions.HiveIP,
                HivePort = _communicationConfigurationOptions.HivePort,
                HiveID = _communicationConfigurationOptions.HiveID
            };

            var httpClient = _httpClientFactory.CreateClient("HiveConnectClient");


            // var uriBuilder = new UriBuilder
            // {
            //     Scheme = _communicationConfigurationOptions.RequestSchema,
            //     Host = _communicationConfigurationOptions.CommunicationControlIP,
            //     Port = _communicationConfigurationOptions.CommunicationControlPort,
            //     Path = $"{_communicationConfigurationOptions.CommunicationControlPath}/connect"
            // };
            

            var uri = new Uri($"{_communicationConfigurationOptions.RequestSchema}://{_communicationConfigurationOptions.CommunicationControlIP}/{$"{_communicationConfigurationOptions.CommunicationControlPath}/connect"}");
            if(!_communicationConfigurationOptions.IsHttpsConnected) uri = new Uri($"{_communicationConfigurationOptions.RequestSchema}://{_communicationConfigurationOptions.CommunicationControlIP}:{_communicationConfigurationOptions.CommunicationControlPort}/{$"{_communicationConfigurationOptions.CommunicationControlPath}/connect"}");

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestSerialize), Encoding.UTF8, "application/json");
            System.Console.WriteLine($"Connecting HiveID: {requestSerialize.HiveID}, URI: {uri}, Request content: {jsonContent}");

            _logger.LogInformation("Attempting to connect Hive. Request: {@request}, URI: {uri}", requestSerialize, uri);

            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    10,
                    retryAttempt => TimeSpan.FromSeconds(2),
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Connecting HiveID: {_communicationConfigurationOptions.HiveID}, retry attempt: {retryCount}. \nRequest URL: {uri}, request content: {jsonContent}");
                    });

            var response = await retryPolicy.ExecuteAsync(() => httpClient.PostAsync(uri, jsonContent));

            if (response.IsSuccessStatusCode)
            {
                var connectResponse = await response.Content.ReadAsStringAsync();
                var hiveConnectResponse = JsonSerializer.Deserialize<HiveConnectResponse>(connectResponse);

                if (hiveConnectResponse != null && hiveConnectResponse.ConnectResult)
                {
                    HiveInMemoryState.OperationalArea = hiveConnectResponse.OperationalArea;
                    HiveInMemoryState.CurrentLocation = _communicationConfigurationOptions.InitialLocation;

                    StartTelemetry();
                }
                else
                {
                    _logger.LogInformation($"Connecting hive failed for ID: {requestSerialize.HiveID}");
                    throw new Exception($"Failed to connect HiveID: {requestSerialize.HiveID}");
                }
            }
            else
            {
                _logger.LogError($"Failed to connect hive, terminating process");
                Environment.Exit(1);
            }

        }

        public void StopAllTelemetry()
        {
            StopTelemetry();
        }

        #region private methods
        private void StartTelemetry()
        {
            if (HiveInMemoryState.IsTelemetryRunning) return;
            // TODO: Sending telemetry each N seconds
            _telemetryTimer = new Timer(SendTelemetry, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            _logger.LogInformation("Telemetry timer started.");
        }

        private void StopTelemetry()
        {
            _telemetryTimer?.Dispose();
            HiveInMemoryState.IsTelemetryRunning = false;

            _logger.LogInformation("Telemetry timer stopped.");
        }

        private async void SendTelemetry(object state)
        {
            var telemetryUri = new Uri($"{_communicationConfigurationOptions.RequestSchema}://{_communicationConfigurationOptions.CommunicationControlIP}/{$"{_communicationConfigurationOptions.CommunicationControlPath}/telemetry"}");
            if(!_communicationConfigurationOptions.IsHttpsConnected) telemetryUri = new Uri($"{_communicationConfigurationOptions.RequestSchema}://{_communicationConfigurationOptions.CommunicationControlIP}:{_communicationConfigurationOptions.CommunicationControlPort}/{$"{_communicationConfigurationOptions.CommunicationControlPath}/telemetry"}");

            var currentLocation = HiveInMemoryState.CurrentLocation;

            try
            {
                var request = new HiveTelemetryRequest
                {
                    HiveID = _communicationConfigurationOptions.HiveID,
                    Location = HiveInMemoryState.CurrentLocation ?? default,
                    // TODO: MOCKED FOR NOW
                    Height = 5,
                    Speed = 15,
                    State = Shared.Enums.State.Move
                };

                var connectResult = await _httpClient.SendCommunicationControlTelemetryAsync(telemetryUri, request);

                _logger.LogInformation($"Telemetry sent for HiveID: {request.HiveID}: {connectResult}");

                if (connectResult != null)
                {
                    // TODO: Store timestamp
                    var hiveConnectResponse = JsonSerializer.Deserialize<HiveTelemetryResponse>(connectResult);
                }
                else
                {
                    _logger.LogError($"Unable to send Hive telemetry for HiveID: {request.HiveID}.");
                    throw new Exception($"Failed to send telemetry for HiveID: {request.HiveID}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending telemetry: {Message}", ex.Message);
            }
        }
        #endregion
    }
}
