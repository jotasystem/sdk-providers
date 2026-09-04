using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Resposta das operacoes de captura, cancelamento e devolucao.
    /// </summary>
    public class CieloOperationResponse
    {
        /// <summary>Status resultante da operacao. Ver <see cref="CieloPaymentStatusEnum"/>.</summary>
        public int Status { get; set; }

        public string? Tid { get; set; }
        public string? ProofOfSale { get; set; }
        public string? AuthorizationCode { get; set; }
        public string? ReturnCode { get; set; }
        public string? ReturnMessage { get; set; }
        public int? ReasonCode { get; set; }
        public string? ReasonMessage { get; set; }
        public List<CieloLink> Links { get; set; } = [];

        /// <summary>Corpo original devolvido pela Cielo, preservado para auditoria.</summary>
        [JsonIgnore]
        public string? RawPayload { get; set; }
    }
}
