# Docker Compose - Qual Arquivo Usar

## Arquivos Disponíveis

### 1. `docker-compose.yml` (Principal - Linux/Mac/Windows)
- **Driver de rede:** `bridge` (funciona em Linux, Mac e Windows)
- **Portas:** 5000 (HTTP) e 5001 (HTTPS)
- **Uso:** Recomendado para a maioria dos casos

### 2. `docker-compose.windows.yml` (Alternativa Windows)
- **Driver de rede:** `nat` (específico para Windows)
- **Portas:** Apenas 5000 (HTTP)
- **Uso:** Apenas se tiver problemas com o arquivo principal no Windows

## Qual Usar

### Recomendação: `docker-compose.yml`

O arquivo `docker-compose.yml` funciona em **todos os sistemas** (Linux, Mac e Windows) e é o arquivo padrão do Docker Compose.

**Comando:**
```bash
docker-compose up -d
```

### Se Tiver Problemas no Windows

Se o `docker-compose.yml` não funcionar no Windows, use:

```bash
docker-compose -f docker-compose.windows.yml up -d
```

## Diferenças

| Característica | docker-compose.yml | docker-compose.windows.yml |
|----------------|-------------------|---------------------------|
| Driver de rede | bridge | nat |
| Porta HTTP | 5000:80 | 5000:80 |
| Porta HTTPS | 5001:443 | Não mapeada |
| Compatibilidade | Linux/Mac/Windows | Windows |

## Recomendação Final

**Use `docker-compose.yml`** - É o arquivo padrão e funciona em todos os sistemas.

O Docker Desktop no Windows suporta o driver `bridge` sem problemas.

