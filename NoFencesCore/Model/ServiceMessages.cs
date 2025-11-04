using System;
using System.Collections.Generic;

namespace NoFences.Core.Model
{
    /// <summary>
    /// Message types for service communication
    /// </summary>
    public enum ServiceMessageType
    {
        /// <summary>
        /// Request service status
        /// </summary>
        StatusRequest,

        /// <summary>
        /// Service status response
        /// </summary>
        StatusResponse,

        /// <summary>
        /// Request to start a service feature
        /// </summary>
        StartFeature,

        /// <summary>
        /// Request to stop a service feature
        /// </summary>
        StopFeature,

        /// <summary>
        /// Service feature state changed
        /// </summary>
        FeatureStateChanged,

        /// <summary>
        /// Heartbeat message
        /// </summary>
        Heartbeat,

        /// <summary>
        /// Error occurred
        /// </summary>
        Error
    }

    /// <summary>
    /// Base message for service communication
    /// </summary>
    [Serializable]
    public class ServiceMessage
    {
        public ServiceMessageType MessageType { get; set; }
        public Guid MessageId { get; set; }
        public DateTime Timestamp { get; set; }

        public ServiceMessage()
        {
            MessageId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Request for service status
    /// </summary>
    [Serializable]
    public class StatusRequestMessage : ServiceMessage
    {
        public StatusRequestMessage()
        {
            MessageType = ServiceMessageType.StatusRequest;
        }
    }

    /// <summary>
    /// Service status response
    /// </summary>
    [Serializable]
    public class StatusResponseMessage : ServiceMessage
    {
        /// <summary>
        /// Whether the service is running
        /// </summary>
        public bool IsServiceRunning { get; set; }

        /// <summary>
        /// Service version
        /// </summary>
        public string ServiceVersion { get; set; }

        /// <summary>
        /// Available features
        /// </summary>
        public List<ServiceFeatureStatus> Features { get; set; }

        public StatusResponseMessage()
        {
            MessageType = ServiceMessageType.StatusResponse;
            Features = new List<ServiceFeatureStatus>();
        }
    }

    /// <summary>
    /// Feature control request (start/stop)
    /// </summary>
    [Serializable]
    public class FeatureControlMessage : ServiceMessage
    {
        /// <summary>
        /// Feature identifier
        /// </summary>
        public string FeatureId { get; set; }

        /// <summary>
        /// Whether to start (true) or stop (false) the feature
        /// </summary>
        public bool Enable { get; set; }

        public FeatureControlMessage()
        {
            MessageType = ServiceMessageType.StartFeature;
        }
    }

    /// <summary>
    /// Feature state changed notification
    /// </summary>
    [Serializable]
    public class FeatureStateChangedMessage : ServiceMessage
    {
        /// <summary>
        /// Feature identifier
        /// </summary>
        public string FeatureId { get; set; }

        /// <summary>
        /// New state
        /// </summary>
        public ServiceFeatureState State { get; set; }

        /// <summary>
        /// Error message if state is Error
        /// </summary>
        public string ErrorMessage { get; set; }

        public FeatureStateChangedMessage()
        {
            MessageType = ServiceMessageType.FeatureStateChanged;
        }
    }

    /// <summary>
    /// Error message
    /// </summary>
    [Serializable]
    public class ErrorMessage : ServiceMessage
    {
        /// <summary>
        /// Error description
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Optional exception details
        /// </summary>
        public string ExceptionDetails { get; set; }

        public ErrorMessage()
        {
            MessageType = ServiceMessageType.Error;
        }
    }

    /// <summary>
    /// Heartbeat message
    /// </summary>
    [Serializable]
    public class HeartbeatMessage : ServiceMessage
    {
        /// <summary>
        /// Service uptime in seconds
        /// </summary>
        public long UptimeSeconds { get; set; }

        public HeartbeatMessage()
        {
            MessageType = ServiceMessageType.Heartbeat;
        }
    }

    /// <summary>
    /// Service feature status
    /// </summary>
    [Serializable]
    public class ServiceFeatureStatus
    {
        /// <summary>
        /// Unique feature identifier
        /// </summary>
        public string FeatureId { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Feature description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Current state
        /// </summary>
        public ServiceFeatureState State { get; set; }

        /// <summary>
        /// Whether user can control this feature
        /// </summary>
        public bool IsControllable { get; set; }

        /// <summary>
        /// Last state change time
        /// </summary>
        public DateTime LastStateChange { get; set; }

        /// <summary>
        /// Error message if state is Error
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Service feature state
    /// </summary>
    public enum ServiceFeatureState
    {
        /// <summary>
        /// Feature is stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Feature is starting
        /// </summary>
        Starting,

        /// <summary>
        /// Feature is running
        /// </summary>
        Running,

        /// <summary>
        /// Feature is stopping
        /// </summary>
        Stopping,

        /// <summary>
        /// Feature encountered an error
        /// </summary>
        Error
    }
}
