using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace RainhaLauncherProvas;

/// <summary>
/// Interface de eventos IDispatch do Excel.Application (AppEvents).
/// </summary>
[ComImport]
[ComVisible(true)]
[Guid("00024413-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IExcelAppEvents
{
    [DispId(1570)]
    void WorkbookBeforeClose(
        [In, MarshalAs(UnmanagedType.IDispatch)] object Wb,
        [In, Out, MarshalAs(UnmanagedType.VariantBool)] ref bool Cancel);

    [DispId(1571)]
    void WorkbookBeforeSave(
        [In, MarshalAs(UnmanagedType.IDispatch)] object Wb,
        [In, MarshalAs(UnmanagedType.VariantBool)] bool SaveAsUI,
        [In, Out, MarshalAs(UnmanagedType.VariantBool)] ref bool Cancel);
}

/// <summary>
/// Receptor COM (Sink) que intercepta o fechamento do Excel para garantir o salvamento automático
/// na pasta de Provas Feitas mesmo se o candidato apenas clicar no "X" sem salvar.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(IExcelAppEvents))]
public class ExcelAppEventSink : IExcelAppEvents
{
    private readonly ExcelSessionManager _manager;

    public ExcelAppEventSink(ExcelSessionManager manager)
    {
        _manager = manager;
    }

    public void WorkbookBeforeClose(object Wb, ref bool Cancel)
    {
        _manager.OnWorkbookBeforeClose(Wb, ref Cancel);
    }

    public void WorkbookBeforeSave(object Wb, bool SaveAsUI, ref bool Cancel)
    {
        // Se o candidato tentar usar "Salvar Como" para outro lugar, garantimos que salve também no arquivo oficial da prova
        if (SaveAsUI)
        {
            try
            {
                dynamic workbook = Wb;
                workbook.Save();
            }
            catch
            {
                // Ignora erros caso não esteja pronto
            }
        }
    }
}

public class ExcelSessionManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private const int SW_MAXIMIZE = 3;

    private dynamic? _excelApp;
    private dynamic? _workbook;
    private IConnectionPoint? _connectionPoint;
    private int _adviseCookie;
    private ExcelAppEventSink? _eventSink;
    private System.Threading.Timer? _monitorTimer;
    private uint _excelProcessId;
    private int _sessionClosedSignaled;

    public string CandidateName { get; }
    public string ExamName { get; }
    public string TemplatePath { get; }
    public string TargetFilePath { get; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }

    public event Action? SessionCompleted;

    public ExcelSessionManager(string candidateName, string examName, string templatePath, string targetFilePath)
    {
        CandidateName = candidateName;
        ExamName = examName;
        TemplatePath = templatePath;
        TargetFilePath = targetFilePath;
    }

    /// <summary>
    /// Copia o template original mantendo sua integridade, remove atributo ReadOnly da cópia,
    /// e abre o novo arquivo no Excel monitorando o salvamento e o fechamento.
    /// </summary>
    public void StartSession()
    {
        StartTime = DateTime.Now;

        // 1. Garante que a pasta de destino (Provas Feitas) exista
        string? targetDir = Path.GetDirectoryName(TargetFilePath);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // 2. Protege o template original (marca como Somente Leitura se ainda não estiver)
        if (File.Exists(TemplatePath))
        {
            try
            {
                var tplAttr = File.GetAttributes(TemplatePath);
                if ((tplAttr & FileAttributes.ReadOnly) == 0)
                {
                    File.SetAttributes(TemplatePath, tplAttr | FileAttributes.ReadOnly);
                }
            }
            catch
            {
                // Se não puder alterar atributo do template, segue normalmente
            }
        }

        // 3. Copia o template para a pasta "Provas Feitas" com o Nome Completo do candidato
        File.Copy(TemplatePath, TargetFilePath, overwrite: true);

        // Remove o atributo ReadOnly da cópia do candidato para que possa ser editada e salva livremente
        var copyAttr = File.GetAttributes(TargetFilePath);
        if ((copyAttr & FileAttributes.ReadOnly) != 0)
        {
            File.SetAttributes(TargetFilePath, copyAttr & ~FileAttributes.ReadOnly);
        }

        // 4. Inicia uma instância isolada do Microsoft Excel via COM
        Type? excelType = Type.GetTypeFromProgID("Excel.Application");
        if (excelType == null)
        {
            throw new InvalidOperationException("O Microsoft Excel não foi encontrado instalado neste computador.");
        }

        _excelApp = Activator.CreateInstance(excelType)
            ?? throw new InvalidOperationException("Não foi possível iniciar o Microsoft Excel.");

        _excelApp.Visible = true;
        _excelApp.DisplayAlerts = true;

        // Conecta o interceptador COM (AppEvents) para salvar automaticamente antes de fechar
        AttachComEvents(_excelApp);

        // Abre a cópia da prova já localizada na pasta "Provas Feitas"
        _workbook = _excelApp.Workbooks.Open(
            TargetFilePath,
            UpdateLinks: 0,
            ReadOnly: false);

        // Sempre posiciona na primeira aba visível da planilha e no topo (A1)
        ActivateFirstVisibleWorksheet();

        // Maximiza e traz a janela do Excel para a frente
        try
        {
            IntPtr hwnd = (IntPtr)(int)_excelApp.Hwnd;
            if (hwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out _excelProcessId);
                ShowWindow(hwnd, SW_MAXIMIZE);
                SetForegroundWindow(hwnd);
            }
        }
        catch
        {
            // Ignora falha ao maximizar
        }

        // 5. Inicia monitoramento para detectar quando o candidato fechou o Excel
        _monitorTimer = new System.Threading.Timer(MonitorExcelCallback, null, 800, 800);
    }

    /// <summary>
    /// Ativa a primeira aba visível da pasta de trabalho, rola a barra inferior de abas
    /// para o início e seleciona a célula A1 no topo da planilha.
    /// </summary>
    private void ActivateFirstVisibleWorksheet()
    {
        if (_workbook == null || _excelApp == null)
            return;

        try
        {
            const int xlSheetVisible = -1;
            const int xlFirst = 1;

            int sheetCount = (int)_workbook.Worksheets.Count;
            dynamic? firstVisibleSheet = null;

            for (int i = 1; i <= sheetCount; i++)
            {
                dynamic ws = _workbook.Worksheets.Item(i);
                try
                {
                    if ((int)ws.Visible == xlSheetVisible)
                    {
                        firstVisibleSheet = ws;
                        break;
                    }
                }
                catch
                {
                    firstVisibleSheet = ws;
                    break;
                }
            }

            if (firstVisibleSheet != null)
            {
                firstVisibleSheet.Activate();

                try
                {
                    // Rola a barra inferior de abas totalmente para a esquerda (primeira aba)
                    _excelApp.ActiveWindow.ScrollWorkbookTabs(Position: xlFirst);
                }
                catch { }

                try
                {
                    // Posiciona a visualização no topo esquerdo da aba e seleciona A1
                    _excelApp.ActiveWindow.ScrollRow = 1;
                    _excelApp.ActiveWindow.ScrollColumn = 1;
                    firstVisibleSheet.Range("A1").Select();
                }
                catch { }
            }
        }
        catch
        {
            // Ignora caso a planilha esteja protegida contra seleção de células
        }
    }

    private void AttachComEvents(object excelComObject)
    {
        try
        {
            if (excelComObject is IConnectionPointContainer container)
            {
                Guid appEventsGuid = new Guid("00024413-0000-0000-C000-000000000046");
                container.FindConnectionPoint(ref appEventsGuid, out _connectionPoint);
                if (_connectionPoint != null)
                {
                    _eventSink = new ExcelAppEventSink(this);
                    _connectionPoint.Advise(_eventSink, out _adviseCookie);
                    Console.WriteLine($"COM_ADVISE_OK: cookie={_adviseCookie}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"COM_ADVISE_ERR: {ex.Message}");
        }
    }

    internal void OnWorkbookBeforeClose(object wbObj, ref bool cancel)
    {
        try
        {
            dynamic wb = wbObj;
            // Força o salvamento automático das respostas do candidato antes do Excel fechar!
            wb.Save();
            wb.Saved = true;
        }
        catch
        {
            // Se o Excel estiver em algum estado transitório, tenta via workbook principal
            try
            {
                _workbook?.Save();
                if (_workbook != null)
                {
                    _workbook.Saved = true;
                }
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Chamado caso o candidato clique no botão "Finalizar e Salvar Prova" diretamente no Launcher.
    /// </summary>
    public void SaveAndCloseSession()
    {
        try
        {
            if (_workbook != null)
            {
                _workbook.Save();
                _workbook.Saved = true;
                _workbook.Close(true);
            }
        }
        catch
        {
        }

        try
        {
            if (_excelApp != null)
            {
                _excelApp.DisplayAlerts = false;
                _excelApp.Quit();
            }
        }
        catch
        {
        }

        SignalSessionClosed();
    }

    private void MonitorExcelCallback(object? state)
    {
        if (_sessionClosedSignaled == 1)
            return;

        bool isStillOpen = false;

        try
        {
            if (_excelApp != null)
            {
                // Verifica se o processo do Excel ainda existe
                if (_excelProcessId != 0)
                {
                    try
                    {
                        var proc = System.Diagnostics.Process.GetProcessById((int)_excelProcessId);
                        if (proc.HasExited)
                        {
                            isStillOpen = false;
                            SignalSessionClosed();
                            return;
                        }
                    }
                    catch
                    {
                        SignalSessionClosed();
                        return;
                    }
                }

                // Verifica se ainda há algum Workbook aberto na instância do Excel
                int count = (int)_excelApp.Workbooks.Count;
                if (count > 0)
                {
                    isStillOpen = true;
                }
                else
                {
                    // O candidato fechou a planilha mas a janela vazia do Excel ficou aberta:
                    // fechamos a instância vazia do Excel automaticamente.
                    try
                    {
                        _excelApp.DisplayAlerts = false;
                        _excelApp.Quit();
                    }
                    catch
                    {
                    }
                    isStillOpen = false;
                }
            }
        }
        catch (COMException comEx)
        {
            // RPC_E_CALL_REJECTED (0x80010001) ocorre quando o candidato está digitando dentro de uma célula!
            // Nesse caso o Excel AINDA ESTÁ ABERTO e em uso.
            const int RPC_E_CALL_REJECTED = unchecked((int)0x80010001);
            const int VBA_E_IGNORE = unchecked((int)0x800AC472);
            if (comEx.ErrorCode == RPC_E_CALL_REJECTED || comEx.ErrorCode == VBA_E_IGNORE)
            {
                isStillOpen = true;
            }
            else
            {
                // Qualquer outro erro RPC (como RPC_S_SERVER_UNAVAILABLE) significa que o Excel foi fechado
                isStillOpen = false;
            }
        }
        catch
        {
            isStillOpen = false;
        }

        if (!isStillOpen)
        {
            SignalSessionClosed();
        }
    }

    private void SignalSessionClosed()
    {
        if (Interlocked.Exchange(ref _sessionClosedSignaled, 1) == 0)
        {
            EndTime = DateTime.Now;
            CleanupCom();
            SessionCompleted?.Invoke();
        }
    }

    private void CleanupCom()
    {
        try
        {
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }
        catch { }

        try
        {
            if (_connectionPoint != null && _adviseCookie != 0)
            {
                _connectionPoint.Unadvise(_adviseCookie);
                _adviseCookie = 0;
            }
        }
        catch { }

        try
        {
            if (_workbook != null && Marshal.IsComObject(_workbook))
            {
                Marshal.ReleaseComObject(_workbook);
            }
        }
        catch { }
        _workbook = null;

        try
        {
            if (_excelApp != null && Marshal.IsComObject(_excelApp))
            {
                Marshal.ReleaseComObject(_excelApp);
            }
        }
        catch { }
        _excelApp = null;
    }

    public void Dispose()
    {
        CleanupCom();
    }
}
