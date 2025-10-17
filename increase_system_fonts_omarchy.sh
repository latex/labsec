#!/bin/bash

# Script para aumentar as fontes do sistema no Omarchy Linux
# Configura fontes em diferentes componentes do sistema

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

# Função para logging
log() {
    echo -e "${BLUE}[$(date +'%H:%M:%S')]${NC} $1"
}

success() {
    echo -e "${GREEN}✅${NC} $1"
}

warning() {
    echo -e "${YELLOW}⚠️${NC} $1"
}

error() {
    echo -e "${RED}❌${NC} $1"
}

# Verificar se está no Omarchy
check_omarchy() {
    if [[ -f "/etc/os-release" ]]; then
        if grep -q "Omarchy" /etc/os-release; then
            success "Sistema Omarchy detectado"
            return 0
        fi
    fi
    
    warning "Sistema Omarchy não detectado, mas continuando..."
    return 1
}

# Função para mostrar menu de tamanhos
show_font_menu() {
    clear
    echo -e "${CYAN}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║                AUMENTAR FONTES DO SISTEMA - OMARCHY          ║${NC}"
    echo -e "${CYAN}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo
    echo -e "${BLUE}Escolha o tamanho das fontes:${NC}"
    echo
    echo -e "${GREEN}1.${NC} 🔤 Pequeno (10pt) - Padrão"
    echo -e "${GREEN}2.${NC} 📝 Médio (12pt) - Recomendado"
    echo -e "${GREEN}3.${NC} 📖 Grande (14pt) - Fácil leitura"
    echo -e "${GREEN}4.${NC} 📚 Muito Grande (16pt) - Acessibilidade"
    echo -e "${GREEN}5.${NC} 🔍 Extra Grande (18pt) - Muito fácil"
    echo -e "${GREEN}6.${NC} ⚙️  Personalizado"
    echo -e "${GREEN}7.${NC} 🔄 Restaurar padrões"
    echo -e "${GREEN}8.${NC} 🚪 Sair"
    echo
    echo -n -e "${YELLOW}Digite sua escolha [1-8]: ${NC}"
}

# Configurar fontes do sistema via gsettings (GNOME)
configure_gnome_fonts() {
    local size="$1"
    local font_name="$2"
    
    log "Configurando fontes GNOME..."
    
    # Fontes principais do sistema
    gsettings set org.gnome.desktop.interface font-name "$font_name $size"
    gsettings set org.gnome.desktop.interface document-font-name "$font_name $size"
    gsettings set org.gnome.desktop.interface monospace-font-name "JetBrains Mono $size"
    
    # Fontes de janelas e títulos
    gsettings set org.gnome.desktop.wm.preferences titlebar-font "$font_name $size"
    
    # Fontes de aplicações
    gsettings set org.gnome.desktop.interface text-scaling-factor 1.0
    
    success "Fontes GNOME configuradas"
}

# Configurar fontes via XFCE
configure_xfce_fonts() {
    local size="$1"
    local font_name="$2"
    
    log "Configurando fontes XFCE..."
    
    # Configurar via xfconf-query
    xfconf-query -c xsettings -p /Gtk/FontName -s "$font_name $size" 2>/dev/null || true
    xfconf-query -c xfce4-desktop -p /desktop-icons/style -s 0 2>/dev/null || true
    
    success "Fontes XFCE configuradas"
}

# Configurar fontes via KDE
configure_kde_fonts() {
    local size="$1"
    local font_name="$2"
    
    log "Configurando fontes KDE..."
    
    # Configurar via kwriteconfig5
    kwriteconfig5 --file kdeglobals --group General --key font "$font_name,$size" 2>/dev/null || true
    kwriteconfig5 --file kdeglobals --group General --key menuFont "$font_name,$size" 2>/dev/null || true
    kwriteconfig5 --file kdeglobals --group General --key taskbarFont "$font_name,$size" 2>/dev/null || true
    
    success "Fontes KDE configuradas"
}

# Configurar fontes do terminal
configure_terminal_fonts() {
    local size="$1"
    local font_name="$2"
    
    log "Configurando fontes do terminal..."
    
    # GNOME Terminal
    if command -v gsettings &> /dev/null; then
        local profile_id=$(gsettings get org.gnome.Terminal.ProfilesList default | tr -d "'")
        gsettings set "org.gnome.Terminal.Legacy.Profile:/org/gnome/terminal/legacy/profiles:/:$profile_id/" font "$font_name $size" 2>/dev/null || true
    fi
    
    # Alacritty
    local alacritty_config="$HOME/.config/alacritty/alacritty.yml"
    if [[ -f "$alacritty_config" ]]; then
        sed -i "s/size: [0-9.]*/size: $size/" "$alacritty_config" 2>/dev/null || true
        sed -i "s/family: \".*\"/family: \"$font_name\"/" "$alacritty_config" 2>/dev/null || true
    fi
    
    # Kitty
    local kitty_config="$HOME/.config/kitty/kitty.conf"
    if [[ -f "$kitty_config" ]]; then
        sed -i "s/font_size [0-9.]*/font_size $size/" "$kitty_config" 2>/dev/null || true
        sed -i "s/font_family .*/font_family $font_name/" "$kitty_config" 2>/dev/null || true
    fi
    
    success "Fontes do terminal configuradas"
}

