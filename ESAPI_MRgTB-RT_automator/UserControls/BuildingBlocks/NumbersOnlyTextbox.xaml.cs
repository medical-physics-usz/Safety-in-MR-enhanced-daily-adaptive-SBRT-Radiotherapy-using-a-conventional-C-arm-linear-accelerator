using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using USZ_RtPlanAutomator.Optimization;
using USZ_RtPlanAutomator.StructureCreation;

namespace USZ_RtPlanAutomator.UserControls
{
    /// <summary>
    /// Interaktionslogik für RingWidthSetting.xaml
    /// </summary>
    /// 
    public enum NumberBoxType{
        ptvRingWidth,
        ptvRingGap,
        sibGap,
        ptvBuildupThickness,
        ptvOrganMargin
    }

    public partial class NumbersOnlyTextbox : UserControl
    {
        // Set correct labels and default values for components
        // Unit label has to be mm or cm or scaling is unknown
        public NumbersOnlyTextbox(NumberBoxType boxType)
        {
            InitializeComponent();
            switch(boxType)
            {
                case NumberBoxType.ptvRingWidth:
                    numbersBoxLabel.Content = "PTV Ring width:";
                    numbersBoxUnit.Content = "mm";
                    SetValue(StructureSettings.GetPtvRingDefaultWidth());
                    break;
                case NumberBoxType.ptvRingGap:
                    numbersBoxLabel.Content = "PTV Ring gap:";
                    numbersBoxUnit.Content = "mm";
                    SetValue(StructureSettings.GetPtvRingDefaultGap());
                    break;
                case NumberBoxType.sibGap:
                    numbersBoxLabel.Content = "SIB gap:";
                    numbersBoxUnit.Content = "mm";
                    SetValue(StructureSettings.GetSbiDefaultGap());
                    break;
                case NumberBoxType.ptvBuildupThickness:
                    numbersBoxLabel.Content = "PTV buildup layer:";
                    numbersBoxUnit.Content = "mm";
                    SetValue(StructureSettings.GetPtvDefaultBuildupLayer());
                    break;
                case NumberBoxType.ptvOrganMargin:
                    numbersBoxLabel.Content = "PTV to organ margin:";
                    numbersBoxUnit.Content = "mm";
                    SetValue(StructureSettings.GetPtvOrganDefaultMargin());
                    break;
            }
        }

        // Allow only numbers and dots to be written in the box
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Returns written value in mm. If not a number, returns -1
        public double GetValue()
        {
            double value;
            if (!Double.TryParse(numbersTextBox.Text, out value))
            {
                return -1;
            }

            // Scale to mm
            int scale = -1;
            switch (numbersBoxUnit.Content)
            {
                case("mm"):
                    scale = 1;
                    break;
                case("cm"):
                    scale = 10;
                    break;
            }

            return value * scale;
        }

        public void SetValue(double value)
        {
            numbersTextBox.Text = value.ToString();
        }
    }
}
