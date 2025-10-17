#!/bin/bash

# Script simples para instalar fontes NetFont
# Uso: ./install_netfont_simple.sh

set -e

echo "=== Instalador Simples de Fontes NetFont ==="

# Configurações
FONT_DIR="$HOME/.local/share/fonts"
TEMP_DIR="/tmp/netfont_install"

# Criar diretórios
mkdir -p "$FONT_DIR"
mkdir -p "$TEMP_DIR"

echo "📁 Diretório de fontes: $FONT_DIR"

# Lista de URLs das fontes NetFont (ajuste conforme necessário)
FONT_URLS=(
    "https://www.netfont.com.br/fonts/NetFont-Regular.ttf"
    "https://www.netfont.com.br/fonts/NetFont-Bold.ttf"
    "https://www.netfont.com.br/fonts/NetFont-Italic.ttf"
    "https://www.netfont.com.br/fonts/NetFont-BoldItalic.ttf"
    "https://www.netfont.com.br/fonts/NetFont-Light.ttf"
    "https://www.netfont.com.br/fonts/NetFont-Medium.ttf"
    "https://www.netfont.com.br/fonts/NetFont-SemiBold.ttf"
    "https://www.netfont.com.br/fonts/NetFont-ExtraBold.ttf"
    "https://www.netfont.com.br/fonts/NetFont-Black.ttf"
)

# Função para baixar uma fonte
download_font() {
    local url="$1"
    local filename=$(basename "$url")
    local filepath="$TEMP_DIR/$filename"
    
    echo "⬇️  Baixando: $filename"
    
    if wget -q --show-progress -O "$filepath" "$url"; then
        echo "✅ $filename baixado com sucesso"
        
        # Copiar para diretório de fontes
        cp "$filepath" "$FONT_DIR/"
        echo "📦 $filename instalado"
        return 0
    else
        echo "❌ Erro ao baixar $filename"
        return 1
    fi
}

# Baixar todas as fontes
echo "🚀 Iniciando download das fontes..."
success_count=0
total_count=${#FONT_URLS[@]}

for url in "${FONT_URLS[@]}"; do
    if download_font "$url"; then
        ((success_count++))
    fi
    echo
done

# Atualizar cache de fontes
echo "🔄 Atualizando cache de fontes..."
if fc-cache -fv "$FONT_DIR"; then
    echo "✅ Cache atualizado com sucesso"
else
    echo "⚠️  Aviso: Falha ao atualizar cache"
fi

# Limpeza
echo "🧹 Limpando arquivos temporários..."
rm -rf "$TEMP_DIR"

# Resultado final
echo
echo "=== Resumo da Instalação ==="
echo "📊 Fontes baixadas: $success_count de $total_count"
echo "📁 Localização: $FONT_DIR"

if [[ $success_count -gt 0 ]]; then
    echo "✅ Instalação concluída com sucesso!"
    echo
    echo "💡 Para usar as fontes:"
    echo "   1. Reinicie suas aplicações"
    echo "   2. Configure a fonte no seu terminal/aplicação"
    echo "   3. Verifique com: fc-list | grep -i netfont"
else
    echo "❌ Nenhuma fonte foi instalada"
    echo "   Verifique as URLs e sua conexão com a internet"
fi
