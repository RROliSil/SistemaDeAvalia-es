---
tags:
  - regras
  - negocio
  - rainha
---

# 📜 Regras de Negócio (RN01 a RN11)

Voltar para: [[00 - Dashboard (MOC)]]

> [!note] Origem
> Todas as regras abaixo estão implementadas diretamente no código C# e sincronizadas com `DOCUMENTACAO_SISTEMA.txt`.

### `RN01` — Listagem Automática de Avaliações
Ao abrir o Launcher, o sistema varre `Templates\` e lista todos os arquivos `.xlsx`, `.xlsm` e `.xls`, ignorando temporários iniciados por `~$`.

### `RN02` — Identificação Obrigatória do Candidato
Ao selecionar uma avaliação, o modal `CandidateNameDialog` exige o **Nome Completo** (mínimo de 3 caracteres válidos), higienizando caracteres inválidos para nome de arquivo no Windows.

### `RN03` — Proteção e Integridade do Template Original
Os templates originais em `Templates\` são marcados como **Somente Leitura (`ReadOnly`)** e **nunca são abertos diretamente**. Antes de abrir o Excel, o sistema copia o modelo para `Provas Feitas\<Nome Completo>.xlsx`.

### `RN04` — Prevenção contra Sobrescrita
Se já existir um arquivo com o mesmo nome na pasta `Provas Feitas\`, o sistema nomeia automaticamente como `<Nome Completo> - <Nome da Avaliação>.xlsx` ou adiciona sufixo numérico `(2)`, `(3)`, preservando todas as entregas anteriores.

### `RN05` — Salvamento Automático ao Fechar e na Troca de Abas
Via automação COM (`ExcelSessionManager.cs` + `IExcelAppEvents`):
- **Troca de aba (`WorkbookSheetDeactivate`):** salva silenciosamente em segundo plano a cada mudança de questão.
- **Fechamento no "X" (`WorkbookBeforeClose`):** executa `Workbook.Save()` e marca `Workbook.Saved = true`, salvando as respostas sem exibir o diálogo *"Deseja salvar as alterações?"*.
- **Digitação em célula:** o monitoramento usa `IsWindow(_excelHwnd)` (Win32) para nunca confundir o Modo de Edição de célula com fechamento do Excel.

### `RN06` — Fechamento pelo Botão do Launcher
Enquanto o Excel está aberto, o Launcher exibe o painel **Avaliação em Andamento** com cronômetro e o botão **`✅ Finalizar e Salvar Avaliação Agora`** (que envia `WM_CLOSE` caso uma célula ainda esteja em edição).

### `RN07` — Instalação Única e Atalho `Avaliações - Rainha`
O executável `Instalador_Rainha_Provas.exe` instala tudo em `C:\RainhaProvasExcel\`, extrai os 2 templates oficiais padrão, cria `Provas Feitas\` e gera o atalho **`Avaliações - Rainha.lnk`** na Área de Trabalho (removendo atalhos legados).

### `RN08` — Registro Automático de Histórico
Ao concluir cada avaliação, grava ID, Nome Completo, Avaliação, Arquivo Salvo, Início, Término, Duração (segundos) e Status em `banco_provas.json` e `historico_provas.csv`.

### `RN09` — Abertura Obrigatória na Primeira Aba Visível
Ao abrir o Excel, `ActivateFirstVisibleWorksheet()` ativa a primeira aba visível (`Worksheets[1]`), rola a barra de abas para a esquerda (`ScrollWorkbookTabs Position: xlFirst`) e posiciona na célula `A1`.

### `RN10` — Controle de Acesso por Senha (`🔒`)
Os botões **`📁 Abrir Pasta de Avaliações Feitas`** e **`📂 Pasta de Templates`** ficam protegidos dentro do botão **`🔒`** no rodapé:
- Senha inicial padrão: **`7272`** (hash SHA-256 em `config_admin.json`).
- Dentro do painel desbloqueado é possível alterar a senha ou resetá-la para `7272`.

### `RN11` — Modo Noturno Persistente
Botão **`🌙 Modo Noturno` / `☀️ Modo Claro`** no rodapé alterna toda a interface (incluindo barra de título `ThemedTitleBar` compatível com Windows 10 IoT e pop-ups `ThemedMessageDialog`), persistindo em `config_theme.json`.

---

## 🔗 Notas Relacionadas
- [[03 - Arquitetura e Fluxo do Sistema]]
- [[04 - Estrutura de Dados e Arquivos]]
