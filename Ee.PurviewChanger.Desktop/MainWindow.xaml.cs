using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Ee.PurviewChanger.Core.Models;
using Ee.PurviewChanger.Core.Services;
using Ee.PurviewChanger.Desktop.Services;
using Microsoft.Win32;

namespace Ee.PurviewChanger.Desktop;

public partial class MainWindow : Window
{
    private readonly PurviewAppOptions _options;
    private readonly LocalFileInspectionService _inspectionService = new();
    private readonly LabelChangePlanner _changePlanner = new();
    private readonly ValidationModeChangeService _changeService = new(new AuditLogService());
    private readonly Microsoft365AuthenticationService _authenticationService;
    private FileInspectionResult? _lastInspection;
    private LabelChangePreview? _lastPreview;

    public MainWindow()
    {
        InitializeComponent();

        _options = AppOptionsLoader.Load();
        _authenticationService = new Microsoft365AuthenticationService(_options.Authentication);

        TargetLabelComboBox.ItemsSource = _options.CandidateLabels;
        CapabilitiesDataGrid.ItemsSource = PurviewCapabilityCatalog.CreateDefault();

        ExecutionModeTextBlock.Text = _options.ValidationMode.Enabled
            ? "실행 모드: Validation mode (기본)"
            : "실행 모드: Live mode";
        FilePathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        RefreshAuthenticationState(AuthenticationSession.NotConfigured(_options.Authentication));
        ResetInspectionUi();
        UpdateAuditLogText(null);
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = BuildFileFilter(_options.SupportedFileExtensions)
        };

        if (dialog.ShowDialog(this) == true)
        {
            FilePathTextBox.Text = dialog.FileName;
        }
    }

    private void InspectButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _lastInspection = _inspectionService.Inspect(FilePathTextBox.Text, _options);
            _lastPreview = null;

            InspectionSummaryTextBlock.Text = _lastInspection.CurrentStateSummary;
            CapabilitySummaryTextBlock.Text = _lastInspection.CapabilitySummary;
            InspectionMessagesItemsControl.ItemsSource = _lastInspection.Messages;
            PreviewSummaryTextBlock.Text = "현재 상태 확인 후 대상 라벨을 선택해 미리보기를 실행하세요.";
            ApplyButton.IsEnabled = false;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "검사 실패", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PreviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_lastInspection is null)
        {
            MessageBox.Show(this, "먼저 현재 상태 확인을 실행하세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var selectedLabel = TargetLabelComboBox.SelectedItem as LabelDefinition;
        _lastPreview = _changePlanner.CreatePreview(_lastInspection, selectedLabel, _options.ValidationMode.Enabled);
        PreviewSummaryTextBlock.Text = _lastPreview.Summary + (string.IsNullOrWhiteSpace(_lastPreview.BlockReason)
            ? string.Empty
            : Environment.NewLine + _lastPreview.BlockReason);
        ApplyButton.IsEnabled = _lastPreview.CanApply;
    }

    private async void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_lastPreview is null)
        {
            MessageBox.Show(this, "먼저 미리보기를 실행하세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"'{_lastPreview.TargetLabel}' 라벨로 변경 요청을 기록할까요?",
            "변경 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        ApplyButton.IsEnabled = false;

        try
        {
            var result = await _changeService.ApplyAsync(
                _lastPreview,
                _options.AuditLogDirectory,
                _authenticationService.CurrentActor);

            PreviewSummaryTextBlock.Text = result.Message;
            UpdateAuditLogText(result.AuditLogPath);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "적용 실패", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ApplyButton.IsEnabled = _lastPreview.CanApply;
        }
    }

    private async void SignInButton_OnClick(object sender, RoutedEventArgs e)
    {
        SignInButton.IsEnabled = false;

        try
        {
            var session = await _authenticationService.SignInAsync(new WindowInteropHelper(this).Handle);
            RefreshAuthenticationState(session);
        }
        finally
        {
            SignInButton.IsEnabled = true;
        }
    }

    private async void SignOutButton_OnClick(object sender, RoutedEventArgs e)
    {
        var session = await _authenticationService.SignOutAsync();
        RefreshAuthenticationState(session);
    }

    private void OpenAuditFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var auditDirectory = Path.GetFullPath(_options.AuditLogDirectory);
        Directory.CreateDirectory(auditDirectory);

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = auditDirectory,
            UseShellExecute = true
        });
    }

    private void RefreshAuthenticationState(AuthenticationSession session)
    {
        AuthenticationStatusTextBlock.Text = session.StatusMessage;
        AuthenticationHintTextBlock.Text = session.Hint;
        SignOutButton.IsEnabled = session.IsSignedIn;
    }

    private void ResetInspectionUi()
    {
        InspectionSummaryTextBlock.Text = "아직 파일 상태를 확인하지 않았습니다.";
        CapabilitySummaryTextBlock.Text = $"지원 형식: {string.Join(", ", _options.SupportedFileExtensions)}";
        InspectionMessagesItemsControl.ItemsSource = Array.Empty<string>();
        PreviewSummaryTextBlock.Text = "현재 상태 확인 후 미리보기를 실행하세요.";
        ApplyButton.IsEnabled = false;
    }

    private void UpdateAuditLogText(string? auditLogPath)
    {
        AuditLogTextBlock.Text = string.IsNullOrWhiteSpace(auditLogPath)
            ? $"감사 로그 경로: {Path.GetFullPath(_options.AuditLogDirectory)}"
            : $"최근 감사 로그: {auditLogPath}";
    }

    private static string BuildFileFilter(IEnumerable<string> extensions)
    {
        var filteredExtensions = extensions
            .Select(extension => extension.StartsWith('.') ? $"*{extension}" : $"*.{extension}")
            .ToArray();

        return $"지원 파일 ({string.Join(';', filteredExtensions)})|{string.Join(';', filteredExtensions)}|모든 파일|*.*";
    }
}
