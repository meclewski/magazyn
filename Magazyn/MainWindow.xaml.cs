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
            
            

            System.Windows.Data.CollectionViewSource cableViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("cableViewSource")));
            // Załaduj dane poprzez ustawienie właściwości CollectionViewSource.Source:
            // cableViewSource.Źródło = [ogólne źródło danych]

            System.Windows.Data.CollectionViewSource personViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("personViewSource")));
            // Załaduj dane poprzez ustawienie właściwości CollectionViewSource.Source:
            // personViewSource.Źródło = [ogólne źródło danych]
        }

        

        //zapisywanie zmian w oknie pobrania z magazynu
        private async void Button_Click_GetCable(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtQuantity.Text != "" && CableSelectCB.SelectedValue != null)
                {
                    int num = Convert.ToInt32(CableSelectCB.SelectedValue.ToString());
                    var cable = db.Cables.Where(w => w.CableId == num).FirstOrDefault();
                    cable.Stock -= Convert.ToInt32(txtQuantity.Text);
                    await db.SaveChangesAsync();

                    
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
                    MessageBox.Show("Wybierz ilość i rodzaj kabla");
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
                if (txtQuantityDel.Text != "" && CableSelectDelCB.SelectedValue != null)
                {
                    int personId = Convert.ToInt32(PersonSelectDelCB.SelectedValue.ToString());
                    int cableQty = Convert.ToInt32(txtQuantityDel.Text);
                    int num = Convert.ToInt32(CableSelectDelCB.SelectedValue.ToString());
                    var cable = db.Cables.Where(w => w.CableId == num).FirstOrDefault();
                    cable.Stock += Convert.ToInt32(txtQuantityDel.Text);
                    await db.SaveChangesAsync();
                    await LogSave(cable.CableId, personId, cableQty, true);
                    ClearNewCableData();
                    ClearOperationData();
                    ReloadData();
                    MessageBox.Show("Dostawa dopisana prawidłowo");
                }
                else
                {
                    MessageBox.Show("Wybierz ilość i rodzaj kabla");
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
                Cable cable = (Cable)cablesDG.SelectedItem;
                CableSelectCB.SelectedItem = cable;
                CableImageDisplay.Source = LoadImage(cable.Image);
            }

        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CableSelectCB.SelectedValue != null )
            {
                var a = Convert.ToInt32(CableSelectCB.SelectedValue.ToString());
                Cable cable = db.Cables.FirstOrDefault(x => x.CableId == a);
                cablesDG.SelectedItem = cable;
            }
        }

        //połączenie wartości CB i DG z widoku dostaw
        private void CablesDelDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cablesDelDG.SelectedItem != null)
            {
                Cable cable = (Cable)cablesDelDG.SelectedItem;
                CableSelectDelCB.SelectedItem = cable;
                CableImageDel.Source = LoadImage(cable.Image);
            }

        }
        private void CableSelectDelCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CableSelectDelCB.SelectedValue != null)
            {
                var a = Convert.ToInt32(CableSelectDelCB.SelectedValue.ToString());
                Cable cable = db.Cables.FirstOrDefault(x => x.CableId == a);
                cablesDelDG.SelectedItem = cable;
            }
        }

        //przeładowanie wszystkich danych (odświeżanie po operacji)
        public void ReloadData()
        {
            cablesDG.ItemsSource = db.Cables.ToList();
            cablesDelDG.ItemsSource = db.Cables.ToList();
            CableSelectCB.ItemsSource = db.Cables.ToList();
            CableSelectDelCB.ItemsSource = db.Cables.ToList();
            PersonSelectCB.ItemsSource = db.People.ToList();
            PersonSelectDelCB.ItemsSource = db.People.ToList();
            CableSelectEditCB.ItemsSource = db.Cables.ToList();
            ClearNewCableData();
            SetData();
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

        private async Task LogSave(int cableId, int personId, int quantity, bool delivery)
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
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CablePrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        //koniec walicacji


            /*
        //usówanie materiału
        private async void Delete(int cableId) {
            try
            {
                var cable = db.Cables.Where(w => w.CableId == cableId).FirstOrDefault();
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
