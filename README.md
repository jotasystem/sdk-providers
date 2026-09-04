# JotaSystem.Sdk.Providers

Biblioteca de integrações externas da **Jota System** para aplicações .NET.

## Descrição

O `JotaSystem.Sdk.Providers` reúne implementações concretas de comunicação com serviços de terceiros e um builder para registro modular dessas integrações no container de DI.

Hoje o pacote contém:

- `Abstractions` com `ApiResponse` e `ProviderBase` para padronizar chamadas HTTP e respostas.
- `Logistics` centraliza consulta de endereços e fretes via `ViaCep`, `OpenCep`, `BrasilApi` e `Correios`.
- `Ai` com integração de chat via `OpenAI`.
- `Communication` centraliza os canais `Email`, `Sms` e `PushNotification`, com implementações de e-mail para `SMTP`, `Brevo`, `SendGrid` e `SendPulse`.
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

Os metadados da cobrança ficam reservados aos ajustes específicos da Cielo, com as chaves de `CieloMetadataKeys`: `soft_descriptor`, `capture`, `boleto_provider`, `boleto_assignor`, `boleto_instructions`, `boleto_demonstrative`, `boleto_identification` e `boleto_number`. Cada um deles também pode ser definido no `PublicConfigJson` da integração ou nas `CieloOptions`, nessa ordem de precedência.

As credenciais são resolvidas nesta ordem: credenciais nomeadas em `NamedCredentials` pela `SecretReference` da integração; `merchantId` do `PublicConfigJson` combinado com a `SecretReference` como `MerchantKey`; e, por último, `DefaultCredentials`. O ambiente (`sandbox` ou `production`) vem da própria integração.

## Providers disponíveis

- Logística: `IViaCepProvider`, `IOpenCepProvider`, `IBrasilApiProvider`, `ICorreiosProvider`, `IMelhorEnvioProvider`
- IA: `IOpenAiProvider`
- Comunicação: `ISmtpProvider`, `IBrevoProvider`, `ISendGridProvider`, `ISendPulseProvider`
- Storage: `IAzureBlobProvider`
- Pagamentos: `IPaymentGatewayProviderResolver`, `IPaymentGatewayProvider` (chave `cielo`), `ICieloProvider`

## Perfil do pacote

Este SDK representa a camada de adaptação para serviços externos. Ele pode ser consumido isoladamente, mas o uso mais natural é como implementação concreta dos contratos de infraestrutura definidos pela arquitetura da Jota System.
