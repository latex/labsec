#!/bin/bash

# Gerenciador de Fontes para Omarchy Linux
# Script principal que combina todas as funcionalidades

set -e

# Cores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

# Função para mostrar menu principal
show_main_menu() {
    clear
    echo -e "${CYAN}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║                GERENCIADOR DE FONTES - OMARCHY LINUX        ║${NC}"
    echo -e "${CYAN}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo
    echo -e "${BLUE}Escolha uma opção:${NC}"
    echo
    echo -e "${GREEN}1.${NC} 🚀 Aumentar fontes rapidamente (14pt)"
    echo -e "${GREEN}2.${NC} 📏 Escolher tamanho específico"
    echo -e "${GREEN}3.${NC} 🎛️  Menu interativo completo"
    echo -e "${GREEN}4.${NC} 🖥️  Abrir configurações gráficas"
    echo -e "${GREEN}5.${NC} 🔍 Verificar fontes atuais"
    echo -e "${GREEN}6.${NC} 📋 Listar fontes disponíveis"
    echo -e "${GREEN}7.${NC} 🔄 Restaurar configurações padrão"
    echo -e "${GREEN}8.${NC} 📖 Mostrar guia de ajuda"
    echo -e "${GREEN}9.${NC} 🚪 Sair"
    echo
    echo -n -e "${YELLOW}Digite sua escolha [1-9]: ${NC}"
}

# Função para mostrar tamanhos
show_size_menu() {
    clear
    echo -e "${BLUE}📏 Escolha o tamanho da fonte:${NC}"
    echo
    echo -e "${GREEN}1.${NC} 🔤 Pequeno (10pt)"
    echo -e "${GREEN}2.${NC} 📝 Médio (12pt)"
    echo -e "${GREEN}3.${NC} 📖 Grande (14pt) - Recomendado"
    echo -e "${GREEN}4.${NC} 📚 Muito Grande (16pt)"
    echo -e "${GREEN}5.${NC} 🔍 Extra Grande (18pt)"
    echo -e "${GREEN}6.${NC} ⚙️  Personalizado"
    echo -e "${GREEN}7.${NC} ⬅️  Voltar"
    echo
    echo -n -e "${YELLOW}Digite sua escolha [1-7]: ${NC}"
}

# Função para aplicar tamanho
apply_size() {
    local size="$1"
    echo -e "${BLUE}🔤 Aplicando fonte $size pt...${NC}"
    
    if [[ -f "quick_font_increase.sh" ]]; then
        ./quick_font_increase.sh "$size"
    else
        echo -e "${RED}❌ Script quick_font_increase.sh não encontrado${NC}"
        echo -e "${YELLOW}💡 Execute este script no diretório correto${NC}"
    fi
}

# Função para verificar fontes atuais
check_current_fonts() {
    echo -e "${BLUE}🔍 Verificando configurações atuais...${NC}"
    echo
    
    # GNOME
    if command -v gsettings &> /dev/null; then
        echo -e "${YELLOW}🖥️  GNOME:${NC}"
        echo "  Fonte do sistema: $(gsettings get org.gnome.desktop.interface font-name 2>/dev/null || echo 'Não configurado')"
        echo "  Fonte de documentos: $(gsettings get org.gnome.desktop.interface document-font-name 2>/dev/null || echo 'Não configurado')"
        echo "  Fonte monospace: $(gsettings get org.gnome.desktop.interface monospace-font-name 2>/dev/null || echo 'Não configurado')"
        echo
    fi
    
    # XFCE
    if command -v xfconf-query &> /dev/null; then
        echo -e "${YELLOW}🖥️  XFCE:${NC}"
        echo "  Fonte GTK: $(xfconf-query -c xsettings -p /Gtk/FontName 2>/dev/null || echo 'Não configurado')"
        echo
    fi
    
    # KDE
    if command -v kreadconfig5 &> /dev/null; then
        echo -e "${YELLOW}🖥️  KDE:${NC}"
        echo "  Fonte principal: $(kreadconfig5 --file kdeglobals --group General --key font 2>/dev/null || echo 'Não configurado')"
        echo
    fi
    
    # Verificar arquivos de configuração
    echo -e "${YELLOW}📁 Arquivos de configuração:${NC}"
    if [[ -f "$HOME/.config/gtk-3.0/settings.ini" ]]; then
        echo "  ✅ GTK 3.0: $HOME/.config/gtk-3.0/settings.ini"
    else
        echo "  ❌ GTK 3.0: Não configurado"
    fi
    
    if [[ -f "$HOME/.gtkrc-2.0" ]]; then
        echo "  ✅ GTK 2.0: $HOME/.gtkrc-2.0"
    else
        echo "  ❌ GTK 2.0: Não configurado"
    fi
}

