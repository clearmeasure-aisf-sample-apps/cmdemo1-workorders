# Note 181

`ConditionalGetEtag.CreateWeakEtagForPlainText` encodes its `text` argument with `Encoding.UTF8.GetBytes` and returns `CreateWeakEtag` of those bytes. `CreateWeakEtag` hashes those bytes with `SHA256.HashData`, formats the digest with `Convert.ToHexStringLower`, and wraps it as `EntityTagHeaderValue` with `isWeak: true`. The methods are `CreateWeakEtagForPlainText` and `CreateWeakEtag` in `src/UI/Api/ConditionalGetEtag.cs`.
