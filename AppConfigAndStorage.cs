using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace RainhaLauncherProvas;

public record ExamTemplateItem(string DisplayName, string FilePath, DateTime LastModified, long SizeBytes);

public record ExamHistoryEntry(
    string Id,
    string NomeCandidato,
    string ProvaRealizada,
    string ArquivoSalvo,
    string DataHoraInicio,
    string DataHoraFim,
    int DuracaoSegundos,
    string Status
);

public static class AppConfigAndStorage
{
    public const string DefaultInstallDir = @"C:\RainhaProvasExcel";
    public const string ShortcutName = "Testes - Rainha.lnk";
    public const string LegacyShortcutName = "Provas de Excel - Rainha.lnk";
    public const string InstalledExeName = "RainhaLauncherProvas.exe";

    public static string BaseDirectory
    {
        get
        {
            // Se estiver rodando de dentro de C:\RainhaProvasExcel ou se a pasta local já tiver Templates, usa a pasta do executável
            string currentExeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Directory.Exists(Path.Combine(currentExeDir, "Templates")) ||
                string.Equals(currentExeDir, DefaultInstallDir, StringComparison.OrdinalIgnoreCase))
            {
                return currentExeDir;
            }

            if (Directory.Exists(DefaultInstallDir))
            {
                return DefaultInstallDir;
            }

            return currentExeDir;
        }
    }

    public static string TemplatesDirectory => Path.Combine(BaseDirectory, "Templates");
    public static string CompletedExamsDirectory => Path.Combine(BaseDirectory, "Provas Feitas");
    public static string HistoryJsonPath => Path.Combine(CompletedExamsDirectory, "banco_provas.json");
    public static string HistoryCsvPath => Path.Combine(CompletedExamsDirectory, "historico_provas.csv");

    /// <summary>
    /// Garante que a estrutura de pastas (Templates e Provas Feitas) e os arquivos básicos existam.
    /// </summary>
    public static void EnsureDirectoriesAndDefaultTemplates(string? targetRootDir = null)
    {
        string root = targetRootDir ?? BaseDirectory;
        string tplDir = Path.Combine(root, "Templates");
        string doneDir = Path.Combine(root, "Provas Feitas");
        string assetsDir = Path.Combine(root, "assets");

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(tplDir);
        Directory.CreateDirectory(doneDir);
        Directory.CreateDirectory(assetsDir);

        // Extrai ícone e logo embutidos
        ExtractResourceIfMissing("Rainha.logo_rainha.png", Path.Combine(assetsDir, "logo_rainha.png"));
        ExtractResourceIfMissing("Rainha.rainha.ico", Path.Combine(assetsDir, "rainha.ico"));

        // Se a pasta Templates estiver vazia, extrai os templates padrão embutidos no executável
        var existingTemplates = Directory.GetFiles(tplDir, "*.xlsx")
            .Where(f => !Path.GetFileName(f).StartsWith("~$"))
            .ToList();

        if (existingTemplates.Count == 0)
        {
            string tpl1 = Path.Combine(tplDir, "Prova de Excel para Aprendiz.xlsx");
            string tpl2 = Path.Combine(tplDir, "Prova de Excel Avançado.xlsx");

            ExtractResourceIfMissing("Rainha.Template.Prova de Excel para Aprendiz.xlsx", tpl1);
            ExtractResourceIfMissing("Rainha.Template.Prova de Excel Avançado.xlsx", tpl2);
        }

        // Marca todos os templates como Somente Leitura para garantir integridade total
        foreach (string file in Directory.GetFiles(tplDir, "*.xlsx"))
        {
            if (Path.GetFileName(file).StartsWith("~$"))
                continue;

            try
            {
                var attr = File.GetAttributes(file);
                if ((attr & FileAttributes.ReadOnly) == 0)
                {
                    File.SetAttributes(file, attr | FileAttributes.ReadOnly);
                }
            }
            catch
            {
            }
        }
    }

    private static void ExtractResourceIfMissing(string resourceName, string destinationPath)
    {
        try
        {
            if (File.Exists(destinationPath))
                return;

            var asm = Assembly.GetExecutingAssembly();
            using Stream? stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
                return;

            using FileStream fs = File.Create(destinationPath);
            stream.CopyTo(fs);
        }
        catch
        {
        }
    }

    public static Image? LoadEmbeddedLogo()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using Stream? stream = asm.GetManifestResourceStream("Rainha.logo_rainha.png");
            if (stream != null)
            {
                return Image.FromStream(stream);
            }

            string localPath = Path.Combine(BaseDirectory, "assets", "logo_rainha.png");
            if (File.Exists(localPath))
            {
                return Image.FromFile(localPath);
            }
        }
        catch
        {
        }
        return null;
    }

    public static Icon? LoadEmbeddedIcon()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using Stream? stream = asm.GetManifestResourceStream("Rainha.rainha.ico");
            if (stream != null)
            {
                return new Icon(stream);
            }

            string localPath = Path.Combine(BaseDirectory, "assets", "rainha.ico");
            if (File.Exists(localPath))
            {
                return new Icon(localPath);
            }
        }
        catch
        {
        }
        return null;
    }

    /// <summary>
    /// Lista todas as provas (.xlsx / .xlsm / .xls) disponíveis na pasta Templates.
    /// </summary>
    public static List<ExamTemplateItem> GetAvailableTemplates()
    {
        EnsureDirectoriesAndDefaultTemplates();
        var list = new List<ExamTemplateItem>();

        if (!Directory.Exists(TemplatesDirectory))
            return list;

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".xlsm", ".xls" };

        foreach (string file in Directory.GetFiles(TemplatesDirectory))
        {
            string fileName = Path.GetFileName(file);
            if (fileName.StartsWith("~$"))
                continue;

            string ext = Path.GetExtension(file);
            if (!extensions.Contains(ext))
                continue;

            var info = new FileInfo(file);
            string displayName = Path.GetFileNameWithoutExtension(file);
            list.Add(new ExamTemplateItem(displayName, file, info.LastWriteTime, info.Length));
        }

        return list.OrderBy(x => x.DisplayName).ToList();
    }

    /// <summary>
    /// Monta o caminho do arquivo a ser salvo em "Provas Feitas" com o Nome Completo do candidato.
    /// Caso já exista um arquivo com o mesmo nome, adiciona o nome da prova ou sufixo numérico
    /// para jamais sobrescrever uma prova anterior.
    /// </summary>
    public static string BuildCandidateOutputPath(string candidateFullName, string templateFilePath)
    {
        Directory.CreateDirectory(CompletedExamsDirectory);

        string sanitizedCandidate = SanitizeFileName(candidateFullName.Trim());
        string ext = Path.GetExtension(templateFilePath);
        if (string.IsNullOrEmpty(ext))
            ext = ".xlsx";

        string primaryPath = Path.Combine(CompletedExamsDirectory, $"{sanitizedCandidate}{ext}");
        if (!File.Exists(primaryPath))
            return primaryPath;

        string examName = SanitizeFileName(Path.GetFileNameWithoutExtension(templateFilePath));
        string withExamPath = Path.Combine(CompletedExamsDirectory, $"{sanitizedCandidate} - {examName}{ext}");
        if (!File.Exists(withExamPath))
            return withExamPath;

        int counter = 2;
        while (true)
        {
            string numberedPath = Path.Combine(CompletedExamsDirectory, $"{sanitizedCandidate} - {examName} ({counter}){ext}");
            if (!File.Exists(numberedPath))
                return numberedPath;
            counter++;
        }
    }

    public static string SanitizeFileName(string input)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (!invalidChars.Contains(c))
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Registra o histórico da prova realizada em JSON e CSV dentro da pasta Provas Feitas.
    /// </summary>
    public static void RecordExamHistory(string candidateName, string examName, string savedFilePath, DateTime startTime, DateTime endTime)
    {
        try
        {
            Directory.CreateDirectory(CompletedExamsDirectory);

            int durationSec = Math.Max(1, (int)Math.Round((endTime - startTime).TotalSeconds));
            var entry = new ExamHistoryEntry(
                Id: Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                NomeCandidato: candidateName,
                ProvaRealizada: examName,
                ArquivoSalvo: Path.GetFileName(savedFilePath),
                DataHoraInicio: startTime.ToString("dd/MM/yyyy HH:mm:ss"),
                DataHoraFim: endTime.ToString("dd/MM/yyyy HH:mm:ss"),
                DuracaoSegundos: durationSec,
                Status: "CONCLUIDA_E_SALVA"
            );

            // 1. Atualiza banco_provas.json
            var history = new List<ExamHistoryEntry>();
            if (File.Exists(HistoryJsonPath))
            {
                try
                {
                    string existingJson = File.ReadAllText(HistoryJsonPath, Encoding.UTF8);
                    var loaded = JsonSerializer.Deserialize<List<ExamHistoryEntry>>(existingJson);
                    if (loaded != null)
                        history = loaded;
                }
                catch { }
            }
            history.Add(entry);

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(HistoryJsonPath, JsonSerializer.Serialize(history, jsonOptions), Encoding.UTF8);

            // 2. Atualiza historico_provas.csv (compatível com Excel PT-BR usando ';')
            bool writeHeader = !File.Exists(HistoryCsvPath);
            using var sw = new StreamWriter(HistoryCsvPath, append: true, Encoding.UTF8);
            if (writeHeader)
            {
                sw.WriteLine("ID;Nome Completo;Prova Realizada;Arquivo Salvo;Inicio;Termino;Duracao (seg);Status");
            }
            sw.WriteLine($"{entry.Id};{EscapeCsv(entry.NomeCandidato)};{EscapeCsv(entry.ProvaRealizada)};{EscapeCsv(entry.ArquivoSalvo)};{entry.DataHoraInicio};{entry.DataHoraFim};{entry.DuracaoSegundos};{entry.Status}");
        }
        catch
        {
        }
    }

    private static string EscapeCsv(string val)
    {
        if (val.Contains(';') || val.Contains('"'))
        {
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
        return val;
    }

    /// <summary>
    /// Executa a instalação completa do sistema na pasta destino (padrão C:\RainhaProvasExcel)
    /// e cria o atalho na Área de Trabalho.
    /// </summary>
    public static string InstallSystemAndCreateShortcut(string? customInstallDir = null)
    {
        string targetDir = customInstallDir ?? DefaultInstallDir;
        try
        {
            EnsureDirectoriesAndDefaultTemplates(targetDir);
        }
        catch
        {
            // Fallback para Documentos caso o usuário não tenha permissão na raiz C:\
            targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RainhaProvasExcel");
            EnsureDirectoriesAndDefaultTemplates(targetDir);
        }

        string currentExePath = Environment.ProcessPath
            ?? ProcessPathFallback();
        string targetExePath = Path.Combine(targetDir, InstalledExeName);

        // Copia o executável atual para a pasta de instalação se estiver rodando de outro local
        if (!string.Equals(Path.GetFullPath(currentExePath), Path.GetFullPath(targetExePath), StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.Copy(currentExePath, targetExePath, overwrite: true);
            }
            catch
            {
                // Se já estiver em execução no destino, mantém o existente
            }
        }

        // Cria o atalho na Área de Trabalho
        CreateDesktopShortcut(targetExePath, targetDir);

        return targetDir;
    }

    public static void CreateDesktopShortcut(string targetExePath, string workingDir)
    {
        try
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // Remove o atalho antigo caso exista
            string legacyPath = Path.Combine(desktopDir, LegacyShortcutName);
            if (File.Exists(legacyPath))
            {
                try { File.Delete(legacyPath); } catch { }
            }

            string shortcutPath = Path.Combine(desktopDir, ShortcutName);
            string iconPath = Path.Combine(workingDir, "assets", "rainha.ico");

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                return;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetExePath;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = "Rainha - Sistema de Aplicação de Provas de Excel";
            if (File.Exists(iconPath))
            {
                shortcut.IconLocation = $"{iconPath},0";
            }
            else
            {
                shortcut.IconLocation = $"{targetExePath},0";
            }
            shortcut.Save();
        }
        catch
        {
        }
    }

    private static string ProcessPathFallback()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe");
    }
}
