#!/bin/bash

# Script rápido para aumentar fontes no Omarchy Linux
# Uso: ./quick_font_increase.sh [tamanho]

set -e

# Cores
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🔤 Aumentando Fontes do Sistema - Omarchy Linux${NC}"
echo

# Tamanho padrão se não especificado
FONT_SIZE="${1:-14}"
FONT_NAME="Cantarell"

echo -e "${YELLOW}📏 Configurando fonte: $FONT_NAME $FONT_SIZE pt${NC}"

# 1. Configurar via gsettings (GNOME)
if command -v gsettings &> /dev/null; then
    echo "⚙️  Configurando GNOME..."
    gsettings set org.gnome.desktop.interface font-name "$FONT_NAME $FONT_SIZE"
    gsettings set org.gnome.desktop.interface document-font-name "$FONT_NAME $FONT_SIZE"
    gsettings set org.gnome.desktop.interface monospace-font-name "JetBrains Mono $FONT_SIZE"
    echo -e "${GREEN}✅ GNOME configurado${NC}"
fi

# 2. Configurar GTK
echo "🎨 Configurando GTK..."
mkdir -p "$HOME/.config/gtk-3.0"
cat > "$HOME/.config/gtk-3.0/settings.ini" << EOF
[Settings]
gtk-font-name=$FONT_NAME $FONT_SIZE
EOF

# GTK 2.0
cat > "$HOME/.gtkrc-2.0" << EOF
gtk-font-name="$FONT_NAME $FONT_SIZE"
EOF

echo -e "${GREEN}✅ GTK configurado${NC}"

# 3. Configurar terminal (GNOME Terminal)
if command -v gsettings &> /dev/null; then
    echo "🖥️  Configurando terminal..."
    PROFILE_ID=$(gsettings get org.gnome.Terminal.ProfilesList default | tr -d "'")
    gsettings set "org.gnome.Terminal.Legacy.Profile:/org/gnome/terminal/legacy/profiles:/:$PROFILE_ID/" font "$FONT_NAME $FONT_SIZE" 2>/dev/null || true
    echo -e "${GREEN}✅ Terminal configurado${NC}"
fi

# 4. Configurar Alacritty (se existir)
ALACRITTY_CONFIG="$HOME/.config/alacritty/alacritty.yml"
if [[ -f "$ALACRITTY_CONFIG" ]]; then
    echo "🖥️  Configurando Alacritty..."
    sed -i "s/size: [0-9.]*/size: $FONT_SIZE/" "$ALACRITTY_CONFIG" 2>/dev/null || true
    sed -i "s/family: \".*\"/family: \"$FONT_NAME\"/" "$ALACRITTY_CONFIG" 2>/dev/null || true
    echo -e "${GREEN}✅ Alacritty configurado${NC}"
fi

# 5. Configurar Kitty (se existir)
KITTY_CONFIG="$HOME/.config/kitty/kitty.conf"
if [[ -f "$KITTY_CONFIG" ]]; then
    echo "🖥️  Configurando Kitty..."
    sed -i "s/font_size [0-9.]*/font_size $FONT_SIZE/" "$KITTY_CONFIG" 2>/dev/null || true
    sed -i "s/font_family .*/font_family $FONT_NAME/" "$KITTY_CONFIG" 2>/dev/null || true
    echo -e "${GREEN}✅ Kitty configurado${NC}"
fi

# 6. Atualizar cache de fontes
echo "🔄 Atualizando cache de fontes..."
fc-cache -fv 2>/dev/null || true

echo
echo -e "${GREEN}🎉 Fontes aumentadas com sucesso!${NC}"
echo
echo -e "${YELLOW}💡 Para aplicar as mudanças:${NC}"
echo "   1. Reinicie suas aplicações"
echo "   2. Ou faça logout/login"
echo "   3. Ou reinicie o sistema"
echo
echo -e "${YELLOW}🔧 Para voltar ao tamanho padrão:${NC}"
echo "   ./quick_font_increase.sh 11"
echo
echo -e "${YELLOW}📏 Tamanhos sugeridos:${NC}"
echo "   • 10pt - Pequeno (padrão)"
echo "   • 12pt - Médio"
echo "   • 14pt - Grande (recomendado)"
echo "   • 16pt - Muito grande"
echo "   • 18pt - Extra grande"
