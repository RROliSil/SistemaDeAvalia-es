using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

internal static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Modo de instalação silenciosa via linha de comando
        if (args.Contains("--silent-install", StringComparer.OrdinalIgnoreCase))
        {
            string installedDir = AppConfigAndStorage.InstallSystemAndCreateShortcut();
            Console.WriteLine($"INSTALADO_COM_SUCESSO:{installedDir}");
            return 0;
        }

        // Modo de autoteste automatizado para validação fim a fim com o Excel real
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAutomatedSelfTest();
        }

        // Se o executável tiver "Instalador" no nome ou receber "--install", abre a tela do Instalador Único
        string currentExeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "");
        if (args.Contains("--install", StringComparer.OrdinalIgnoreCase) ||
            currentExeName.Contains("Instalador", StringComparison.OrdinalIgnoreCase))
        {
            Application.Run(new InstallerForm());
            return 0;
        }

        // Inicialização normal do Launcher: garante pastas, templates e atalho "Testes - Rainha" no Desktop
        AppConfigAndStorage.EnsureDirectoriesAndDefaultTemplates();
        string exePath = Environment.ProcessPath ?? Path.Combine(AppConfigAndStorage.BaseDirectory, AppConfigAndStorage.InstalledExeName);
        string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string desktopShortcut = Path.Combine(desktopDir, AppConfigAndStorage.ShortcutName);
        string legacyShortcut = Path.Combine(desktopDir, AppConfigAndStorage.LegacyShortcutName);

        if (!File.Exists(desktopShortcut) || File.Exists(legacyShortcut))
        {
            AppConfigAndStorage.CreateDesktopShortcut(exePath, AppConfigAndStorage.BaseDirectory);
        }

        Application.Run(new MainForm());
        return 0;
    }

    private static int RunAutomatedSelfTest()
    {
        try
        {
            AppConfigAndStorage.EnsureDirectoriesAndDefaultTemplates();
            var templates = AppConfigAndStorage.GetAvailableTemplates();
            if (templates.Count == 0)
            {
                Console.Error.WriteLine("FALHA: Nenhum template encontrado.");
                return 1;
            }

            var chosen = templates[0];
            string testCandidate = "Candidato Teste Automatizado Rainha";
            string expectedPath = Path.Combine(AppConfigAndStorage.CompletedExamsDirectory, $"{testCandidate}.xlsx");
            if (File.Exists(expectedPath))
                File.Delete(expectedPath);

            string outputPath = AppConfigAndStorage.BuildCandidateOutputPath(testCandidate, chosen.FilePath);

            using var session = new ExcelSessionManager(testCandidate, chosen.DisplayName, chosen.FilePath, outputPath);
            session.StartSession();

            // Obtém o workbook via reflexão na sessão para simular edição pelo candidato e fechamento SEM clicar em salvar
            var wbField = typeof(ExcelSessionManager).GetField("_workbook", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var appField = typeof(ExcelSessionManager).GetField("_excelApp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            dynamic wb = wbField!.GetValue(session)!;
            dynamic app = appField!.GetValue(session)!;

            bool completed = false;
            session.SessionCompleted += () => completed = true;

            dynamic sheet = wb.Worksheets.Item(1);
            sheet.Range("E6").Value2 = "RESPOSTA_CANDIDATO_SALVA_AUTOMATICAMENTE";

            // Simula o clique no botão "X" da janela do Excel (WM_CLOSE = 0x0010) com message pump ativo (como no WinForms)
            IntPtr hwnd = (IntPtr)(int)app.Hwnd;
            PostMessage(hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!completed && sw.ElapsedMilliseconds < 5000)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(50);
            }

            AppConfigAndStorage.RecordExamHistory(testCandidate, chosen.DisplayName, outputPath, session.StartTime, DateTime.Now);

            // Reabre o arquivo em Provas Feitas para comprovar que a alteração foi salva automaticamente
            // e que o template original permanece intacto!
            Type excelType = Type.GetTypeFromProgID("Excel.Application")!;
            dynamic verifyApp = Activator.CreateInstance(excelType)!;
            verifyApp.Visible = false;
            verifyApp.DisplayAlerts = false;

            string savedVal = "";
            string templateVal = "";
            try
            {
                dynamic savedWb = verifyApp.Workbooks.Open(outputPath, ReadOnly: true);
                savedVal = Convert.ToString(savedWb.Worksheets.Item(1).Range("E6").Value2) ?? "";
                savedWb.Close(false);

                dynamic tplWb = verifyApp.Workbooks.Open(chosen.FilePath, ReadOnly: true);
                templateVal = Convert.ToString(tplWb.Worksheets.Item(1).Range("E6").Value2) ?? "";
                tplWb.Close(false);
            }
            finally
            {
                verifyApp.Quit();
            }

            // Remove o arquivo de teste após validar para deixar a pasta Provas Feitas limpa
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            if (savedVal == "RESPOSTA_CANDIDATO_SALVA_AUTOMATICAMENTE" && string.IsNullOrEmpty(templateVal))
            {
                Console.WriteLine("SELF_TEST_OK: Arquivo salvo automaticamente ao fechar e template original 100% intacto!");
                return 0;
            }

            Console.Error.WriteLine($"SELF_TEST_FAIL: savedVal='{savedVal}', templateVal='{templateVal}'");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SELF_TEST_EXCEPTION: {ex}");
            return 3;
        }
    }
}
