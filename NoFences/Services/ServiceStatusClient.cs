using log4net;
using NoFences.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NoFences.Services
{
    /// <summary>
    /// Client for communicating with NoFencesService via named pipe.
    /// Allows querying service status and controlling features without elevation.
    /// </summary>
    public class ServiceStatusClient
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ServiceStatusClient));

        private const string PipeName = "NoFencesServiceStatus";
        private const int ConnectionTimeoutMs = 2000;
        private const int ReadTimeoutMs = 5000;

        /// <summary>
        /// Event fired when service status is received
        /// </summary>
        public event EventHandler<StatusResponseMessage> StatusReceived;

        /// <summary>
        /// Event fired when feature state changes
        /// </summary>
        public event EventHandler<FeatureStateChangedMessage> FeatureStateChanged;

        public ServiceStatusClient()
        {
            log.Info("ServiceStatusClient initialized");
        }

        #region Service Communication

        /// <summary>
        /// Check if service is running and accessible
        /// </summary>
        public async Task<bool> IsServiceRunningAsync()
        {
            try
            {
                var status = await GetServiceStatusAsync();
                return status != null && status.IsServiceRunning;
            }
            catch (Exception ex)
            {
                log.Error($"Service not accessible: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get current service status
        /// </summary>
        public async Task<StatusResponseMessage> GetServiceStatusAsync()
        {
            try
            {
                var request = new StatusRequestMessage();
                var response = await SendMessageAsync<StatusResponseMessage>(request);

                StatusReceived?.Invoke(this, response);
                return response;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting service status: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Enable a service feature
        /// </summary>
        public async Task<bool> EnableFeatureAsync(string featureId)
        {
            return await ControlFeatureAsync(featureId, true);
        }

        /// <summary>
        /// Disable a service feature
        /// </summary>
        public async Task<bool> DisableFeatureAsync(string featureId)
        {
            return await ControlFeatureAsync(featureId, false);
        }

        /// <summary>
        /// Control a service feature (enable/disable)
        /// </summary>
        private async Task<bool> ControlFeatureAsync(string featureId, bool enable)
        {
            try
            {
                var request = new FeatureControlMessage
                {
                    FeatureId = featureId,
                    Enable = enable,
                    MessageType = enable ? ServiceMessageType.StartFeature : ServiceMessageType.StopFeature
                };

                var response = await SendMessageAsync<ServiceMessage>(request);

                if (response is FeatureStateChangedMessage stateChanged)
                {
                    log.Info($"Feature '{featureId}' {(enable ? "enabled" : "disabled")}: {stateChanged.State}");
                    FeatureStateChanged?.Invoke(this, stateChanged);
                    return true;
                }
                else if (response is ErrorMessage error)
                {
                    log.Warn($"Feature control failed: {error.Error}");
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                log.Error($"Error controlling feature '{featureId}': {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Send heartbeat to service
        /// </summary>
        public async Task<long?> SendHeartbeatAsync()
        {
            try
            {
                var request = new HeartbeatMessage();
                var response = await SendMessageAsync<HeartbeatMessage>(request);

                if (response != null)
                {
                    log.Debug($"Service uptime: {response.UptimeSeconds}s");
                    return response.UptimeSeconds;
                }

                return null;
            }
            catch (Exception ex)
            {
                log.Debug($"Heartbeat failed: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Message Communication

        /// <summary>
        /// Send a message to the service and wait for response
        /// </summary>
        private async Task<T> SendMessageAsync<T>(ServiceMessage message) where T : ServiceMessage
        {
            using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                // Connect to service
                using (var cts = new CancellationTokenSource(ConnectionTimeoutMs))
                {
                    try
                    {
                        await pipeClient.ConnectAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TimeoutException($"Connection to service timed out after {ConnectionTimeoutMs}ms");
                    }
                }

                log.Debug($"Connected to service pipe: {PipeName}");

                // Send message
                await WriteMessageAsync(pipeClient, message);

                // Read response
                using (var cts = new CancellationTokenSource(ReadTimeoutMs))
                {
                    var response = await ReadMessageAsync(pipeClient, cts.Token);

                    if (response is T typedResponse)
                    {
                        return typedResponse;
                    }
                    else if (response is ErrorMessage error)
                    {
                        throw new InvalidOperationException($"Service error: {error.Error}");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected response type: {response?.GetType().Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Write message to pipe
        /// </summary>
        private async Task WriteMessageAsync(NamedPipeClientStream pipe, ServiceMessage message)
        {
            try
            {
                var xml = SerializeMessage(message);

                using (var writer = new StreamWriter(pipe, Encoding.UTF8, 4096, leaveOpen: true))
                {
                    await writer.WriteAsync(xml);
                    await writer.FlushAsync();
                }

                log.Debug($"Sent message: {message.MessageType}");
            }
            catch (Exception ex)
            {
                log.Error($"Error writing message: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Read message from pipe
        /// </summary>
        private async Task<ServiceMessage> ReadMessageAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
        {
            try
            {
                using (var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true))
                {
                    var xml = await reader.ReadToEndAsync();
                    if (string.IsNullOrEmpty(xml))
                        return null;

                    var message = DeserializeMessage(xml);
                    log.Debug($"Received message: {message.MessageType}");

                    return message;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error reading message: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Message Serialization

        /// <summary>
        /// Serialize message to XML
        /// </summary>
        private string SerializeMessage(ServiceMessage message)
        {
            var serializer = new XmlSerializer(message.GetType());
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, message);
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Deserialize message from XML
        /// </summary>
        private ServiceMessage DeserializeMessage(string xml)
        {
            // Simple type detection by checking root element
            if (xml.Contains("<StatusResponseMessage"))
            {
                var serializer = new XmlSerializer(typeof(StatusResponseMessage));
                using (var reader = new StringReader(xml))
                {
                    return (ServiceMessage)serializer.Deserialize(reader);
                }
            }
            else if (xml.Contains("<FeatureStateChangedMessage"))
            {
                var serializer = new XmlSerializer(typeof(FeatureStateChangedMessage));
                using (var reader = new StringReader(xml))
                {
                    return (ServiceMessage)serializer.Deserialize(reader);
                }
            }
            else if (xml.Contains("<HeartbeatMessage"))
            {
                var serializer = new XmlSerializer(typeof(HeartbeatMessage));
                using (var reader = new StringReader(xml))
                {
                    return (ServiceMessage)serializer.Deserialize(reader);
                }
            }
            else if (xml.Contains("<ErrorMessage"))
            {
                var serializer = new XmlSerializer(typeof(ErrorMessage));
                using (var reader = new StringReader(xml))
                {
                    return (ServiceMessage)serializer.Deserialize(reader);
                }
            }

            throw new InvalidOperationException("Unknown message type in XML");
        }

        #endregion
    }
}
