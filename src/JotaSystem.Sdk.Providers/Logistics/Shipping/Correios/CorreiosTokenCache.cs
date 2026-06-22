using System.Collections.Concurrent;

namespace JotaSystem.Sdk.Providers.Logistics.Shipping.Correios
{
    internal interface ICorreiosTokenCache
    {
        bool TryGet(string key, DateTimeOffset minimumExpiration, out string token);
        void Set(string key, string token, DateTimeOffset expiresAt);
    }

    internal class CorreiosTokenCache : ICorreiosTokenCache
    {
        private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new();

        public bool TryGet(string key, DateTimeOffset minimumExpiration, out string token)
        {
            if (_tokens.TryGetValue(key, out var entry) && entry.ExpiresAt > minimumExpiration)
            {
                token = entry.Token;
                return true;
            }

            _tokens.TryRemove(key, out _);
            token = string.Empty;
            return false;
        }

        public void Set(string key, string token, DateTimeOffset expiresAt) =>
            _tokens[key] = new TokenEntry(token, expiresAt);

        private sealed record TokenEntry(string Token, DateTimeOffset ExpiresAt);
    }
}
