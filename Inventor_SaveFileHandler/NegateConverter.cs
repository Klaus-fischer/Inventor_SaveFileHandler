// <copyright file="NegateConverter.cs" company="MTL - Montagetechnik Larem GmbH">
// Copyright (c) MTL - Montagetechnik Larem GmbH. All rights reserved.
// </copyright>

namespace InvAddIn
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Converter to negate boolean values.
    /// </summary>
    public class NegateConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return !true.Equals(value);
            }

            return null;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return !true.Equals(value);
            }

            return null;
        }
    }
}
