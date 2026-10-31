# Note 175

EchoController.FormatRemoteIp returns null when the remote address is null, `address.MapToIPv4().ToString()` when `address.IsIPv4MappedToIPv6` is true, and `address.ToString()` otherwise (`address is null ? null : address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString()`). `BuildEchoResponse` assigns that value to `EchoResponse.RemoteIpAddress` (`RemoteIpAddress: FormatRemoteIp(httpContext.Connection.RemoteIpAddress)`). The method is `FormatRemoteIp` in `src/UI/Api/Controllers/EchoController.cs`.
