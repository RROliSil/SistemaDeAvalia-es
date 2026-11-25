---
tags:
  - dados
  - estrutura
  - json
  - csv
---

# 🗄️ Estrutura de Dados e Arquivos

Voltar para: [[00 - Dashboard (MOC)]]

## 📁 Árvore de Diretórios Instalada (`C:\RainhaProvasExcel\`)

```text
C:\RainhaProvasExcel\
├── RainhaLauncherProvas.exe          # Executável principal autônomo
├── DOCUMENTACAO_SISTEMA.txt          # Documentação técnica e regras
├── config_admin.json                 # Senha administrativa (SHA-256)
├── config_theme.json                 # Preferência de tema (Claro / Noturno)
├── assets\
│   ├── logo_rainha.png               # Logotipo oficial Rainha
│   └── rainha.ico                    # Ícone do executável e atalho
├── Templates\                        # Modelos originais protegidos (ReadOnly)
│   ├── TESTE EXCEL - APRENDIZ .xlsx
│   └── TESTE EXCEL- AUX ADMINISTRATIVO.xlsx
└── Provas Feitas\                    # Avaliações concluídas pelos candidatos
    ├── <Nome Completo>.xlsx
    ├── banco_provas.json             # Histórico estruturado JSON
    └── historico_provas.csv          # Planilha de histórico (separador ';')
```

---

## 📊 Entidades de Dados

### 1. `ExamHistoryEntry` (`banco_provas.json` / `historico_provas.csv`)
| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `Id` | `VARCHAR(8)` | Identificador único hexadecimal |
| `NomeCandidato` | `VARCHAR(120)` | Nome completo informado pelo candidato |
| `ProvaRealizada` | `VARCHAR(150)` | Nome da avaliação selecionada |
| `ArquivoSalvo` | `VARCHAR(260)` | Nome do `.xlsx` salvo em `Provas Feitas\` |
| `DataHoraInicio` | `DATETIME` | Data e hora da abertura no Excel |
| `DataHoraFim` | `DATETIME` | Data e hora do encerramento/salvamento |
| `DuracaoSegundos` | `INT` | Duração total em segundos |
| `Status` | `VARCHAR(30)` | Status final (`CONCLUIDA_E_SALVA`) |

### 2. `AdminConfigData` (`config_admin.json`)
| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `PasswordHash` | `CHAR(64)` | Hash SHA-256 da senha do cadeado (padrão `7272`) |
| `UpdatedAt` | `DATETIME` | Data e hora da última alteração/reset |

### 3. `ThemeConfigData` (`config_theme.json`)
| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `DarkMode` | `BOOLEAN` | `true` = Modo Noturno; `false` = Modo Claro |
| `UpdatedAt` | `DATETIME` | Data e hora da última alternância de tema |

---

## 🔗 Notas Relacionadas
- [[02 - Regras de Negócio (RN01-RN11)]]
- [[05 - Guia de Instalação e Operação (Windows 10 IoT e 11)]]
