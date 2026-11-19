using System;
using System.Diagnostics;
using System.IO;
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
    [DispId(1562)]
    void WorkbookSheetDeactivate(
        [In, MarshalAs(UnmanagedType.IDispatch)] object Wb,
        [In, MarshalAs(UnmanagedType.IDispatch)] object Sh);

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
/// Receptor COM (Sink) que intercepta eventos do Excel para garantir o salvamento automático
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

    public void WorkbookSheetDeactivate(object Wb, object Sh)
    {
        // Salva silenciosamente a cada troca de aba (questão) como backup preventivo
        try
        {
            dynamic workbook = Wb;
            workbook.Save();
        }
        catch
        {
        }
    }

    public void WorkbookBeforeClose(object Wb, ref bool Cancel)
    {
        _manager.OnWorkbookBeforeClose(Wb, ref Cancel);
    }

    public void WorkbookBeforeSave(object Wb, bool SaveAsUI, ref bool Cancel)
    {
        if (SaveAsUI)
        {
            try
            {
                dynamic workbook = Wb;
                workbook.Save();
            }
            catch
            {
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

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const int SW_MAXIMIZE = 3;
    private const uint WM_CLOSE = 0x0010;

    private dynamic? _excelApp;
    private dynamic? _workbook;
    private IConnectionPoint? _connectionPoint;
    private int _adviseCookie;
    private ExcelAppEventSink? _eventSink;
    private System.Threading.Timer? _monitorTimer;
    private IntPtr _excelHwnd;
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

        // Maximiza e traz a janela do Excel para a frente, capturando Hwnd e ProcessId
        try
        {
            long hwndVal = Convert.ToInt64(_excelApp.Hwnd);
            _excelHwnd = new IntPtr(hwndVal);
            if (_excelHwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(_excelHwnd, out _excelProcessId);
                ShowWindow(_excelHwnd, SW_MAXIMIZE);
                SetForegroundWindow(_excelHwnd);
            }
        }
        catch
        {
        }

        // 5. Inicia monitoramento para detectar exclusivamente quando o candidato fechou o Excel
        _monitorTimer = new System.Threading.Timer(MonitorExcelCallback, null, 1000, 1000);
    }

    /// <summary>
    /// Ativa a primeira aba visível da pasta de trabalho, rola a barra inferior de abas
    /// para o início e seleciona a célula A1 no topo da planilha.
    /// </summary>
    private void ActivateFirstVisibleWorksheet()
    {
        var wb = _workbook;
        var app = _excelApp;
        if (wb == null || app == null)
            return;

        try
        {
            const int xlSheetVisible = -1;
            const int xlFirst = 1;

            int sheetCount = (int)wb.Worksheets.Count;
            dynamic? firstVisibleSheet = null;

            for (int i = 1; i <= sheetCount; i++)
            {
                dynamic ws = wb.Worksheets.Item(i);
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
                    app.ActiveWindow.ScrollWorkbookTabs(Position: xlFirst);
                }
                catch { }

                try
                {
                    app.ActiveWindow.ScrollRow = 1;
                    app.ActiveWindow.ScrollColumn = 1;
                    firstVisibleSheet.Range("A1").Select();
                }
                catch { }

                try
                {
                    wb.Save();
                }
                catch { }
            }
        }
        catch
        {
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
                }
            }
        }
        catch
        {
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
            try
            {
                if (_workbook != null)
                {
                    _workbook.Save();
                    _workbook.Saved = true;
                }
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Chamado caso o candidato clique no botão "Finalizar e Salvar Prova Agora" diretamente no Launcher.
    /// </summary>
    public void SaveAndCloseSession()
    {
        bool savedDirectly = false;
        try
        {
            if (_workbook != null)
            {
                _workbook.Save();
                _workbook.Saved = true;
                _workbook.Close(true);
                savedDirectly = true;
            }
        }
        catch
        {
            // Se o candidato estava com o cursor piscando dentro de uma célula (Modo de Edição),
            // enviamos WM_CLOSE para a janela do Excel confirmar a célula e acionar o WorkbookBeforeClose.
            if (_excelHwnd != IntPtr.Zero && IsWindow(_excelHwnd))
            {
                PostMessage(_excelHwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                return;
            }
        }

        if (savedDirectly)
        {
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
    }

    private bool IsExcelWindowOrProcessAlive()
    {
        if (_excelHwnd != IntPtr.Zero)
        {
            return IsWindow(_excelHwnd);
        }

        if (_excelProcessId != 0)
        {
            try
            {
                using var proc = Process.GetProcessById((int)_excelProcessId);
                return !proc.HasExited;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private void MonitorExcelCallback(object? state)
    {
        if (_sessionClosedSignaled == 1)
            return;

        // 1. Se a janela nativa do Excel ou o processo já tiver sido encerrado, finaliza a sessão
        if (!IsExcelWindowOrProcessAlive())
        {
            SignalSessionClosed();
            return;
        }

        // 2. A janela do Excel está aberta. Verifica se o candidato fechou apenas a planilha
        // deixando a janela do Excel vazia (Workbooks.Count == 0).
        // IMPORTANTE: Se o candidato estiver digitando em uma célula, o Excel rejeita chamadas COM
        // lançando RuntimeBinderException ou COMException. Enquanto a janela do Excel existir,
        // qualquer exceção COM significa que o candidato está digitando/editando — NUNCA que fechou!
        try
        {
            var app = _excelApp;
            if (app == null)
                return;

            int count = (int)app.Workbooks.Count;
            if (count == 0)
            {
                // O candidato fechou a planilha (que já foi salva pelo WorkbookBeforeClose),
                // então fechamos a janela vazia do Excel e concluímos a prova.
                try
                {
                    app.DisplayAlerts = false;
                    app.Quit();
                }
                catch
                {
                }
                SignalSessionClosed();
            }
        }
        catch
        {
            // O candidato está digitando dentro de uma célula ou com menu/fórmula em edição no Excel.
            // Como IsExcelWindowOrProcessAlive() é true, a prova continua normalmente em andamento!
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
