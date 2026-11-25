---
tags:
  - visao-geral
  - identidade-visual
  - rainha
---

# 🎨 Visão Geral e Identidade Visual

Voltar para: [[00 - Dashboard (MOC)]]

## 🎯 Objetivo
Permitir que candidatos realizem avaliações práticas de **Microsoft Excel** com apenas um duplo clique no atalho **`Avaliações - Rainha`**, sem risco de:
1. Sobrescrever ou danificar os modelos originais em `Templates\`.
2. Perder as respostas ao clicar no **"X"** do Excel sem clicar em Salvar.
3. Acessar avaliações de outros candidatos ou alterar templates sem autorização.

---

## 🎨 Paleta de Cores Oficial (Rainha — Desde 2007)

### ☀️ Modo Claro (`ThemePalette.Light`)
| Token | Hex | RGB | Uso |
| :--- | :--- | :--- | :--- |
| `BrandRed` | `#DF462A` | `223, 70, 42` | Títulos principais, botões de ação e cadeado |
| `BrandOrange` | `#F18024` | `241, 128, 36` | Destaques, bordas ativas e faixa tricolor |
| `BrandYellow` | `#FCC32B` | `252, 195, 43` | Gradiente da faixa tricolor Rainha |
| `WindowBg` | `#FFF9F5` | `255, 249, 245` | Fundo creme suave das janelas |
| `HeaderBg` | `#FFFFFF` | `255, 255, 255` | Cabeçalho e barra de título própria |
| `TextPrimary` | `#2D2522` | `45, 37, 34` | Textos principais |

### 🌙 Modo Noturno (`ThemePalette.Dark`)
| Token | Hex | RGB | Uso |
| :--- | :--- | :--- | :--- |
| `BrandRed` | `#E85438` | `232, 84, 56` | Botões de ação e títulos em destaque |
| `BrandOrange` | `#F68E36` | `246, 142, 54` | Bordas e destaques |
| `WindowBg` | `#1A1615` | `26, 22, 21` | Fundo escuro principal |
| `HeaderBg` | `#231E1C` | `35, 30, 28` | Cabeçalho e barra de título própria |
| `CardBg` | `#2A2422` | `42, 36, 34` | Cartões de avaliação e painel ativo |
| `TextPrimary` | `#F5EEEA` | `245, 238, 234` | Textos principais claros |

> [!tip] Persistência de Tema
> A escolha entre Modo Claro e Modo Noturno é salva em `config_theme.json` e aplicada em toda a interface, incluindo barra de título (`ThemedTitleBar`), cartões e pop-ups (`ThemedMessageDialog`).

---

## 🔗 Notas Relacionadas
- [[02 - Regras de Negócio (RN01-RN11)]]
- [[03 - Arquitetura e Fluxo do Sistema]]
