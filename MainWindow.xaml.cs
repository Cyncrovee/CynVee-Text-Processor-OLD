using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;
using WinRT.Interop;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Text;
using Windows.Storage.Search;
using Windows.Storage.Pickers.Provider;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI;
using Microsoft.UI.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CynVee_Text_Processor
{
    public sealed partial class MainWindow : Window
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public MainWindow()
        {
            this.InitializeComponent();

            // Default font is Segoe UI Variable
            List<string> items = new List<string>
            {
                // Variable fonts
                "Segoe UI Variable", "Bahnschrift",
                // Sans-serif fonts
                "Segoe UI", "Arial", "Calibri", "Consolas", "Selawik",
                // Serif fonts
                "Cambria", "Courier New", "Georgia", "Times New Roman"
            };
            // Default font size is 14
            int[] fontSizeArray = Enumerable.Range(1, 256).ToArray();

            fontSelectionBox.ItemsSource = items;
            fontSizeSelectionBox.ItemsSource = fontSizeArray;

            string testFont = noteBox.FontFamily.Source.ToString();
            string testFontSize = noteBox.FontSize.ToString();
            System.Diagnostics.Debug.WriteLine(testFont);
            System.Diagnostics.Debug.WriteLine(testFontSize);



            if (noteBox.IsSpellCheckEnabled == true)
            {
                spellcheckBtn.IsChecked = true;
            }
            else if (noteBox.IsSpellCheckEnabled == false)
            {
                spellcheckBtn.IsChecked = false;
            }
            if (noteBox.IsTextPredictionEnabled == true)
            {
                textPredictionBtn.IsChecked = true;
            }
            else if (noteBox.IsTextPredictionEnabled == false)
            {
                textPredictionBtn.IsChecked = false;
            }

            noteBox.TextAlignment = TextAlignment.Left;
            alignLeftBtn.IsChecked = true;
        }

        // Declare variables
        bool underline = false;

        string openWorkspace = null;
        string openFile = null;


        // Methods for "File" menubar buttons
        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        // Methods for "Edit" menubar buttons
        private void undoBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.Document.Undo();
        }
        private void redoBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.Document.Redo();
        }
        private void cutBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.Document.Selection.Cut();
        }
        private void copyBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.Document.Selection.Copy();
        }

        // Methods for "Help" menubar buttons
        private async void aboutBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog aboutDialog = new ContentDialog
            {
                Title = "CynVee Text Processor",
                Content = "This is a simple rich text editing app made with the WinUI 3 Framework",
                CloseButtonText = "Ok",
            };

            aboutDialog.XamlRoot = this.Content.XamlRoot;
            ContentDialogResult result = await aboutDialog.ShowAsync();
        }


        // Methods for top left ui buttons
        private async void quickSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (openFile != null)
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(openFile);
                using (FileStream fs = new FileStream(openFile, FileMode.Open))
                {
                    fs.SetLength(0);
                }
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    noteBox.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                }
                refreshList();
            }
            else
            {
                saveBtn_Click(sender, e);
                refreshList();
            }
        }
        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text File", new List<string>() { ".rtf" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    noteBox.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                }

                // Let Windows know that we're finished changing the file so the
                // other app can update the remote version of the file
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
                openFile = file.Path;
            }
            refreshList();

        }
        private async void openBtn_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new()
            {
                ViewMode = PickerViewMode.List,
                FileTypeFilter = { ".rtf" },
            };

            nint windowHandle = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(fileOpenPicker, windowHandle);

            StorageFile file = await fileOpenPicker.PickSingleFileAsync();

            if (file != null)
            {
                if (file != null)
                {
                    using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                        await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        noteBox.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                    }
                }
                openFile = file.Path;
            }
        }
        private async void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog checkClear = new ContentDialog
            {
                Title = "Are You Sure You Want to Clear All Text?",
                Content = "This will delete ALL text from the text box, though you may be able to undo this action with CTRL + Z.",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
            };

            checkClear.XamlRoot = this.Content.XamlRoot;
            ContentDialogResult result = await checkClear.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                noteBox.Document.SetText((Microsoft.UI.Text.TextSetOptions)Windows.UI.Text.TextSetOptions.FormatRtf, null);
            }
        }
        private async void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog checkDelete = new ContentDialog
            {
                Title = "Are You Sure You Want to Delete The Selected File?",
                Content = "This will delete the currently selected file in the file list.",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
            };

            checkDelete.XamlRoot = this.Content.XamlRoot;
            ContentDialogResult result = await checkDelete.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (noteList.SelectedItem != null)
                {
                    noteBox.Document.SetText((Microsoft.UI.Text.TextSetOptions)Windows.UI.Text.TextSetOptions.FormatRtf, null);
                    string delNote = noteList.SelectedItem.ToString();

                    File.Delete(delNote);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Delete Operation cancelled.");
                }
            }
            refreshList();
        }

        // Methods for comboboxes
        private void fontSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedFont = fontSelectionBox.SelectedItem.ToString();
            noteBox.FontFamily = new FontFamily(selectedFont);
        }
        private void fontSizeSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedFontSize = (int)fontSizeSelectionBox.SelectedItem;
            noteBox.FontSize = selectedFontSize;
        }


        // Methods for top right ui buttons
        private void textPredictionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (noteBox.IsTextPredictionEnabled == true)
            {
                noteBox.IsTextPredictionEnabled = false;
                textPredictionBtn.IsChecked = false;
            }
            else if (noteBox.IsTextPredictionEnabled == false)
            {
                noteBox.IsTextPredictionEnabled = true;
                textPredictionBtn.IsChecked = true;
            }
        }
        private void spellcheckBtn_Click(object sender, RoutedEventArgs e)
        {
            if (noteBox.IsSpellCheckEnabled == true)
            {
                noteBox.IsSpellCheckEnabled = false;
                spellcheckBtn.IsChecked = false;
            }
            else if (noteBox.IsSpellCheckEnabled == false)
            {
                noteBox.IsSpellCheckEnabled = true;
                spellcheckBtn.IsChecked = true;
            }
        }
        private void alignLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.TextAlignment = TextAlignment.Left;

            alignLeftBtn.IsChecked = true;
            alignCenterBtn.IsChecked = false;
            alignRightBtn.IsChecked = false;
        }
        private void alignCenterBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.TextAlignment = TextAlignment.Center;

            alignLeftBtn.IsChecked = false;
            alignCenterBtn.IsChecked = true;
            alignRightBtn.IsChecked = false;
        }
        private void alignRightBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.TextAlignment = TextAlignment.Right;

            alignLeftBtn.IsChecked = false;
            alignCenterBtn.IsChecked = false;
            alignRightBtn.IsChecked = true;
        }


        // Methods for right side ui buttons
        private void boldBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;

            if (noteBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On)
            {
                boldBtn.IsChecked = true;
            }
            else if (noteBox.Document.Selection.CharacterFormat.Bold == FormatEffect.Off)
            {
                boldBtn.IsChecked = false;
            }

        }
        private void italicBtn_Click(object sender, RoutedEventArgs e)
        {
            noteBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;

            if (noteBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                italicBtn.IsChecked = true;
            }
            else if (noteBox.Document.Selection.CharacterFormat.Italic == FormatEffect.Off)
            {
                italicBtn.IsChecked = false;
            }

        }
        private void underlineBtn_Click(object sender, RoutedEventArgs e)
        {
            if (underline == false)
            {
                noteBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
                underline = true;
                underlineBtn.IsChecked = true;
            }
            else if (underline == true)
            {
                noteBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
                underline = false;
                underlineBtn.IsChecked = false;
            }
        }


        // noteList functions
        private async void noteList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (noteList.SelectedItem != null)
            {
                string findNote = noteList.SelectedItem.ToString();
                StorageFile storageFile = await StorageFile.GetFileFromPathAsync(findNote);
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    noteBox.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                }
                openFile = findNote;
            }
            else
            {
                //do nothing
            }
        }
        private async void atozSortButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(openWorkspace);
                noteList.Items.Clear();
                IReadOnlyList<StorageFile> sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByName);
                foreach (StorageFile file in sortedItems)
                {
                    noteList.Items.Add(file.Path);
                }
                localSettings.Values["sortMethod"] = 1;
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Could not refresh list.");
            }
        }
        private async void bydateSortButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(openWorkspace);
                noteList.Items.Clear();
                IReadOnlyList<StorageFile> sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
                foreach (StorageFile file in sortedItems)
                {
                    noteList.Items.Add(file.Path);
                }
                localSettings.Values["sortMethod"] = 2;
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Could not refresh list.");
            }
        }

        private async void refreshList()
        {
            try
            {
                string folderPath = (string)localSettings.Values["lastOpenFolder"];
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                noteList.Items.Clear();
                IReadOnlyList<StorageFile> sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByName);
                switch(localSettings.Values["sortMethod"])
                {
                    case 1:
                        sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByName);
                        break;
                    case 2:
                        sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
                        break;
                    case null:
                        sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
                        break;

                }
                foreach (StorageFile file in sortedItems)
                {
                    noteList.Items.Add(file.Path);
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Could not refresh list");
            }
        }


        // functions for buttons above noteList
        private void refreshListButton_Click(object sender, RoutedEventArgs e)
        {
            refreshList();
        }
        private async void fetchFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = localSettings.Values["lastOpenFolder"];
            if (folderPath != null)
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync((string)folderPath);
                if (folder != null)
                {
                    noteList.Items.Clear();
                    IReadOnlyList<StorageFile> sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
                    switch (localSettings.Values["sortMethod"])
                    {
                        case 1:
                            sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByName);
                            break;
                        case 2:
                            sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
                            break;
                        case null:
                            sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
                            break;
                    }
                    
                    foreach (StorageFile file in sortedItems)
                    {
                        noteList.Items.Add(file.Path);
                    }
                    openWorkspace = folder.Path;
                    localSettings.Values["lastOpenFolder"] = folder.Path;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Open folder operation cancelled.");
                }
            }
        }
        private async void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            nint windowHandle = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(folderPicker, windowHandle);

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                noteList.Items.Clear();
                IReadOnlyList<StorageFile> sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
                foreach (StorageFile file in sortedItems)
                {
                    noteList.Items.Add(file.Path);
                }
                openWorkspace = folder.Path;
                localSettings.Values["lastOpenFolder"] = folder.Path;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Open folder operation cancelled.");
            }
        }

        private async void lightThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog changeTheme = new ContentDialog
            {
                Title = "To apply theme change, a restart of the app is required",
                Content = "You will have to resart the app manually",
                PrimaryButtonText = "Ok",
                CloseButtonText = "Cancel"
            };

            changeTheme.XamlRoot = this.Content.XamlRoot;
            ContentDialogResult result = await changeTheme.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                localSettings.Values["themeSetting"] = 0;
            }
        }

        private async void darkThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["themeSetting"] = 1;
            ContentDialog changeTheme = new ContentDialog
            {
                Title = "To apply theme change, a restart of the app is required",
                Content = "You will have to resart the app manually",
                PrimaryButtonText = "Ok",
                CloseButtonText = "Cancel"
            };

            changeTheme.XamlRoot = this.Content.XamlRoot;
            ContentDialogResult result = await changeTheme.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                localSettings.Values["themeSetting"] = 1;
            }
        }
    }
}
