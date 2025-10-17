# Scripts de Instalação de Fontes

Este diretório contém scripts para instalar fontes no sistema Linux, especialmente focado em fontes NetFont e fontes para programação.

## Scripts Disponíveis

### 1. `install_netfont_fonts.sh` - Script Completo para NetFont
**Descrição**: Script robusto e completo para baixar e instalar todas as fontes NetFont.

**Características**:
- ✅ Verificação de dependências
- ✅ Tratamento de erros robusto
- ✅ Logging detalhado com cores
- ✅ Múltiplos métodos de download
- ✅ Verificação de instalação
- ✅ Limpeza automática

**Uso**:
```bash
./install_netfont_fonts.sh
```

### 2. `install_netfont_simple.sh` - Script Simples para NetFont
**Descrição**: Versão simplificada para instalação rápida das fontes NetFont.

**Características**:
- ✅ Interface simples e direta
- ✅ Emojis para melhor visualização
- ✅ Resumo da instalação
- ✅ Instruções de uso

**Uso**:
```bash
./install_netfont_simple.sh
```

### 3. `install_programming_fonts.sh` - Fontes para Programação
**Descrição**: Instala fontes populares para desenvolvimento e programação.

**Fontes incluídas**:
- Fira Code Nerd Font
- JetBrains Mono Nerd Font
- Cascadia Code (Microsoft)
- Source Code Pro (Adobe)
- Hack Nerd Font
- Ubuntu Mono Nerd Font
- Meslo LG
- Roboto Mono
- Inconsolata

**Uso**:
```bash
./install_programming_fonts.sh
```

## Pré-requisitos

### Dependências Necessárias
```bash
# Arch Linux
sudo pacman -S wget unzip curl fontconfig

# Ubuntu/Debian
sudo apt install wget unzip curl fontconfig

# Fedora
sudo dnf install wget unzip curl fontconfig
```

### Verificação de Dependências
```bash
# Verificar se as ferramentas estão instaladas
which wget unzip curl fc-cache
```

## Como Usar

### 1. Tornar os Scripts Executáveis
```bash
chmod +x *.sh
```

### 2. Executar o Script Desejado
```bash
# Para fontes NetFont (versão completa)
./install_netfont_fonts.sh

# Para fontes NetFont (versão simples)
./install_netfont_simple.sh

# Para fontes de programação
./install_programming_fonts.sh
```

### 3. Verificar Instalação
```bash
# Listar fontes instaladas
fc-list | grep -i netfont

# Listar todas as fontes do usuário
fc-list | grep "$HOME/.local/share/fonts"

# Atualizar cache manualmente (se necessário)
fc-cache -fv
```

## Localização das Fontes

### Diretório de Instalação
- **Usuário**: `~/.local/share/fonts/`
- **Sistema**: `/usr/share/fonts/` (requer sudo)

### Estrutura de Diretórios
```
~/.local/share/fonts/
├── NetFont-Regular.ttf
├── NetFont-Bold.ttf
├── FiraCode-Regular.ttf
├── JetBrainsMono-Regular.ttf
└── ...
```

## Configuração no Terminal

### 1. Terminal Padrão (GNOME Terminal)
1. Abra o terminal
2. Vá em `Edit > Preferences`
3. Selecione a aba `Text`
4. Escolha a fonte desejada

### 2. Alacritty
Edite o arquivo `~/.config/alacritty/alacritty.yml`:
```yaml
font:
  normal:
    family: "Fira Code Nerd Font"
    style: Regular
  size: 12.0
```

### 3. Kitty
Edite o arquivo `~/.config/kitty/kitty.conf`:
```conf
font_family Fira Code Nerd Font
font_size 12.0
```

### 4. iTerm2 (macOS)
1. Abra iTerm2
2. Vá em `iTerm2 > Preferences`
3. Selecione `Profiles > Text`
4. Escolha a fonte

## Configuração em Editores

### VS Code
1. Abra VS Code
2. Vá em `File > Preferences > Settings`
3. Procure por "font family"
4. Adicione: `"Fira Code", "JetBrains Mono", monospace`

### Vim/Neovim
Adicione ao seu `.vimrc` ou `init.vim`:
```vim
set guifont=Fira\ Code\ Nerd\ Font:h12
```

## Solução de Problemas

### Fontes não aparecem
```bash
# Verificar se as fontes foram instaladas
ls -la ~/.local/share/fonts/

# Atualizar cache
fc-cache -fv

# Reiniciar aplicações
```

### Erro de permissão
```bash
# Verificar permissões do diretório
ls -la ~/.local/share/fonts/

# Corrigir permissões se necessário
chmod 755 ~/.local/share/fonts/
```

### Dependências faltando
```bash
# Instalar dependências no Arch Linux
sudo pacman -S wget unzip curl fontconfig

# Verificar instalação
which wget unzip curl fc-cache
```

### URLs inválidas
Se as URLs das fontes NetFont estiverem inválidas:
1. Verifique o site oficial do NetFont
2. Atualize as URLs no script
3. Use fontes alternativas do script de programação

## Fontes Recomendadas por Uso

### 🖥️ Terminal
- **Fira Code Nerd Font**: Excelente para terminal com ligatures
- **JetBrains Mono Nerd Font**: Ótima legibilidade
- **Cascadia Code**: Fonte moderna da Microsoft

### 💻 Desenvolvimento
- **Source Code Pro**: Fonte clássica da Adobe
- **Hack**: Fonte open source otimizada
- **Ubuntu Mono**: Fonte do Ubuntu

### 🎨 Design
- **Roboto Mono**: Fonte do Google
- **Inconsolata**: Fonte clássica para código

## Informações Adicionais

### Sobre Nerd Fonts
As Nerd Fonts são fontes modificadas que incluem ícones e símbolos especiais, muito úteis para:
- Terminais com temas (Oh My Zsh, Powerlevel10k)
- Editores com ícones de arquivo
- Status bars e prompts personalizados

### Ligatures
Algumas fontes (como Fira Code) suportam ligatures, que substituem sequências de caracteres por símbolos visuais:
- `!=` → `≠`
- `>=` → `≥`
- `<=` → `≤`
- `=>` → `⇒`

### Suporte a Ícones
Fontes Nerd Font incluem suporte a:
- Ícones de arquivo
- Símbolos de Git
- Ícones de linguagens de programação
- Símbolos de status

## Contribuição

Para melhorar os scripts:
1. Teste em diferentes distribuições Linux
2. Adicione novas fontes populares
3. Melhore o tratamento de erros
4. Adicione suporte a mais formatos de fonte

## Licença

Estes scripts são fornecidos "como estão" para facilitar a instalação de fontes. As fontes instaladas mantêm suas respectivas licenças originais.
