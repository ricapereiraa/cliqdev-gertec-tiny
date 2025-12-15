# Validação da API Tiny ERP - Checklist

## AVISO: IMPORTANTE: Validação Necessária

Antes de usar em produção, é **ESSENCIAL** validar a estrutura real da API do Tiny ERP com um token real.

## Checklist de Validação

### 1. Estrutura de Resposta da API

#### Teste Manual

```bash
# Teste com curl
curl -X POST https://api.tiny.com.br/api2/produto.pesquisa.php \
 -d "token=SEU_TOKEN" \
 -d "formato=json" \
 -d "pesquisa=7891234567890"
```

#### O que Validar:

- [ ] A resposta tem o campo `status`?
- [ ] O campo `status` é `"OK"` quando produto encontrado?
- [ ] A estrutura é `retorno.produto` ou `retorno.produtos`?
- [ ] Quais campos existem no objeto produto?
- [ ] O campo de código de barras é `codigo`, `codigo_barras` ou outro?
- [ ] O campo de preço é `preco`, `preco_venda`, `valor` ou outro?
- [ ] Existe campo `precoPromocional` ou similar?

### 2. Campos do Produto

#### Campos Esperados na Implementação Atual:

```csharp
public class Produto
{
  public string Id { get; set; }
  public string Codigo { get; set; }   // Código de barras
  public string Nome { get; set; }
  public string Preco { get; set; }
  public string PrecoPromocional { get; set; }
  public string Descricao { get; set; }
  public string Estoque { get; set; }
}
```

#### Campos Reais da API:

**Teste e documente:**
- [ ] Campo de ID: `_________________`
- [ ] Campo de código de barras: `_________________`
- [ ] Campo de nome: `_________________`
- [ ] Campo de preço: `_________________`
- [ ] Campo de preço promocional: `_________________`
- [ ] Outros campos importantes: `_________________`

### 3. Formato de Preço

#### O que Validar:

- [ ] O preço vem como string ou número?
- [ ] Formato: `"29.90"`, `"29,90"`, `29.90` ou outro?
- [ ] Precisa conversão de vírgula para ponto?
- [ ] Existe separador de milhar?

### 4. Tratamento de Erros

#### Cenários a Testar:

- [ ] Produto não encontrado - qual a resposta?
- [ ] Token inválido - qual a resposta?
- [ ] Erro de conexão - como tratar?
- [ ] Timeout - qual o comportamento?

### 5. Exemplo de Resposta Real

**Cole aqui um exemplo real de resposta da API:**

```json
{
 "status": "OK",
 "retorno": {
  "produto": {
   // COLE AQUI A RESPOSTA REAL
  }
 }
}
```

## Ajustes Necessários Após Validação

### Se a Estrutura for Diferente:

1. **Atualizar Modelo:**
  ```csharp
  // Ajustar em Models/OlistApiModels.cs
  ```

2. **Atualizar Parser:**
  ```csharp
  // Ajustar em Services/OlistApiService.cs
  ```

3. **Testar:**
  ```bash
  dotnet run
  # Testar busca de produto
  ```

## Script de Teste

Crie um arquivo `test-api.ps1` (Windows) ou `test-api.sh` (Linux):

```powershell
# Windows PowerShell
$token = "SEU_TOKEN"
$barcode = "7891234567890"

$body = @{
  token = $token
  formato = "json"
  pesquisa = $barcode
}

$response = Invoke-WebRequest -Uri "https://api.tiny.com.br/api2/produto.pesquisa.php" -Method POST -Body $body
$response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

Execute e documente a resposta real!

