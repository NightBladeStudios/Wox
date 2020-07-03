namespace Wox.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using Infrastructure.Logger;
    using Infrastructure.UserSettings;
    using NLog;
    using Plugin;

    public class ResultsViewModel : BaseModel
    {
        public class ResultCollection : Collection<ResultViewModel>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            #region MonoBehaviour

            public void Update(List<ResultViewModel> newItems, CancellationToken token)
            {
                if (token.IsCancellationRequested) return;

                Clear();
                foreach (var i in newItems)
                {
                    if (token.IsCancellationRequested) break;
                    Add(i);
                }

                if (CollectionChanged != null)
                    // wpf use directx / double buffered already, so just reset all won't cause ui flickering
                    CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            #endregion

            #region Public

            public void RemoveAll()
            {
                Clear();
                if (CollectionChanged != null) CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            #endregion
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static readonly DependencyProperty FormattedTextProperty = DependencyProperty.RegisterAttached(
            "FormattedText",
            typeof(Inline),
            typeof(ResultsViewModel),
            new PropertyMetadata(null, FormattedTextPropertyChanged));

        public ResultCollection Results { get; }

        public int MaxHeight => MaxResults * 50;

        public int SelectedIndex { get; set; }

        public ResultViewModel SelectedItem { get; set; }
        public Thickness Margin { get; set; }
        public Visibility Visibility { get; set; } = Visibility.Collapsed;

        public int Count => Results.Count;


        private int MaxResults => _settings?.MaxResultsToShow ?? 6;
        private readonly object _collectionLock = new object();

        private readonly Settings _settings;

        public ResultsViewModel()
        {
            Results = new ResultCollection();
            BindingOperations.EnableCollectionSynchronization(Results, _collectionLock);
        }

        public ResultsViewModel(Settings settings) : this()
        {
            _settings = settings;
            _settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_settings.MaxResultsToShow)) OnPropertyChanged(nameof(MaxHeight));
            };
        }

        #region Public

        public void SelectNextResult()
        {
            SelectedIndex = NewIndex(SelectedIndex + 1);
        }

        public void SelectPrevResult()
        {
            SelectedIndex = NewIndex(SelectedIndex - 1);
        }

        public void SelectNextPage()
        {
            SelectedIndex = NewIndex(SelectedIndex + MaxResults);
        }

        public void SelectPrevPage()
        {
            SelectedIndex = NewIndex(SelectedIndex - MaxResults);
        }

        public void SelectFirstResult()
        {
            SelectedIndex = NewIndex(0);
        }

        public void Clear()
        {
            Results.RemoveAll();
        }

        public void AddResults(List<Result> newRawResults, string resultId)
        {
            var token = new CancellationTokenSource().Token;
            var updates = new List<ResultsForUpdate>
            {
                new ResultsForUpdate(newRawResults, resultId, token)
            };
            AddResults(updates);
        }

        /// <summary>
        /// To avoid deadlock, this method should not called from main thread
        /// </summary>
        public void AddResults(List<ResultsForUpdate> updates)
        {
            var updatesNotCanceled = updates.Where(u => !u.Token.IsCancellationRequested);

            CancellationToken token;
            try
            {
                token = updates.Select(u => u.Token).Distinct().First();
            }
            catch (InvalidOperationException e)
            {
                Logger.WoxError("more than one not canceled query result in same batch processing", e);
                return;
            }

            // https://stackoverflow.com/questions/14336750
            lock (_collectionLock)
            {
                var newResults = NewResults(updates, token);
                Logger.WoxTrace($"newResults {newResults.Count}");
                Results.Update(newResults, token);
            }

            if (Results.Count > 0)
            {
                Margin = new Thickness {Top = 8};
                SelectedIndex = 0;
            }
            else
            {
                Margin = new Thickness {Top = 0};
            }
        }

        public static void SetFormattedText(DependencyObject textBlock, IList<int> value)
        {
            textBlock.SetValue(FormattedTextProperty, value);
        }

        public static Inline GetFormattedText(DependencyObject textBlock)
        {
            return (Inline) textBlock.GetValue(FormattedTextProperty);
        }

        #endregion

        #region Private

        private int NewIndex(int i)
        {
            var n = Results.Count;
            if (n > 0)
            {
                i = (n + i) % n;
                return i;
            }

            // SelectedIndex returns -1 if selection is empty.
            return -1;
        }

        private List<ResultViewModel> NewResults(List<ResultsForUpdate> updates, CancellationToken token)
        {
            if (token.IsCancellationRequested) return Results.ToList();
            var newResults = Results.ToList();
            if (updates.Count > 0)
            {
                if (token.IsCancellationRequested) return Results.ToList();
                var resultsFromUpdates = updates.SelectMany(u => u.Results).ToList();

                if (token.IsCancellationRequested) return Results.ToList();
                newResults.RemoveAll(r => updates.Any(u => u.ID == r.Result.PluginID));

                if (token.IsCancellationRequested) return Results.ToList();
                var vm = resultsFromUpdates.Select(r => new ResultViewModel(r));
                newResults.AddRange(vm);

                if (token.IsCancellationRequested) return Results.ToList();
                var sorted = newResults.OrderByDescending(r => r.Result.Score).Take(MaxResults * 4).ToList();

                return sorted;
            }

            return Results.ToList();
        }

        private static void FormattedTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as TextBlock;
            if (textBlock == null) return;

            var inline = (Inline) e.NewValue;

            textBlock.Inlines.Clear();
            if (inline == null) return;

            textBlock.Inlines.Add(inline);
        }

        #endregion
    }
}