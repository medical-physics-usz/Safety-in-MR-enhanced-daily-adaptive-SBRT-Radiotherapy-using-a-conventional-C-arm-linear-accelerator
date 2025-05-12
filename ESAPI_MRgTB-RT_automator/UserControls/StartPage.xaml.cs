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
using System.Drawing;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using USZ_RtPlanAutomator.DataExtraction;
using USZ_RtPlanAutomator.Optimization;
using USZ_RtPlanAutomator.UserControls;
using USZ_RtPlanAutomator.QA;
using USZ_RtPlanAutomator.Reporting;
using System.ComponentModel;
using System.IO;
using USZ_RtPlanAutomator.Actions;
using USZ_RtPlanAutomator.StructureCreation;
using System.Windows.Documents.DocumentStructures;
using USZ_RtPlanAutomator.DataQualification;



namespace USZ_RtPlanAutomator
{
    /// <summary>
    /// Interaktionslogik für UserControlStartPage.xaml
    /// </summary>
    /// 
    public partial class StartPage : System.Windows.Controls.UserControl
    {
        public SelectPtvItv selectPtvItv;
        private Patient patient;
        private ScriptContext context;
        public bool enableOverride = true;
        public string couchModel = "";
        private bool clinicalVersion;
        public Nullable<double> ptvRingWidth = null;
        public Nullable<double> ptvRingGap = null;
        public Nullable<double> sibGapWidth = null;
        public StartPage(ScriptContext context, bool clinicalVersion=true)
        {
            InitializeComponent();
            this.context = context;
            patient = context.Patient;
            this.clinicalVersion = clinicalVersion;

            this.selectPtvItv = new USZ_RtPlanAutomator.UserControls.SelectPtvItv();
            ptvItvStack.Children.Add(selectPtvItv);

            //checkBoxCouch.IsChecked = true;
            checkBoxOverride.IsChecked = true;
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

        private String GetPlanType(PlanSetup plan)
        {
            // get the beams
            List<Beam> inBeams = plan.Beams.ToList();
            List<Beam> therapyBeams = new List<Beam>();
            foreach (Beam beam in inBeams) { if (!beam.IsSetupField) { therapyBeams.Add(beam); } }

            // prepare the output
            String output = "none";

            //decision tree
            if (plan.Id.StartsWith("RA") && therapyBeams[0].EnergyModeDisplayName.Contains("6X")) { output = "RA"; }

            if ((plan.Id.StartsWith("SB") || plan.Id.StartsWith("RS")) && !plan.Id.Contains("bra") && therapyBeams[0].EnergyModeDisplayName.Contains("6X-FFF")) { output = "SB"; }

            if (plan.Id.EndsWith("1z")) { output = "MR"; }

            // output
            return output;
        }

        private void FillCoursesComboBox()
        {
            if (patient.Courses==null || patient.Courses.Count()==0)
            {
                return;
            }
            foreach (Course c in patient.Courses)
            {
                cmbCourse.Items.Add(c.Id);
            }
        }

        private void FillPlansComboBox()
        {
            cmbPlan.Items.Clear();
            Course course = GetSelectedCourse();
            if (course != null && course.PlanSetups != null && course.PlanSetups.Count() > 0)
            {
                foreach (PlanSetup ps in course.PlanSetups)
                {
                    cmbPlan.Items.Add(ps.Id);
                }
            }
        }

       /* private void FillStructuresComboBox()
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
            if (selectedStructure != null && plan.StructureSet.Structures.Any(x => x.Id.Equals(selectedStructure.ToString())))
            {
                cmbStructure.SelectedValue = selectedStructure;
            }
        } */


        private void CmbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectPtvItv.FillPtvComboBox(null);
            selectPtvItv.FillItvComboBox(null);
            selectPtvItv.FillOarComboBox(null);
            FillPlansComboBox();
        }

