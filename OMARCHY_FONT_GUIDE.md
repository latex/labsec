# Guia para Aumentar Fontes no Omarchy Linux

Este guia mostra como aumentar as fontes do sistema no Omarchy Linux usando diferentes métodos.

## 🚀 Métodos Rápidos

### 1. Script Automático (Recomendado)
```bash
# Aumentar para 14pt (recomendado)
./quick_font_increase.sh 14

# Aumentar para 16pt (muito grande)
./quick_font_increase.sh 16

# Aumentar para 18pt (extra grande)
./quick_font_increase.sh 18
```

### 2. Menu Interativo
```bash
./increase_system_fonts_omarchy.sh
```

### 3. Interface Gráfica
```bash
./configure_fonts_gui.sh
```

## 🖥️ Métodos Manuais

### Via Configurações do Sistema

#### GNOME (Ambiente padrão do Omarchy)
1. **Abrir Configurações**:
   - Pressione `Super` (tecla Windows)
   - Digite "Configurações" e pressione Enter
   - Ou clique no ícone de configurações no dock

2. **Configurar Fontes**:
   - Vá para "Aparência" ou "Fontes"
   - Ajuste o "Tamanho da Fonte"
   - Ou use "Fator de Escala" para aumentar tudo

3. **Configurar Terminal**:
   - Abra o terminal
   - Vá em "Editar" > "Preferências"
   - Na aba "Texto", aumente o tamanho da fonte

#### XFCE
1. **Abrir Configurações**:
   - Menu > Configurações > Configurações do Sistema
   - Ou pressione `Alt + F2` e digite `xfce4-settings-manager`

2. **Configurar Fontes**:
   - Clique em "Aparência"
   - Vá para a aba "Fontes"
   - Ajuste o tamanho das fontes

#### KDE
1. **Abrir Configurações**:
   - Menu > Configurações do Sistema
   - Ou pressione `Alt + F2` e digite `systemsettings5`

2. **Configurar Fontes**:
   - Vá para "Aparência" > "Fontes"
   - Ajuste o tamanho das fontes
   - Ou use "Escala da Interface"

### Via Linha de Comando

#### GNOME (gsettings)
```bash
# Aumentar fonte do sistema para 14pt
gsettings set org.gnome.desktop.interface font-name "Cantarell 14"

# Aumentar fonte de documentos
gsettings set org.gnome.desktop.interface document-font-name "Cantarell 14"

# Aumentar fonte monospace (terminal)
gsettings set org.gnome.desktop.interface monospace-font-name "JetBrains Mono 14"

# Aumentar fonte de títulos de janela
gsettings set org.gnome.desktop.wm.preferences titlebar-font "Cantarell 14"
```

#### XFCE (xfconf-query)
```bash
# Configurar fonte principal
xfconf-query -c xsettings -p /Gtk/FontName -s "Cantarell 14"

# Configurar fonte do desktop
xfconf-query -c xfce4-desktop -p /desktop-icons/style -s 0
```

#### KDE (kwriteconfig5)
```bash
# Configurar fonte principal
kwriteconfig5 --file kdeglobals --group General --key font "Cantarell,14"

# Configurar fonte de menu
kwriteconfig5 --file kdeglobals --group General --key menuFont "Cantarell,14"
```

## 📏 Tamanhos Recomendados

| Tamanho | Uso | Comando |
|---------|-----|---------|
| 10pt | Pequeno (padrão) | `./quick_font_increase.sh 10` |
| 12pt | Médio | `./quick_font_increase.sh 12` |
| 14pt | Grande (recomendado) | `./quick_font_increase.sh 14` |
| 16pt | Muito grande | `./quick_font_increase.sh 16` |
| 18pt | Extra grande | `./quick_font_increase.sh 18` |

## 🎯 Configurações Específicas

### Terminal
```bash
# GNOME Terminal
gsettings set org.gnome.Terminal.Legacy.Profile:/org/gnome/terminal/legacy/profiles:/:$(gsettings get org.gnome.Terminal.ProfilesList default | tr -d "'")/ font "JetBrains Mono 14"

# Alacritty
echo "font:" >> ~/.config/alacritty/alacritty.yml
echo "  size: 14" >> ~/.config/alacritty/alacritty.yml
echo "  family: JetBrains Mono" >> ~/.config/alacritty/alacritty.yml

# Kitty
echo "font_size 14" >> ~/.config/kitty/kitty.conf
echo "font_family JetBrains Mono" >> ~/.config/kitty/kitty.conf
```

