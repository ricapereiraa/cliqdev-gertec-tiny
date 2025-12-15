# Push para Docker Hub - icapereiraa/carol-api

## Status

✅ **Imagem construída com sucesso!**
- Imagem: `icapereiraa/carol-api:latest`
- Tamanho: ~200MB

## Próximo Passo: Fazer Login no Docker Hub

Antes de fazer push, você precisa fazer login no Docker Hub:

```bash
docker login
```

Ou especificando o usuário:

```bash
docker login -u icapereiraa
```

Você será solicitado a inserir:
- Username: `icapereiraa`
- Password: [sua senha do Docker Hub]

## Após o Login

Execute o push:

```bash
docker push icapereiraa/carol-api:latest
```

## Comandos Completos

```bash
# 1. Login no Docker Hub
docker login -u icapereiraa

# 2. Fazer push da imagem
docker push icapereiraa/carol-api:latest
```

## Verificar Push

Após o push bem-sucedido, você pode verificar no Docker Hub:
- URL: https://hub.docker.com/r/icapereiraa/carol-api

## Usar a Imagem

Depois do push, outros podem usar a imagem:

```bash
docker pull icapereiraa/carol-api:latest
docker run -d -p 5000:80 --env-file .env icapereiraa/carol-api:latest
```

## Nota

Certifique-se de que o repositório `icapereiraa/carol-api` existe no Docker Hub antes de fazer o push. Se não existir, crie no site do Docker Hub primeiro.

