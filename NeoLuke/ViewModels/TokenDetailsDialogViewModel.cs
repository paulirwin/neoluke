using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeoLuke.Models.Analysis;

namespace NeoLuke.ViewModels;

public partial class TokenDetailsDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _tokenTerm = string.Empty;

    [ObservableProperty]
    private ObservableCollection<TokenAttributeDetail> _attributeDetails = [];

    public TokenDetailsDialogViewModel()
    {
    }

    public TokenDetailsDialogViewModel(AnalyzedToken token)
    {
        TokenTerm = token.Term;
        AttributeDetails = new ObservableCollection<TokenAttributeDetail>(token.DetailedAttributes);
    }
}
