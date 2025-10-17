#!/bin/bash

# Script para instalar fontes populares para programação
# Inclui fontes Nerd Font e outras fontes recomendadas para desenvolvimento

set -e

echo "=== Instalador de Fontes para Programação ==="

# Configurações
FONT_DIR="$HOME/.local/share/fonts"
TEMP_DIR="/tmp/programming_fonts"

# Criar diretórios
mkdir -p "$FONT_DIR"
mkdir -p "$TEMP_DIR"

echo "📁 Diretório de fontes: $FONT_DIR"

# Função para baixar e instalar fonte
install_font() {
    local name="$1"
    local url="$2"
    local filename=$(basename "$url")
    local filepath="$TEMP_DIR/$filename"
    
    echo "⬇️  Baixando: $name"
    
    if wget -q --show-progress -O "$filepath" "$url"; then
        echo "✅ $name baixado com sucesso"
        
        # Extrair se for zip
        if [[ "$filename" == *.zip ]]; then
            echo "📦 Extraindo: $name"
            unzip -q "$filepath" -d "$TEMP_DIR/$name"
            
            # Copiar fontes extraídas
            find "$TEMP_DIR/$name" -name "*.ttf" -o -name "*.otf" | while read -r font; do
                cp "$font" "$FONT_DIR/"
                echo "📦 Instalada: $(basename "$font")"
            done
        else
            # Copiar arquivo direto
            cp "$filepath" "$FONT_DIR/"
            echo "📦 $name instalado"
        fi
        return 0
    else
        echo "❌ Erro ao baixar $name"
        return 1
    fi
}

# Lista de fontes populares para programação
echo "🚀 Instalando fontes populares para programação..."

# Fira Code Nerd Font (muito popular)
install_font "Fira Code Nerd Font" "https://github.com/ryanoasis/nerd-fonts/releases/latest/download/FiraCode.zip"

# JetBrains Mono Nerd Font
install_font "JetBrains Mono Nerd Font" "https://github.com/ryanoasis/nerd-fonts/releases/latest/download/JetBrainsMono.zip"

# Cascadia Code (Microsoft)
install_font "Cascadia Code" "https://github.com/microsoft/cascadia-code/releases/latest/download/CascadiaCode.zip"

# Source Code Pro (Adobe)
install_font "Source Code Pro" "https://github.com/adobe-fonts/source-code-pro/releases/latest/download/source-code-pro.zip"

# Hack Nerd Font
install_font "Hack Nerd Font" "https://github.com/ryanoasis/nerd-fonts/releases/latest/download/Hack.zip"

# Ubuntu Mono Nerd Font
install_font "Ubuntu Mono Nerd Font" "https://github.com/ryanoasis/nerd-fonts/releases/latest/download/UbuntuMono.zip"

# Meslo LG (Powerline)
install_font "Meslo LG" "https://github.com/andreberg/Meslo-Font/raw/master/dist/v1.2.1/Meslo%20LG%20v1.2.1.zip"

# Roboto Mono
install_font "Roboto Mono" "https://fonts.google.com/download?family=Roboto%20Mono"

# Inconsolata
install_font "Inconsolata" "https://fonts.google.com/download?family=Inconsolata"

# Consolas (Windows font, alternativa)
echo "📝 Nota: Consolas é uma fonte do Windows. Se você quiser usá-la,"
echo "   instale o pacote 'ttf-ms-fonts' do AUR:"
echo "   yay -S ttf-ms-fonts"

# Atualizar cache de fontes
echo
echo "🔄 Atualizando cache de fontes..."
if fc-cache -fv "$FONT_DIR"; then
    echo "✅ Cache atualizado com sucesso"
else
    echo "⚠️  Aviso: Falha ao atualizar cache"
fi

# Limpeza
echo "🧹 Limpando arquivos temporários..."
rm -rf "$TEMP_DIR"

# Mostrar fontes instaladas
echo
echo "=== Fontes Instaladas ==="
echo "📋 Listando fontes disponíveis:"
fc-list | grep -E "(Fira|JetBrains|Cascadia|Source|Hack|Ubuntu|Meslo|Roboto|Inconsolata)" | head -20

echo
echo "✅ Instalação concluída!"
echo
echo "💡 Fontes recomendadas para diferentes usos:"
echo "   🖥️  Terminal: Fira Code Nerd Font, JetBrains Mono Nerd Font"
echo "   💻 IDE/Editor: Cascadia Code, Source Code Pro"
echo "   🎨 Design: Roboto Mono, Inconsolata"
echo
echo "🔧 Para configurar no seu terminal:"
echo "   1. Abra as configurações do terminal"
echo "   2. Vá para 'Font' ou 'Fonte'"
echo "   3. Selecione uma das fontes instaladas"
echo
echo "📝 Para ver todas as fontes: fc-list | grep -i [nome_da_fonte]"
