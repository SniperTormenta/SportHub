using System.Collections.ObjectModel;

namespace SportHubBase.Services.Results.Data
{
    public class SwissResultsData : ResultsData
    {
        public ObservableCollection<SwissResultRow> Rows { get; set; } = new ObservableCollection<SwissResultRow>();
        public ObservableCollection<int> HeaderNumbers { get; set; } = new ObservableCollection<int>();
    }
}
