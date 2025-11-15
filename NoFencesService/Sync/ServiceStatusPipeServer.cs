using log4net;
using NoFences.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NoFencesService.Sync
{
    /// <summary>
    /// Named pipe server for service status communication with main application.
    /// Allows bidirectional communication without requiring elevation.
    /// </summary>
    public class ServiceStatusPipeServer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ServiceStatusPipeServer));

        private const string PipeName = "NoFencesServiceStatus";
        private readonly Dictionary<string, ServiceFeatureStatus> features;
        private readonly object featuresLock = new object();
        private CancellationTokenSource cancellationTokenSource;
        private Task serverTask;
        private DateTime serviceStartTime;

        public ServiceStatusPipeServer()
        {
            features = new Dictionary<string, ServiceFeatureStatus>();
            serviceStartTime = DateTime.UtcNow;
            log.Info("ServiceStatusPipeServer initialized");
        }

        #region Feature Management

        /// <summary>
        /// Register a service feature
        /// </summary>
        public void RegisterFeature(string featureId, string displayName, string description, bool isControllable = true)
        {
            lock (featuresLock)
            {
                if (!features.ContainsKey(featureId))
                {
                    features[featureId] = new ServiceFeatureStatus
                    {
                        FeatureId = featureId,
                        DisplayName = displayName,
                        Description = description,
                        State = ServiceFeatureState.Stopped,
                        IsControllable = isControllable,
                        LastStateChange = DateTime.UtcNow
                    };

                    log.Info($"Registered feature: {featureId} ({displayName})");
                }
            }
        }

        /// <summary>
        /// Update feature state
        /// </summary>
        public void UpdateFeatureState(string featureId, ServiceFeatureState state, string errorMessage = null)
        {
            lock (featuresLock)
            {
                if (features.TryGetValue(featureId, out var feature))
                {
                    feature.State = state;
                    feature.LastStateChange = DateTime.UtcNow;
                    feature.ErrorMessage = errorMessage;

                    log.Info($"Feature '{featureId}' state changed: {state}");
                }
            }
        }

        /// <summary>
        /// Get current feature statuses
        /// </summary>
        public List<ServiceFeatureStatus> GetFeatureStatuses()
        {
            lock (featuresLock)
            {
                return features.Values.Select(f => new ServiceFeatureStatus
                {
                    FeatureId = f.FeatureId,
                    DisplayName = f.DisplayName,
                    Description = f.Description,
                    State = f.State,
                    IsControllable = f.IsControllable,
                    LastStateChange = f.LastStateChange,
                    ErrorMessage = f.ErrorMessage
                }).ToList();
            }
        }

        #endregion

        #region Server Lifecycle

        /// <summary>
        /// Start the pipe server
        /// </summary>
        public void Start()
        {
            if (serverTask != null)
            {
                log.Warn("Pipe server already running");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            serverTask = Task.Run(() => RunServerAsync(cancellationTokenSource.Token));

            log.Info($"Pipe server started: {PipeName}");
        }

        /// <summary>
        /// Stop the pipe server
        /// </summary>
        public void Stop()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                try
                {
                    serverTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    log.Error("Error stopping pipe server", ex);
                }

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                serverTask = null;
            }

            log.Info("Pipe server stopped");
        }

        #endregion

        #region Server Loop

        /// <summary>
        /// Main server loop - handles incoming connections
        /// </summary>
        private async Task RunServerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous))
                    {
                        log.Debug("Waiting for client connection...");

                        // Wait for client connection
                        await pipeServer.WaitForConnectionAsync(cancellationToken);

                        log.Debug("Client connected");

                        // Handle this client connection
                        await HandleClientAsync(pipeServer, cancellationToken);

                        log.Debug("Client disconnected");
                    }
                }
                catch (OperationCanceledException)
                {
                    log.Debug("Server loop cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    log.Error($"Error in server loop: {ex.Message}", ex);
                    await Task.Delay(1000, cancellationToken); // Wait before retrying
                }
            }

            log.Info("Server loop ended");
        }

        /// <summary>
        /// Handle a single client connection
        /// </summary>
        private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                // Read message from client
                var message = await ReadMessageAsync(pipeServer, cancellationToken);

                if (message != null)
                {
                    // Process message and get response
                    var response = ProcessMessage(message);

                    // Send response back to client
                    if (response != null)
                    {
                        await WriteMessageAsync(pipeServer, response, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error handling client: {ex.Message}", ex);

                // Try to send error message to client
                try
                {
                    var errorMsg = new ErrorMessage
                    {
                        Error = "Server error",
                        ExceptionDetails = ex.Message
                    };
                    await WriteMessageAsync(pipeServer, errorMsg, cancellationToken);
                }
                catch
                {
                    // Ignore if we can't send error
                }
            }
        }

        #endregion

        #region Message Processing

        /// <summary>
        /// Process incoming message and generate response
        /// </summary>
        private ServiceMessage ProcessMessage(ServiceMessage message)
        {
            log.Debug($"Processing message: {message.MessageType}");

            switch (message.MessageType)
            {
                case ServiceMessageType.StatusRequest:
                    return HandleStatusRequest();

                case ServiceMessageType.StartFeature:
                case ServiceMessageType.StopFeature:
                    return HandleFeatureControl((FeatureControlMessage)message);

                case ServiceMessageType.Heartbeat:
                    return new HeartbeatMessage
                    {
                        UptimeSeconds = (long)(DateTime.UtcNow - serviceStartTime).TotalSeconds
                    };

                default:
                    log.Warn($"Unknown message type: {message.MessageType}");
                    return new ErrorMessage
                    {
                        Error = $"Unknown message type: {message.MessageType}"
                    };
            }
        }

        /// <summary>
        /// Handle status request
        /// </summary>
        private StatusResponseMessage HandleStatusRequest()
        {
            return new StatusResponseMessage
            {
                IsServiceRunning = true,
                ServiceVersion = GetServiceVersion(),
                Features = GetFeatureStatuses()
            };
        }

        /// <summary>
        /// Handle feature control request (start/stop)
        /// </summary>
        private ServiceMessage HandleFeatureControl(FeatureControlMessage message)
        {
            log.Info($"Feature control request: {message.FeatureId} -> {(message.Enable ? "Enable" : "Disable")}");

            lock (featuresLock)
            {
                if (!features.TryGetValue(message.FeatureId, out var feature))
                {
                    return new ErrorMessage
                    {
                        Error = $"Feature not found: {message.FeatureId}"
                    };
                }

                if (!feature.IsControllable)
                {
                    return new ErrorMessage
                    {
                        Error = $"Feature is not controllable: {message.FeatureId}"
                    };
                }

                // Update state
                var newState = message.Enable ? ServiceFeatureState.Starting : ServiceFeatureState.Stopping;
                UpdateFeatureState(message.FeatureId, newState);

                // TODO: Actually start/stop the feature
                // For now, just transition to final state
                var finalState = message.Enable ? ServiceFeatureState.Running : ServiceFeatureState.Stopped;
                UpdateFeatureState(message.FeatureId, finalState);

                return new FeatureStateChangedMessage
                {
                    FeatureId = message.FeatureId,
                    State = finalState
                };
            }
        }

        /// <summary>
        /// Get service version
        /// </summary>
        private string GetServiceVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        #endregion

        #region Message Serialization

        /// <summary>
        /// Read message from pipe
        /// </summary>
        private async Task<ServiceMessage> ReadMessageAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            try
            {
                using (var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true))
                {
                    var xml = await reader.ReadToEndAsync();
                    if (string.IsNullOrEmpty(xml))
                        return null;

                    // Deserialize based on message type marker
                    return DeserializeMessage(xml);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error reading message: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Write message to pipe
        /// </summary>
        private async Task WriteMessageAsync(NamedPipeServerStream pipe, ServiceMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var xml = SerializeMessage(message);

                if (!pipe.IsConnected)
                {
                    await pipe.WaitForConnectionAsync();
                }

                using (var writer = new StreamWriter(pipe, Encoding.UTF8, 4096, leaveOpen: true))
                {
                    await writer.WriteAsync(xml);
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error writing message: {ex.Message}", ex);
            }
        }

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
            if (xml.Contains("<StatusRequestMessage"))
            {
                var serializer = new XmlSerializer(typeof(StatusRequestMessage));
                using (var reader = new StringReader(xml))
                {
                    return (ServiceMessage)serializer.Deserialize(reader);
                }
            }
            else if (xml.Contains("<FeatureControlMessage"))
            {
                var serializer = new XmlSerializer(typeof(FeatureControlMessage));
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

            throw new InvalidOperationException("Unknown message type");
        }

        #endregion
    }
}
