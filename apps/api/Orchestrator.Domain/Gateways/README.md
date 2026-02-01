# Gateway Pattern Architecture

This folder contains the Gateway pattern implementation for external system integrations (LLM, Calendar, etc.).

## Structure

```
Gateways/
├── GatewayFacade.cs              # Base facade (partial class)
├── GatewayFacade.{Integration}.cs # Per-integration facades (e.g., GatewayFacade.Llm.cs)
├── Managers/                      # Gateway managers that call external APIs
│   └── {Integration}Manager.cs
├── Models/                        # External API resource models
│   └── {Integration}/            # Per-integration folder
│       ├── {Integration}Request.cs
│       └── {Integration}Response.cs
├── Mappers/                       # Model mappers
│   └── {Integration}Mapper.cs
├── Exceptions/                    # Gateway-specific exceptions
│   ├── GatewayApiException.cs
│   ├── GatewayConnectionException.cs
│   ├── GatewayConfigurationException.cs
│   └── {Integration}{Type}Exception.cs
└── HttpClients/                   # HTTP client wrappers (optional)
    └── {Integration}HttpClient.cs
```

## Usage

### 1. Configuration

Add configuration to `appsettings.json`:

```json
{
  "Gateways": {
    "Llm": {
      "ApiKey": "your-api-key",
      "BaseUrl": "https://api.openai.com/v1",
      "Timeout": 30
    },
    "Calendar": {
      "ApiKey": "your-calendly-api-key",
      "BaseUrl": "https://api.calendly.com",
      "Timeout": 30
    }
  }
}
```

### 2. Access from DomainFacade

Gateway methods are accessed through `DomainFacade`:

```csharp
var domainFacade = new DomainFacade();
var result = await domainFacade.GatewayFacade.GenerateCompletion(request);
```

### 3. Creating a New Gateway Integration

Use the templates in `apps/api/.cursor/templates/create/`:

- `GatewayFacade.Base.hbs` - Creates GatewayFacade.{Integration}.cs
- `GatewayManager.hbs` - Creates {Integration}Manager.cs
- `GatewayResourceModel.Request.hbs` - Creates request models
- `GatewayResourceModel.Response.hbs` - Creates response models
- `GatewayMapper.hbs` - Creates {Integration}Mapper.cs
- `GatewayException.hbs` - Creates exception classes
- `GatewayHttpClient.hbs` - (Optional) Creates HTTP client wrapper

## Architecture Principles

1. **Domain Shielding**: GatewayFacade exposes business-focused methods that shield the domain from API details
2. **Separation of Concerns**: Managers handle HTTP communication, Mappers handle model conversion
3. **Error Handling**: Gateway exceptions inherit from `TechnicalBaseException`
4. **Configuration**: All settings accessed via `ServiceLocator` and `ConfigurationProvider`
5. **Testability**: Managers can be tested via custom test doubles (no external mocking frameworks)

## Example: LLM Integration

```csharp
// GatewayFacade.Llm.cs
public sealed partial class GatewayFacade
{
    private LlmManager? _llmManager;
    private LlmManager LlmManager => _llmManager ??= new LlmManager(_serviceLocator);

    public async Task<string> GenerateCompletion(ChatCompletionRequest request)
    {
        return await LlmManager.GenerateCompletion(request).ConfigureAwait(false);
    }
}

// LlmManager.cs handles HTTP calls, error handling, and mapping
// LlmMapper.cs converts between domain models and API resource models
```

