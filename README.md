# JotaSystem.Sdk.Providers

Pacote de provedores e integrações externas da **Jota System**, com implementações voltadas à comunicação com serviços de terceiros.

---

## 📦 Descrição

O **JotaSystem.Sdk.Providers** concentra as implementações de integração externas, fornecendo provedores prontos e padronizados que podem ser utilizados de forma isolada ou em conjunto com outros SDKs da Jota System.

Inclui:
- **Provedores HTTP e REST** com autenticação e logging.
- **Integrações com serviços externos** (ex: APIs de pagamento, envio de e-mails, notificações, etc.).
- **Interfaces e contratos de provedores** para uso genérico.
- **Mecanismos de fallback e retry.**

---

## ⚙️ Como usar os Providers

Para utilizar qualquer provider do pacote (ex.: ViaCepProvider, BrasilApiCepProvider), é obrigatório registrar as dependências na sua aplicação utilizando o método de extensão **AddJotaSystemProviders**.

### 🛠 Registro no `Program.cs` ou `Startup.cs`

```csharp
builder.Services.AddJotaSystemProviders();