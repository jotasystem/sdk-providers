# JotaSystem.Sdk.Providers

Biblioteca de integrações externas da **Jota System** para aplicações .NET.

## Descrição

O `JotaSystem.Sdk.Providers` reúne implementações concretas de comunicação com serviços de terceiros e um builder para registro modular dessas integrações no container de DI.

Hoje o pacote contém:

- `Abstractions` com `ApiResponse` e `ProviderBase` para padronizar chamadas HTTP e respostas.
- `Logistics` centraliza consulta de endereços e fretes via `ViaCep`, `OpenCep`, `BrasilApi` e `Correios`.
- `Ai` com integração de chat via `OpenAI`.
- `Communication` centraliza os canais `Email`, `Sms`, `WhatsApp` e `PushNotification`, com implementações de e-mail para `SMTP`, `Brevo`, `SendGrid` e `SendPulse` e de WhatsApp para a `Evolution API`.
- `Storage` com implementação para `Azure Blob Storage`.
- `Payments` com o contrato de gateway de pagamento e a integração completa com a `Cielo` (API E-commerce 3.0).
- `DependencyInjection` com `AddJotaSystemProviders()` e extensões modulares por área.

## Registro e composição

O pacote usa um builder próprio para permitir composição por provider:

```csharp
builder.Services
    .AddJotaSystemProviders()
    .AddViaCep()
    .AddOpenCep()
    .AddBrasilApi()
    .AddCorreios(options =>
    {
        options.DefaultCredentials = new CorreiosCredentials
        {
            UserName = "...",
            AccessCode = "...",
            PostingCardNumber = "..."
        };
    })
    .AddOpenAi(options =>
    {
        options.ApiKey = "...";
        options.Model = "...";
    })
    .AddSmtp(options =>
    {
        options.Host = "...";
    })
    .AddAzureBlob(options =>
    {
        options.ConnectionString = "...";
    })
    .AddPayments()
    .AddCielo(options =>
    {
        options.DefaultCredentials = new CieloCredentials
        {
            MerchantId = "...",
            MerchantKey = "...",
            Environment = CieloEnvironmentEnum.Sandbox
        };
        options.SoftDescriptor = "MinhaLoja";
        options.BoletoProvider = "Bradesco2";
    });
```

## Pagamentos

`AddPayments()` registra o `IPaymentGatewayProviderResolver`, que escolhe o gateway pela chave gravada na integração do sistema consumidor. `AddCielo()` registra a Cielo sob a chave `cielo` e também expõe o `ICieloProvider`, com o contrato completo da API E-commerce: venda, consulta por `PaymentId` e por `MerchantOrderId`, captura, cancelamento, devolução Pix e gestão da recorrência agendada.

Meios de pagamento aceitos no código do método (`MethodCode`):

| Código | Meio |
|---|---|
| `credit_card` | Cartão de crédito |
| `credit_card_recurrent` | Cartão de crédito com recorrência agendada pela Cielo |
| `pix` | QRCode Pix |
| `boleto` | Boleto registrado |

Cartão, endereço do pagador e parâmetros de recorrência vêm do próprio contrato de pagamento do `JotaSystem.Sdk.Core`: `PaymentProviderRequest.Card` (`PaymentCard`), `Customer.Address` (`PaymentAddress`) e `PaymentProviderRequest.Recurrence` (`PaymentRecurrence`). Em `PaymentCard`, o número e o código de segurança não são serializados — chegam à Cielo, mas ficam fora de log e de payload persistido.

### Captura do cartão no navegador (Silent Order Post)

O caminho recomendado para crédito é não deixar o cartão passar pelo servidor da loja. `CreateCheckoutSessionAsync` abre uma sessão do **Silent Order Post**: o SDK autentica no OAuth2 da Cielo com `ClientId`/`ClientSecret`, pede um `AccessToken` de sessão e devolve, junto dele, o endereço do script e o ambiente. A página de checkout carrega o script, marca os campos com as classes `bp-sop-cardnumber`, `bp-sop-cardexpirationdate`, `bp-sop-cardholdername` e `bp-sop-cardcvv`, e chama `bpSop_silentOrderPost`. A Cielo devolve ao navegador um `PaymentToken` de uso único, válido por 20 minutos.

