// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
using System.Windows.Data;

namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Modern {
    internal class BoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Visibility v && v == Visibility.Visible;
    }

    internal class InverseBoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Visibility v && v == Visibility.Collapsed;
    }

    internal class InverseBoolConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;
    }

    internal class StepToBoldConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int current && parameter is string stepStr && int.TryParse(stepStr, out int step))
                return current == step ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
            return System.Windows.FontWeights.Normal;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    internal class StepToBrushConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int current && parameter is string stepStr && int.TryParse(stepStr, out int step))
                return current == step
                    ? System.Windows.Media.Brushes.DodgerBlue
                    : (current > step ? System.Windows.Media.Brushes.Gray : System.Windows.Media.Brushes.DarkGray);
            return System.Windows.Media.Brushes.DarkGray;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
