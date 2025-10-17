#!/bin/bash

# Script de demonstração para instalação de fontes
# Mostra como usar os scripts criados

set -e

echo "🎨 === DEMONSTRAÇÃO DE INSTALAÇÃO DE FONTES ==="
echo

# Verificar se estamos no diretório correto
if [[ ! -f "install_fonts_universal.sh" ]]; then
    echo "❌ Execute este script no diretório que contém os scripts de instalação"
    exit 1
fi

echo "📁 Scripts disponíveis:"
ls -la install_*.sh | while read -r line; do
    echo "  $line"
done

echo
echo "📖 Documentação disponível:"
if [[ -f "FONT_INSTALLATION_README.md" ]]; then
    echo "  ✅ FONT_INSTALLATION_README.md"
else
    echo "  ❌ FONT_INSTALLATION_README.md não encontrado"
fi

echo
echo "🚀 Como usar:"
echo
echo "1. Para instalar fontes NetFont:"
echo "   ./install_netfont_simple.sh"
echo
echo "2. Para instalar fontes de programação:"
echo "   ./install_programming_fonts.sh"
echo
echo "3. Para usar o instalador universal (recomendado):"
echo "   ./install_fonts_universal.sh"
echo
echo "4. Para ler a documentação completa:"
echo "   cat FONT_INSTALLATION_README.md"
echo

# Verificar dependências
echo "🔍 Verificando dependências..."
missing_deps=()

for cmd in wget unzip curl fc-cache; do
    if command -v "$cmd" &> /dev/null; then
        echo "  ✅ $cmd"
    else
        echo "  ❌ $cmd (faltando)"
        missing_deps+=("$cmd")
    fi
done

if [[ ${#missing_deps[@]} -gt 0 ]]; then
    echo
    echo "⚠️  Dependências faltando: ${missing_deps[*]}"
    echo "💡 Instale com: sudo pacman -S ${missing_deps[*]}"
else
    echo
    echo "✅ Todas as dependências estão instaladas!"
fi

echo
echo "📋 Exemplo de uso rápido:"
echo
echo "# Tornar scripts executáveis (se necessário)"
echo "chmod +x install_*.sh"
echo
echo "# Executar instalador universal"
echo "./install_fonts_universal.sh"
echo
echo "# Ou instalar fontes específicas"
echo "./install_netfont_simple.sh"
echo
echo "# Verificar fontes instaladas"
echo "fc-list | grep -i netfont"
echo

echo "🎯 Pronto para instalar fontes!"
echo "Execute um dos scripts acima para começar."
