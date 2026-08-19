# Plano: Upgrade do Backend de .NET 8 para .NET 10

## Resumo

| Aspecto | Detalhe |
|---------|---------|
| **De** | .NET 8 (C# 12, LTS atual) |
| **Para** | .NET 10 (C# 14, LTS) |
| **Risco** | Baixo a moderado |
| **Tempo estimado** | 2-4 horas |
| **Arquivos modificados** | 4 arquivos (obrigatório) |

---

## Análise do Backend Atual

### Stack

| Componente | Versão Atual |
|------------|-------------|
| Runtime | .NET 8 |
| C# | 12 |
| ASP.NET Core | 8.0 |
| Swashbuckle | 6.5.0 |
| xUnit | 2.7.0 |
| Moq | 4.20.72 |
| coverlet | 6.0.1 |

### Features C# 12 já em uso

- Primary constructors (classes e structs)
- Collection expressions `[]` e spread `[..]`
- UTF-8 string literals `u8`
- File-scoped namespaces
- Records com `init`
- Top-level statements
- Pattern matching avançado

### Pacotes NuGet

| Pacote | Compatível com .NET 10? | Ação |
|--------|------------------------|------|
| `Swashbuckle.AspNetCore` 6.5.0 | **NÃO** — precisa ser substituído | Migrar para `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` |
| `Microsoft.NET.Test.Sdk` 17.9.0 | Sim, mas desatualizado | Atualizar para 17.14.x+ |
| `Moq` 4.20.72 | Sim | Atualizar para última versão |
| `coverlet.collector` 6.0.1 | **CUIDADO** — incompatível com MTP do .NET 10 | Adicionar `TestingPlatformDotnetTestSupport=false` ou migrar para `coverlet.MTP` |
| `xunit` 2.7.0 | Sim, mas desatualizado | Atualizar para 2.9.x+ |
| `xunit.runner.visualstudio` 2.5.7 | CUIDADO — pode precisar atualização | Atualizar para 2.8.x+ |

---

## Breaking Changes que Afetam o Projeto

### ALTO IMPACTO

| Breaking Change | Arquivos Afetados | Mitigação |
|----------------|-------------------|-----------|
| **Swashbuckle removido do template padrão** | `Program.cs`, `Sonaris.Backend.csproj` | Substituir por `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` |
| **coverlet.collector incompatível com MTP** | `Sonaris.Backend.Tests.csproj` | Adicionar `<TestingPlatformDotnetTestSupport>false</TestingPlatformDotnetTestSupport>` |

### MÉDIO IMPACTO

| Breaking Change | Afeta o Projeto? |
|----------------|-----------------|
| `field` contextual keyword (C# 14) | NÃO — nenhum campo chamado `field` existe no projeto |
| Cookie login redirects desabilitados | NÃO — não há auth configurada |
| `UseAuthorization()` sem `AddAuthorization()` | SIM — chamada órfã em `Program.cs`, deve ser removida ou completada |

### BAIXO IMPACTO

| Breaking Change | Afeta o Projeto? |
|----------------|-----------------|
| Nullable reference types | NÃO — está desabilitado no projeto |
| `WebHostBuilder` obsoleto | NÃO — usa `WebApplication.CreateBuilder` |

---

## Plano de Upgrade — Passo a Passo

### Fase 1: Preparação (risco baixo)

1. Verificar que os testes passam no .NET 8 atual: `dotnet test`
2. Branch já criada: `feat/dotnet-10-upgrade`

### Fase 2: Target Framework (risco baixo)

3. Em `Sonaris.Backend.csproj`:
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```
4. Em `Tests/Sonaris.Backend.Tests.csproj`:
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

### Fase 3: Pacotes NuGet (risco médio)

5. **Remover Swashbuckle** e adicionar OpenAPI nativo:
   ```bash
   dotnet remove package Swashbuckle.AspNetCore
   dotnet add package Microsoft.AspNetCore.OpenApi
   dotnet add package Scalar.AspNetCore
   ```

6. **Atualizar pacotes de teste**:
   ```bash
   cd Tests
   dotnet add package Microsoft.NET.Test.Sdk --version 17.14.0
   dotnet add package xunit --version 2.9.3
   dotnet add package xunit.runner.visualstudio --version 2.8.2
   dotnet add package coverlet.collector --version 6.0.4
   ```

7. **Adicionar compatibilidade MTP** ao test `.csproj`:
   ```xml
   <PropertyGroup>
     <TestingPlatformDotnetTestSupport>false</TestingPlatformDotnetTestSupport>
   </PropertyGroup>
   ```

### Fase 4: Migração de Código (risco médio)

8. **Reescrever `Program.cs`** — migração OpenAPI:

   **Remover:**
   ```csharp
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen();
   // ...
   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI();
   }
   ```

   **Adicionar:**
   ```csharp
   builder.Services.AddOpenApi();
   // ...
   if (app.Environment.IsDevelopment())
   {
       app.MapOpenApi();
       app.MapScalarApiReference();
   }
   ```

9. **Remover chamada órfã de autorização**:
   ```csharp
   // Remover ou completar:
   app.UseAuthorization();
   // Opção A: Remover (não há auth)
   // Opção B: Adicionar builder.Services.AddAuthorization();
   ```

10. **Corrigir pattern matching redundante** em `BaseResponseExtensions.cs`:
    ```csharp
    // Antes:
    if (ex is SonarisException)
    {
        var exception = ex as SonarisException;
        // ...
    }
    // Depois:
    if (ex is SonarisException exception)
    {
        // ...
    }
    ```

### Fase 5: Dockerfile (risco baixo)

11. **Atualizar imagens base**:
    ```dockerfile
    FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
    FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
    ```

12. Verificar que `python3-mutagen` e `eyed3` continuam disponíveis na imagem base.

### Fase 6: Build e Testes (crítico)

13. `dotnet restore && dotnet build`
14. Corrigir erros de compilação (esperados: mínimos)
15. `dotnet test`
16. Corrigir falhas de testes relacionadas a MTP/coverlet
17. `docker build -t sonaris-backend .`
18. Teste de integração do container

---

## Modernizações Opcionais (pós-upgrade)

| Melhoria | Onde Aplicar | Benefício |
|----------|-------------|-----------|
| Habilitar `<Nullable>enable</Nullable>` | Todos os arquivos | Null safety melhorado |
| Usar `field` keyword (C# 14) | `FileSystemItemDto.AbsolutePath`, `ErrorModel` | Eliminar backing fields |
| Converter extensões para blocos C# 14 | `BaseResponseExtensions.cs` | Organização mais limpa |
| Usar `IOptions<T>` para configuração | `ArquivoService`, `MusicSearchService` | Configuração tipada |
| Async `MusicMetadataWriter` | `SalvarMetadados` | Usar `await lockSalvamento.WaitAsync()` |
| Converter `PageInfoRequest` para `readonly record struct` | `PageInfoRequest.cs` | Imutabilidade |
| Converter `ErrorModel` para `record` | `ErrorModel.cs` | Simplificação |

---

## Arquivos que Precisam de Modificação

| # | Arquivo | Tipo de Mudança |
|---|---------|----------------|
| 1 | `Sonaris.Backend.csproj` | TFM + substituição de pacote |
| 2 | `Tests/Sonaris.Backend.Tests.csproj` | TFM + atualização de pacotes + propriedade MTP |
| 3 | `Program.cs` | Migração Swashbuckle → OpenAPI |
| 4 | `Dockerfile` | Tags das imagens base |

### Arquivos que NÃO precisam de alteração

Todos os 40+ arquivos de Domain, Services, Controllers e Tests permanecem **inalterados**. O código já usa features C# 12 que são totalmente compatíveis com C# 14 / .NET 10.

---

## Avaliação de Risco

| Área | Risco | Justificativa |
|------|-------|---------------|
| Target framework | **Baixo** | Mudança direta, .NET 10 é LTS |
| Swashbuckle removal | **Baixo-Médio** | Apenas 3-4 linhas em Program.cs, padrão bem documentado |
| Compatibilidade NuGet | **Baixo** | Apenas 1 pacote de produção (Swashbuckle) com substituição clara |
| Breaking APIs | **Baixo** | Nenhum API deprecated utilizada |
| Linguagem | **Baixo** | C# 14 é retrocompatível com C# 12 |
| Docker | **Baixo** | Mesma base Debian/Ubuntu |
| Infraestrutura de testes | **Médio** | coverlet/MTP requer workaround |
| Comportamento | **Baixo** | Sem auth, sem Blazor, sem Razor |

---

## Roteiro de Execução

```
1. dotnet test                    ← Verificar baseline no .NET 8
2. Atualizar .csproj files        ← TFM + pacotes
3. dotnet restore && dotnet build ← Verificar compilação
4. Program.cs                     ← Migração OpenAPI
5. dotnet test                    ← Verificar testes no .NET 10
6. Dockerfile                     ← Atualizar imagens
7. docker build + test            ← Verificar container
8. Modernizações opcionais        ← Nullable, field keyword, etc.
```
