using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using WK.Libraries.BetterFolderBrowserNS;

namespace Find_Replace
{
    public class MViewModel : INotifyPropertyChanged
    {
        public MViewModel()
        {
            _textDirDialog = "C:\\";
            _masks = "*.";
            _searchText = "";
            _replaceText = "";
            _resultProperty = new ObservableCollection<string>();
            Result = new List<string>();
            _cancel = false;
            _progressBarValue = 0;
            _inProcess = false;
            _select = 0;
            Replace = false;

        }

        private string _textDirDialog;
        private string _masks;
        private string _searchText;
        private string _replaceText;
        private ObservableCollection<string> _resultProperty;
        private bool _cancel;
        private decimal _progressBarValue;
        private bool _inProcess;
        private int _select;

        public List<string> Result { get; set; }
        public bool Replace { get; set; }

        public string TextDirDialog { get { return _textDirDialog; } set { _textDirDialog = value; OnPropertyChanged(nameof(TextDirDialog)); } }
        public string Masks { get { return _masks; } set { _masks = value; OnPropertyChanged(nameof(Masks)); } }
        public string SearchText { get { return _searchText; } set { _searchText = value; OnPropertyChanged(nameof(SearchText)); } }
        public string ReplaceText { get { return _replaceText; } set { _replaceText = value; OnPropertyChanged(nameof(ReplaceText)); } }

        public ObservableCollection<string> ResultProperty
        {
            get { return _resultProperty; }
            set { _resultProperty = value; OnPropertyChanged(nameof(ResultProperty)); }
        }
        public bool Cancel { get { return _cancel; } set { _cancel = value; OnPropertyChanged(nameof(Cancel)); } }
        public decimal ProgressBarValue { get { return _progressBarValue; } set { _progressBarValue = value; OnPropertyChanged(nameof(ProgressBarValue)); } }
        public bool InProcess { get { return _inProcess; } set { _inProcess = value; OnPropertyChanged(nameof(InProcess)); } }
        public int Select { get { return _select; } set { _select = value; OnPropertyChanged(nameof(Select)); } }

        private ICommand _openDirDialogCommand;
        private ICommand _searchCommand;
        private ICommand _searchAndReplaceCommand;
        private ICommand _cancelCommand;
        private ICommand _openFolderCommand;

        public ICommand OpenDirDialogCommand { get { return _openDirDialogCommand ?? (_openDirDialogCommand = new RelayCommand(OpenDirDialogCommandExe)); } }
        public ICommand SearchCommand { get { return _searchCommand ?? (_openDirDialogCommand = new RelayCommand(SearchCommandExe, () => !InProcess)); } }
        public ICommand SearchAndReplaceCommand { get { return _searchAndReplaceCommand ?? (_openDirDialogCommand = new RelayCommand(SearchAndReplaceCommandExe)); } }
        public ICommand CancelCommand { get { return _cancelCommand ?? (_cancelCommand = new RelayCommand(CancelCommandExe)); } }
        public ICommand OpenFolderCommand { get { return _openFolderCommand ?? (_openFolderCommand = new RelayCommand(OpenFolderCommandExe)); } }

        private void OpenDirDialogCommandExe()
        {
            var betterFolderBrowser = new BetterFolderBrowser
            {
                Title = "Выберите папку...",
                RootFolder = TextDirDialog,
                Multiselect = true
            };
            if (betterFolderBrowser.ShowDialog() == DialogResult.OK)
            {
                TextDirDialog = "";
                string[] selectedFolders = betterFolderBrowser.SelectedFolders;
                for (int i = 0; i < selectedFolders.Length; i++)
                {
                    TextDirDialog += selectedFolders[i] + (i == selectedFolders.Length - 1 ? "" : ";");
                }
            }
        }

        private void SearchCommandExe()
        {
            var bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            bw.DoWork += Bw_DoWork;
            bw.ProgressChanged += Bw_ProgressChanged;
            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
            bw.RunWorkerAsync(this);
            bw.RunWorkerCompleted += (sender, args) =>
            {
                ResultProperty.Clear();
                for (int i = 0; i < Result.Count; i++)
                {
                    ResultProperty.Add(Result[i]);
                }
                Cancel = false;
                Replace = false;
            };
        }

        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var bw = (BackgroundWorker)sender;

            if (e.Error == null)
            {
                var result = e.Result;
            }
            else
            {
                var ex = e.Error;
            }
        }

        private void Bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarValue = e.ProgressPercentage;
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var bw = (BackgroundWorker)sender;
            var vm = (MViewModel)e.Argument;
            vm.Result.Clear();
            var directs = TextDirDialog.Split(';');

            List<string> files = new List<string>();

            for (int i = 0; i < directs.Length; i++)
            {
                foreach (var VARIABLE in Directory.GetFiles(directs[i], Masks, SearchOption.AllDirectories))
                {
                    files.Add(VARIABLE);
                }

            }

            if (SearchText == "")
            {
                for (int i = 0; i < files.Count; i++)
                {
                    vm.Result.Add(files[i]);
                }
                return;
            }

            for (int i = 0; i < files.Count && !Cancel; i++)
            {
                var arrDataFile = File.ReadAllText(files[i]);
                string[] arrWord = arrDataFile.Split(' ', '\n', '\\', '\r', '\t');
                string[] arrSearchWord = SearchText.Split(' ', '\n', '\\', '\r', '\t');
                int k = 0;
                int j;
                for (j = 0; j < arrWord.Length && k < arrSearchWord.Length; j++)
                {
                    if (arrWord[j] == "")
                    {
                        continue;
                    }
                    if (arrWord[j] == arrSearchWord[k])
                    {
                        k++;
                    }
                    else
                    {
                        j -= k;
                        k = 0;
                    }
                }
                if (k == arrSearchWord.Length) vm.Result.Add(files[i]);
                bw.ReportProgress((int)Math.Round((double)(((i + 1.0) / files.Count) * 100)));

                if (Replace)
                {
                    var replace = arrDataFile.Replace(SearchText, ReplaceText);
                    File.WriteAllText(files[i], replace);
                }
            }
            bw.ReportProgress(0, "Конец");
        }

        private void SearchAndReplaceCommandExe()
        {
            Replace = true;
            SearchCommandExe();
        }

        private void CancelCommandExe()
        {
            Cancel = true;
        }

        private void OpenFolderCommandExe()
        {
            Process PrFolder = new Process();
            ProcessStartInfo psi = new ProcessStartInfo();
            string file = ResultProperty[Select];
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Normal;
            psi.FileName = "explorer";
            psi.Arguments = @"/n, /select, " + file;
            PrFolder.StartInfo = psi;
            PrFolder.Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
                return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            _execute.Invoke();
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            if (parameter is T arg)
            {
                _execute.Invoke(arg);
            }
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is T arg)
            {
                return _canExecute?.Invoke(arg) ?? true;
            }
            return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }
    }
}
