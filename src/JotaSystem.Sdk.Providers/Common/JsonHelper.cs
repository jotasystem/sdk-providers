using System.Text.Json;

namespace JotaSystem.Sdk.Providers.Common
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Desserializa o JSON em T, retornando ApiResponse com sucesso ou erro.
        /// </summary>
        public static ApiResponse<T> Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ApiResponse<T>.CreateFail("JSON vazio ou nulo.");

            try
            {
                var obj = JsonSerializer.Deserialize<T>(json, _options);
                if (obj == null)
                    return ApiResponse<T>.CreateFail("Falha ao desserializar JSON: objeto nulo.");

                return ApiResponse<T>.CreateSuccess(obj);
            }
            catch (JsonException ex)
            {
                return ApiResponse<T>.CreateFail($"Erro ao desserializar JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<T>.CreateFail($"Erro inesperado ao desserializar JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Serializa o objeto em JSON camelCase.
        /// </summary>
        public static string Serialize(object obj)
        {
            if (obj == null)
                return string.Empty;

            try
            {
                return JsonSerializer.Serialize(obj, _options);
            }
            catch
            {
                return string.Empty; // fallback silencioso
            }
        }
    }
}