# Função para listar fontes
list_available_fonts() {
    echo -e "${BLUE}📋 Fontes disponíveis no sistema:${NC}"
    echo
    
    echo -e "${YELLOW}🔍 Fontes do sistema (primeiras 10):${NC}"
    fc-list | grep -E "(Cantarell|Ubuntu|Liberation)" | head -10
    
    echo
    echo -e "${YELLOW}💻 Fontes monospace (primeiras 10):${NC}"
    fc-list | grep -i mono | head -10
    
    echo
    echo -e "${YELLOW}🎨 Fontes de programação:${NC}"
    fc-list | grep -E "(Fira|JetBrains|Cascadia|Source|Hack)" | head -10
    
    echo
    echo -e "${YELLOW}💡 Para ver todas as fontes: fc-list${NC}"
    echo -e "${YELLOW}💡 Para buscar uma fonte específica: fc-list | grep -i [nome]${NC}"
}

# Função para restaurar padrões
restore_defaults() {
    echo -e "${BLUE}🔄 Restaurando configurações padrão...${NC}"
    
    # GNOME
    if command -v gsettings &> /dev/null; then
        gsettings reset org.gnome.desktop.interface font-name 2>/dev/null || true
        gsettings reset org.gnome.desktop.interface document-font-name 2>/dev/null || true
        gsettings reset org.gnome.desktop.interface monospace-font-name 2>/dev/null || true
        gsettings reset org.gnome.desktop.wm.preferences titlebar-font 2>/dev/null || true
        echo -e "${GREEN}✅ Configurações GNOME restauradas${NC}"
    fi
    
    # XFCE
    if command -v xfconf-query &> /dev/null; then
        xfconf-query -c xsettings -p /Gtk/FontName -r 2>/dev/null || true
        echo -e "${GREEN}✅ Configurações XFCE restauradas${NC}"
    fi
    
    # KDE
    if command -v kwriteconfig5 &> /dev/null; then
        kwriteconfig5 --file kdeglobals --group General --key font "" 2>/dev/null || true
        echo -e "${GREEN}✅ Configurações KDE restauradas${NC}"
    fi
    
    # Remover arquivos de configuração personalizados
    rm -f "$HOME/.gtkrc-2.0"
    rm -f "$HOME/.config/gtk-3.0/settings.ini"
    rm -f "$HOME/.config/gtk-4.0/settings.ini"
    
    echo -e "${GREEN}✅ Arquivos de configuração removidos${NC}"
    echo -e "${YELLOW}💡 Reinicie suas aplicações para aplicar as mudanças${NC}"
}

