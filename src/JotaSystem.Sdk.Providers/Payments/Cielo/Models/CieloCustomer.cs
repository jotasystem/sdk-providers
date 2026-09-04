namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Dados do comprador enviados no no <c>Customer</c>.
    /// </summary>
    public class CieloCustomer
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }

        /// <summary>Data de nascimento no formato <c>yyyy-MM-dd</c>.</summary>
        public string? Birthdate { get; set; }

        /// <summary>Numero do documento do comprador, somente digitos.</summary>
        public string? Identity { get; set; }

        /// <summary><c>CPF</c> ou <c>CNPJ</c>.</summary>
        public string? IdentityType { get; set; }

        public CieloAddress? Address { get; set; }
        public CieloAddress? DeliveryAddress { get; set; }
    }

    /// <summary>
    /// Endereco do comprador. Obrigatorio para boleto registrado.
    /// </summary>
    public class CieloAddress
    {
        public string? Street { get; set; }
        public string? Number { get; set; }
        public string? Complement { get; set; }
        public string? ZipCode { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; } = "BRA";
    }
}