        private void CmbPlan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectPtvItv.FillPtvComboBox(GetSelectedPlan());
            selectPtvItv.FillItvComboBox(GetSelectedPlan());
            selectPtvItv.FillOarComboBox(GetSelectedPlan());
        }

        //private void CmbStructure_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
            
        //}

        private void ResetOutput()
        {
            txtOutputQA.Text = " ";
        }


        /*
        private void BtnPrepapre_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            try
            {
                List<string> warnings = Preparer.PreparePatient(plan, selectPtvItv.GetSelectedPtvs(plan), enableOverride: enableOverride, createCouch: (bool)checkBoxCouch.IsChecked, ptvRingWidth:ptvRingWidth, overrideAir:(bool)checkBoxAir.IsChecked, populateObjectives: (bool)checkBoxObjetives.IsChecked);
                if (warnings != null && warnings.Count > 0)
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                System.Windows.MessageBox.Show($"Plan prepared for optimization");
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrepareSBRT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plan = GetSelectedPlan();
                List<string> warnings = Preparer.PreparePatient(plan, selectPtvItv.GetSelectedPtvs(plan), selectPtvItv.GetSelectedItvs(plan), OptimizationType.SBRT, (bool)checkBoxCouch.IsChecked, enableOverride: enableOverride, ptvRingWidth:ptvRingWidth, overrideAir:(bool)checkBoxAir.IsChecked , populateObjectives: (bool)checkBoxObjetives.IsChecked);
                if (warnings != null && warnings.Count>0)
                {
                    foreach(string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                System.Windows.MessageBox.Show($"SBRT plan prepared for optimization");
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrepareSIB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plan = GetSelectedPlan();
                List<string> warnings = Preparer.PreparePatient(plan, selectPtvItv.GetSelectedPtvs(plan), selectPtvItv.GetSelectedItvs(plan), OptimizationType.SIB, (bool)checkBoxCouch.IsChecked, enableOverride:enableOverride, ptvRingWidth:ptvRingWidth, sibGapWidth:sibGapWidth, overrideAir:(bool)checkBoxAir.IsChecked, populateObjectives: (bool)checkBoxObjetives.IsChecked);
                
                if (warnings != null && warnings.Count > 0)
                {
                    foreach (string warning in warnings)
                    {
                        System.Windows.MessageBox.Show(warning, "Warning message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                System.Windows.MessageBox.Show($"SIB plan prepared for optimization");
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/

        private void BtnRetreiveStrctureSets_Click(object sender, RoutedEventArgs e)
        {
            // loop over the patient data and retreive all structure sets
            // fill the check box

            lbImagesAvailable.Items.Clear();


            foreach (StructureSet tempStructureSet in context.Patient.StructureSets)
            {
                VMS.TPS.Common.Model.API.Image tempImage = tempStructureSet.Image;

                //if ((DateTime.Today - tempImage.CreationDateTime) < TimeSpan.FromDays(Convert.ToDouble(tb_InDays.Text)))
                if ((DateTime.Today - tempImage.CreationDateTime) < TimeSpan.FromDays(Convert.ToDouble(1)))
                    {
                    if (!tempImage.Id.Contains("kVCBCT") && !tempImage.Id.Contains("QW") && !tempImage.Id.Contains("QB"))
                    {
                        CheckedListItem myItem = new CheckedListItem();
                        //myItem.Name = "StructureSet ID = " + tempStructureSet.Id + " \tSeries description = " + tempImage.Series.Comment + "\t\t UID = " + tempStructureSet.UID;
                        myItem.Name = "StructureSet ID = " + tempStructureSet.Id + "\t\t UID = " + tempStructureSet.UID;
                        myItem.IsEnabled = true;
                        myItem.IsChecked = false;
                        lbImagesAvailable.Items.Add(myItem);
                    }
                }

            }
        }

            private void BtnRetreiveImages_Click(object sender, RoutedEventArgs e)
            {
                
                // loop over the patient data and retreive all images
                // fill the check box

                lbImagesAvailable.Items.Clear();

                foreach (Study tempStudy in context.Patient.Studies)
                {
                    foreach (VMS.TPS.Common.Model.API.Image tempImage in tempStudy.Images3D)
                    {
                        //MessageBox.Show(tempImage.Id + "\n" + tempImage.CreationDateTime.ToString() + "\n" + DateTime.Today.ToString() + "\n" + (DateTime.Today - tempImage.CreationDateTime).ToString() + "\n" + TimeSpan.FromDays(Convert.ToDouble(tb_InDays.Text)));


                        //if ((DateTime.Today - tempImage.CreationDateTime) < TimeSpan.FromDays(Convert.ToDouble(tb_InDays.Text)))
                        if ((DateTime.Today - tempImage.CreationDateTime) < TimeSpan.FromDays(Convert.ToDouble(1)))
                        {
                            if (!tempImage.Id.Contains("kVCBCT") && !tempImage.Id.Contains("QW") && !tempImage.Id.Contains("QB"))
                            {
                                CheckedListItem myItem = new CheckedListItem();
                                myItem.Name = "Current ID = " + USZ_RtPlanAutomator.Actions.ActionTools.Id2TwentyChar(tempImage.Id) + " \t\tProposed ID = " + USZ_RtPlanAutomator.Actions.ActionTools.Image2ProposedId(tempImage) + " \tSeries description = " + tempImage.Series.Comment + "\t\t UID = " + tempImage.Series.UID + tempImage.Id;
                                myItem.IsEnabled = true;
                                myItem.IsChecked = false;
                                lbImagesAvailable.Items.Add(myItem);
                            }
                        }
                    }
                }
                

            }


        private void BtnChangeId_Click(object sender, RoutedEventArgs e)
        {
            // loop over the selected images and change the IDs

            foreach (CheckedListItem tempItem in lbImagesAvailable.Items)
            {
                if (tempItem.IsChecked)
                {


                    // find the image selected
                    VMS.TPS.Common.Model.API.Image tempImage = null;
                    bool foundImage = false;

                    foreach (Study tempStudy in context.Patient.Studies)
                    {
                        foreach (VMS.TPS.Common.Model.API.Image temp2Image in tempStudy.Images3D)
                        {

                            if (tempItem.Name.Contains(temp2Image.Series.UID + temp2Image.Id))
                            {
                                foundImage = true;
                                tempImage = temp2Image;
                            }
                        }
                    }


                    // if found, change id
                    if (foundImage)
                    {
                        // ready to change, safety check to see if the ID already exists

                        for (int tempSuffixN = 0; tempSuffixN < 11; tempSuffixN++)
                        {
                            bool existsAlready = false;
                            string tempProposedId = USZ_RtPlanAutomator.Actions.ActionTools.Image2ProposedId(tempImage);

                            if (tempSuffixN == 0)
                            {
                                // do nothing, the suffix would be 0 there are no other images
                            }
                            else
                            {
                                if (tempProposedId.EndsWith("0") || tempProposedId.EndsWith("1") || tempProposedId.EndsWith("2") || tempProposedId.EndsWith("3") || tempProposedId.EndsWith("4") || tempProposedId.EndsWith("5") || tempProposedId.EndsWith("6") || tempProposedId.EndsWith("7") || tempProposedId.EndsWith("8") || tempProposedId.EndsWith("9"))
                                {
                                    // it ends with a numbber (the date), add "_" in between
                                    tempProposedId = USZ_RtPlanAutomator.Actions.ActionTools.Image2ProposedId(tempImage) + "_" + tempSuffixN.ToString();
                                }
                                else
                                {
                                    // it does not end with a number, just append the number
                                    tempProposedId = USZ_RtPlanAutomator.Actions.ActionTools.Image2ProposedId(tempImage) + tempSuffixN.ToString();
                                }
                            }

                            foreach (Study tempStudy in context.Patient.Studies)
                            {
                                foreach (VMS.TPS.Common.Model.API.Image temp2Image in tempStudy.Images3D)
                                {
                                    if (temp2Image.Id.Equals(tempProposedId))
                                    {
                                        existsAlready = true;
                                    }


                                }
                            }

                            // if it does not exist, rename and break the for loop
                            if (!existsAlready && tempSuffixN < 10)
                            {

                                // last saf

                                tempImage.Id = tempProposedId;
                                //tempImage.Series.Id. = tempProposedId; // this is read only!! I cannot change it!!

                                break;
                            }

                            if (tempSuffixN >= 10)
                            {
                                System.Windows.MessageBox.Show("Too many images with the same ID!\nThe image with ID = " + tempImage.Id + "\nand description = " + tempImage.Series.Comment + "\nwill not be renamed, please do it manually.");

                            }
                        }
                    }
                }
            }
        }

        private void BtnComparePlans_Click(object sender, RoutedEventArgs e)
        {
            // Retreive common variables
            PlanSetup selectedPlan = GetSelectedPlan();
            PlanSetup originalPlan = QA.Tools.GetOriginalPlan(selectedPlan);

            // Output string
            string outputQA = "";

            // Compare dose per fraction
            //outputQA = outputQA + "DOSE/FX COMPARISON:\n";
            //outputQA = outputQA + "Adapted = " + selectedPlan.DosePerFraction.ValueAsString + "\n";
            //outputQA = outputQA + "Original = " + originalPlan.DosePerFraction.ValueAsString + "\n";
            if (selectedPlan.DosePerFraction.ValueAsString.Equals(originalPlan.DosePerFraction.ValueAsString))
            {
                outputQA = outputQA + "Dose/fx is identical\n";
            }
            else
            {
                outputQA = outputQA + "ERROR! - Dose/fx is different!\n";
            }
            outputQA = outputQA + "________________________________\n\n";

            // Compare reference point
            //outputQA = outputQA + "REFERENCE POINT COMPARISON:\n";
            //outputQA = outputQA + "Adapted = " + selectedPlan.PrimaryReferencePoint.Id + "\n";
            //outputQA = outputQA + "Original = " + originalPlan.PrimaryReferencePoint.Id + "\n";
            if (selectedPlan.PrimaryReferencePoint.Id.Equals(originalPlan.PrimaryReferencePoint.Id))
            {
                outputQA = outputQA + "Ref. point is identical\n";
            }
            else
            {
                outputQA = outputQA + "ERROR! - Ref. point is different!\n";
            }
            outputQA = outputQA + "________________________________\n\n";

            // Compare MMO
            List<Beam> inBeams_selected = selectedPlan.Beams.ToList();
            List<Beam> therapyBeams_selected = new List<Beam>();
            foreach (Beam beam in inBeams_selected)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams_selected.Add(beam);
                }
            }
            int NTherapyBeams_selected = therapyBeams_selected.Count();
            int counter_selected = 0;
            int counter_original = 0;
            //outputQA = outputQA + "COMPLEXITY COMPARISON:\n";
            foreach (double MMO_i in MetricsCalc.MMO(selectedPlan.Beams))
            {
                if (counter_selected==0 || counter_selected > NTherapyBeams_selected)
                    {
                    
                }
                else { outputQA = outputQA + "Adapted MMO = " + MMO_i.ToString("F3") + "\n"; }
                counter_selected = counter_selected + 1;
            }
            foreach (double MMO_i in MetricsCalc.MMO(originalPlan.Beams))
            {
                if (counter_original == 0 || counter_original > NTherapyBeams_selected)
                {

                }
                else{outputQA = outputQA + "Original MMO = " + MMO_i.ToString("F3") + "\n"; }
                counter_original = counter_original + 1;
            }
            //outputQA = outputQA + "Limits: 16.5mm (RA) / 8mm (SBRT)" + "\n";
            outputQA = outputQA + "________________________________\n\n";


            // Compare target volumes
            //outputQA = outputQA + "VOLUMES COMPARISON:\n";
            outputQA = outputQA + QA.Tools.CompareAllTargetVolumes(selectedPlan, originalPlan);
            //outputQA = outputQA + "Max PTV vol change = 20%" + "\n";
            outputQA = outputQA + "________________________________\n\n";


            // Compare MU
            double MU_Total_selected = 0;
            double MU_Total_original = 0;
            for (int iBeam = 0; iBeam < NTherapyBeams_selected; iBeam++)
            {
                MU_Total_selected = MU_Total_selected + therapyBeams_selected[iBeam].Meterset.Value;
            }

            List<Beam> inBeams_original = originalPlan.Beams.ToList();
            List<Beam> therapyBeams_original = new List<Beam>();
            foreach (Beam beam in inBeams_original)
            {
                if (!beam.IsSetupField)
                {
                    therapyBeams_original.Add(beam);
                }
            }
            int NTherapyBeams_original = therapyBeams_original.Count();
            for (int iBeam = 0; iBeam < NTherapyBeams_original; iBeam++)
            {
                MU_Total_original = MU_Total_original + therapyBeams_original[iBeam].Meterset.Value;
            }
            //outputQA = outputQA + "MU COMPARISON:\n";
            //outputQA = outputQA + "Adapted = " + MU_Total_selected.ToString("F2") + "\n";
            //outputQA = outputQA + "Original = " + MU_Total_original.ToString("F2") + "\n";

            double MUChangePerc = 100 * (MU_Total_selected - MU_Total_original) / MU_Total_original;
            outputQA = outputQA + "MUs Change = " + MUChangePerc.ToString("F2") + " % " + "\n";
            //outputQA = outputQA + "Tolerance = 20%" + "\n";

            outputQA = outputQA + "________________________________\n\n";



            // Write output for user
            txtOutputQA.Text = outputQA;

        }

        private void BtnAIRStruct_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            //List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
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
                    System.Windows.MessageBox.Show($"New Air override structure created");
                }
                else
                {
                    System.Windows.MessageBox.Show($"No air found!");
                }
            }
            catch (Exception excp)
            {
                System.Windows.MessageBox.Show(excp.Message, "Error message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnORStruct_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();

            // If no OARS have been selected in the combo box, prompt the user
            List<Structure> oars = selectPtvItv.GetSelectedOARs(plan);
            if (oars == null)
            {
                System.Windows.MessageBox.Show($"Please select one or more OARs to override with water");
            }

            //Introducing some useful variables
            string airId = "OR_Air_Ph";
            string OldairId = "OR_Air_Water_Ph"; //air contoured in the base plan
            Structure OldairStructure = null;

            // Check if OR_AIR_Ph exists. If not, prompt the user
            Structure airStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.Id.Equals(airId));
            if (airStructure == null) { System.Windows.MessageBox.Show($"Please contour the Air first"); }

            // Check if it is an adaption and OR_Air_Water_Ph exists. If not, prompt the user
            bool isAdaption = false;
            if (plan.StructureSet.Id.EndsWith("A") || plan.StructureSet.Id.EndsWith("B") || plan.StructureSet.Id.EndsWith("C") || plan.StructureSet.Id.EndsWith("D") || plan.StructureSet.Id.EndsWith("E"))
            {
                isAdaption = true;
                OldairStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.Id.Equals(OldairId));
                if (OldairStructure == null) { System.Windows.MessageBox.Show($"'{OldairId}' missing! Copy '{airId}' from the base plan into the new structure set and rename it as '{OldairId}'"); }
            }
            

            //Create new structures
            double HUToAssingToOAR = 0; // Voxels inside the selected OARs should be assigned to water.
            foreach (Structure tempOAR in oars) 
            {
                string structureDicomType = "CONTROL";
                string ORoarId = "OR_"+ tempOAR.Id+"_Water_Ph";
                var ORoar = plan.StructureSet.AddStructure(structureDicomType, ORoarId);
                try
                {
                    ORoar.SegmentVolume = tempOAR.Sub(airStructure);
                    ORoar.Color = tempOAR.Color;
                    ORoar.SetAssignedHU(HUToAssingToOAR);
                    if (isAdaption) 
                    {
                        OldairStructure.SegmentVolume = OldairStructure.Sub(airStructure);
                        OldairStructure.SetAssignedHU(HUToAssingToOAR); 
                    }
                }
                catch 
                {
                    plan.StructureSet.RemoveStructure(ORoar);
                    continue;
                }

                //Remove the structure if it does not overlap with the air
                var airOARoverlap = plan.StructureSet.AddStructure(structureDicomType, "temp_"+ORoarId);
                airOARoverlap.SegmentVolume = tempOAR.And(airStructure);
                if (airOARoverlap.IsEmpty)
                {
                    plan.StructureSet.RemoveStructure(ORoar);
                    plan.StructureSet.RemoveStructure(airOARoverlap);

                }
                else
                {
                    plan.StructureSet.RemoveStructure(airOARoverlap);
                    System.Windows.MessageBox.Show($"Structure '{ORoarId}' created with HU = 0"); 
                }
            }
        }

        private void BtnHDStruct_Click(object sender, RoutedEventArgs e)
        {
            List<string> warnings;
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);

            if (ptvs == null)
            {
                System.Windows.MessageBox.Show($"Please select a PTV");
                return;
               
            }

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
            //FillStructuresComboBox();
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
                    System.Windows.MessageBox.Show($"New SBRT Ring structures created for selected PTV(s)");
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
            //FillStructuresComboBox();
        }


        private void BtnCopyPlan_Click(object sender, RoutedEventArgs e)
        {

            // Retreive the selected structure set
            StructureSet newStructureSet = null;
            bool foundStructureSet = false;

            foreach (CheckedListItem tempItem in lbImagesAvailable.Items)
            {
                if (tempItem.IsChecked)
                {

                    // find the structureset
                    foreach (StructureSet tempStructureSet in context.Patient.StructureSets)
                    {

                        if (tempItem.Name.Contains(tempStructureSet.UID))
                        {
                            foundStructureSet = true;
                            newStructureSet = tempStructureSet;
                        }

                    }
                }
                //break;
            }

            if (!foundStructureSet) { System.Windows.MessageBox.Show($"No structure set selected"); }

            // Retreive the fraction
            if (string.IsNullOrEmpty(tb_Fractions.Text))
            {
                System.Windows.MessageBox.Show($"Please specify the fraction");
            }

            // Retreive the selected plan
            var plan = GetSelectedPlan();
            string planID = plan.Id;


            // Check that original plan was selected
            if (planID.EndsWith("aA") || planID.EndsWith("aB") || planID.EndsWith("aC") || planID.EndsWith("aD") || planID.EndsWith("aE"))
            {
                System.Windows.MessageBox.Show($"Please select the original plan");

            }

            //Duplicate the original plan and rename it
            PlanSetup newPlan = plan.Course.CopyPlanSetup(plan, newStructureSet, null);
            newPlan.Id = plan.Id+ tb_Fractions.Text;
            newPlan.Name = plan.Name + tb_Fractions.Text;

            //Find PTV to fit the jaws
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(newPlan);
            if (ptvs == null)
            {
                System.Windows.MessageBox.Show($"Please select a PTV");
            }
            Structure ptv = ptvs.FirstOrDefault();


            foreach (Beam tempBeam in newPlan.Beams)
            {
                if (!tempBeam.Id.Contains("Setup")) 
                {
                    tempBeam.FitCollimatorToStructure(new FitToStructureMargins(20, 20, 20, 20), ptv, false, false, false);
                }
                
            }

            // Copy objectives (only Point, Mean and NT objectives implemented so far
            var objectives = plan.OptimizationSetup.Objectives;
            foreach (VMS.TPS.Common.Model.API.OptimizationObjective objective in objectives)
            {
                if (objective is OptimizationPointObjective)
                {
                    var priority = objective.Priority;
                    var objectiveOperator = objective.Operator;
                    var originalStructure = objective.Structure;
                    Structure newStructure = null;
                    foreach (Structure tempStructure in newPlan.StructureSet.Structures)
                    {
                        if (tempStructure.Id.Equals(originalStructure.Id))
                        {
                            newStructure = tempStructure;
                        }
                        
                    }
                    var dose = (objective as OptimizationPointObjective).Dose;
                    var volume = (objective as OptimizationPointObjective).Volume;
                    newPlan.OptimizationSetup.AddPointObjective(newStructure, objectiveOperator, dose, volume, priority);
                }
                else if (objective is OptimizationMeanDoseObjective) 
                {
                    var priority = objective.Priority;
                    var originalStructure = objective.Structure;
                    Structure newStructure = null;
                    foreach (Structure tempStructure in newPlan.StructureSet.Structures)
                    {
                        if (tempStructure.Id.Equals(originalStructure.Id))
                        {
                            newStructure = tempStructure;
                        }
                    }
                    var dose = (objective as OptimizationMeanDoseObjective).Dose;
                    newPlan.OptimizationSetup.AddMeanDoseObjective(newStructure, dose, priority);

                }
            }

            //NTO
            newPlan.OptimizationSetup.AddNormalTissueObjective(150, 0.5, 100, 30, 0.3);
            

        }

        private void BtnSimpleOpt_Click(object sender, RoutedEventArgs e)
        {
            // Retreive the selected plan
            var plan = GetSelectedPlan();

            // Check if the plan is null
            if (plan == null) 
            {
                System.Windows.MessageBox.Show($"No plan selected.");
                return;
            }

            // Set the calculation model 
            plan.SetCalculationModel(CalculationType.PhotonVMATOptimization, "PO16.1.0");
            // Set the MR level at restart for photon Optimizer
            bool fastOpt = plan.SetCalculationOption("PO16.1.0", "/PhotonOptimizerCalculationOptions/VMAT/@MRLevelAtRestart", "MR4");
            if (fastOpt)
            {
                ExternalPlanSetup eps = (ExternalPlanSetup)plan;
                System.Windows.MessageBox.Show($"Optimization is starting. Warning: no progress will be shown. Be patient!");
                eps.OptimizeVMAT();
                System.Windows.MessageBox.Show($"Optimization successfully completed. The dose will now be calculated. Warning: no progress will be shown. Be patient!");
                eps.CalculateDose();
                System.Windows.MessageBox.Show($"Dose calculation successfully completed.");

            }
            else { System.Windows.MessageBox.Show($"Failed to set the MR level at restart."); }

        }


        private void BtnCouchStruct_Click(object sender, RoutedEventArgs e)
        {
            string couchModel = "Exact_IGRT_Couch_Top_medium";
            List<string> warnings;

            try
            {
                StructureCreator.CreateCouchStructures(GetSelectedPlan().StructureSet, couchModel, true, out warnings);
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
            //FillStructuresComboBox();
        }

        private void Btn2cmRing_Click(object sender, RoutedEventArgs e)
        {
            var plan = GetSelectedPlan();
            List<Structure> ptvs = selectPtvItv.GetSelectedPtvs(plan);
            List<string> warnings;
            try
            {
                StructureCreator.CreatePTVRing(plan.StructureSet, ptvs, (bool)checkBoxOverride.IsChecked, out warnings, 20, 0);
                if (warnings == null || warnings.Count == 0)
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
            //FillStructuresComboBox();
        }

        private void BtnPerformQA_Click(object sender, RoutedEventArgs e)
        {
            //check run from original plan in original course
            bool checkwhichPlan = USZ_RtPlanAutomator.Tools.Actions.CheckIfRunFromOriginalPlan(context, GetSelectedCourse(), GetSelectedPlan());
            bool checkcopyPlan = USZ_RtPlanAutomator.Tools.Actions.CheckIfCopyOriginalPlan(context, GetSelectedCourse(), GetSelectedPlan());
            //PlanSetup CopyOriginalPlan = USZ_sCT_PSQA.Tools.Actions.FindCopyOriginalPlan(context, GetSelectedCourse(), GetSelectedPlan());

            if (checkwhichPlan == true)
            {
                if (checkcopyPlan == true)
                {
                    // Do all the steps to perform the QA calculation

                    // Retreive if water or bulk recalculation
                    bool CreateQW = true;

                    // Create variables
                    string outputsCT = "";
                    StructureSet qaStructureSetWater = null;
                    PlanSetup qaPlanWater = null;
                    
                    // (1) Create a PSQA course
                    Course qaCourse = USZ_RtPlanAutomator.Tools.Actions.CreateQaCourse(context, GetSelectedCourse());

                    // (2) Create PSQA plan
                    if (CreateQW) { qaStructureSetWater = USZ_RtPlanAutomator.Tools.Actions.CreateQaStructureSet(context, GetSelectedPlan(), "QW"); }
                    
                    // (3) Copy the plan into the new structure set
                    if (CreateQW) { qaPlanWater = USZ_RtPlanAutomator.Tools.Actions.CreateQaPlan(context, GetSelectedCourse(), GetSelectedPlan(), "QW"); }
                    
                    // (4) Perform the Water override
                    if (CreateQW) { Structure BodyOverride = USZ_RtPlanAutomator.Tools.Actions.OverrideBody(context, GetSelectedCourse(), GetSelectedPlan(), "QW", qaStructureSetWater.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL")); }

                    // (5) Recalculate the dose
                    if (CreateQW) { USZ_RtPlanAutomator.Tools.Actions.RecalculatePlan(context, GetSelectedPlan(), qaPlanWater); }

                    // Report dose
                    double DMean_Original = USZ_RtPlanAutomator.Tools.ExtractData.GetPTVDmean(context, GetSelectedPlan());
                    double DMean_Water = USZ_RtPlanAutomator.Tools.ExtractData.GetPTVDmean(context, qaPlanWater);
                    double dMean_diff = (100 * (DMean_Water - DMean_Original)) / DMean_Original;
                    outputsCT = outputsCT + "Mean " + USZ_RtPlanAutomator.Tools.ExtractData.FindPTV(context, GetSelectedPlan()).Id + " dose difference: " + dMean_diff.ToString("F2") + "%" + "\n";
                    outputsCT = outputsCT + "Tolerance for male pelvis: (-1%,+4%) " + "\n ";
                    
                    // Write output for user
                    txtOutputsCT.Text = outputsCT;

                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please copy original plan in QA course before running the script");
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Run the script from the original plan in the original course!! (NOT from the copy in the QA course)");
            }
        }

        private void txtOutputQAsCT_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtOutputQA_TextChanged(object sender, TextChangedEventArgs e)
        {

        }



        private void BtnCreateRule_Click(object sender, RoutedEventArgs e)
        {
            // Start by asking the user which rule type you would like to implement
            string ruleType = Actions.Rules.SelectRuleType();

            // and then route code accordingly
            if (ruleType == "Expand") { Actions.Rules.CreateExpansion(GetSelectedPlan()); }
            if (ruleType == "Subtract") { Actions.Rules.CreateSubtraction(GetSelectedPlan()); }
            if (ruleType == "Add") { Actions.Rules.CreateAddition(GetSelectedPlan()); }



        }

        private void BtnInspectRules_Click(object sender, RoutedEventArgs e)
        {
            Actions.Rules.ViewRules(GetSelectedPlan());

        }

        private void BtnApplyRules_Click(object sender, RoutedEventArgs e)
        {
            Actions.Rules.ApplyRules(GetSelectedPlan());
        }

        private void BtnDeleteRules_Click(object sender, RoutedEventArgs e)
        {
            Actions.Rules.DeleteRules(GetSelectedPlan());
        }

        private void BtnResampleCT_Click(object sender, RoutedEventArgs e)
        {
            Tools.Actions.ResampleCT(context);
        }
    } 
}
