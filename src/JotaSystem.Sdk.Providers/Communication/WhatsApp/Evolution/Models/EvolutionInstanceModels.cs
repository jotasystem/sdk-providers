namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution.Models
{
    /// <summary>Resposta da raiz do servidor (<c>GET /</c>).</summary>
    public class EvolutionServerInfo
    {
        public int? Status { get; set; }
        public string? Message { get; set; }
        public string? Version { get; set; }
        public string? ClientName { get; set; }
        public string? ManagerUrl { get; set; }
    }

    /// <summary>
    /// Comportamento da sessao no WhatsApp. Usado na criacao da instancia e em
    /// <c>POST /settings/set/{instance}</c>.
    /// </summary>
    public class EvolutionInstanceSettings
    {
        /// <summary>Recusa chamadas de voz e video recebidas.</summary>
        public bool RejectCall { get; set; }

        /// <summary>Mensagem enviada automaticamente a quem ligou, quando <see cref="RejectCall"/> esta ligado.</summary>
        public string? MsgCall { get; set; }

        /// <summary>Ignora mensagens de grupos (nao geram webhook).</summary>
        public bool GroupsIgnore { get; set; } = true;

        /// <summary>Mantem o numero sempre como "online".</summary>
        public bool AlwaysOnline { get; set; }

        /// <summary>Marca como lidas as mensagens recebidas.</summary>
        public bool ReadMessages { get; set; }

        /// <summary>Marca como vistos os status dos contatos.</summary>
        public bool ReadStatus { get; set; }

        /// <summary>Sincroniza o historico completo de conversas ao conectar.</summary>
        public bool SyncFullHistory { get; set; }
    }

    /// <summary>Configuracao do webhook da instancia.</summary>
    public class EvolutionWebhookConfig
    {
        public bool Enabled { get; set; } = true;
        public string Url { get; set; } = string.Empty;

        /// <summary>Headers enviados pelo servidor em cada notificacao (Evolution API 2.1+).</summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>Quando ligado, cada evento vai para <c>{Url}/{evento}</c>.</summary>
        public bool ByEvents { get; set; }

        /// <summary>Envia a midia recebida em base64 dentro do payload.</summary>
        public bool Base64 { get; set; }

        /// <summary>Eventos assinados. Vazio usa <see cref="EvolutionApiOptions.DefaultWebhookEvents"/>.</summary>
        public List<string> Events { get; set; } = [];
    }

    /// <summary>Dados para criar uma instancia (<c>POST /instance/create</c>).</summary>
    public class EvolutionCreateInstanceRequest
    {
        /// <summary>Nome unico da instancia no servidor. Identifica o numero em todas as chamadas.</summary>
        public string InstanceName { get; set; } = string.Empty;

        /// <summary>Token proprio da instancia. Sem valor, o servidor gera um.</summary>
        public string? Token { get; set; }

        /// <summary>Numero a parear. Quando informado, o servidor tambem devolve o codigo de pareamento.</summary>
        public string? Number { get; set; }

        /// <summary>Devolve o primeiro QR Code ja na criacao.</summary>
        public bool QrCode { get; set; } = true;

        /// <summary>Motor da instancia. Sem valor usa <see cref="EvolutionApiOptions.Integration"/>.</summary>
        public string? Integration { get; set; }

        public EvolutionInstanceSettings? Settings { get; set; }
        public EvolutionWebhookConfig? Webhook { get; set; }
    }

    /// <summary>QR Code e codigo de pareamento devolvidos ao iniciar a conexao.</summary>
    public class EvolutionQrCode
    {
        /// <summary>Conteudo bruto do QR Code (texto lido pelo aplicativo).</summary>
        public string? Code { get; set; }

        /// <summary>Imagem do QR Code em data URI (<c>data:image/png;base64,...</c>).</summary>
        public string? Base64 { get; set; }

        /// <summary>Codigo de 8 caracteres para parear digitando no celular, quando o numero foi informado.</summary>
        public string? PairingCode { get; set; }

        /// <summary>Quantas vezes o QR Code ja foi renovado nesta tentativa.</summary>
        public int? Count { get; set; }

        public bool HasContent =>
            !string.IsNullOrWhiteSpace(Base64) || !string.IsNullOrWhiteSpace(Code) || !string.IsNullOrWhiteSpace(PairingCode);
    }

    public class EvolutionCreateInstanceResponse
    {
        public string InstanceName { get; set; } = string.Empty;
        public string? InstanceId { get; set; }
        public string? Status { get; set; }

        /// <summary>Token da instancia (<c>hash</c>), aceito no header <c>apikey</c> das chamadas dela.</summary>
        public string? Token { get; set; }

        public EvolutionQrCode? QrCode { get; set; }
        public string? RawPayload { get; set; }
    }

    /// <summary>
    /// Resultado de <c>GET /instance/connect/{instance}</c>: o QR Code para parear ou,
    /// quando a sessao ja esta aberta, apenas o estado.
    /// </summary>
    public class EvolutionConnectResponse
    {
        public EvolutionConnectionStateEnum State { get; set; }
        public EvolutionQrCode? QrCode { get; set; }
        public string? RawPayload { get; set; }

        public bool IsConnected => State == EvolutionConnectionStateEnum.Open;
    }

    public class EvolutionConnectionState
    {
        public string InstanceName { get; set; } = string.Empty;
        public EvolutionConnectionStateEnum State { get; set; }

        /// <summary>Valor devolvido pelo servidor (<c>open</c>, <c>connecting</c>, <c>close</c>).</summary>
        public string? RawState { get; set; }
    }

    /// <summary>Instancia listada em <c>GET /instance/fetchInstances</c>.</summary>
    public class EvolutionInstanceInfo
    {
        public string InstanceName { get; set; } = string.Empty;
        public string? InstanceId { get; set; }
        public EvolutionConnectionStateEnum State { get; set; }
        public string? RawState { get; set; }

        /// <summary>JID do numero conectado, ex.: <c>5511999999999@s.whatsapp.net</c>.</summary>
        public string? OwnerJid { get; set; }

        /// <summary>Numero conectado, apenas digitos, extraido do <see cref="OwnerJid"/>.</summary>
        public string? PhoneNumber { get; set; }

        public string? ProfileName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Integration { get; set; }
        public string? Token { get; set; }
        public int? DisconnectionReasonCode { get; set; }
        public DateTimeOffset? DisconnectionAt { get; set; }
    }
}
