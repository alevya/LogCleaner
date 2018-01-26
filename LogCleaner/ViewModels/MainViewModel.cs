using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using LogCleaner.Commands;
using LogCleaner.Configuration;

namespace LogCleaner.ViewModels
{
  internal class MainViewModel : BaseViewModel
  {
    private readonly ConfigurationApp _configurationApp;
    private const string FILE_EXTENSIONS = ".log";
    private const string FOREMAN = "Foreman7";
    private const string USBDISK = "USBDisk";
    private const string NANDFLASH = "NandFlash";
    private readonly Func<string> _openFileFunc;
    private readonly IDictionary<string, MachineDetailViewModel> _dictMachineDetails = new Dictionary<string, MachineDetailViewModel>();
    private ObservableCollection<MachineDetailViewModel> _machinesDetails;
    private bool _cursorWait;
    private bool _enabledGui = true;
    private bool _allSelected;

    public MainViewModel(Func<string> openFileFunc)
    {
      _configurationApp = new ConfigurationApp();
      _openFileFunc = openFileFunc;
      FileOpenCommand = new DelegateCommand(FileOpen);
      SaveCommand = new DelegateCommand(SaveAsync);
    }

    #region Properties

    /// <summary>
    /// Путь для сохранения лог-файлов
    /// </summary>
    public string SavePath
    {
      get
      {
        var cSavePath = _configurationApp.SavePath;

        if (string.IsNullOrEmpty(cSavePath))
        {
          return Path.GetTempPath();
        }
        var dirInfo = new DirectoryInfo(cSavePath);
        return dirInfo.Root.Exists ? cSavePath : Path.GetTempPath();
      }
      set
      {
        _configurationApp.SavePath = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Удалять лог-файлы с блоков после сохранения
    /// </summary>
    public bool RemoveFromBlocks
    {
      get { return _configurationApp.RemoveFromBlocks; }
    }


    /// <summary>
    /// Сжимать в пакет лог-файлы
    /// </summary>
    public bool Zipped
    {
      get { return _configurationApp.Zipped; }
      set
      {
        _configurationApp.Zipped = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Свойство ожидания процесса выполнения
    /// </summary>
    public bool CursorWait
    {
      get { return _cursorWait; }
      set
      {
        _cursorWait = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Возвращает или задаёт значение, указывающее, требуется ли активация элементов в пользовательском интерфейсе
    /// </summary>
    public bool EnabledGui
    {
      get { return _enabledGui; }
      set
      {
        _enabledGui = value;
        OnPropertyChanged();
      }
    }

    public bool AllSelected
    {
      get
      {
        return _allSelected;
      }
      set
      {
        _allSelected = value;
        foreach (var item in MachinesDetails)
        {
          item.IsSelected = value;
        }
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public ObservableCollection<MachineDetailViewModel> MachinesDetails
    {
      get { return _machinesDetails; }
      set
      {
        _machinesDetails = value;
        OnPropertyChanged();
      }
    }
    #endregion

    #region Command
    public ICommand FileOpenCommand { get; }
    public ICommand SaveCommand { get; }
    #endregion

    #region Methods

    /// <summary>
    /// Инициализация свойств из конфигурации
    /// </summary>
    public void InitConfig()
    {

      //CheckConfig();

      SavePath = _configurationApp.SavePath;
      Zipped = _configurationApp.Zipped;

      var machineItems = _configurationApp.MachineItems;
      if (machineItems == null || machineItems.Count == 0)
      {
        throw new ConfigException("Отсутствует элемент 'StartupMachines' в файле конфигурации");
        
      }
      _dictMachineDetails.Clear();
      foreach (MachineElement item in machineItems)
      {
        var md = new MachineDetailViewModel(item.MachineNumber, item.MachineIp);
        if (!_dictMachineDetails.ContainsKey(item.MachineNumber))
        {
          _dictMachineDetails.Add(item.MachineNumber, md);
        }
        else
        {
          _dictMachineDetails[item.MachineNumber] = md;
        }
      }

      MachinesDetails = new ObservableCollection<MachineDetailViewModel>(_dictMachineDetails.Values);
    }

    
    /// <summary>
    /// Обработчик команды при открытии файла конфигурации 
    /// </summary>
    /// <param name="obj"></param>
    private void FileOpen(object obj)
    {
      var res = _openFileFunc?.Invoke();
      string message = null;
      try
      {
        _configurationApp.Load(res);
        InitConfig();
      }
      catch (XmlException ex)
      {
        message = string.Concat("Конфигурационный файл имеет неправильный формат:", Environment.NewLine
          , ex.Message);
      }
      catch (ConfigException ex)
      {
        message = message = string.Concat("Ошибка разбора конфигурационного файла",
          Environment.NewLine,
          ex.Message);
      }
      catch (Exception ex)
      {
        message = string.Concat("При загрузке конфигурационного файла возникла исключительная ситуация!",
          Environment.NewLine,
          ex.Message);
      }

      if (message != null)
      {
        MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Hand);
      }
    }

    /// <summary>
    /// Обработчик команды при сохранении лог-файлов
    /// </summary>
    /// <param name="obj"></param>
    private async void SaveAsync(object obj)
    {
      MachinesDetailClear();
      ActionProgress();

      var listMd = MachinesDetails.Where(item => item.IsSelected).ToList();

      const int CONCURRENCY_LEVEL = 10;
      var mapTasks = new Dictionary<Task, string>();
      int nextIndex = 0;

      while (nextIndex < CONCURRENCY_LEVEL && nextIndex < listMd.Count)
      {
        var md = listMd.ElementAt(nextIndex);

        mapTasks.Add(DoDownloadAndDeleteAsync(md.Ip), md.Number);
        nextIndex++;

      }
      while (mapTasks.Count > 0)
      {
        string numberValue = String.Empty;
        try
        {
          Task resultTask = await Task.WhenAny(mapTasks.Keys).ConfigureAwait(true);
          mapTasks.TryGetValue(resultTask, out numberValue);
          mapTasks.Remove(resultTask);
          await resultTask;

          UpdateMachineDetailItem(numberValue, "Успешно!", true);
        }
        catch (Exception exc)
        {
          UpdateMachineDetailItem(numberValue, exc.Message.TrimEnd(), false);
        }

        if (nextIndex >= listMd.Count)
          continue;

        var md = listMd.ElementAt(nextIndex);
        mapTasks.Add(DoDownloadAndDeleteAsync(md.Ip), md.Number);
        nextIndex++;
      }

      ActionCompleted();
    }

    /// <summary>
    /// Запуск загрузки файлов из удаленного ресурса <paramref name="address"/>
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private async Task DoDownloadAndDeleteAsync(string address)
    {
      var logFolderName = BuildLogFolderName();
      var rootPathTrg = Path.Combine(SavePath, address);

      var ftpLoader = new FtpClient.FtpClient(address, _configurationApp.RequestTimeout, _configurationApp.ReadWriteTimeout);

      var listUsbDisk = await ftpLoader.ListFilesAsync(BuildRemotePath(USBDISK));
      var listNandFlash = await ftpLoader.ListFilesAsync(BuildRemotePath(NANDFLASH));

      var logsUsbDisk = listUsbDisk.Where(file => file.EndsWith(FILE_EXTENSIONS));
      var logsNandDisk = listNandFlash.Where(file => file.EndsWith(FILE_EXTENSIONS));

      if (Zipped)
      {
        Directory.CreateDirectory(rootPathTrg);
        var pathPackage = Path.Combine(rootPathTrg, logFolderName);
        using (var package = Package.Open(pathPackage + ".zip", FileMode.Create, FileAccess.ReadWrite))
        {
          foreach (var fileSrc in logsUsbDisk)
          {
            var pathSrc = Path.Combine(BuildRemotePath(USBDISK), fileSrc);
            var partPackageName = Path.Combine(USBDISK, fileSrc);
            await DownloadZipAsync(pathSrc, package, partPackageName, ftpLoader);
          }
          foreach (var fileSrc in logsNandDisk)
          {
            var pathSrc = Path.Combine(BuildRemotePath(NANDFLASH), fileSrc);
            var partPackageName = Path.Combine(NANDFLASH, fileSrc);
            await DownloadZipAsync(pathSrc, package, partPackageName, ftpLoader);
          }
        }
      }
      else
      {
        var diUsbDisk = Directory.CreateDirectory(Path.Combine(rootPathTrg, logFolderName, USBDISK));
        var diNandFlash = Directory.CreateDirectory(Path.Combine(rootPathTrg, logFolderName, NANDFLASH));

        foreach (var fileSrc in logsUsbDisk)
        {
          var pathSrc = Path.Combine(BuildRemotePath(USBDISK), fileSrc);
          var pathTrg = Path.Combine(diUsbDisk.FullName, fileSrc);
          await DownloadAsync(pathSrc, pathTrg, ftpLoader);
        }
        foreach (var fileSrc in logsNandDisk)
        {
          var pathSrc = Path.Combine(BuildRemotePath(NANDFLASH), fileSrc);
          var pathTrg = Path.Combine(diNandFlash.FullName, fileSrc);
          await DownloadAsync(pathSrc, pathTrg, ftpLoader);
        }
      }
    }

    /// <summary>
    /// Асинхронная загрузка файла средством <paramref name="ftpLoader"/>
    /// </summary>
    /// <param name="pathSrc">путь удаленного источника</param>
    /// <param name="pathTrg">локальный путь для сохранения</param>
    /// <param name="ftpLoader">объект, используемый для работы по ftp-протоколу</param>
    /// <returns></returns>
    private async Task DownloadAsync(string pathSrc, string pathTrg, FtpClient.FtpClient ftpLoader)
    {
      var result = await ftpLoader.DownloadFileAsync(pathSrc, pathTrg);

      if (result == FtpStatusCode.ClosingData && RemoveFromBlocks)
      {
        await ftpLoader.DeleteFileAsync(pathSrc);
      }
    }

    /// <summary>
    /// Асинхронная загрузка файла средством <paramref name="ftpLoader"/>  с сохранением в zip-пакет
    /// </summary>
    /// <param name="pathSrc">путь удаленного источника</param>
    /// <param name="zipPackage">объект, представляющий контейнер для хранения нескольких объектов</param>
    /// <param name="partPackageName">имя части пакета</param>
    /// <param name="ftpLoader">объект, используемый для работы по ftp-протоколу</param>
    /// <returns></returns>
    private async Task DownloadZipAsync(string pathSrc, Package zipPackage, string partPackageName, FtpClient.FtpClient ftpLoader)
    {
      var partUri = PackUriHelper.CreatePartUri(new Uri(partPackageName, UriKind.Relative));
      var packagePart = zipPackage.CreatePart(partUri, System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Normal);
      var result = await ftpLoader.DownloadFileAsync(pathSrc, packagePart.GetStream());

      if (result == FtpStatusCode.ClosingData && RemoveFromBlocks)
      {
        await ftpLoader.DeleteFileAsync(pathSrc);
      }
    }

    /// <summary>
    /// Создание конечной части имени пути удаленного ресурса 
    /// </summary>
    /// <param name="nameRootPath"></param>
    /// <returns></returns>
    private string BuildRemotePath(string nameRootPath)
    {
      return Path.Combine(nameRootPath, FOREMAN);
    }

    /// <summary>
    /// Создание имени папки для лог-файлов
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private string BuildLogFolderName(string name = "")
    {
      return string.Format("log_{0}", DateTime.Today.ToShortDateString());
    }

    private void UpdateMachineDetailItem(string number, string message, bool success)
    {
      _dictMachineDetails.TryGetValue(number, out MachineDetailViewModel md);
      if (md != null)
      {
        md.Message = message;
        if (!success)
          md.IsSelected = false;
      }
    }

    private void MachinesDetailClear()
    {
      foreach (var item in _dictMachineDetails.Values)
      {
        item.Message = string.Empty;
      }
    }

    private void ActionProgress()
    {
      EnabledGui = false;
      CursorWait = true;

    }

    private void ActionCompleted()
    {
      EnabledGui = true;
      CursorWait = false;

    }
    #endregion
  }

}
