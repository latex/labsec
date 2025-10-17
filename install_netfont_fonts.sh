#!/bin/bash

# Script para baixar e instalar todas as fontes do NetFont no sistema
# Compatível com Arch Linux e sistemas baseados em systemd

set -euo pipefail

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para logging
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Verificar se está rodando como root
check_root() {
    if [[ $EUID -eq 0 ]]; then
        error "Este script não deve ser executado como root"
        error "Execute como usuário normal. O script irá instalar as fontes no diretório do usuário"
        exit 1
    fi
}

# Verificar dependências
check_dependencies() {
    log "Verificando dependências..."
    
    local missing_deps=()
    
    if ! command -v wget &> /dev/null; then
        missing_deps+=("wget")
    fi
    
    if ! command -v unzip &> /dev/null; then
        missing_deps+=("unzip")
    fi
    
    if ! command -v curl &> /dev/null; then
        missing_deps+=("curl")
    fi
    
    if ! command -v fc-cache &> /dev/null; then
        missing_deps+=("fontconfig")
    fi
    
    if [[ ${#missing_deps[@]} -gt 0 ]]; then
        error "Dependências faltando: ${missing_deps[*]}"
        log "Instale as dependências com:"
        log "sudo pacman -S ${missing_deps[*]}"
        exit 1
    fi
    
    success "Todas as dependências estão instaladas"
}

# Criar diretórios necessários
setup_directories() {
    log "Configurando diretórios..."
    
    # Diretório temporário
    TEMP_DIR="/tmp/netfont_fonts_$$"
    mkdir -p "$TEMP_DIR"
    
    # Diretório de fontes do usuário
    USER_FONT_DIR="$HOME/.local/share/fonts"
    mkdir -p "$USER_FONT_DIR"
    
    # Diretório de fontes do sistema (se necessário)
    SYSTEM_FONT_DIR="/usr/share/fonts"
    
    success "Diretórios configurados"
}

# URLs das fontes NetFont (exemplos - ajuste conforme necessário)
get_netfont_urls() {
    # Lista de URLs das fontes NetFont
    # Nota: Estas URLs são exemplos - você precisará verificar as URLs reais
    cat << 'EOF'
https://www.netfont.com.br/fonts/NetFont-Regular.ttf
https://www.netfont.com.br/fonts/NetFont-Bold.ttf
https://www.netfont.com.br/fonts/NetFont-Italic.ttf
https://www.netfont.com.br/fonts/NetFont-BoldItalic.ttf
https://www.netfont.com.br/fonts/NetFont-Light.ttf
https://www.netfont.com.br/fonts/NetFont-Medium.ttf
https://www.netfont.com.br/fonts/NetFont-SemiBold.ttf
https://www.netfont.com.br/fonts/NetFont-ExtraBold.ttf
https://www.netfont.com.br/fonts/NetFont-Black.ttf
EOF
}

# Baixar fontes individuais
download_fonts() {
    log "Iniciando download das fontes NetFont..."
    
    local urls_file="$TEMP_DIR/urls.txt"
    get_netfont_urls > "$urls_file"
    
    local downloaded=0
    local failed=0
    
    while IFS= read -r url; do
        if [[ -n "$url" && ! "$url" =~ ^# ]]; then
            local filename=$(basename "$url")
            local filepath="$TEMP_DIR/$filename"
            
            log "Baixando: $filename"
            
            if wget -q --show-progress -O "$filepath" "$url" 2>/dev/null; then
                success "✓ $filename baixado"
                ((downloaded++))
            else
                warning "✗ Falha ao baixar $filename"
                ((failed++))
            fi
        fi
    done < "$urls_file"
    
    log "Download concluído: $downloaded sucessos, $failed falhas"
}

# Baixar pacote completo (alternativa)
download_font_package() {
    log "Tentando baixar pacote completo de fontes..."
    
    local package_urls=(
        "https://www.netfont.com.br/downloads/netfont_fonts.zip"
        "https://www.netfont.com.br/downloads/netfont-complete.zip"
        "https://github.com/netfont/fonts/archive/main.zip"
    )
    
    for url in "${package_urls[@]}"; do
        log "Tentando: $url"
        if wget -q --show-progress -O "$TEMP_DIR/netfont_package.zip" "$url" 2>/dev/null; then
            success "Pacote baixado com sucesso"
            return 0
        else
            warning "Falha ao baixar pacote de: $url"
        fi
    done
    
    error "Não foi possível baixar nenhum pacote de fontes"
    return 1
}

# Extrair fontes
extract_fonts() {
    log "Extraindo fontes..."
    
    local package_file="$TEMP_DIR/netfont_package.zip"
    
    if [[ -f "$package_file" ]]; then
        if unzip -q "$package_file" -d "$TEMP_DIR/extracted" 2>/dev/null; then
            success "Fontes extraídas com sucesso"
            
            # Mover fontes extraídas para o diretório temporário principal
            find "$TEMP_DIR/extracted" -name "*.ttf" -o -name "*.otf" -o -name "*.woff" -o -name "*.woff2" | \
                while read -r font_file; do
                    cp "$font_file" "$TEMP_DIR/"
                done
        else
            error "Falha ao extrair o pacote de fontes"
            return 1
        fi
    fi
}

# Instalar fontes
install_fonts() {
    log "Instalando fontes..."
    
    local installed=0
    local skipped=0
    
    # Encontrar todas as fontes no diretório temporário
    while IFS= read -r -d '' font_file; do
        local filename=$(basename "$font_file")
        local dest_file="$USER_FONT_DIR/$filename"
        
        # Verificar se a fonte já existe
        if [[ -f "$dest_file" ]]; then
            log "Fonte já existe, pulando: $filename"
            ((skipped++))
            continue
        fi
        
        # Copiar fonte
        if cp "$font_file" "$dest_file" 2>/dev/null; then
            success "✓ Instalada: $filename"
            ((installed++))
        else
            warning "✗ Falha ao instalar: $filename"
        fi
        
    done < <(find "$TEMP_DIR" -type f \( -name "*.ttf" -o -name "*.otf" -o -name "*.woff" -o -name "*.woff2" \) -print0)
    
    log "Instalação concluída: $installed novas fontes, $skipped já existentes"
}

# Atualizar cache de fontes
update_font_cache() {
    log "Atualizando cache de fontes..."
    
    if fc-cache -fv "$USER_FONT_DIR" 2>/dev/null; then
        success "Cache de fontes atualizado"
    else
        warning "Falha ao atualizar cache de fontes"
    fi
}

# Verificar instalação
verify_installation() {
    log "Verificando instalação..."
    
    local font_count=$(find "$USER_FONT_DIR" -name "*netfont*" -o -name "*NetFont*" | wc -l)
    
    if [[ $font_count -gt 0 ]]; then
        success "Instalação verificada: $font_count fontes NetFont encontradas"
        
        log "Fontes instaladas:"
        find "$USER_FONT_DIR" -name "*netfont*" -o -name "*NetFont*" | while read -r font; do
            echo "  - $(basename "$font")"
        done
    else
        warning "Nenhuma fonte NetFont foi encontrada após a instalação"
    fi
}

# Limpeza
cleanup() {
    log "Limpando arquivos temporários..."
    
    if [[ -n "${TEMP_DIR:-}" && -d "$TEMP_DIR" ]]; then
        rm -rf "$TEMP_DIR"
        success "Arquivos temporários removidos"
    fi
}

# Função principal
main() {
    log "=== Instalador de Fontes NetFont ==="
    log "Sistema: $(uname -a)"
    log "Usuário: $(whoami)"
    log "Diretório de fontes: $HOME/.local/share/fonts"
    
    # Verificações iniciais
    check_root
    check_dependencies
    setup_directories
    
    # Trap para limpeza em caso de erro
    trap cleanup EXIT
    
    # Tentar baixar fontes
    if ! download_fonts; then
        log "Tentando método alternativo (pacote completo)..."
        if download_font_package; then
            extract_fonts
        else
            error "Não foi possível baixar as fontes NetFont"
            exit 1
        fi
    fi
    
    # Instalar fontes
    install_fonts
    update_font_cache
    verify_installation
    
    success "=== Instalação concluída com sucesso! ==="
    log "As fontes NetFont estão agora disponíveis no sistema"
    log "Reinicie aplicações que usam fontes para vê-las disponíveis"
    
    # Mostrar informações adicionais
    echo
    log "Para usar as fontes no terminal:"
    log "1. Abra as configurações do seu terminal"
    log "2. Procure por 'Font' ou 'Fonte'"
    log "3. Selecione uma das fontes NetFont instaladas"
    
    echo
    log "Para verificar fontes disponíveis:"
    log "fc-list | grep -i netfont"
}

# Executar script
main "$@"
