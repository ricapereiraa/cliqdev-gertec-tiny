# Instruções para Push - ricapereiraa/carol-api

## Repositório Docker Hub
- **Repositório:** `ricapereiraa/carol-api`
- **Tag:** `latest`
- **URL:** https://hub.docker.com/repository/docker/ricapereiraa/carol-api/general

## Status Atual
✅ Imagem construída: `icapereiraa/carol-api:latest`
✅ Tag criada: `ricapereiraa/carol-api:latest`
⏳ Push pendente: Requer login no Docker Hub

## Passos para Fazer Push

### 1. Fazer Login no Docker Hub

```bash
docker login -u ricapereiraa
```

Você será solicitado a inserir:
- **Username:** `ricapereiraa`
- **Password:** [sua senha do Docker Hub]

### 2. Fazer Push da Imagem

```bash
docker push ricapereiraa/carol-api:latest
```

## Comandos Completos (Copiar e Colar)

```bash
# 1. Login no Docker Hub
docker login -u ricapereiraa

# 2. Push da imagem
docker push ricapereiraa/carol-api:latest
```

## Verificar Push

Após o push bem-sucedido, você pode verificar:
- **Docker Hub:** https://hub.docker.com/repository/docker/ricapereiraa/carol-api/general
- **Comando:** `docker images ricapereiraa/carol-api`

## Como Usar a Imagem (Após Push)

### Baixar e Rodar

```bash
# 1. Baixar a imagem
docker pull ricapereiraa/carol-api:latest

# 2. Criar arquivo .env com as configurações

# 3. Rodar o container
docker run -d \
  --name carol-api \
  -p 5000:80 \
  --env-file .env \
  --restart unless-stopped \
  ricapereiraa/carol-api:latest
```

## Troubleshooting

### Erro: "unauthorized: authentication required"
- **Solução:** Execute `docker login -u ricapereiraa` novamente

### Erro: "repository does not exist"
- **Solução:** Certifique-se de que o repositório existe no Docker Hub
- Crie em: https://hub.docker.com/repository/create

### Erro: "denied: requested access to the resource is denied"
- **Solução:** Verifique se você tem permissão para fazer push no repositório

## Nota Importante

Certifique-se de que o repositório `ricapereiraa/carol-api` existe no Docker Hub antes de fazer o push. Se não existir, crie primeiro no site do Docker Hub.

