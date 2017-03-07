// <copyright file="AsmNumberDialog.xaml.cs" company="MTL - Montagetechnik Larem GmbH">
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

    /// <summary>
    /// Code behind file for AsmNumberDialog.xaml.
    /// </summary>
    public partial class AsmNumberDialog : Window
    {
        private readonly Regex fieldValiddationRule = new Regex(@"^[^\\\/:\*\?\<\>\|" + "\"]{3,}$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="AsmNumberDialog"/> class.
        /// </summary>
        public AsmNumberDialog()
        {
            this.InitializeComponent();
            this.Loaded += this.AsmNumberDialog_Loaded;
        }

        /// <summary>
        /// Gets or sets the full name of current project.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets description of item.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets vendor of item.
        /// </summary>
        public string Vendor { get; set; }

        /// <summary>
        /// Gets or sets part number of item
        /// </summary>
        public string Partnumber { get; set; }

        /// <summary>
        /// Gets or sets expected Filename extension
        /// </summary>
        public string Suffix { get; set; } = "iam";

        /// <summary>
        /// Gets or sets contains all path properties.
        /// </summary>
        public WorkingDir WorkingDir { get; set; }

        /// <summary>
        /// Gets user choice
        /// </summary>
        public EPartType PartType { get; private set; } = EPartType.CustomerPart;

        /// <summary>
        /// Gets the recursive option for buy parts.
        /// </summary>
        public bool? Recursive { get; private set; }

        /// <summary>
        /// Gets the key of the project (everything before the underscore).
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

        /// <summary>
        /// Gets the text of the current project.
        /// </summary>
        public string ProjectText
        {
            get
            {
                if (this.ProjectName.Contains("_"))
                {
                    return this.ProjectName.Substring(this.ProjectName.IndexOf('_') + 1);
                }
                else
                {
                    return this.ProjectName;
                }
            }
        }

        private void AsmNumberDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.AsmNumberDialog_Loaded;

            List<string> vendorNames = new List<string>();

            vendorNames.AddRange(Directory.EnumerateDirectories(this.WorkingDir.Kaufteile, "*", SearchOption.TopDirectoryOnly).Select(o => Path.GetFileName(o)));

            if (!string.IsNullOrWhiteSpace(this.Vendor))
            {
                if (!vendorNames.Any(o => o.ToUpper() == this.Vendor.ToUpper()))
                {
                    vendorNames.Add(this.Vendor);
                }

                vendorNames.Sort();
                this.cb_vendor.ItemsSource = vendorNames;
                this.cb_vendor.SelectedItem = this.Vendor;
                this.tc_main.SelectedIndex = 1;
            }
            else
            {
                this.cb_vendor.ItemsSource = vendorNames;
                this.tc_main.SelectedIndex = 0;
            }

            if (Directory.EnumerateFiles(this.WorkingDir.CAD, $"{this.ProjectKey}_B_*.{this.Suffix}").Any())
            {
                this.cb_mainAssembly.IsChecked = false;
            }
            else
            {
                this.cb_mainAssembly.IsChecked = true;
            }

            this.Cb_mainAssembly_Checked(sender, e);
        }

        private void OK_Clicked(object sender, RoutedEventArgs e)
        {
            if (this.cb_mainAssembly.IsChecked == true || this.tc_main.SelectedIndex == 0)
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

                this.Vendor = string.Empty;
                this.Partnumber = this.tb_partnumber.Text.Trim();
                this.Description = this.tb_description.Text.Trim();
                this.Recursive = null;
                this.PartType = EPartType.MakePart;
            }
            else
            {
                if (!this.fieldValiddationRule.IsMatch(this.tb_vendorPartnumber.Text))
                {
                    this.tb_vendorPartnumber.SelectAll();
                    this.tb_vendorPartnumber.Focus();
                    return;
                }

                if (!this.fieldValiddationRule.IsMatch(this.tb_vendorDescription.Text))
                {
                    this.tb_vendorDescription.SelectAll();
                    this.tb_vendorDescription.Focus();
                    return;
                }

                if (this.cb_vendor.SelectedIndex == -1)
                {
                    if (!this.fieldValiddationRule.IsMatch(this.cb_vendor.Text))
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

                this.Partnumber = this.tb_vendorPartnumber.Text.Trim();
                this.Description = this.tb_vendorDescription.Text.Trim();
                this.Recursive = this.cb_recursive.IsChecked;
                this.PartType = EPartType.BuyPart;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Wird noch nicht unterstützt");
            this.cb_recursive.IsChecked = false;
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
            if (this.cb_mainAssembly.IsChecked == true || this.tc_main.SelectedIndex == 0)
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

        private void Cb_mainAssembly_Checked(object sender, RoutedEventArgs e)
        {
            if (this.cb_mainAssembly.IsChecked == true)
            {
                this.tb_partnumber.Text = string.IsNullOrWhiteSpace(this.Partnumber) ?
                    $"{this.ProjectKey}_B" :
                    this.Partnumber;

                this.tb_description.Text = string.IsNullOrWhiteSpace(this.Description) ?
                    this.ProjectText :
                    this.Description;
                this.tb_description.Focus();
            }
            else
            {
                this.tb_partnumber.Text = string.IsNullOrWhiteSpace(this.Partnumber) ?
                    Routines.GetNextPartNumber($"{this.ProjectKey}_B", this.Suffix, this.WorkingDir.CAD) :
                    this.Partnumber;

                this.tb_description.Text = string.IsNullOrWhiteSpace(this.Description) ?
                    string.Empty :
                    this.Description;

                this.tb_description.Focus();
            }

            this.UpdatePreview();
        }
    }
}
