# Análise: Atualização de Preços Tiny → Gertec

## Como Funciona Atualmente

### Fluxo de Atualização de Preços

```
1. Preço atualizado no Tiny ERP
   ↓
2. Monitoramento detecta mudança (a cada 5 minutos)
   ↓
3. Aplicação atualiza cache interno
   ↓
4. Envia mensagem ao Gertec: "Preços atualizados"
   ↓
5. Quando cliente escanear código → Busca preço ATUALIZADO do Tiny
```

## Análise do Código

### 1. Monitoramento de Preços

**Arquivo:** `Services/IntegrationService.cs` - Método `MonitorPriceChangesAsync`

**O que faz:**
- Verifica mudanças de preços a cada X minutos (padrão: 5 minutos)
- Compara preços com cache interno
- Quando detecta mudança:
  - Atualiza cache interno
  - Envia mensagem ao Gertec informando atualização

**Código atual:**
```csharp
if (produtoCache.Preco != produto.Preco || 
    produtoCache.PrecoPromocional != produto.PrecoPromocional)
{
    _logger.LogInformation($"Preço alterado para produto {produto.Nome}");
    
    // Atualiza cache
    _productCache[chaveProduto] = produto;
    
    // Envia mensagem ao Gertec
    await _gertecService.SendMessageAsync(
        "Precos atualizados", 
        $"Produto: {produto.Nome}", 
        3);
}
```

### 2. Consulta em Tempo Real

**Arquivo:** `Services/IntegrationService.cs` - Método `OnBarcodeReceived`

**O que faz:**
- Quando código de barras é escaneado no Gertec
- Busca produto **SEMPRE** no Tiny ERP (em tempo real)
- Retorna preço **ATUALIZADO** do Tiny

**Código:**
```csharp
// Busca produto no Olist (sempre busca preço atualizado)
var produto = await _olistService.GetProductByBarcodeAsync(barcode);

// Envia preço atualizado ao Gertec
await _gertecService.SendProductInfoAsync(nomeFormatado, precoFormatado);
```

## Resposta à Pergunta

### "Ao atualizar no Tiny, atualiza no Gertec?"

**Resposta:** **SIM, mas de forma indireta**

**Como funciona:**
1. **Atualização no Tiny:** Preço é alterado no Tiny ERP
2. **Detecção:** Aplicação detecta mudança no monitoramento (a cada 5 min)
3. **Cache:** Atualiza cache interno com novo preço
4. **Notificação:** Envia mensagem ao Gertec informando atualização
5. **Exibição:** Quando alguém escanear o código, o Gertec mostra o preço **ATUALIZADO** (buscado em tempo real do Tiny)

### Importante

O Gertec **NÃO armazena produtos/preços localmente**. Ele sempre consulta o servidor quando um código é escaneado.

**Isso significa:**
- Preços sempre atualizados quando escaneados
- Não precisa "sincronizar" preços com o Gertec
- A busca é feita em tempo real do Tiny

## Limitações Atuais

### 1. Monitoramento Periódico

**Problema:** Detecta mudanças apenas a cada 5 minutos (configurável)

**Solução atual:** Quando escaneia código, sempre busca preço atualizado

### 2. Mensagem Genérica

**Problema:** Quando detecta mudança, envia mensagem genérica "Preços atualizados"

**Não faz:** Não envia o preço atualizado diretamente (porque Gertec não armazena)

## Melhorias Possíveis

### Opção 1: Webhook do Tiny (Ideal)

Se o Tiny ERP suportar webhooks:
- Tiny notifica a aplicação imediatamente quando preço muda
- Aplicação atualiza cache instantaneamente
- Próxima consulta já retorna preço atualizado

### Opção 2: Intervalo Menor de Monitoramento

Reduzir intervalo de verificação:
```env
PRICE_MONITORING__CHECK_INTERVAL_MINUTES=1  # Verifica a cada 1 minuto
```

**Desvantagem:** Mais requisições à API do Tiny

### Opção 3: Atualização Imediata no Cache

Quando detecta mudança, invalidar cache do produto específico:
- Próxima consulta busca preço atualizado
- Já funciona assim, mas pode melhorar

## Conclusão

### O que a aplicação FAZ:

1. **Monitora mudanças de preços** no Tiny (a cada 5 minutos)
2. **Atualiza cache interno** quando detecta mudança
3. **Notifica Gertec** sobre atualização
4. **Sempre busca preço atualizado** quando código é escaneado

### O que a aplicação NÃO FAZ:

1. **Não armazena produtos no Gertec** (Gertec não suporta)
2. **Não atualiza preços diretamente no Gertec** (não é possível)
3. **Não detecta mudanças instantaneamente** (depende do intervalo)

### Resultado Prático:

**Quando atualizar preço no Tiny:**
- Em até 5 minutos: aplicação detecta e atualiza cache
- Imediatamente: quando alguém escanear, mostra preço atualizado (busca em tempo real)

**Resumo:** A aplicação **GARANTE** que o Gertec sempre mostra preços atualizados quando códigos são escaneados, pois busca em tempo real do Tiny. O monitoramento serve apenas para manter o cache atualizado e notificar sobre mudanças.

