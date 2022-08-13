using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Newtonsoft.Json;
using System.Windows.Threading;
using System.Windows.Forms;
using Windows.UI.Notifications;

namespace EbayListings
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string jsonPath = System.IO.Directory.GetCurrentDirectory() + "..\\..\\..\\resources\\data.json";
        private List<Listing> Listings { get; set; }

        public static List<Listing> readJSON(string filePath)
        {
             if (!File.Exists(filePath)) File.Create(filePath).Dispose();
            string json = File.ReadAllText(filePath);
            List<Listing> ls = JsonConvert.DeserializeObject<List<Listing>>(json);
            if (ls == null) ls = new List<Listing>();
            return ls;
        }

        public static void writeJSON(string filePath, List<Listing> listings)
        {
            string json = JsonConvert.SerializeObject(listings);
            File.WriteAllText(filePath, json);
        }

        public static bool CheckDuplicates(List<Listing> list, Listing key) 
        {
            foreach (Listing l in list) 
            {
                if (l.Name == key.Name && l.EndTime == key.EndTime) {
                    return true;
                }
            }
            return false;
        }

        public MainWindow()
        {
            InitializeComponent();

            Listings = readJSON(jsonPath);
            writeJSON(jsonPath, Listings);

            DataContext = this;

            NotifyIcon nIcon = new NotifyIcon();
            nIcon.Icon = Properties.Resources.main;
            nIcon.Visible = true;
            nIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            timer.Tick += OnTimerTick;
            timer.Start();

            foreach (Listing listing in Listings)
            {
                if (!listing.Use)
                {
                    listing.UpdateListing();
                    writeJSON(jsonPath, Listings);
                    dataGrid.ItemsSource = null;
                    dataGrid.ItemsSource = Listings;
                }
            }

        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) {
                this.Hide();
            }

            base.OnStateChanged(e);
        }

        private void OnTimerTick(object sender, EventArgs e) 
        {
            foreach (Listing listing in Listings)
            {
                if (!listing.Use)
                {
                    listing.UpdateListing();
                    if (listing.endingSoon && listing.Status)
                    {
                        NotificationToast("Listing Ending Soon", listing.Name + " is ending in" + listing.EndTime.Subtract(DateTime.Now).ToString("mm 'mins'"));
                    }
                    if (listing.changedPrice)
                    {
                        NotificationToast("Listing Price has Changed", "The price of " + listing.Name + "has increased to £" + (Math.Truncate(listing.Price * 100) / 100).ToString());
                        listing.changedPrice = false;
                    }
                    if (listing.endingSoon && !listing.Status) 
                    {
                        NotificationToast("Listing has Ended", listing.Name + "has ended for a price of £" + (Math.Truncate(listing.Price * 100) / 100).ToString());
                    }
                    writeJSON(jsonPath, Listings);
                    dataGrid.ItemsSource = null;
                    dataGrid.ItemsSource = Listings;
                }
            }
        }

        
        private void NotificationToast(string title, string content) 
        {
            var toastXML = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
            var stringElements = toastXML.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXML.CreateTextNode(title));
            stringElements[1].AppendChild(toastXML.CreateTextNode(content));

            var toast = new ToastNotification(toastXML);
            ToastNotificationManager.CreateToastNotifier("My Toast").Show(toast);
        }
        

        private void DataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            (sender as System.Windows.Controls.DataGrid).SelectedItem = null;
        }

        private void AddNewLink_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Listing tempListing = new Listing(newURL.Text);

                if (!CheckDuplicates(Listings, tempListing))
                {
                    Listings.Add(tempListing);
                    writeJSON(jsonPath, Listings);
                    dataGrid.ItemsSource = null;
                    dataGrid.ItemsSource = Listings;
                } else {
                    System.Windows.MessageBox.Show("This listing already exists.", "Duplicate", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                }
            }
            catch (Exception a) {
                string msgBoxText = "The URL might be invalud. An Error has occured: " + a.Message;
                System.Windows.MessageBox.Show(msgBoxText, "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }
            newURL.Text = "";
        }

        private void UpdateAll_Clicked(object sender, RoutedEventArgs e)
        {
            foreach (Listing listing in Listings) 
            {
                if (!listing.Use)
                {
                    listing.UpdateListing();
                    writeJSON(jsonPath, Listings);
                    dataGrid.ItemsSource = null;
                    dataGrid.ItemsSource = Listings;
                }
            }
        }

        private void RemoveAll_Clicked(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("You are about to remove all the stored listings.\n Please Confirm whether you wish to continue. ", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes && Listings.Count != 0) { 
                Listings.Clear(); 
                writeJSON(jsonPath, Listings);
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = Listings;
            }
        }

        private void RemoveSelected_Clicked(object sender, RoutedEventArgs e)
        {
            var selectedItems = dataGrid.SelectedItems;
            List<Listing> tempList = new List<Listing>();

            foreach (var item in selectedItems) 
            {
                tempList.Add((Listing)item);
            }

            string msg = "You are about to remove the following listings: \n"; 

            foreach (Listing l in tempList) 
            {
                msg = msg + "   -" + l.Name + "\n";
            }

            msg = msg + "Please Confirm whether you wish to continue. ";
            MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes) {
                List<Listing> finaList = Listings.Except(tempList).ToList();
                Listings = finaList;
                writeJSON(jsonPath, Listings);
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = Listings;
            }
        }

        private void UpdateSelected_Clicked(object sender, RoutedEventArgs e)
        {
            var selectedItems = dataGrid.SelectedItems;
            List<Listing> tempList = new List<Listing>();

            foreach (var item in selectedItems)
            {
                tempList.Add((Listing)item);
            }

            foreach (Listing l in Listings) 
            {
                if (tempList.Any(item => item.Equals(l))) {
                    l.UpdateListing();
                }
            }

            writeJSON(jsonPath, Listings);
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = Listings;

        }

        private void OpenUrl_Clicked(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }
    }

}
