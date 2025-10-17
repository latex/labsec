#!/bin/bash

# Script para configurar fontes via interface gráfica no Omarchy
# Abre as configurações de fonte do sistema

set -e

echo "🎨 Abrindo configurações de fonte do sistema..."

# Detectar ambiente desktop
DESKTOP_ENV="${XDG_CURRENT_DESKTOP:-$DESKTOP_SESSION}"

case "$DESKTOP_ENV" in
    *GNOME*|*gnome*)
        echo "🖥️  Ambiente GNOME detectado"
        echo "📝 Abrindo Configurações do Sistema..."
        
        # Abrir configurações de fonte do GNOME
        gnome-control-center fonts 2>/dev/null || \
        gnome-control-center appearance 2>/dev/null || \
        gnome-control-center 2>/dev/null &
        
        echo "✅ Configurações abertas"
        echo
        echo "💡 Para aumentar as fontes no GNOME:"
        echo "   1. Vá para 'Aparência' ou 'Fontes'"
        echo "   2. Ajuste o 'Tamanho da Fonte'"
        echo "   3. Ou use 'Fator de Escala' para aumentar tudo"
        ;;
        
    *XFCE*|*xfce*)
        echo "🖥️  Ambiente XFCE detectado"
        echo "📝 Abrindo Configurações do Sistema..."
        
        # Abrir configurações do XFCE
        xfce4-settings-manager 2>/dev/null || \
        xfce4-appearance-settings 2>/dev/null &
        
        echo "✅ Configurações abertas"
        echo
        echo "💡 Para aumentar as fontes no XFCE:"
        echo "   1. Vá para 'Aparência'"
        echo "   2. Clique na aba 'Fontes'"
        echo "   3. Ajuste o tamanho das fontes"
        ;;
        
    *KDE*|*kde*|*plasma*)
        echo "🖥️  Ambiente KDE detectado"
        echo "📝 Abrindo Configurações do Sistema..."
        
        # Abrir configurações do KDE
        systemsettings5 2>/dev/null || \
        kcmshell5 fonts 2>/dev/null &
        
        echo "✅ Configurações abertas"
        echo
        echo "💡 Para aumentar as fontes no KDE:"
        echo "   1. Vá para 'Aparência' > 'Fontes'"
        echo "   2. Ajuste o tamanho das fontes"
        echo "   3. Ou use 'Escala da Interface'"
        ;;
        
    *)
        echo "⚠️  Ambiente desktop não detectado"
        echo "📝 Tentando abrir configurações genéricas..."
        
        # Tentar abrir configurações genéricas
        xfce4-settings-manager 2>/dev/null || \
        gnome-control-center 2>/dev/null || \
        systemsettings5 2>/dev/null || \
        echo "❌ Não foi possível abrir configurações automaticamente"
        ;;
esac

echo
echo "🔧 Alternativas via linha de comando:"
echo
echo "📏 Aumentar fontes rapidamente:"
echo "   ./quick_font_increase.sh 14"
echo
echo "🎛️  Configurar via menu interativo:"
echo "   ./increase_system_fonts_omarchy.sh"
echo
echo "📋 Ver fontes disponíveis:"
echo "   fc-list | head -20"
echo
echo "🔄 Atualizar cache de fontes:"
echo "   fc-cache -fv"
