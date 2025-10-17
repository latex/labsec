#!/bin/bash

# Instalador Universal de Fontes
# Suporta NetFont, fontes de programação e outras fontes populares

set -e

# Cores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

# Função para mostrar menu
show_menu() {
    clear
    echo -e "${CYAN}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║                    INSTALADOR UNIVERSAL DE FONTES            ║${NC}"
    echo -e "${CYAN}╚══════════════════════════════════════════════════════════════╝${NC}"
    echo
    echo -e "${BLUE}Escolha uma opção:${NC}"
    echo
    echo -e "${GREEN}1.${NC} 🎨 Instalar fontes NetFont"
    echo -e "${GREEN}2.${NC} 💻 Instalar fontes para programação"
    echo -e "${GREEN}3.${NC} 🚀 Instalar todas as fontes (NetFont + Programação)"
    echo -e "${GREEN}4.${NC} 🔍 Verificar fontes instaladas"
    echo -e "${GREEN}5.${NC} 🧹 Limpar cache de fontes"
    echo -e "${GREEN}6.${NC} 📋 Listar fontes disponíveis"
    echo -e "${GREEN}7.${NC} ❓ Ajuda"
    echo -e "${GREEN}8.${NC} 🚪 Sair"
    echo
    echo -n -e "${YELLOW}Digite sua escolha [1-8]: ${NC}"
}

# Função para instalar NetFont
install_netfont() {
    echo -e "${BLUE}🎨 Instalando fontes NetFont...${NC}"
    
    if [[ -f "install_netfont_simple.sh" ]]; then
        ./install_netfont_simple.sh
    else
        echo -e "${RED}❌ Script install_netfont_simple.sh não encontrado${NC}"
        echo -e "${YELLOW}💡 Execute este script no diretório correto${NC}"
    fi
}

# Função para instalar fontes de programação
install_programming() {
    echo -e "${BLUE}💻 Instalando fontes para programação...${NC}"
    
    if [[ -f "install_programming_fonts.sh" ]]; then
        ./install_programming_fonts.sh
    else
        echo -e "${RED}❌ Script install_programming_fonts.sh não encontrado${NC}"
        echo -e "${YELLOW}💡 Execute este script no diretório correto${NC}"
    fi
}

# Função para instalar todas as fontes
install_all() {
    echo -e "${BLUE}🚀 Instalando todas as fontes...${NC}"
    
    install_netfont
    echo
    install_programming
}

# Função para verificar fontes instaladas
check_fonts() {
    echo -e "${BLUE}🔍 Verificando fontes instaladas...${NC}"
    echo
    
    local font_dir="$HOME/.local/share/fonts"
    
    if [[ -d "$font_dir" ]]; then
        local count=$(find "$font_dir" -name "*.ttf" -o -name "*.otf" | wc -l)
        echo -e "${GREEN}✅ Diretório de fontes: $font_dir${NC}"
        echo -e "${GREEN}📊 Total de fontes: $count${NC}"
        echo
        
        echo -e "${YELLOW}📋 Fontes NetFont:${NC}"
        find "$font_dir" -name "*netfont*" -o -name "*NetFont*" | while read -r font; do
            echo "  - $(basename "$font")"
        done
        
        echo
        echo -e "${YELLOW}📋 Fontes de Programação:${NC}"
        find "$font_dir" -name "*Fira*" -o -name "*JetBrains*" -o -name "*Cascadia*" -o -name "*Source*" -o -name "*Hack*" | while read -r font; do
            echo "  - $(basename "$font")"
        done
    else
        echo -e "${RED}❌ Diretório de fontes não encontrado: $font_dir${NC}"
    fi
}

# Função para limpar cache
clear_cache() {
    echo -e "${BLUE}🧹 Limpando cache de fontes...${NC}"
    
    if fc-cache -fv; then
        echo -e "${GREEN}✅ Cache limpo com sucesso${NC}"
    else
        echo -e "${RED}❌ Erro ao limpar cache${NC}"
    fi
}

