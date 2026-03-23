# JotaSystem.Sdk.Providers

Biblioteca de integrações externas da **Jota System** para aplicações .NET.

## Descrição

O `JotaSystem.Sdk.Providers` reúne implementações concretas de comunicação com serviços de terceiros e um builder para registro modular dessas integrações no container de DI.

Hoje o pacote contém:

- `Abstractions` com `ApiResponse` e `ProviderBase` para padronizar chamadas HTTP e respostas.
- `Address` com providers de consulta de endereço e bancos via `ViaCep` e `BrasilApi`.
- `Ai` com integração de chat via `OpenAI`.
- `Email` com implementações para `SMTP`, `Brevo`, `SendGrid` e `SendPulse`.
- `Storage` com implementação para `Azure Blob Storage`.
- `DependencyInjection` com `AddJotaSystemProviders()` e extensões modulares por área.

## Registro e composição

O pacote usa um builder próprio para permitir composição por provider:

```csharp
builder.Services
    .AddJotaSystemProviders()
    .AddViaCep()
    .AddBrasilApi()
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
    });
```

## Providers disponíveis

- Endereço: `IViaCepProvider`, `IBrasilApiProvider`
- IA: `IOpenAiProvider`
- E-mail: `ISmtpProvider`, `IBrevoProvider`, `ISendGridProvider`, `ISendPulseProvider`
- Storage: `IAzureBlobProvider`

## Perfil do pacote

Este SDK representa a camada de adaptação para serviços externos. Ele pode ser consumido isoladamente, mas o uso mais natural é como implementação concreta dos contratos de infraestrutura definidos pela arquitetura da Jota System.
