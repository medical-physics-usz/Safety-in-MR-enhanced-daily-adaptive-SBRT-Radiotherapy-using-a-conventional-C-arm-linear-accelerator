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
using System.Windows.Forms;
using System.IO;
using USZ_RtPlanAutomator.DataExtraction;
using USZ_RtPlanAutomator.QA;
using USZ_RtPlanAutomator.UserControls;
using USZ_RtPlanAutomator.Optimization;
using USZ_RtPlanAutomator.StructureCreation;

namespace USZ_RtPlanAutomator
{
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class AdvancedSettings : System.Windows.Controls.UserControl
    {
        public SelectPtvItv selectPtvItv;
        public NumbersOnlyTextbox ringWidthSetting;
        public NumbersOnlyTextbox ringGapSetting;
        public NumbersOnlyTextbox sibGapWidthSetting;
        public NumbersOnlyTextbox ptvBuildupWidthSetting;
        public NumbersOnlyTextbox ptvOrganMarginSetting;
        private Patient patient;
        private ScriptContext context;
        private bool clinicalVersion;

        public AdvancedSettings(ScriptContext context, bool clinicalVersion = true)
        {
            InitializeComponent();
            this.patient = context.Patient;
            this.context = context;
            this.clinicalVersion = clinicalVersion;

            this.selectPtvItv = new USZ_RtPlanAutomator.UserControls.SelectPtvItv();
            ptvItvStack.Children.Add(selectPtvItv);

            this.ringWidthSetting = new USZ_RtPlanAutomator.UserControls.NumbersOnlyTextbox(NumberBoxType.ptvRingWidth);
            ringWidthStack.Children.Add(ringWidthSetting);

            this.ringGapSetting = new USZ_RtPlanAutomator.UserControls.NumbersOnlyTextbox(NumberBoxType.ptvRingGap);
            ringGapStack.Children.Add(ringGapSetting);

            this.sibGapWidthSetting = new USZ_RtPlanAutomator.UserControls.NumbersOnlyTextbox(NumberBoxType.sibGap);
            sibGapWidthStack.Children.Add(sibGapWidthSetting);

            this.ptvBuildupWidthSetting = new USZ_RtPlanAutomator.UserControls.NumbersOnlyTextbox(NumberBoxType.ptvBuildupThickness);
            ptvBuildupWidthStack.Children.Add(ptvBuildupWidthSetting);

            this.ptvOrganMarginSetting = new USZ_RtPlanAutomator.UserControls.NumbersOnlyTextbox(NumberBoxType.ptvOrganMargin);
            ptvOrganMarginStack.Children.Add(ptvOrganMarginSetting);

            FillCoursesComboBox();
            try
            {
                cmbCourse.SelectedItem = context.Course.Id;
                cmbPlan.SelectedItem = context.PlanSetup.Id;
            }
            catch
            {
                // Not a big deal if no course or plan open, user can select these manually
            }
            checkBoxOverride.IsChecked = true;
            FillCouchesComboBox();
        }
        private Course GetSelectedCourse()
        {
            if (cmbCourse.SelectedValue == null)
            {
                return null;
            }
            return patient.Courses.FirstOrDefault(c => c.Id == cmbCourse.SelectedValue.ToString());
        }

        private PlanSetup GetSelectedPlan()
        {
            if (cmbPlan.SelectedValue == null)
            {
                return null;
            }
            return GetSelectedCourse().PlanSetups.FirstOrDefault(p => p.Id == cmbPlan.SelectedValue.ToString());
        }

        private Structure GetSelectedStructure()
        {
            if (cmbStructure.SelectedValue == null)
            {
                return null;
            }
            return GetSelectedPlan().StructureSet.Structures.FirstOrDefault(s => s.Id == cmbStructure.SelectedValue.ToString());
        }

        public string GetSelectedCouch()
        {
            if (cmbCouch.SelectedValue == null)
            {
                return null;
            }
            return cmbCouch.SelectedValue.ToString();
        }

        private void FillCoursesComboBox()
        {
            var selectedCourse = cmbCourse.SelectedItem;
            cmbCourse.Items.Clear();
            foreach (Course c in patient.Courses)
            {
                cmbCourse.Items.Add(c.Id);
            }
            if (selectedCourse != null && patient.Courses.Any(x => x.Id.Equals(selectedCourse)))
            {
                cmbStructure.SelectedItem = selectedCourse;
            }
        }

        private void FillPlansComboBox()
        {
            var course = GetSelectedCourse();
            cmbPlan.Items.Clear();
            if (course != null)
            {
                foreach (PlanSetup ps in course.PlanSetups)
                {
                    cmbPlan.Items.Add(ps.Id);
                }
            }
        }

        private void FillStructuresComboBox()
        {
            var selectedStructure = cmbStructure.SelectedValue;
            cmbStructure.Items.Clear();
            var plan = GetSelectedPlan();
            if (plan != null)
            {
                foreach (Structure s in plan.StructureSet.Structures)
                {
                    cmbStructure.Items.Add(s.Id);
                }
            }
            if (selectedStructure!=null && plan.StructureSet.Structures.Any(x => x.Id.Equals(selectedStructure.ToString())))
            {
                cmbStructure.SelectedValue = selectedStructure;
            }
        }

        private void FillCouchesComboBox()
        {
            cmbCouch.Items.Clear();

            // Find a plan, can be any plan, as only used to find couch names
            PlanSetup plan = GetSelectedPlan();
            if (plan==null)
            {
                foreach(Course course in patient.Courses)
                {
                    if (course.PlanSetups!=null && course.PlanSetups.Count()>0)
                    {
                        plan = course.PlanSetups.FirstOrDefault();
                        break;
                    }
                }
            }
            try
            {
                var couchModels = CouchDataExtractor.GetCouchModels(plan.StructureSet.Image);
                foreach (var model in couchModels)
                {
                    cmbCouch.Items.Add(model);
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            cmbCouch.SelectedItem = CouchDataExtractor.GetDefaultCouchModel();
        }

        private void ClearGoalsTable()
        {
            lsbStructure.Items.Clear();
            lsbType.Items.Clear();
            lsbObjective.Items.Clear();
            lsbValue.Items.Clear();
            lsbPriority.Items.Clear();
            lsbTolerance.Items.Clear();
        }

        private void ClearObjectivesTable()
        {
            lsbObjsStruct.Items.Clear();
            lsbObjsVol.Items.Clear();
            lsbObjsOper.Items.Clear();
            lsbObjsDose.Items.Clear();
            lsbObjsPrior.Items.Clear();
        }

        private void UpdateGoalsTable()
        {
            ClearGoalsTable();

            if (GetSelectedPlan()==null)
            {
                return;
            }

            List<ClinicalGoal> CGList = DataExtractor.SelectGoalsByStructure(GetSelectedPlan(), GetSelectedStructure());

            if (CGList == null || CGList.Count==0)
            {
                String noPlansMessage = $"No clinical goals for plan { GetSelectedPlan().Id }";
                if (GetSelectedStructure()!=null)
                {
                    noPlansMessage = $"No clinical goals for structure { GetSelectedStructure().Id }";
                }
                lsbStructure.Items.Add(
                           new ListBoxItem
                           {
                               Content = noPlansMessage,
                               Background = Brushes.White
                           });
                return;
            }

            foreach (ClinicalGoal cg in CGList)
            {
                Brush backGroundColor = Brushes.White; // Default: not calculated
                switch ((int)cg.EvaluationResult)
                {
                    case 0: //  Passed
                        backGroundColor = Brushes.LightGreen;
                        break;
                    case 1: // Within acceptable variation
                        backGroundColor = Brushes.LightYellow;
                        break;
                    case 2: // Failed
                        backGroundColor = Brushes.LightCoral;
                        break;
                }
                lsbStructure.Items.Add(
                    new ListBoxItem
                    {
                        Content = cg.StructureId,
                        Background = backGroundColor
                    });
                lsbType.Items.Add(
                    new ListBoxItem
                    {
                        Content = cg.MeasureType,
                        Background = backGroundColor
                    });
                lsbObjective.Items.Add(
                    new ListBoxItem
                    {
                        Content = cg.ObjectiveAsString,
                        Background = backGroundColor
                    });
                lsbValue.Items.Add(
                    new ListBoxItem
                    {
                        Content = cg.ActualValueAsString,
                        Background = backGroundColor
                    });
                lsbPriority.Items.Add(
                    new ListBoxItem
                    {
                        Content = cg.Priority,
                        Background = backGroundColor
                    });
                lsbTolerance.Items.Add(
                    new ListBoxItem
                    {
                        Content = cg.VariationAcceptableAsString,
                        Background = backGroundColor
                    });
            }
        }

        public void UpdateObjectivesTable()
        {
            ClearObjectivesTable();

            if (GetSelectedPlan()==null)
            {
                return;
            }

            List<MyObjective> objectives = DataExtractor.SelectObjectivesByStructure(GetSelectedPlan(), GetSelectedStructure());

            foreach (MyObjective o in objectives)
            {
                lsbObjsStruct.Items.Add(
                    new ListBoxItem
                    {
                        Content = o.StructureId
                    });
                lsbObjsVol.Items.Add(
                    new ListBoxItem
                    {
                        Content = o.VolumeToMyString()
                    });
                lsbObjsOper.Items.Add(
                    new ListBoxItem
                    {
                        Content = o.OperToMyString()
                    });
                lsbObjsDose.Items.Add(
                    new ListBoxItem
                    {
                        Content = o.Dose.ToString()
                    });
                lsbObjsPrior.Items.Add(
                    new ListBoxItem
                    {
                        Content = o.Priority.ToString()
                    });
            }
        }

        private void CmbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbStructure.Items.Clear();
            selectPtvItv.FillPtvComboBox(null);
            selectPtvItv.FillItvComboBox(null);
            FillPlansComboBox();
            ClearObjectivesTable();
            ClearGoalsTable();
        }

        private void CmbPlan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FillStructuresComboBox();
            selectPtvItv.FillPtvComboBox(GetSelectedPlan());
            selectPtvItv.FillItvComboBox(GetSelectedPlan());
            UpdateGoalsTable();
            UpdateObjectivesTable();
            cmbCouch.SelectedItem = CouchDataExtractor.GetDefaultCouchModel();
        }

        private void CmbStructure_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGoalsTable();
            UpdateObjectivesTable();
        }

