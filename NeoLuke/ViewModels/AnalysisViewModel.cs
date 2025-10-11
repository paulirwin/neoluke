using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoLuke.Models.Analysis;
using NeoLuke.Utilities;
using Microsoft.Extensions.Logging;

namespace NeoLuke.ViewModels;

public partial class AnalysisViewModel : ViewModelBase
{
    private readonly ILogger<AnalysisViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<AnalyzerInfo> _analyzerTypes = [];

    [ObservableProperty]
    private AnalyzerInfo? _selectedAnalyzer;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<AnalyzedToken> _analyzedTokens = [];

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private string _statusMessage = "Select an analyzer and enter text to analyze";

    public AnalysisViewModel()
    {
        _logger = Program.LoggerFactory.CreateLogger<AnalysisViewModel>();
        LoadAnalyzerTypes();
    }

    /// <summary>
    /// Discovers all available analyzer types via reflection
    /// </summary>
    private void LoadAnalyzerTypes()
    {
        _logger.LogInformation("Discovering analyzer types via reflection");

        var analyzers = AnalyzerDiscovery.DiscoverAnalyzers();
        AnalyzerTypes = new ObservableCollection<AnalyzerInfo>(analyzers);

        // Select StandardAnalyzer by default if available
        SelectedAnalyzer = AnalyzerTypes
            .FirstOrDefault(a => a.SimpleName == "StandardAnalyzer")
            ?? AnalyzerTypes.FirstOrDefault();

        _logger.LogInformation("Discovered {Count} analyzer types", AnalyzerTypes.Count);
    }

    partial void OnInputTextChanged(string value)
    {
        TestAnalyzerCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedAnalyzerChanged(AnalyzerInfo? value)
    {
        TestAnalyzerCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanTestAnalyzer))]
    private async Task TestAnalyzer()
    {
        if (SelectedAnalyzer == null || string.IsNullOrWhiteSpace(InputText))
        {
            _logger.LogWarning("Cannot test analyzer: analyzer or input text is null/empty");
            return;
        }

        IsAnalyzing = true;
        StatusMessage = "Analyzing...";
        AnalyzedTokens.Clear();

        try
        {
            _logger.LogInformation("Analyzing text with {Analyzer}", SelectedAnalyzer.SimpleName);

            var model = new AnalysisModel();

            // Run analysis in background to keep UI responsive
            var tokens = await Task.Run(() =>
                model.AnalyzeText(SelectedAnalyzer.Type, InputText));

            // Update UI with results
            foreach (var token in tokens)
            {
                AnalyzedTokens.Add(token);
            }

            StatusMessage = $"Analysis complete: {tokens.Count} token(s) produced";
            _logger.LogInformation("Analysis produced {Count} tokens", tokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            StatusMessage = $"Analysis error: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private bool CanTestAnalyzer() =>
        SelectedAnalyzer != null &&
        !string.IsNullOrWhiteSpace(InputText) &&
        !IsAnalyzing;
}
