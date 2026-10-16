namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution.Models
{
    /// <summary>Mensagem de texto (<c>POST /message/sendText/{instance}</c>).</summary>
    public class EvolutionSendTextRequest
    {
        /// <summary>Numero com DDI (apenas digitos) ou JID do destinatario.</summary>
        public string Number { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        /// <summary>Atraso, em milissegundos, com o indicador "digitando..." antes do envio.</summary>
        public int? Delay { get; set; }

        /// <summary>Gera a pre-visualizacao do primeiro link do texto.</summary>
        public bool? LinkPreview { get; set; }
    }

    /// <summary>Mensagem com midia (<c>POST /message/sendMedia/{instance}</c>).</summary>
    public class EvolutionSendMediaRequest
    {
        public string Number { get; set; } = string.Empty;

        /// <summary>Ver <see cref="EvolutionMediaTypes"/>.</summary>
        public string MediaType { get; set; } = EvolutionMediaTypes.Document;

        /// <summary>Tipo MIME do arquivo, ex.: <c>application/pdf</c>.</summary>
        public string? MimeType { get; set; }

        /// <summary>URL publica do arquivo ou conteudo em base64 (sem o prefixo data URI).</summary>
        public string Media { get; set; } = string.Empty;

        public string? Caption { get; set; }

        /// <summary>Nome exibido para documentos.</summary>
        public string? FileName { get; set; }

        public int? Delay { get; set; }
    }

    /// <summary>Mensagem aceita pelo servidor para envio.</summary>
    public class EvolutionMessageResponse
    {
        /// <summary>Identificador da mensagem no WhatsApp (<c>key.id</c>).</summary>
        public string? MessageId { get; set; }

        public string? RemoteJid { get; set; }

        /// <summary>Situacao inicial informada pelo servidor (normalmente <c>PENDING</c>).</summary>
        public string? Status { get; set; }

        public long? MessageTimestamp { get; set; }
        public string? RawPayload { get; set; }
    }

    /// <summary>Resultado de <c>POST /chat/whatsappNumbers/{instance}</c>.</summary>
    public class EvolutionNumberCheck
    {
        public string Number { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public string? Jid { get; set; }
    }
}