Esse token é enviado à cobrança em `PaymentCard.SingleUseToken`; o provider o traduz para `Payment.CreditCard.PaymentToken` e **não envia nenhum outro dado do cartão** — número, validade e CVV já estão dentro do token, e reenviá-los faz a Cielo recusar a autorização.

O token OAuth2 dura cerca de dez minutos e é reaproveitado entre requisições por um cache em memória, então abrir vários checkouts não gera uma autenticação por checkout.

Os metadados da cobrança ficam reservados aos ajustes específicos da Cielo, com as chaves de `CieloMetadataKeys`: `soft_descriptor`, `capture`, `boleto_provider`, `boleto_assignor`, `boleto_instructions`, `boleto_demonstrative`, `boleto_identification` e `boleto_number`. Cada um deles também pode ser definido no `PublicConfigJson` da integração ou nas `CieloOptions`, nessa ordem de precedência.

As credenciais são resolvidas nesta ordem: credenciais nomeadas em `NamedCredentials` pela `SecretReference` da integração; `merchantId` do `PublicConfigJson` combinado com a `SecretReference` como `MerchantKey`; e, por último, `DefaultCredentials`. O ambiente (`sandbox` ou `production`) vem da própria integração.

## WhatsApp (Evolution API)

`AddEvolutionApi()` registra o `IEvolutionApiProvider`, que fala com um servidor da [Evolution API](https://doc.evolution-api.com) v2. Cada número de WhatsApp é uma **instância** do servidor; todas as chamadas recebem o nome da instância e são autenticadas pelo header `apikey`.

```csharp
services.AddJotaSystemProviders()
    .AddEvolutionApi(options =>
    {
        // Opcional: sem servidor padrão, cada chamada informa as próprias EvolutionApiCredentials.
        options.DefaultCredentials = new EvolutionApiCredentials
        {
            BaseUrl = "https://evolution.minhaempresa.com.br",
            ApiKey = "AUTHENTICATION_API_KEY do servidor"
        };
    });
```

Fluxo para conectar um número:

1. `CreateInstanceAsync` cria a instância (com as `EvolutionInstanceSettings` e o webhook) e já devolve o primeiro QR Code e o token da instância.
2. `ConnectInstanceAsync` renova o QR Code (ele expira em cerca de 40 segundos). Informando o número, devolve também o código de pareamento de 8 caracteres.
3. `GetConnectionStateAsync` ou o evento `CONNECTION_UPDATE` do webhook indicam quando a sessão abre (`Open`). `FetchInstancesAsync` devolve o número e o nome do perfil conectados.
4. `LogoutInstanceAsync` desconecta o aparelho; `DeleteInstanceAsync` remove a instância.

Mensagens: `SendTextAsync`, `SendMediaAsync` (URL ou base64) e `CheckWhatsAppNumbersAsync`. Números nacionais com 10 ou 11 dígitos recebem o DDI de `EvolutionApiOptions.DefaultCountryCode` (padrão `55`) — ver `EvolutionPhoneNumber`.

Webhook: `EvolutionWebhookParser.Parse` (ou `IEvolutionApiProvider.ParseWebhook`) lê a notificação e normaliza o evento para as constantes de `EvolutionWebhookEvents`, preenchendo o estado da conexão, o QR Code ou a mensagem conforme o evento.

## Providers disponíveis

- Logística: `IViaCepProvider`, `IOpenCepProvider`, `IBrasilApiProvider`, `ICorreiosProvider`, `IMelhorEnvioProvider`
- IA: `IOpenAiProvider`
- Comunicação: `ISmtpProvider`, `IBrevoProvider`, `IBrevoSmsProvider`, `ISendGridProvider`, `ISendPulseProvider`, `IEvolutionApiProvider` (WhatsApp)
- Storage: `IAzureBlobProvider`
- Pagamentos: `IPaymentGatewayProviderResolver`, `IPaymentGatewayProvider` (chave `cielo`), `ICieloProvider`

## Perfil do pacote

Este SDK representa a camada de adaptação para serviços externos. Ele pode ser consumido isoladamente, mas o uso mais natural é como implementação concreta dos contratos de infraestrutura definidos pela arquitetura da Jota System.
