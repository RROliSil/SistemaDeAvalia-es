---
tags:
  - operacao
  - instalacao
  - windows-iot
  - rh
---

# 🛠️ Guia de Instalação e Operação (Windows 10 IoT e Windows 11)

Voltar para: [[00 - Dashboard (MOC)]]

## 1️⃣ Como Instalar em um Novo Computador
1. Copie apenas o arquivo **`Instalador_Rainha_Provas.exe`** para a máquina destino (requer apenas o **Microsoft Excel** instalado; não precisa instalar `.NET`, Python ou outras dependências).
2. Dê **duplo clique** em `Instalador_Rainha_Provas.exe`.
3. Clique em **`🚀 Instalar Agora (1 Clique)`**.
4. O instalador criará `C:\RainhaProvasExcel\`, extrairá os 2 modelos oficiais (`TESTE EXCEL - APRENDIZ .xlsx` e `TESTE EXCEL- AUX ADMINISTRATIVO.xlsx`) e criará o atalho **`Avaliações - Rainha`** na Área de Trabalho.

> [!tip] Instalação Silenciosa (TI)
> Para implantação via script ou rede:
> ```powershell
> .\Instalador_Rainha_Provas.exe --silent-install
> ```

---

## 2️⃣ Como Acessar as Avaliações Feitas e Templates (`🔒`)
1. No rodapé do Launcher, clique no ícone de **cadeado vermelho (`🔒`)**.
2. Digite a senha administrativa (padrão inicial: **`7272`**) e clique em **`🔓 Acessar`**.
3. Dentro do painel administrativo você pode:
   - Abrir **`📁 Abrir Pasta de Avaliações Feitas`** (onde ficam os `.xlsx` com o nome dos candidatos e o `historico_provas.csv`).
   - Abrir **`📂 Pasta de Templates`** (para adicionar ou atualizar modelos `.xlsx`).
   - Alterar a senha do cadeado ou clicar em **`🔄 Resetar Senha para o Padrão (7272)`**.
4. Após adicionar um novo modelo na pasta `Templates`, clique em **`🔄 Atualizar Lista`** no rodapé do Launcher.

---

## 3️⃣ Atualizando Estações Windows 10 IoT
Para atualizar uma máquina já instalada para a versão mais recente:
- Execute o novo `Instalador_Rainha_Provas.exe` **ou** substitua o arquivo `C:\RainhaProvasExcel\RainhaLauncherProvas.exe`.
- O tema (`ThemedTitleBar` e `ThemedMessageDialog`) funciona de forma idêntica no **Windows 10 IoT**, **Windows 10** e **Windows 11**.
