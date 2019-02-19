using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Magazyn
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        dbWarehouseEntities db;
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            db = new dbWarehouseEntities();

            ReloadData();
            
            CollectionViewSource cableViewSource = (CollectionViewSource)FindResource("cableViewSource");
            // Załaduj dane poprzez ustawienie właściwości CollectionViewSource.Source:
            // cableViewSource.Źródło = [ogólne źródło danych]

            CollectionViewSource personViewSource = (CollectionViewSource)FindResource("personViewSource");
            // Załaduj dane poprzez ustawienie właściwości CollectionViewSource.Source:
            // personViewSource.Źródło = [ogólne źródło danych]

            CollectionViewSource logViewSource = (CollectionViewSource)FindResource("logViewSource");
            // Załaduj dane poprzez ustawienie właściwości CollectionViewSource.Source:
            // personViewSource.Źródło = [ogólne źródło danych]
        }



        //zapisywanie zmian w oknie pobrania z magazynu
        private async void Button_Click_GetCable(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtQuantity.Text != "" && CableSelectCB.SelectedValue != null && PersonSelectCB != null)
                {
                    int cableId = Convert.ToInt32(CableSelectCB.SelectedValue.ToString());
                    var cable = db.Cables.Where(w => w.CableId == cableId).FirstOrDefault();
                    cable.Stock -= Convert.ToInt32(txtQuantity.Text);
                    await db.SaveChangesAsync();

                    //zapisanie logów
                    int personId = Convert.ToInt32(PersonSelectCB.SelectedValue.ToString());
                    int cableQty = Convert.ToInt32(txtQuantity.Text);
                    await LogSaveAsync(cable.CableId, personId, cableQty, false);

                    if (cable.MinStock >= (cable.Stock + cable.OrderedQty - Convert.ToInt32(txtQuantity.Text)))
                    {
                        MessageBox.Show("Uwaga stan minimalny został przekroczony!!!");
                    }
                    else
                    {
                        MessageBox.Show("Pobranie zapisane prawidłowo");
                    }
                    ClearNewCableData();
                    ClearOperationData();
                    ReloadData();
                }
                else
                {
                    MessageBox.Show("Wybierz ilość, rodzaj kabla oraz osobę");
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        //zapisywanie zmian w oknie dostaw
        private async void Button_Click_Delivery(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtQuantityDel.Text != "" && CableSelectDelCB.SelectedValue != null && PersonSelectDelCB != null)
                {
                    int cableId = Convert.ToInt32(CableSelectDelCB.SelectedValue.ToString());
                    var cable = db.Cables.Where(w => w.CableId == cableId).FirstOrDefault();
                    cable.Stock += Convert.ToInt32(txtQuantityDel.Text);
                    await db.SaveChangesAsync();

                    //zapisanie logów
                    int personId = Convert.ToInt32(PersonSelectDelCB.SelectedValue.ToString());
                    int cableQty = Convert.ToInt32(txtQuantityDel.Text);
                    await LogSaveAsync(cable.CableId, personId, cableQty, true);


                    ClearNewCableData();
                    ClearOperationData();
                    ReloadData();
                    MessageBox.Show("Dostawa dopisana prawidłowo");
                }
                else
                {
                    MessageBox.Show("Wybierz ilość, rodzaj kabla oraz osobę");
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        //połączenie wartości CB i DG z widoku pobrań
        private void CablesDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (cablesDG.SelectedItem != null )
            {
                var cable = (Cable)cablesDG.SelectedItem;
                CableSelectCB.SelectedItem = cable;
                CableImageDisplay.Source = LoadImage(cable.Image);
            }

        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CableSelectCB.SelectedValue != null )
            {
                var a = Convert.ToInt32(CableSelectCB.SelectedValue.ToString());
                var cable = db.Cables.FirstOrDefault(x => x.CableId == a);
                cablesDG.SelectedItem = cable;
            }
        }

        //połączenie wartości CB i DG z widoku dostaw
        private void CablesDelDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cablesDelDG.SelectedItem != null)
            {
                var cable = (Cable)cablesDelDG.SelectedItem;
                CableSelectDelCB.SelectedItem = cable;
                CableImageDel.Source = LoadImage(cable.Image);
            }

        }
        private void CableSelectDelCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CableSelectDelCB.SelectedValue != null)
            {
                var a = Convert.ToInt32(CableSelectDelCB.SelectedValue.ToString());
                var cable = db.Cables.FirstOrDefault(x => x.CableId == a);
                cablesDelDG.SelectedItem = cable;
            }
        }

        //przeładowanie wszystkich danych (odświeżanie po operacji)
        public void ReloadData()
        {
            cablesDG.ItemsSource = db.Cables.OrderBy(x => x.CableName).ToList();
            cablesDelDG.ItemsSource = db.Cables.OrderBy(x => x.CableName).ToList();
            CableSelectCB.ItemsSource = db.Cables.OrderBy(x => x.CableName).ToList();
            CableSelectDelCB.ItemsSource = db.Cables.OrderBy(x => x.CableName).ToList();
            PersonSelectCB.ItemsSource = db.People.OrderBy(x => x.Name).ToList();
            PersonSelectDelCB.ItemsSource = db.People.OrderBy(x => x.Name).ToList();
            CableSelectEditCB.ItemsSource = db.Cables.OrderBy(x => x.CableName).ToList();
            ClearNewCableData();
            SetData();
            LogDisplay();
        }

        //konwersja BitmapImage na tablicę byte[], do zapisu w bd
        public byte[] GetJPGFromImageControl(BitmapImage imageC)
        {
            MemoryStream memStream = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageC));
            encoder.Save(memStream);
            return memStream.ToArray();
        }

        //wyświetlanie zdjęcia (konwersja na BitmapImage
        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
        
        //wybieranie zdjęcia materiału z pliku
        private void Button_Click_OpenImage(object sender, RoutedEventArgs e)
        {
            
            OpenFileDialog openDlg = new OpenFileDialog
            {
                Filter = "JPEG | *.jpg",
                Title = "Wybierz plik JPEG"
            };
            var res = openDlg.ShowDialog();
            if (res == true)
            {
                CableImageNew.Source = new BitmapImage(new Uri(openDlg.FileName));
            }
            else
            {
                //MessageBox.Show("Wybierz plik...");
            }
        }

        //zapis edycji/nowego materiału w bd
        private async void Button_Click_SaveItem(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CableSelectEditCB.SelectedValue != null)
                {
                    int num = Convert.ToInt32(CableSelectEditCB.SelectedValue.ToString());
                    var cable = db.Cables.Where(w => w.CableId == num).FirstOrDefault();
                    cable.Image = GetJPGFromImageControl(CableImageNew.Source as BitmapImage);
                    cable.CableName = cableNameTextBox.Text;
                    cable.CablePN = cablePNTextBox.Text;
                    cable.Stock = Convert.ToInt32(stockTextBox.Text);
                    cable.MinStock = Convert.ToInt32(minStockTextBox.Text);
                    cable.OrderedQty = Convert.ToInt32(orderedQtyTextBox.Text);
                    cable.Price = Convert.ToDecimal(priceTextBox.Text);
                    cable.Desc = descTextBox.Text;
                    
                }
                else
                {
                    Cable cable = new Cable
                    {
                        Image = GetJPGFromImageControl(CableImageNew.Source as BitmapImage),
                        CableName = cableNameTextBox.Text,
                        CablePN = cablePNTextBox.Text,
                        Stock = Convert.ToInt32(stockTextBox.Text),
                        MinStock = Convert.ToInt32(minStockTextBox.Text),
                        OrderedQty = Convert.ToInt32(orderedQtyTextBox.Text),
                        Price = Convert.ToDecimal(priceTextBox.Text),
                        Desc = descTextBox.Text
                    };

                    db.Cables.Add(cable);
                }
                await db.SaveChangesAsync();
                ReloadData();
               
                MessageBox.Show("Zapisano poprawnie");
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
                
            }
        }

        //zapisanie operacji do tabeli Logs
        private async Task LogSaveAsync(int cableId, int personId, int quantity, bool delivery)
        {
            try
            {
                var log = new Log
                {
                    Date = DateTime.Now,
                    CableId = cableId,
                    PersonId = personId,
                    Quantity = quantity,
                    Delivery = delivery
                };

                db.Logs.Add(log);
                await db.SaveChangesAsync();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        //wyświetlanie informacji o pobraniach i dostawach
        private void LogDisplay()
        {
            var logs = from d in db.Logs
                       select new
                       {
                           d.LogId,
                           d.Person.Name,
                           d.Cable.CableName,
                           d.Delivery,
                           d.Quantity,
                           d.Date,
                           d.Person.Department.DeptName
                       };
            if (chkDelivery.IsChecked ?? true)
            {
                LogsDG.ItemsSource = logs.OrderBy(x => x.Date).ToList();
            }
            else
            {
                LogsDG.ItemsSource = logs
                    .Where(x => x.Delivery == false)
                    .OrderBy(x => x.Date)
                    .ToList();
            }
        }

        //resetowanie formularza
        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            ClearNewCableData();
            SetData();
        }

        //wyświetlenie danych w oknie edycji materiału
        private void CableSelectEditCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CableSelectEditCB.SelectedValue != null)
            {
                var a = Convert.ToInt32(CableSelectEditCB.SelectedValue.ToString());
                Cable cable = db.Cables.FirstOrDefault(x => x.CableId == a);
                CableImageNew.Source = LoadImage(cable.Image);
                cableNameTextBox.Text = cable.CableName;
                cablePNTextBox.Text = cable.CablePN;
                stockTextBox.Text = cable.Stock.ToString();
                minStockTextBox.Text = cable.MinStock.ToString();
                orderedQtyTextBox.Text = cable.OrderedQty.ToString();
                priceTextBox.Text = cable.Price.ToString();
                descTextBox.Text = cable.Desc;
            }

        }

        //czyszczenie danych w oknie edycji materiału
        private void ClearNewCableData()
        {
            CableSelectEditCB.SelectedItem = null;
            CableImageNew.Source = null;
            cableNameTextBox.Text = null;
            cablePNTextBox.Text = null;
            stockTextBox.Text = null;
            minStockTextBox.Text = null;
            orderedQtyTextBox.Text = null;
            priceTextBox.Text = null;
            descTextBox.Text = null;
            
        }

        //czyszczenie danych po operacji dodania/pobrania
        private void ClearOperationData()
        {
            //PersonSelectCB.SelectedItem = null;
            txtQuantity.Text = null;
            txtQuantityDel.Text = null;
        }

        private void SetData()
        {
            priceTextBox.Text = "0";
            orderedQtyTextBox.Text = "0";
            minStockTextBox.Text = "0";
            stockTextBox.Text = "0";
        }
   
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LogDisplay();
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LogDisplay();
        }

        //tworzenie raportu w pliku excel
        private void ExportToExcel()
        {
            try
            {
                LogsDG.SelectAllCells();
                LogsDG.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                ApplicationCommands.Copy.Execute(null, LogsDG);
                var resultat = (string)Clipboard.GetData(DataFormats.CommaSeparatedValue);
                var result = (string)Clipboard.GetData(DataFormats.Text);
                LogsDG.UnselectAllCells();
                StreamWriter file1 = new StreamWriter(@"C:\Report\report " + DateTime.Now.ToShortDateString() + ".xls");
                file1.WriteLine(result.Replace(',', ' '));
                file1.Close();

                MessageBox.Show("Zapisano do pliku");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        //walidacja pól

        private void StockTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CableMin_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CableOrdered_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CablePrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^\d*\.?\d?$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void TxtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TxtQuantityDel_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        //koniec walicacji


        /*
    //usówanie materiału
    private async void Delete(int cableId) {
        try
        {
            var cable = db.Cables.Where(w => w.CableId == cableId).First();
            db.Cables.Remove(cable);
            await db.SaveChangesAsync();
            ReloadData();

            MessageBox.Show("Usunięto prawidłowo");
        }
        catch(Exception e)
        {
            MessageBox.Show(e.Message);
        }
    }

        */

    }
}
