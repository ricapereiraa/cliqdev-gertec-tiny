# Integração com PostgreSQL - Implementação Completa

## ✅ Implementação Concluída

A aplicação foi modificada para usar **PostgreSQL** ao invés de arquivo TXT para armazenar os produtos.

## 📋 Estrutura da Tabela

A tabela `PRODUCT` é criada automaticamente com a seguinte estrutura:

```sql
CREATE TABLE PRODUCT (
    BAR_CODE VARCHAR(50) PRIMARY KEY,  -- Código de barras (GTIN ou código)
    DESCRIPTION VARCHAR(500),           -- Nome/descrição do produto
    PRICE_1 DECIMAL(10,2),              -- Preço normal
    PRICE_2 DECIMAL(10,2),              -- Preço promocional
    UPDATED_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_product_bar_code ON PRODUCT(BAR_CODE);
```

## 🔧 Configuração

### appsettings.json

```json
{
  "Database": {
    "Host": "localhost",
    "Port": "5432",
    "Database": "tinytog2",
    "Username": "tinytog2",
    "Password": "tinytog2",
    "TableName": "PRODUCT"
  }
}
```

### Variáveis de Ambiente (.env)

```env
DB__HOST=localhost
DB__PORT=5432
DB__DATABASE=tinytog2
DB__USERNAME=tinytog2
DB__PASSWORD=tinytog2
DB__TABLE_NAME=PRODUCT
```

**Nota:** Variáveis de ambiente têm prioridade sobre `appsettings.json`.

## 🚀 Funcionalidades Implementadas

### 1. DatabaseService

Serviço completo para gerenciar operações no PostgreSQL:

- ✅ **TestConnectionAsync()** - Testa conexão com o banco
- ✅ **EnsureTableExistsAsync()** - Cria tabela automaticamente se não existir
- ✅ **ProductExistsAsync()** - Verifica se produto existe pelo código de barras
- ✅ **InsertProductAsync()** - Insere novo produto
- ✅ **UpdateProductAsync()** - Atualiza produto existente
- ✅ **UpsertProductAsync()** - Insere ou atualiza (mais eficiente)
- ✅ **ProcessProductsAsync()** - Processa múltiplos produtos (com controle de novos/atualizados)
- ✅ **UpsertProductsAsync()** - Processa múltiplos produtos usando UPSERT

### 2. Lógica de Atualização

A aplicação agora:

1. **Busca produtos da API Tiny** - Mantém a mesma lógica de busca
2. **Verifica se produto existe** - Usa `BAR_CODE` como chave primária
3. **Atualiza produtos existentes** - Quando preço ou descrição mudam
4. **Adiciona produtos novos** - Quando não existem no banco
5. **Monitora mudanças** - A cada 1 minuto verifica atualizações

### 3. Mapeamento de Campos

- **BAR_CODE** ← `produto.Gtin` ou `produto.Codigo` (fallback)
- **DESCRIPTION** ← `produto.Nome`
- **PRICE_1** ← `produto.Preco` (preço normal)
- **PRICE_2** ← `produto.PrecoPromocional` (preço promocional)

## 📦 Dependências Adicionadas

- **Npgsql 8.0.3** - Driver PostgreSQL para .NET

## 🔄 Fluxo de Funcionamento

1. **Inicialização:**
   - Testa conexão com banco
   - Cria tabela `PRODUCT` se não existir
   - Pré-carrega cache de produtos da API

2. **Sincronização Inicial:**
   - Busca todos os produtos da API Tiny
   - Processa e insere/atualiza no banco usando UPSERT
   - Loga quantidade de produtos processados

3. **Monitoramento Contínuo:**
   - A cada 1 minuto busca atualizações
   - Compara preços com cache local
   - Atualiza apenas produtos que mudaram
   - Adiciona novos produtos detectados

## 🎯 Vantagens da Implementação

- ✅ **Performance** - Banco de dados é mais rápido que arquivo
- ✅ **Concorrência** - Múltiplas conexões simultâneas
- ✅ **Integridade** - Chave primária evita duplicações
- ✅ **Escalabilidade** - Suporta grandes volumes de dados
- ✅ **Queries** - Possibilidade de consultas complexas
- ✅ **Backup** - Fácil backup e restauração

## 🧪 Teste de Conexão

A aplicação testa automaticamente a conexão na inicialização. Se falhar, exibe erro e não inicia o serviço.

## 📝 Logs

A aplicação registra:
- Conexão com banco testada
- Tabela criada/verificada
- Produtos inseridos
- Produtos atualizados
- Erros de processamento

## 🚨 Pronto para Produção

A implementação está completa e pronta para produção:

- ✅ Tratamento de erros
- ✅ Logs detalhados
- ✅ Criação automática de tabela
- ✅ Validação de conexão
- ✅ Processamento eficiente (UPSERT)
- ✅ Configuração flexível (env vars)

## 📌 Notas Importantes

1. **Driver PostgreSQL:** O JAR mencionado (`postgresql-42.3.1.jar`) é para Java. Esta implementação usa **Npgsql** (driver nativo .NET).

2. **Conexão Local:** A configuração padrão aponta para `localhost`. Ajuste conforme necessário.

3. **Segurança:** Em produção, use variáveis de ambiente para senhas, nunca hardcode.

4. **Compatibilidade:** O `GertecDataFileService` foi mantido para compatibilidade, mas não é mais usado pelo `IntegrationService`.

