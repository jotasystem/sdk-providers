using System.Collections.Concurrent;

namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    internal interface ICieloAuthTokenCache
    {
        bool TryGet(string key, DateTimeOffset minimumExpiration, out string token);
        void Set(string key, string token, DateTimeOffset expiresAt);
    }

    /// <summary>
    /// O token OAuth2 do Silent Order Post vale cerca de dez minutos e e pedido a cada
    /// checkout aberto, entao vale reaproveita-lo entre requisicoes.
    /// </summary>
    internal sealed class CieloAuthTokenCache : ICieloAuthTokenCache
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