        private void BtnPtvRing_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            List<string> warnings;
            try
            {
                StructureCreator.CreatePTVRing(plan.StructureSet, ptvs, (bool)checkBoxOverride.IsChecked, out warnings, ringWidthSetting.GetValue(), ringGapSetting.GetValue());
                if (warnings == null || warnings.Count==0)
                {
                    System.Windows.MessageBox.Show($"PTV Ring structure created for selected PTV(s)");
                }
                else
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }

        private void BtnSbrtRing_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            List<Structure> itvs = selectPtvItv.GetSelectedItvs(plan);
            List<string> warnings;

            try
            {
                StructureCreator.CreateSbrtRings(plan.StructureSet, ptvs, itvs, (bool)checkBoxOverride.IsChecked, out warnings);
                if (warnings == null || warnings.Count == 0)
                {
                    System.Windows.MessageBox.Show($"New SBRT Ring structures created for selected PTV(s) and ITV(s)");
                }
                else
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }

        private void BtnAirStruct_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            List<string> warnings;

            try
            {
                List<Structure> airStructures = StructureCreator.CreateAirStructures(plan.StructureSet, (bool)checkBoxOverride.IsChecked, out warnings);
                if (warnings != null && warnings.Count > 0)
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                if (airStructures != null && airStructures.Count > 0)
                {
                    System.Windows.MessageBox.Show($"New Air override structure created for selected PTV(s)");
                }
                else
                {
                    System.Windows.MessageBox.Show($"No air found inside selected PTV(s)!");
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }

        private void BtnHDStruct_Click(object sender, RoutedEventArgs e)
        {
            List<string> warnings;
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            try
            {
                StructureCreator.CreateHighDensityStructures(plan.StructureSet, (bool)checkBoxOverride.IsChecked, out warnings, ptvs);
                if (warnings != null && warnings.Count > 0) // High density structure creation allways gives a warning, as material can not be designed through scripting
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show($"No high density areas found inside body!");
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }

        private void BtnCouchStruct_Click(object sender, RoutedEventArgs e)
        {
            string couchModel = GetSelectedCouch();
            List<string> warnings;

            try
            {
                StructureCreator.CreateCouchStructures(GetSelectedPlan().StructureSet, couchModel, (bool)checkBoxOverride.IsChecked, out warnings);
                if (warnings == null || warnings.Count == 0)
                {
                    System.Windows.MessageBox.Show($"New couch structures created");
                }
                else
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }

        private void BtnPtvPh_Click(object sender, RoutedEventArgs e)
        {
            List<string> warnings;
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            try
            {
                var phStructures = StructureCreator.CreatePtvsWithoutBuildupLayer(plan.StructureSet, ptvs, (bool)checkBoxOverride.IsChecked, out warnings, ptvBuildupWidthSetting.GetValue());
                if (warnings != null || warnings.Count > 0)
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                if (phStructures != null && phStructures.Count > 0)
                {
                    string newPtvs = "";
                    foreach(var ptv in phStructures)
                    {
                        newPtvs += newPtvs.Length > 0 ? $", {ptv.Id}" : ptv.Id;
                    }
                    System.Windows.MessageBox.Show($"New PTV physics structures created to cut PTV form buildup layer: {newPtvs}");
                }else
                {
                    System.Windows.MessageBox.Show($"No PTVs close to body edges!");
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOptmz_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> warnings;
                Optimizer.Run(GetSelectedPlan(), out warnings);
                if (warnings != null || warnings.Count > 0)
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                System.Windows.MessageBox.Show("Optimization complete!");
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UpdateGoalsTable();
        }

        // TODO: check if works, selected course and plan should stay unchanged when verification plan is created
        // If does not work, just do not update comboboxes
        private void BtnVerifPlan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Verificator.CreateVerificationPlan(patient, GetSelectedPlan(), (bool) radioBtnEmpty.IsChecked, clinicalVersion);
                System.Windows.MessageBox.Show($"New verification plan created");
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnSibPh_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            List<string> warnings;

            try
            {
                StructureCreator.CreateSbiStructures(plan.StructureSet, ptvs, (bool)checkBoxOverride.IsChecked, out warnings, sibGapWidthSetting.GetValue());
                if (warnings == null || warnings.Count == 0)
                {
                    System.Windows.MessageBox.Show($"New SIB structures created for selected PTV(s)");
                }
                else
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }

        private void BtnBolus_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            List<string> warnings;

            try
            {
                StructureCreator.CreateBolusStructure(plan.StructureSet, ptvs, (bool)checkBoxOverride.IsChecked, out warnings);
                if (warnings == null || warnings.Count == 0)
                {
                    System.Windows.MessageBox.Show($"New bolus structure created from selected PTV(s)");
                }
                else
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }

        private void BtnOrganInPtv_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            var organ = GetSelectedStructure();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            List<string> warnings;

            try
            {
                StructureCreator.SplitOrganPartlyInPtv(plan.StructureSet, ptvs, GetSelectedStructure(), (bool)checkBoxOverride.IsChecked, out warnings, ptvOrganMarginSetting.GetValue());
                if (warnings == null || warnings.Count == 0)
                {
                    System.Windows.MessageBox.Show($"New structures created for organ {organ.Id} inside and outside selected PTV(s)");
                }
                else
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            FillStructuresComboBox();
        }
    }
}
