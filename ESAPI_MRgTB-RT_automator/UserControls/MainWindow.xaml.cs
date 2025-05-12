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
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.UserControls
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : UserControl
    {
        private ScriptContext context;
        private System.Windows.Window window;
        private string toSettingsText = "Advanced settings";
        private string backToStartText = "Back to normal view";
        private AdvancedSettings settingsPage;
        private StartPage startPage;
        private string aboutMsg = "Aria plugin-in program for the preparation of patient data before optimization. \n\n  " +
            "Automatically performed steps: \n " +
            "- PTV ring structure creation \n " +
            "- SBRT ring structure creation (if requested) \n " +
            "- SIB ring structure creation (if requested) \n " +
            "- Couch structures creation (if requested) \n " +
            "- Air in PTV check & structure creation (if requested) \n " +
            "- High density areas in body check & structure creation if needed \n " +
            "- Removal of high density markers from the body \n " +
            "- Objective creation \n\n " +
            "cc Riikka Ruuth 2021";

        public MainWindow(ScriptContext context, System.Windows.Window window, bool clinicalVersion = true)
        {
            InitializeComponent();
            this.context = context;
            this.window = window;
            this.startPage = new StartPage(context, clinicalVersion);
            this.settingsPage = new USZ_RtPlanAutomator.AdvancedSettings(context, clinicalVersion);
            btnChangeView.Content = toSettingsText;
            myStack.Children.Add(startPage);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(aboutMsg);
        }

        private void BtnChangeView_Click(object sender, RoutedEventArgs e)
        {

            System.Windows.MessageBox.Show("Not implemented yet");

            /*
            myStack.Children.Clear();
            if (btnChangeView.Content.ToString() == toSettingsText)
            {
                btnChangeView.Content = backToStartText;
                settingsPage.cmbCourse.SelectedItem = startPage.cmbCourse.SelectedItem;
                settingsPage.cmbPlan.SelectedItem = startPage.cmbPlan.SelectedItem;
                settingsPage.selectPtvItv.CopyStructureSelections(startPage.selectPtvItv);
                settingsPage.UpdateObjectivesTable();
                myStack.Children.Add(settingsPage);
            }
            else
            {
                btnChangeView.Content = toSettingsText;
                startPage.cmbCourse.SelectedItem = settingsPage.cmbCourse.SelectedItem;
                startPage.cmbPlan.SelectedItem = settingsPage.cmbPlan.SelectedItem;
                startPage.selectPtvItv.CopyStructureSelections(settingsPage.selectPtvItv);
                startPage.enableOverride = (bool)settingsPage.checkBoxOverride.IsChecked;
                startPage.couchModel = settingsPage.GetSelectedCouch();
                startPage.ptvRingWidth = settingsPage.ringWidthSetting.GetValue();
                startPage.ptvRingGap = settingsPage.ringGapSetting.GetValue();
                startPage.sibGapWidth = settingsPage.sibGapWidthSetting.GetValue();
                myStack.Children.Add(startPage);
            }
            */
        }

        public void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            myScrollView.MaxHeight = window.ActualHeight - 160;
        }
    }
}
