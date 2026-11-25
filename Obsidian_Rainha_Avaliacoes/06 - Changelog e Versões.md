---
tags:
  - changelog
  - versoes
  - git
---

# 📅 Changelog e Versões

Voltar para: [[00 - Dashboard (MOC)]]

## `v1.2.0` — Experiência Visual, Templates Oficiais e Obsidian
- **Templates Padrão Oficiais:** Substituição dos modelos simulados por `TESTE EXCEL - APRENDIZ .xlsx` e `TESTE EXCEL- AUX ADMINISTRATIVO.xlsx` embutidos no instalador.
- **Modo Noturno Persistente:** Alternância entre Modo Claro e Modo Noturno (`config_theme.json`) com `🌙 Modo Noturno` / `☀️ Modo Claro` no rodapé.
- **Pop-ups Temáticos (`ThemedMessageDialog`):** Substituição do `MessageBox` padrão por pop-up com suporte completo ao Modo Noturno.
- **Compatibilidade Windows 10 IoT (`ThemedTitleBar`):** Barra de título própria responsiva ao tema para garantir topo claro no Modo Claro e escuro no Modo Noturno em Windows 10 IoT.
- **Correções de Layout:** Posicionamento dinâmico dos botões do rodapé e remoção da barra de rolagem horizontal na lista de avaliações.
- **Cofre Obsidian:** Criação da pasta `Obsidian_Rainha_Avaliacoes\` com documentação interligada.

## `v1.1.0` — Segurança e Padronização de Nomenclatura
- **Cadeado Administrativo (`🔒`):** Pastas restritas protegidas por senha (padrão `7272` em SHA-256) com opção de troca e reset dentro do painel.
- **Padronização:** Substituição dos termos *"Prova"* por *"Avaliação"* e atalho renomeado para **`Avaliações - Rainha`**.

## `v1.0.0` — Lançamento Inicial
- Launcher + Instalador Único `win-x64` Self-Contained em C# (.NET 10 WinForms) com identidade visual Rainha.
- Automação COM do Excel com auto-save ao fechar (`WorkbookBeforeClose`), salvamento por troca de aba (`WorkbookSheetDeactivate`), proteção de digitação em célula (`IsWindow`) e abertura automática na 1ª aba visível (`A1`).