# Função para mostrar ajuda
show_help() {
    clear
    echo -e "${BLUE}📖 Guia de Ajuda - Gerenciador de Fontes Omarchy${NC}"
    echo
    echo -e "${YELLOW}🎯 Sobre este script:${NC}"
    echo "Este script facilita o aumento das fontes do sistema no Omarchy Linux."
    echo "Ele funciona com GNOME, XFCE e KDE."
    echo
    echo -e "${YELLOW}🚀 Uso rápido:${NC}"
    echo "1. Execute: ./omarchy_font_manager.sh"
    echo "2. Escolha a opção 1 para aumentar rapidamente para 14pt"
    echo "3. Ou escolha a opção 2 para selecionar um tamanho específico"
    echo
    echo -e "${YELLOW}📏 Tamanhos recomendados:${NC}"
    echo "• 10pt - Pequeno (padrão)"
    echo "• 12pt - Médio"
    echo "• 14pt - Grande (recomendado para leitura confortável)"
    echo "• 16pt - Muito grande (acessibilidade)"
    echo "• 18pt - Extra grande (muito fácil de ler)"
    echo
    echo -e "${YELLOW}🔧 Comandos diretos:${NC}"
    echo "• Aumentar para 14pt: ./quick_font_increase.sh 14"
    echo "• Menu interativo: ./increase_system_fonts_omarchy.sh"
    echo "• Configurações gráficas: ./configure_fonts_gui.sh"
    echo
    echo -e "${YELLOW}📁 Arquivos criados:${NC}"
    echo "• ~/.config/gtk-3.0/settings.ini"
    echo "• ~/.gtkrc-2.0"
    echo "• Configurações do terminal"
    echo
    echo -e "${YELLOW}💡 Dicas:${NC}"
    echo "• Reinicie aplicações após mudar as fontes"
    echo "• Use logout/login para aplicar em todo o sistema"
    echo "• Algumas aplicações têm configurações próprias de fonte"
    echo
    echo -e "${YELLOW}📚 Documentação:${NC}"
    echo "• Guia completo: cat OMARCHY_FONT_GUIDE.md"
    echo "• Scripts individuais disponíveis"
    echo
    echo -e "${YELLOW}🆘 Solução de problemas:${NC}"
    echo "• Se as fontes não mudaram: fc-cache -fv"
    echo "• Para restaurar: escolha a opção 7 neste menu"
    echo "• Verificar configurações: escolha a opção 5"
}

# Função principal
main() {
    # Verificar se está no diretório correto
    if [[ ! -f "quick_font_increase.sh" ]]; then
        echo -e "${RED}❌ Execute este script no diretório que contém os scripts de fonte${NC}"
        exit 1
    fi
    
    while true; do
        show_main_menu
        read -r choice
        
        case $choice in
            1)
                apply_size "14"
                ;;
            2)
                while true; do
                    show_size_menu
                    read -r size_choice
                    
                    case $size_choice in
                        1) apply_size "10"; break ;;
                        2) apply_size "12"; break ;;
                        3) apply_size "14"; break ;;
                        4) apply_size "16"; break ;;
                        5) apply_size "18"; break ;;
                        6)
                            echo -n "Digite o tamanho (ex: 15): "
                            read -r custom_size
                            if [[ "$custom_size" =~ ^[0-9]+$ ]]; then
                                apply_size "$custom_size"
                                break
                            else
                                echo -e "${RED}❌ Tamanho inválido${NC}"
                            fi
                            ;;
                        7) break ;;
                        *) echo -e "${RED}❌ Opção inválida${NC}" ;;
                    esac
                done
                ;;
            3)
                if [[ -f "increase_system_fonts_omarchy.sh" ]]; then
                    ./increase_system_fonts_omarchy.sh
                else
                    echo -e "${RED}❌ Script increase_system_fonts_omarchy.sh não encontrado${NC}"
                fi
                ;;
            4)
                if [[ -f "configure_fonts_gui.sh" ]]; then
                    ./configure_fonts_gui.sh
                else
                    echo -e "${RED}❌ Script configure_fonts_gui.sh não encontrado${NC}"
                fi
                ;;
            5)
                check_current_fonts
                ;;
            6)
                list_available_fonts
                ;;
            7)
                restore_defaults
                ;;
            8)
                show_help
                ;;
            9)
                echo -e "${GREEN}👋 Obrigado por usar o Gerenciador de Fontes Omarchy!${NC}"
                exit 0
                ;;
            *)
                echo -e "${RED}❌ Opção inválida. Digite um número de 1 a 9.${NC}"
                ;;
        esac
        
        echo
        echo -e "${YELLOW}⏸️  Pressione Enter para continuar...${NC}"
        read -r
    done
}

# Executar script
main "$@"
