using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using Fitpad.Services;

public class DaySummariesViewModel
{
    public ObservableCollection<DaySummaryModel> Items { get; } = new ObservableCollection<DaySummaryModel>();

    public async Task LoadAsync(string userId, DateTime from, DateTime to)
    {
        Items.Clear();
        if (string.IsNullOrWhiteSpace(userId)) return;

        var fs = new FirestoreService();
        var list = await fs.GetDaySummariesAsync(userId, from, to);
        foreach (var s in list)
            Items.Add(s);
    }
}
