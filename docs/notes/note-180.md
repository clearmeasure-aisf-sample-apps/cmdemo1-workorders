# Note 180

`ConditionalGetEtag.CreateWeakEtagForJson` serializes its value with `JsonSerializer.SerializeToUtf8Bytes` using `ConditionalGetEtag.JsonSerializerOptions` (`new(JsonSerializerDefaults.Web)`) and returns the weak entity tag from the private `CreateWeakEtag` helper: SHA-256 of those UTF-8 bytes (`SHA256.HashData`), lowercase hex (`Convert.ToHexStringLower`), wrapped as `EntityTagHeaderValue` with `isWeak: true`. The methods are `CreateWeakEtagForJson` and `CreateWeakEtag` in `src/UI/Api/ConditionalGetEtag.cs`.