# Configurar fontes do navegador
configure_browser_fonts() {
    local size="$1"
    
    log "Configurando fontes do navegador..."
    
    # Firefox
    local firefox_profile="$HOME/.mozilla/firefox"
    if [[ -d "$firefox_profile" ]]; then
        local profile_dir=$(find "$firefox_profile" -name "*.default*" -type d | head -1)
        if [[ -n "$profile_dir" ]]; then
            local user_js="$profile_dir/user.js"
            echo "user_pref(\"font.size.fixed.x-western\", $size);" >> "$user_js"
            echo "user_pref(\"font.size.variable.x-western\", $size);" >> "$user_js"
        fi
    fi
    
    # Chrome/Chromium
    local chrome_config="$HOME/.config/google-chrome/Default/Preferences"
    if [[ -f "$chrome_config" ]]; then
        # Backup do arquivo original
        cp "$chrome_config" "$chrome_config.backup" 2>/dev/null || true
    fi
    
    success "Fontes do navegador configuradas"
}

# Configurar fontes via arquivos de configuração
configure_config_files() {
    local size="$1"
    local font_name="$2"
    
    log "Configurando arquivos de configuração..."
    
    # GTK 2.0
    local gtk2_config="$HOME/.gtkrc-2.0"
    cat > "$gtk2_config" << EOF
gtk-font-name="$font_name $size"
EOF
    
    # GTK 3.0
    local gtk3_config="$HOME/.config/gtk-3.0/settings.ini"
    mkdir -p "$HOME/.config/gtk-3.0"
    cat > "$gtk3_config" << EOF
[Settings]
gtk-font-name=$font_name $size
EOF
    
    # GTK 4.0
    local gtk4_config="$HOME/.config/gtk-4.0/settings.ini"
    mkdir -p "$HOME/.config/gtk-4.0"
    cat > "$gtk4_config" << EOF
[Settings]
gtk-font-name=$font_name $size
EOF
    
    success "Arquivos de configuração criados"
}

# Aplicar configurações
apply_font_settings() {
    local size="$1"
    local font_name="$2"
    
    log "Aplicando configurações de fonte..."
    
    # Detectar ambiente desktop
    local desktop_env="${XDG_CURRENT_DESKTOP:-$DESKTOP_SESSION}"
    
    case "$desktop_env" in
        *GNOME*|*gnome*)
            configure_gnome_fonts "$size" "$font_name"
            ;;
        *XFCE*|*xfce*)
            configure_xfce_fonts "$size" "$font_name"
            ;;
        *KDE*|*kde*|*plasma*)
            configure_kde_fonts "$size" "$font_name"
            ;;
        *)
            warning "Ambiente desktop não detectado, aplicando configurações genéricas"
            configure_config_files "$size" "$font_name"
            ;;
    esac
    
    # Configurar terminal e navegador
    configure_terminal_fonts "$size" "$font_name"
    configure_browser_fonts "$size"
    
    # Atualizar cache de fontes
    log "Atualizando cache de fontes..."
    fc-cache -fv 2>/dev/null || true
    
    success "Configurações aplicadas com sucesso!"
}

# Restaurar configurações padrão
restore_defaults() {
    log "Restaurando configurações padrão..."
    
    # GNOME
    if command -v gsettings &> /dev/null; then
        gsettings reset org.gnome.desktop.interface font-name 2>/dev/null || true
        gsettings reset org.gnome.desktop.interface document-font-name 2>/dev/null || true
        gsettings reset org.gnome.desktop.interface monospace-font-name 2>/dev/null || true
        gsettings reset org.gnome.desktop.wm.preferences titlebar-font 2>/dev/null || true
    fi
    
    # Remover arquivos de configuração personalizados
    rm -f "$HOME/.gtkrc-2.0"
    rm -f "$HOME/.config/gtk-3.0/settings.ini"
    rm -f "$HOME/.config/gtk-4.0/settings.ini"
    
    success "Configurações padrão restauradas"
}

# Função principal
main() {
    check_omarchy
    
    while true; do
        show_font_menu
        read -r choice
        
        case $choice in
            1)
                apply_font_settings "10" "Cantarell"
                ;;
            2)
                apply_font_settings "12" "Cantarell"
                ;;
            3)
                apply_font_settings "14" "Cantarell"
                ;;
            4)
                apply_font_settings "16" "Cantarell"
                ;;
            5)
                apply_font_settings "18" "Cantarell"
                ;;
            6)
                echo -n "Digite o tamanho da fonte (ex: 14): "
                read -r custom_size
                echo -n "Digite o nome da fonte (ex: Cantarell): "
                read -r custom_font
                if [[ "$custom_size" =~ ^[0-9]+$ ]] && [[ -n "$custom_font" ]]; then
                    apply_font_settings "$custom_size" "$custom_font"
                else
                    error "Tamanho ou fonte inválidos"
                fi
                ;;
            7)
                restore_defaults
                ;;
            8)
                echo -e "${GREEN}👋 Até logo!${NC}"
                exit 0
                ;;
            *)
                error "Opção inválida. Digite um número de 1 a 8."
                ;;
        esac
        
        echo
        echo -e "${YELLOW}⏸️  Pressione Enter para continuar...${NC}"
        read -r
    done
}

# Executar script
main "$@"