# Função para listar fontes disponíveis
list_fonts() {
    echo -e "${BLUE}📋 Fontes disponíveis no sistema:${NC}"
    echo
    
    echo -e "${YELLOW}🔍 Buscando fontes populares...${NC}"
    echo
    
    # Listar fontes por categoria
    echo -e "${GREEN}🎨 Fontes NetFont:${NC}"
    fc-list | grep -i netfont | head -10 || echo "  Nenhuma fonte NetFont encontrada"
    
    echo
    echo -e "${GREEN}💻 Fontes de Programação:${NC}"
    fc-list | grep -E "(Fira|JetBrains|Cascadia|Source|Hack|Ubuntu|Meslo|Roboto|Inconsolata)" | head -10
    
    echo
    echo -e "${GREEN}🖥️  Fontes Monospace:${NC}"
    fc-list | grep -i monospace | head -10
    
    echo
    echo -e "${YELLOW}💡 Para ver todas as fontes: fc-list${NC}"
    echo -e "${YELLOW}💡 Para buscar uma fonte específica: fc-list | grep -i [nome]${NC}"
}

# Função de ajuda
show_help() {
    echo -e "${BLUE}❓ Ajuda - Instalador Universal de Fontes${NC}"
    echo
    echo -e "${YELLOW}📖 Sobre este script:${NC}"
    echo "Este script facilita a instalação de fontes no Linux, especialmente"
    echo "fontes NetFont e fontes populares para programação."
    echo
    echo -e "${YELLOW}🎯 Opções disponíveis:${NC}"
    echo "1. NetFont - Fontes específicas do NetFont"
    echo "2. Programação - Fontes populares para desenvolvimento"
    echo "3. Todas - Instala ambos os tipos de fontes"
    echo "4. Verificar - Mostra fontes já instaladas"
    echo "5. Limpar Cache - Atualiza o cache de fontes"
    echo "6. Listar - Mostra fontes disponíveis no sistema"
    echo
    echo -e "${YELLOW}📁 Localização das fontes:${NC}"
    echo "• Usuário: ~/.local/share/fonts/"
    echo "• Sistema: /usr/share/fonts/ (requer sudo)"
    echo
    echo -e "${YELLOW}🔧 Pré-requisitos:${NC}"
    echo "• wget, unzip, curl, fontconfig"
    echo "• Conexão com internet"
    echo
    echo -e "${YELLOW}💡 Dicas:${NC}"
    echo "• Reinicie aplicações após instalar fontes"
    echo "• Use fc-list para ver todas as fontes"
    echo "• Configure fontes no terminal/editor"
    echo
    echo -e "${YELLOW}📚 Documentação:${NC}"
    echo "• README: FONT_INSTALLATION_README.md"
    echo "• Scripts individuais disponíveis"
}

# Função principal
main() {
    # Verificar dependências
    local missing_deps=()
    
    for cmd in wget unzip curl fc-cache; do
        if ! command -v "$cmd" &> /dev/null; then
            missing_deps+=("$cmd")
        fi
    done
    
    if [[ ${#missing_deps[@]} -gt 0 ]]; then
        echo -e "${RED}❌ Dependências faltando: ${missing_deps[*]}${NC}"
        echo -e "${YELLOW}💡 Instale com: sudo pacman -S ${missing_deps[*]}${NC}"
        exit 1
    fi
    
    # Loop principal
    while true; do
        show_menu
        read -r choice
        
        case $choice in
            1)
                install_netfont
                ;;
            2)
                install_programming
                ;;
            3)
                install_all
                ;;
            4)
                check_fonts
                ;;
            5)
                clear_cache
                ;;
            6)
                list_fonts
                ;;
            7)
                show_help
                ;;
            8)
                echo -e "${GREEN}👋 Obrigado por usar o Instalador Universal de Fontes!${NC}"
                exit 0
                ;;
            *)
                echo -e "${RED}❌ Opção inválida. Digite um número de 1 a 8.${NC}"
                ;;
        esac
        
        echo
        echo -e "${YELLOW}⏸️  Pressione Enter para continuar...${NC}"
        read -r
    done
}

# Executar script
main "$@"
