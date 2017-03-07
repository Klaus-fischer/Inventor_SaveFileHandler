using Inventor_SaveFileHandler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace InvAddIn
{
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class AsmNumberDialog : Window
    {
        readonly Regex FieldValiddationRule = new Regex(@"^[^\\\/:\*\?\<\>\|" + "\"]{3,}$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Fill name of current project
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Description of item
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Vendor of item
        /// </summary>
        public string Vendor { get; set; }

        /// <summary>
        /// Partnumber of item
        /// </summary>
        public string Partnumber { get; set; }

        /// <summary>
        /// expected Filename extension
        /// </summary>
        public string Suffix { get; set; } = "iam";

        /// <summary>
        /// Contains all path properties.
        /// </summary>
        public WorkingDir WorkingDir { get; set; }

        /// <summary>
        /// User choice
        /// </summary>
        public EPartType PartType { get; private set; } = EPartType.CustomerPart;

        public bool? Recursive { get; private set; }

        /// <summary>
        /// Gets the key of the project (everything before the underscore)
        /// </summary>
        public string ProjectKey
        {
            get
            {
                Match m = Regex.Match(this.ProjectName, @"^(\w+\d+)[_\s]");

                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
                else
                {
                    return ProjectName;
                }
            }
        }

        public string ProjectText
        {
            get
            {
                if (ProjectName.Contains("_"))
                {
                    return ProjectName.Substring(ProjectName.IndexOf('_') + 1);
                }
                else
                {
                    return ProjectName;
                }
            }
        }

        public AsmNumberDialog()
        {
            InitializeComponent();
            this.Loaded += AsmNumberDialog_Loaded;
        }

        private void AsmNumberDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= AsmNumberDialog_Loaded;

            #region === Kaufteile =============================================

            List<string> vendorNames = new List<string>();

            vendorNames.AddRange(Directory.EnumerateDirectories(this.WorkingDir.Kaufteile, "*", SearchOption.TopDirectoryOnly).Select(o => Path.GetFileName(o)));

            if (!string.IsNullOrWhiteSpace(this.Vendor))
            {
                if (!vendorNames.Any(o => o.ToUpper() == this.Vendor.ToUpper()))
                {
                    vendorNames.Add(this.Vendor);
                }
                vendorNames.Sort();
                cb_vendor.ItemsSource = vendorNames;
                cb_vendor.SelectedItem = this.Vendor;
                tc_main.SelectedIndex = 1;
            }
            else
            {
                cb_vendor.ItemsSource = vendorNames;
                tc_main.SelectedIndex = 0;
            }

            #endregion

            #region === Main Assembly =========================================

            if (Directory.EnumerateFiles(this.WorkingDir.CAD, $"{ProjectKey}_B_*.{Suffix}").Any())
            {
                cb_mainAssembly.IsChecked = false;
            }
            else
            {
                cb_mainAssembly.IsChecked = true;
            }

            this.cb_mainAssembly_Checked(sender, e);

            #endregion
        }

        private void OK_Clicked(object sender, RoutedEventArgs e)
        {
            if (cb_mainAssembly.IsChecked == true || tc_main.SelectedIndex == 0) // Normal
            {

                if (!FieldValiddationRule.IsMatch(this.tb_partnumber.Text))
                {
                    this.tb_partnumber.SelectAll();
                    this.tb_partnumber.Focus();
                    return;
                }

                if (!FieldValiddationRule.IsMatch(this.tb_description.Text))
                {
                    this.tb_description.SelectAll();
                    this.tb_description.Focus();
                    return;
                }

                this.Vendor = string.Empty;
                this.Partnumber = tb_partnumber.Text.Trim();
                this.Description = tb_description.Text.Trim();
                this.Recursive = null;
                this.PartType = EPartType.MakePart;
            }
            else
            {
                if (!FieldValiddationRule.IsMatch(this.tb_vendorPartnumber.Text))
                {
                    this.tb_vendorPartnumber.SelectAll();
                    this.tb_vendorPartnumber.Focus();
                    return;
                }

                if (!FieldValiddationRule.IsMatch(this.tb_vendorDescription.Text))
                {
                    this.tb_vendorDescription.SelectAll();
                    this.tb_vendorDescription.Focus();
                    return;
                }

                if (this.cb_vendor.SelectedIndex == -1)
                {
                    if (!FieldValiddationRule.IsMatch(this.cb_vendor.Text))
                    {
                        this.cb_vendor.Focus();
                        return;
                    }
                    this.Vendor = this.cb_vendor.Text.Trim();
                }
                else
                {
                    this.Vendor = this.cb_vendor.SelectedItem as string;
                }

                this.Partnumber = tb_vendorPartnumber.Text.Trim();
                this.Description = tb_vendorDescription.Text.Trim();
                this.Recursive = cb_recursive.IsChecked;
                this.PartType = EPartType.BuyPart;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Wird noch nicht unterstützt");
            cb_recursive.IsChecked = false;
        }

        private void UpdatePreview(object sender, SelectionChangedEventArgs e)
        {
            this.UpdatePreview();
        }

        private void UpdatePreview(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox && (sender as TextBox).IsReadOnly)
            {
                return;
            }

            this.UpdatePreview();
        }

        private void UpdatePreview(object sender, System.Windows.Input.KeyEventArgs e)
        {
            this.UpdatePreview();
        }

        private void UpdatePreview()
        {
            string text = string.Empty;
            if (this.cb_mainAssembly.IsChecked == true || this.tc_main.SelectedIndex == 0) // Normal
            {
                text = $"{this.tb_partnumber.Text.Trim()}_{this.tb_description.Text.Trim()}.{this.Suffix}";
            }
            else
            {
                text = (this.cb_vendor.SelectedIndex == -1) ?
                    this.cb_vendor.Text.Trim() :
                    this.cb_vendor.SelectedItem as string;
                text = $"{text}_{this.tb_vendorPartnumber.Text.Trim()}_{this.tb_vendorDescription.Text.Trim()}.{this.Suffix}";
            }

            this.tb_preview.Text = text;
        }

        private void cb_mainAssembly_Checked(object sender, RoutedEventArgs e)
        {
            if (cb_mainAssembly.IsChecked == true)
            {
                tb_partnumber.Text = string.IsNullOrWhiteSpace(this.Partnumber) ?
                    $"{ProjectKey}_B" :
                    this.Partnumber;

                tb_description.Text = string.IsNullOrWhiteSpace(this.Description) ?
                    this.ProjectText :
                    this.Description;
                tb_description.Focus();
            }
            else
            {
                tb_partnumber.Text = string.IsNullOrWhiteSpace(this.Partnumber) ?
                    Routines.GetNextPartNumber($"{ProjectKey}_B", this.Suffix, this.WorkingDir.CAD) :
                    this.Partnumber;

                tb_description.Text = string.IsNullOrWhiteSpace(this.Description) ?
                    string.Empty :
                    this.Description;

                tb_description.Focus();
            }

            this.UpdatePreview();
        }

    }
}
