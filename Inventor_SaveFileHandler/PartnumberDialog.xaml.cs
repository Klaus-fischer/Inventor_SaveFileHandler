// <copyright file="PartnumberDialog.xaml.cs" company="MTL - Montagetechnik Larem GmbH">
// Copyright (c) MTL - Montagetechnik Larem GmbH. All rights reserved.
// </copyright>

namespace InvAddIn
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using Iv=Inventor;

    /// <summary>
    /// Code behind file for PartnumberDialog.xaml.
    /// </summary>
    public partial class PartnumberDialog : Window
    {
        /// <summary>
        /// Validation rule for text fields.
        /// </summary>
        private readonly Regex fieldValiddationRule = new Regex(@"^[^\\\/:\*\?\<\>\|" + "\"]{3,}$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="PartnumberDialog"/> class.
        /// </summary>
        public PartnumberDialog()
        {
            this.InitializeComponent();
            this.Loaded += this.PartnumberDialog_Loaded;
        }

        /// <summary>
        /// Gets or sets fill name of current project
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets description of item
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets vendor of item
        /// </summary>
        public string Vendor { get; set; }

        /// <summary>
        /// Gets or sets part number of item
        /// </summary>
        public string Partnumber { get; set; }

        /// <summary>
        /// Gets or sets expected Filename extension
        /// </summary>
        public string Suffix { get; set; } = "ipt";

        /// <summary>
        /// Gets or sets contains all path properties.
        /// </summary>
        public WorkingDir WorkingDir { get; set; }

        /// <summary>
        /// Gets user choice
        /// </summary>
        public EPartType PartType { get; private set; } = EPartType.CustomerPart;

        /// <summary>
        /// Gets the content of the outer dimension text box on close.
        /// </summary>
        public string OuterDimensions { get; private set; }

        /// <summary>
        /// Gets a value indicating whether part is a rotating part.
        /// </summary>
        public bool IsRotatePart { get; private set; }

        /// <summary>
        /// Gets a value indicating whether outer dimensions should be re-elevated on save.
        /// </summary>
        public bool ReElevateDimensionOnSave { get; private set; }

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
                    return this.ProjectName;
                }
            }
        }

        private void PartnumberDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.PartnumberDialog_Loaded;

            this.tb_partnumber.Text = this.Partnumber;
            this.tb_description.Text = this.Description;

            List<string> vendorNames = new List<string>();

            vendorNames.AddRange(Directory.EnumerateDirectories(this.WorkingDir.Kaufteile, "*", SearchOption.TopDirectoryOnly).Select(o => Path.GetFileName(o)));

            if (!string.IsNullOrWhiteSpace(this.Vendor))
            {
                if (!vendorNames.Any(o => o.ToUpper() == this.Vendor.ToUpper()))
                {
                    vendorNames.Add(this.Vendor);
                }

                vendorNames.Sort();
                this.tb_vendor.ItemsSource = vendorNames;
                this.tb_vendor.SelectedItem = this.Vendor;
                this.rb_buypart.IsChecked = true;
            }
            else
            {
                this.tb_vendor.ItemsSource = vendorNames;
                this.rb_makepart.IsChecked = true;
            }

            this.tb_description.Focus();
        }

        private void Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((sender is TextBox) && (sender as TextBox).IsReadOnly)
            {
                return;
            }

            if (this.rb_buypart.IsChecked == true)
            {
                if (this.tb_vendor.SelectedIndex == -1)
                {
                    this.tb_preview.Text = $"{this.tb_vendor.Text.Trim()}_{this.tb_partnumber.Text.Trim()}_{this.tb_description.Text.Trim()}.{this.Suffix}";
                }
                else
                {
                    this.tb_preview.Text = $"{this.tb_vendor.SelectedItem}_{this.tb_partnumber.Text.Trim()}_{this.tb_description.Text.Trim()}.{this.Suffix}";
                }
            }
            else
            {
                this.tb_preview.Text = $"{this.tb_partnumber.Text.Trim()}_{this.tb_description.Text.Trim()}.{this.Suffix}";
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == this.rb_makepart)
            {
                this.PartType = EPartType.MakePart;
                this.tb_partnumber.Text = Routines.GetNextPartNumber($"{this.ProjectKey}_T", this.Suffix, this.WorkingDir.CAD);
                this.RecalculateDimensions(sender, e);
            }
            else if (sender == this.rb_customerpart)
            {
                this.PartType = EPartType.CustomerPart;
                this.tb_partnumber.Text = Routines.GetNextPartNumber($"{this.ProjectKey}_K", this.Suffix, this.WorkingDir.Kundenteile);
            }
            else
            {
                this.PartType = EPartType.BuyPart;
                this.tb_partnumber.Text = this.Partnumber;
            }
        }

        private void OK_Clicked(object sender, RoutedEventArgs e)
        {
            if (!this.fieldValiddationRule.IsMatch(this.tb_partnumber.Text))
            {
                this.tb_partnumber.SelectAll();
                this.tb_partnumber.Focus();
                return;
            }

            if (!this.fieldValiddationRule.IsMatch(this.tb_description.Text))
            {
                this.tb_description.SelectAll();
                this.tb_description.Focus();
                return;
            }

            if (this.rb_buypart.IsChecked == true)
            {
                if (this.tb_vendor.SelectedIndex == -1)
                {
                    if (!this.fieldValiddationRule.IsMatch(this.tb_vendor.Text))
                    {
                        this.tb_vendor.Focus();
                        return;
                    }
                    else
                    {
                        this.Vendor = this.tb_vendor.Text.Trim();
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

            this.OuterDimensions = this.tb_dimensions.Text.Trim();
            this.IsRotatePart = true.Equals(this.cb_rotatePart.IsChecked);
            this.ReElevateDimensionOnSave = true.Equals(this.cb_recalcOnSave.IsChecked);

            this.DialogResult = true;
            this.Close();
        }

        private void Vendor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Textbox_TextChanged(sender, null);
        }

        private void Vendor_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            this.Textbox_TextChanged(sender, null);
        }

        private void RecalculateDimensions(object sender, RoutedEventArgs e)
        {
            Iv.PartDocument doc = StandardAddInServer.This.InventorApplication.ActiveDocument as Iv.PartDocument;

            if (doc != null)
            {
                this.tb_dimensions.Text = Routines.DetermineOuterDimensions(doc, true.Equals(this.cb_rotatePart.IsChecked));
            }
        }
    }
}
