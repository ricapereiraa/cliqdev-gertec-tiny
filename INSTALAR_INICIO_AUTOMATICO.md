# Instalar Cliqdev Gertec e Tiny para Iniciar Automaticamente

## Opção 1: Serviço Systemd (Recomendado - Inicia com o sistema)

### Passo 1: Compilar a aplicação em Release

```bash
cd "/home/ricardopereira/Área de trabalho/projetos/api-carol"
dotnet publish -c Release -o bin/Release/net8.0/publish
```

### Passo 2: Editar o arquivo de serviço

Edite o arquivo `cliqdev-gertec-tiny.service` e ajuste:
- `User=%USER%` → Substitua por seu usuário (ex: `User=ricardopereira`)
- Caminhos absolutos se necessário

### Passo 3: Copiar o arquivo de serviço

```bash
sudo cp cliqdev-gertec-tiny.service /etc/systemd/system/
```

### Passo 4: Recarregar systemd

```bash
sudo systemctl daemon-reload
```

### Passo 5: Habilitar o serviço (inicia automaticamente)

```bash
sudo systemctl enable cliqdev-gertec-tiny.service
```

### Passo 6: Iniciar o serviço

```bash
sudo systemctl start cliqdev-gertec-tiny.service
```

### Verificar status

```bash
sudo systemctl status cliqdev-gertec-tiny.service
```

### Ver logs

```bash
sudo journalctl -u cliqdev-gertec-tiny.service -f
```

### Comandos úteis

```bash
# Parar o serviço
sudo systemctl stop cliqdev-gertec-tiny.service

# Reiniciar o serviço
sudo systemctl restart cliqdev-gertec-tiny.service

# Desabilitar início automático
sudo systemctl disable cliqdev-gertec-tiny.service
```

## Opção 2: Autostart do Desktop (Inicia quando usuário faz login)

### Passo 1: Tornar o script executável

```bash
chmod +x run-api.sh
```

### Passo 2: Editar o arquivo .desktop

Edite `cliqdev-gertec-tiny.desktop` e ajuste os caminhos se necessário.

### Passo 3: Copiar para autostart

```bash
mkdir -p ~/.config/autostart
cp cliqdev-gertec-tiny.desktop ~/.config/autostart/
```

### Passo 4: Tornar o .desktop executável

```bash
chmod +x ~/.config/autostart/cliqdev-gertec-tiny.desktop
```

A aplicação iniciará automaticamente quando você fizer login.

## Opção 3: Criar Ícone na Área de Trabalho

### Passo 1: Tornar o script executável

```bash
chmod +x run-api.sh
```

### Passo 2: Tornar o .desktop executável

```bash
chmod +x cliqdev-gertec-tiny.desktop
```

### Passo 3: Copiar para a área de trabalho

```bash
cp cliqdev-gertec-tiny.desktop ~/Área\ de\ trabalho/
```

OU arraste o arquivo `cliqdev-gertec-tiny.desktop` para a área de trabalho.

### Passo 4: Tornar executável na área de trabalho

```bash
chmod +x ~/Área\ de\ trabalho/cliqdev-gertec-tiny.desktop
```

## Configurar Ícone Personalizado

### Passo 1: Baixar/Converter a logo CLIQDEV

1. Salve a logo CLIQDEV como `icon.png` na pasta do projeto
2. Ou converta de outro formato:
   ```bash
   convert logo.png -resize 256x256 icon.png
   ```

### Passo 2: Atualizar o caminho no .desktop

Edite `cliqdev-gertec-tiny.desktop` e ajuste o caminho do ícone:
```
Icon=/home/ricardopereira/Área de trabalho/projetos/api-carol/icon.png
```

## Notas Importantes

1. **Serviço Systemd**: Inicia mesmo sem login do usuário (melhor para servidor)
2. **Autostart Desktop**: Inicia apenas quando o usuário faz login
3. **Ícone na Área de Trabalho**: Permite iniciar manualmente com duplo clique
4. **Arquivo .env**: Certifique-se de que está configurado antes de iniciar

## Verificar se está funcionando

```bash
# Verificar se o serviço está rodando
sudo systemctl status cliqdev-gertec-tiny.service

# Verificar se a API está respondendo
curl http://localhost:5000/api/config/status

# Acessar o painel
xdg-open http://localhost:5000/painel.html
```

