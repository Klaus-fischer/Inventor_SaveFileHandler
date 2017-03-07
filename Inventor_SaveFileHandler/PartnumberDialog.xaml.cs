using Inventor_SaveFileHandler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace InvAddIn
{
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class PartnumberDialog : Window
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
        public string Suffix { get; set; } = "ipt";

        /// <summary>
        /// Contains all path properties.
        /// </summary>
        public WorkingDir WorkingDir { get; set; }

        /// <summary>
        /// User choice
        /// </summary>
        public EPartType PartType { get; private set; } = EPartType.CustomerPart;

        /// <summary>
        /// Gets the key of the project (everything before the underscore)
        /// </summary>
        public string ProjectKey
        {
            get
            {
                Match m = Regex.Match(this.ProjectName,@"^(\w+\d+)[_\s]");

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

        public PartnumberDialog()
        {
            InitializeComponent();
            this.Loaded += PartnumberDialog_Loaded;
        }

        private void PartnumberDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= PartnumberDialog_Loaded;

            tb_partnumber.Text = this.Partnumber;
            tb_description.Text = this.Description;

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
                tb_vendor.ItemsSource = vendorNames;
                tb_vendor.SelectedItem = this.Vendor;
                rb_buypart.IsChecked = true;
            }
            else
            {
                tb_vendor.ItemsSource = vendorNames;
                rb_makepart.IsChecked = true;
            }

            #endregion

            tb_description.Focus();
        }

        private void Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((sender is TextBox) && (sender as TextBox).IsReadOnly)
            {
                return;
            }

            if (rb_buypart.IsChecked == true)
            {
                if (tb_vendor.SelectedIndex == -1)
                {
                    tb_preview.Text = $"{tb_vendor.Text.Trim()}_{tb_partnumber.Text.Trim()}_{tb_description.Text.Trim()}.{this.Suffix}";
                }
                else
                {
                    tb_preview.Text = $"{tb_vendor.SelectedItem}_{tb_partnumber.Text.Trim()}_{tb_description.Text.Trim()}.{this.Suffix}";
                }
            }
            else
            {
                tb_preview.Text = $"{tb_partnumber.Text.Trim()}_{tb_description.Text.Trim()}.{this.Suffix}";
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == this.rb_makepart)
            {
                this.PartType = EPartType.MakePart;
                this.tb_partnumber.Text = Routines.GetNextPartNumber($"{ProjectKey}_T", this.Suffix, this.WorkingDir.CAD);
            }
            else if (sender == rb_customerpart)
            {
                this.PartType = EPartType.CustomerPart;
                this.tb_partnumber.Text = Routines.GetNextPartNumber($"{ProjectKey}_K", this.Suffix, this.WorkingDir.Kundenteile);
            }
            else
            {
                this.PartType = EPartType.BuyPart;
                this.tb_partnumber.Text = string.Empty;
            }
        }

        private void OK_Clicked(object sender, RoutedEventArgs e)
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

            if (this.rb_buypart.IsChecked == true)
            {
                if (this.tb_vendor.SelectedIndex == -1)
                {
                    if (!FieldValiddationRule.IsMatch(this.tb_vendor.Text))
                    {
                        this.tb_vendor.Focus();
                        return;
                    }
                    else
                    {
                        this.Vendor = tb_vendor.Text.Trim();
                    }
                }
                else
                {
                    this.Vendor = this.tb_vendor.SelectedItem as string;
                }

            }
            else
            {
                this.Vendor = string.Empty;
            }
            this.Partnumber = this.tb_partnumber.Text.Trim();
            this.Description = this.tb_description.Text.Trim();

            this.DialogResult = true;
            this.Close();
        }

        private void tb_vendor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Textbox_TextChanged(sender, null);
        }

        private void tb_vendor_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Textbox_TextChanged(sender, null);
        }
    }
}