### Navegador

#### Firefox
1. Abra o Firefox
2. Digite `about:config` na barra de endereço
3. Procure por `font.size.fixed.x-western`
4. Altere o valor (padrão é 13, tente 16)

#### Chrome/Chromium
1. Abra o Chrome
2. Vá em Configurações > Aparência
3. Ajuste o "Tamanho da fonte"

### Editores de Código

#### VS Code
1. Abra o VS Code
2. Vá em Arquivo > Preferências > Configurações
3. Procure por "font size"
4. Altere o valor (padrão é 14, tente 16)

#### Vim/Neovim
```vim
" Adicionar ao ~/.vimrc ou ~/.config/nvim/init.vim
set guifont=JetBrains\ Mono:h14
```

## 🔧 Solução de Problemas

### Fontes não mudaram
```bash
# Atualizar cache de fontes
fc-cache -fv

# Reiniciar aplicações
killall gnome-shell  # Para GNOME
killall xfce4-panel  # Para XFCE
killall plasmashell  # Para KDE
```

### Aplicações não respeitam as configurações
```bash
# Verificar configurações GTK
cat ~/.config/gtk-3.0/settings.ini

# Recriar configurações
rm ~/.config/gtk-3.0/settings.ini
./quick_font_increase.sh 14
```

### Terminal não mudou
```bash
# Verificar configurações do terminal
gsettings get org.gnome.Terminal.Legacy.Profile:/org/gnome/terminal/legacy/profiles:/:$(gsettings get org.gnome.Terminal.ProfilesList default | tr -d "'")/ font

# Aplicar configuração manualmente
gsettings set org.gnome.Terminal.Legacy.Profile:/org/gnome/terminal/legacy/profiles:/:$(gsettings get org.gnome.Terminal.ProfilesList default | tr -d "'")/ font "JetBrains Mono 14"
```

## 📋 Comandos Úteis

### Verificar fontes instaladas
```bash
# Listar todas as fontes
fc-list

# Listar fontes por nome
fc-list | grep -i cantarell

# Listar fontes monospace
fc-list | grep -i mono
```

### Verificar configurações atuais
```bash
# GNOME
gsettings get org.gnome.desktop.interface font-name

# XFCE
xfconf-query -c xsettings -p /Gtk/FontName

# KDE
kreadconfig5 --file kdeglobals --group General --key font
```

### Restaurar configurações padrão
```bash
# GNOME
gsettings reset org.gnome.desktop.interface font-name

# XFCE
xfconf-query -c xsettings -p /Gtk/FontName -r

# KDE
kwriteconfig5 --file kdeglobals --group General --key font ""
```

## 🎨 Fontes Recomendadas

### Para Sistema
- **Cantarell** (padrão do GNOME)
- **Ubuntu** (padrão do Ubuntu)
- **Liberation Sans** (alternativa livre)

### Para Terminal/Programação
- **JetBrains Mono** (com ligatures)
- **Fira Code** (com ligatures)
- **Source Code Pro** (Adobe)
- **Cascadia Code** (Microsoft)

### Para Acessibilidade
- **Open Sans** (muito legível)
- **Roboto** (Google)
- **Lato** (humanista)

## 💡 Dicas

1. **Reiniciar aplicações**: Após mudar as fontes, reinicie as aplicações para ver o efeito
2. **Fazer logout/login**: Para aplicar mudanças em todo o sistema
3. **Usar fator de escala**: No GNOME, use "Fator de Escala" para aumentar tudo proporcionalmente
4. **Testar diferentes tamanhos**: Experimente diferentes tamanhos até encontrar o ideal
5. **Configurar por aplicação**: Algumas aplicações têm configurações próprias de fonte

## 🆘 Suporte

Se você tiver problemas:
1. Execute `./quick_font_increase.sh 14` para configuração básica
2. Verifique se as dependências estão instaladas: `gsettings`, `xfconf-query`, `kwriteconfig5`
3. Consulte os logs do sistema: `journalctl -f`
4. Teste em um usuário novo para isolar problemas de configuração
