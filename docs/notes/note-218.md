# Note 218

`RealtimeNotificationKinds.RemotableEvent` in `src/UI/Server/Notifications/RealtimeNotificationHub.cs` is the public constant `remotableEvent` (`public const string RemotableEvent = "remotableEvent";`). `RealtimeNotificationHub.BroadcastRemotableEventAsync` sets the WebSocket envelope kind to that constant (`new RealtimeNotificationEnvelope(RealtimeNotificationKinds.RemotableEvent, messageJson)`).
