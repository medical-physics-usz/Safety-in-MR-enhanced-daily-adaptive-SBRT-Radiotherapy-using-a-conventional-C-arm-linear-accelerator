using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using System.Windows.Controls;
using System.Drawing;
using Image = VMS.TPS.Common.Model.API.Image;
using VMS.TPS.Common.Model.Types;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using USZ_RtPlanAutomator.DataExtraction;
using USZ_RtPlanAutomator.Optimization;
using USZ_RtPlanAutomator.UserControls;
using System.ComponentModel;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Diagnostics;


namespace USZ_RtPlanAutomator.Tools
{
    class Actions
    {

        public static Course CreateQaCourse(ScriptContext context, Course selectedCourse)
        {
            // Create the QA course for the sCT

            // Get the course number to then create a new QA course
            string courseNumber = "";
            if (selectedCourse.Id.Length >= 2)
            {
                courseNumber = selectedCourse.Id.Substring(0, 2);
                if (courseNumber.EndsWith("_"))
                {
                    courseNumber = selectedCourse.Id.Substring(0, 1);
                }
            }

            // Intended ID of the new course and check if it exists
            string qaCourseId = courseNumber + "_sCT_QA";
            bool qaCourseAlreadyExists = false;

            IEnumerable<Course> allCourses = context.Patient.Courses;
            foreach (Course tempCourse in allCourses)
            {
                if (tempCourse.Id.Equals(qaCourseId))
                {
                    qaCourseAlreadyExists = true;
                }
            }

            // Create QA course
            Course qaCourse = null;

            if (!qaCourseAlreadyExists)
            {
                // create it if it does not exist
                qaCourse = context.Patient.AddCourse();
                qaCourse.Id = qaCourseId;

            }
            else
            {
                // get it if it already exists
                foreach (Course tempCourse in allCourses)
                {
                    if (tempCourse.Id.Equals(qaCourseId))
                    {
                        qaCourse = tempCourse;
                        //System.Windows.Forms.MessageBox.Show("it already exists");  // used to debug
                    }
                }
            }

            // Return the QA course
            return qaCourse;

        }

        //public static PlanSetup CopyOriginalPlan(ScriptContext context, Course selectedCourse, PlanSetup selectedPlan)
        //{
        //    StructureSet structureSetOriginalPlan = selectedPlan.StructureSet;
        //    Course qaCourse = USZ_sCT_PSQA.Tools.Actions.CreateQaCourse(context, selectedCourse);


        //    // Copy the selected plan to a new structure set
        //    StringBuilder outMessage = new StringBuilder("null");

        //    // Intended ID of the new plan and check if it exists
        //    string CopyOriginalPlanID = selectedPlan.Id;

        //    bool copyOriginalPlanAlreadyExists = false;

        //    IEnumerable<PlanSetup> allPlans = qaCourse.PlanSetups;
        //    foreach (PlanSetup tempPlan in allPlans)
        //    {
        //        if (tempPlan.Id.Equals(CopyOriginalPlanID))
        //        {
        //            copyOriginalPlanAlreadyExists = true;
        //        }
        //    }

        //    // Create QA Plan
        //    PlanSetup CopyOriginalPlan = null;

        //    if (!copyOriginalPlanAlreadyExists)
        //    {
        //        // create it if it does not exist
        //        CopyOriginalPlan = qaCourse.CopyPlanSetup(selectedPlan);
        //        CopyOriginalPlan.Id = CopyOriginalPlanID;

        //    }
        //    else
        //    {
        //        // get it if it already exists
        //        foreach (PlanSetup tempPlan in allPlans)
        //        {
        //            if (tempPlan.Id.Equals(CopyOriginalPlanID))
        //            {
        //                CopyOriginalPlan = tempPlan;
        //                //System.Windows.Forms.MessageBox.Show("Copy original plan already created");
        //            }
        //        }

        //    }
        //    return CopyOriginalPlan;
        //}

        public static PlanSetup FindCopyOriginalPlan(ScriptContext context, Course selectedCourse, PlanSetup selectedPlan)
        {

            //PlanSetup CopyOriginalPlan = null;
            Course qaCourse = USZ_RtPlanAutomator.Tools.Actions.CreateQaCourse(context, selectedCourse);
            string OriginalPlanID = selectedPlan.Id;


            IEnumerable<PlanSetup> allPlans = qaCourse.PlanSetups;
            foreach (PlanSetup tempPlan in allPlans)
            {
                if (tempPlan.Id.Equals(OriginalPlanID))
                {
                    return tempPlan;
                }
                //else
                //{
                //    System.Windows.Forms.MessageBox.Show("No Copy of the original plan found");
                //    return null;
                //}

            }
            return null;

        }

        public static bool CheckIfCopyOriginalPlan(ScriptContext context, Course selectedCourse, PlanSetup selectedPlan)
        {
            Course qaCourse = USZ_RtPlanAutomator.Tools.Actions.CreateQaCourse(context, selectedCourse);
            string OriginalPlanID = selectedPlan.Id;

            bool CheckIfCopyOriginalPlan = false;

            //is there a copy of original plan in qa course
            IEnumerable<PlanSetup> allPlans = qaCourse.PlanSetups;
            foreach (PlanSetup tempPlan in allPlans)
            {
                if (tempPlan.Id.Equals(OriginalPlanID))
                {
                    CheckIfCopyOriginalPlan = true;
                }
            }
            return CheckIfCopyOriginalPlan;
        }

        public static StructureSet CreateQaStructureSet(ScriptContext context, PlanSetup selectedPlan, string prefixID)
        {
            // Duplicate structure set from selected plan

            StructureSet currentStructureSet = selectedPlan.StructureSet;

            // Get the structure set date
            string structureSetDate = "";
            if (currentStructureSet.Id.Length >= 8)
            {
                //structureSetDate = currentStructureSet.Id.Substring(currentStructureSet.Id.Length - 8, 8);
                //// additional option if the date has only one digit
                //if (structureSetDate.StartsWith("_"))
                //{
                //    structureSetDate = structureSetDate.Substring(1);
                //}

                structureSetDate = currentStructureSet.Id.Substring(2); //position of the _
                if (structureSetDate.StartsWith("_"))
                {
                    structureSetDate = structureSetDate.Substring(0);
                }
                else
                {
                    structureSetDate = structureSetDate.Substring(1);
                }
            }


            // Intended ID of the new structure set and check if it exists
            if (prefixID.Length < 1 || prefixID.Length > 4)
            {
                // assign a standard if too long/short
                prefixID = "QA";
            }

            string qaStructureSetId = prefixID + structureSetDate;
            bool qaStrsAlreadyExists = false;

            IEnumerable<StructureSet> allStrs = context.Patient.StructureSets;
            foreach (StructureSet tempStrs in allStrs)
            {
                if (tempStrs.Id.Equals(qaStructureSetId))
                {
                    qaStrsAlreadyExists = true;
                }
            }

            // Create QA Structure set
            StructureSet qaStructureSet = null;

            if (!qaStrsAlreadyExists)
            {
                IEnumerable<Structure> allStructures = currentStructureSet.Structures;
                // Find the body
                Structure strBody = null;
                bool bodyFound = false;
                foreach (Structure tempStr in allStructures)
                {
                    if (tempStr.DicomType.Equals("EXTERNAL")) // body has strcutre type EXTERNAL
                    {
                        strBody = tempStr;
                        bodyFound = true;
                    }
                }
                // If found, check if is approved

                if (bodyFound && strBody.IsApproved)
                {
                    System.Windows.Forms.MessageBox.Show("WARNING the BODY is approved. " +
                        "Please un-approve body before launching the script");
                    return null; //interrupt execution of the method
                }
                else
                {
                    qaStructureSet = currentStructureSet.Copy();
                    qaStructureSet.Id = qaStructureSetId;
                    qaStructureSet.Image.Id = qaStructureSetId;
                }
            }
            else
            {
                // get it if it already exists
                foreach (StructureSet tempStrs in allStrs)
                {
                    if (tempStrs.Id.Equals(qaStructureSetId))
                    {
                        qaStructureSet = tempStrs;
                        qaStructureSet.Image.Id = qaStructureSetId;
                        //System.Windows.Forms.MessageBox.Show("it already exists");  // used to debug
                    }
                }

            }

            if (!qaStrsAlreadyExists)
            {
                // Rename related 3D image with the same ID

                // Intended ID of the new image and check if it exists
                string qaImageId = prefixID + structureSetDate;
                bool qaImageIdExists = false;

                IEnumerable<Image> allImages = selectedPlan.StructureSet.Image.Series.Images;
                foreach (Image tempImage in allImages)
                {
                    if (tempImage.Id.Equals(qaImageId))
                    {
                        qaImageIdExists = true;
                    }
                }

                // Rename it

                if (!qaImageIdExists)
                {
                    // rename it
                    qaStructureSet.Image.Id = qaImageId;

                }
                else
                {
                    // get it if it already exists
                    // keep the eclipse-created name
                }
            }



            return qaStructureSet;


        }


        public static PlanSetup CreateQaPlan(ScriptContext context, Course selectedCourse, PlanSetup selectedPlan, string prefixID)
        {

            Course qaCourse = USZ_RtPlanAutomator.Tools.Actions.CreateQaCourse(context, selectedCourse);
            StructureSet qaStructureSet = USZ_RtPlanAutomator.Tools.Actions.CreateQaStructureSet(context, selectedPlan, prefixID);

            PlanSetup CopyOriginalPlan = USZ_RtPlanAutomator.Tools.Actions.FindCopyOriginalPlan(context, selectedCourse, selectedPlan);

            // Copy the selected plan to a new structure set
            StringBuilder outMessage = new StringBuilder("null");

            // Intended ID of the new plan and check if it exists

            // Intended ID of the new structure set and check if it exists
            if (prefixID.Length < 1 || prefixID.Length > 4)
            {
                // assign a standard if too long/short
                prefixID = "QA";
            }

            string qaPlanId = prefixID;
            if (selectedPlan.Id.Length >= 3)
            {
                qaPlanId = prefixID + selectedPlan.Id.Substring(2);
            }

            bool qaPlanAlreadyExists = false;

            IEnumerable<PlanSetup> allPlans = qaCourse.PlanSetups;
            foreach (PlanSetup tempPlan in allPlans)
            {
                if (tempPlan.Id.Equals(qaPlanId))
                {
                    qaPlanAlreadyExists = true;
                }
            }

            // Create QA Plan
            PlanSetup qaPlan = null;

            //PlanSetup FindCopyOriginalPlan = USZ_sCT_PSQA.Tools.Actions.FindCopyOriginalPlan(context, selectedCourse, selectedPlan);
            if (!qaPlanAlreadyExists)
            {
                // create it if it does not exist
                qaPlan = qaCourse.CopyPlanSetup(CopyOriginalPlan, qaStructureSet, outMessage);
                qaPlan.Id = qaPlanId;
                if (qaCourse.Id.Length >= 2) { qaPlan.Name = qaCourse.Id.Substring(0, 2) + qaPlanId; }
                // System.Windows.Forms.MessageBox.Show(outMessage.ToString());  // used to debug

            }
            else
            {
                // get it if it already exists
                foreach (PlanSetup tempPlan in allPlans)
                {
                    if (tempPlan.Id.Equals(qaPlanId))
                    {
                        qaPlan = tempPlan;
                    }
                }

            }


            return qaPlan;
        }


        public static void RecalculatePlan(ScriptContext context, PlanSetup originalPlan, PlanSetup qaPlan)
        {

            PlanSetup selectedPlan = originalPlan;
            Course selectedCourse = selectedPlan.Course;

            Course qaCourse = USZ_RtPlanAutomator.Tools.Actions.CreateQaCourse(context, selectedCourse);
            //PlanSetup CopyOriginalPlan = USZ_sCT_PSQA.Tools.Actions.FindCopyOriginalPlan(context, selectedCourse, selectedPlan);

            // Find the external plan corresponding to the qa plan

            IEnumerable<ExternalPlanSetup> allExtPlans = qaCourse.ExternalPlanSetups;
            ExternalPlanSetup qaExtPlan = null;
            foreach (ExternalPlanSetup tempExtPlan in allExtPlans)
            {
                if (tempExtPlan.Id.Equals(qaPlan.Id))
                {
                    qaExtPlan = tempExtPlan;
                }
            }

            // Check that there is dose on the original plan
            if (USZ_RtPlanAutomator.Tools.ExtractData.GetTotalMu(selectedPlan) > 0)
            { // Calculate dose
                System.Windows.Forms.MessageBox.Show("Dose will recalculate, please be patient, no progress bar is shown");
                qaExtPlan.CalculateDose();

                System.Windows.Forms.MessageBox.Show("Dose calculation done!");


                // Re-normalize the QA plan to have the same MU as the original plan
                double ratioMU = USZ_RtPlanAutomator.Tools.ExtractData.GetTotalMu(selectedPlan)
                    / USZ_RtPlanAutomator.Tools.ExtractData.GetTotalMu(qaPlan);
                qaExtPlan.PlanNormalizationValue /= ratioMU;

            }
            else
            {
                System.Windows.Forms.MessageBox.Show("No dose on original plan!\nPlease calculate dose on the original plan and re-run the script");
            }

        }


        public static Structure OverrideBody(ScriptContext context, Course selectedCourse, PlanSetup selectedPlan, string prefixID, Structure strBody)
        {
            PlanSetup qaPlanWater = USZ_RtPlanAutomator.Tools.Actions.CreateQaPlan(context, selectedCourse, selectedPlan, "QW");

            // Assign water to the body of this structure set 
            StructureSet qaStructureSetWater = qaPlanWater.StructureSet;
            IEnumerable<Structure> allStructures = qaStructureSetWater.Structures;

            // Find the body
            bool bodyFound = false;
            foreach (Structure tempStr in allStructures)
            {
                if (tempStr.DicomType.Equals("EXTERNAL")) // body has strcutre type EXTERNAL
                {
                    strBody = tempStr;
                    bodyFound = true;
                }
            }

            // If found, assign HU=0
            string errorMsg;
            if (bodyFound && strBody.CanSetAssignedHU(out errorMsg))
            {
                strBody.SetAssignedHU(0);
            }

            return strBody;
            // tested on 00950319
        }


        public static Structure OverrideBulk(Structure strBD, int HU_Value)
        {

            // Assign HU to the BD of this structure set 

            // Find Bulk Density structures "BD_Fat", "BD_Bone", "BD_Tissue", "BD_Air", "BD_Lung"
            bool BDFound = false;

            if (strBD != null)
            {
                BDFound = true;
            }

            // If found, assign HU of the Bulk Density
            string errorMsg;
            if (BDFound && strBD.CanSetAssignedHU(out errorMsg))
            {
                strBD.SetAssignedHU(HU_Value);
            }
            return strBD;
        }

        public static Structure FindStructure(StructureSet strSetSearch, string IdSearch)
        {
            // Find a strcuture from its id

            foreach (Structure tempStr in strSetSearch.Structures)
            {
                if (tempStr.Id.Equals(IdSearch))
                {
                    return tempStr;
                }

            }
            return null;
        }

        public static PlanSetup FindQCPlan(ScriptContext context, Course selectedCourse, string IdPlanSearch)
        {
            Course qaCourse = USZ_RtPlanAutomator.Tools.Actions.CreateQaCourse(context, selectedCourse);

            // Find QC plan from its id

            foreach (PlanSetup tempPlan in qaCourse.PlanSetups)
            {
                if (tempPlan.Id.StartsWith(IdPlanSearch))
                {
                    return tempPlan;
                }
            }

            return null;

        }

        public static bool CheckIfRunFromOriginalPlan(ScriptContext context, Course selectedCourse, PlanSetup selectedPlan)
        {
            Course qaCourse = USZ_RtPlanAutomator.Tools.Actions.CreateQaCourse(context, selectedCourse);
            //string qaCourseID = qaCourse.Id;
            //string selectedCourseID = selectedPlan.Course.Id;

            bool CheckIfRunFromOriginalPlan = false;

            //is selected plan in the original course
            if (selectedCourse.Id.Contains("sCT_QA"))
            {
                CheckIfRunFromOriginalPlan = false;
            }
            else
            {
                CheckIfRunFromOriginalPlan = true;//it is in the original course
            }
            return CheckIfRunFromOriginalPlan;
        }


        public static void ResampleCT(ScriptContext context)
        {
            // Uses an external exe (python) to resample the sCT to 1mm before importing them
            // Get patient id.
            Patient patient = context.Patient;

            if (patient == null)
            {
                MessageBox.Show("Please select a patient");

            }
            else
            {

                // a patient was selected, copy the exe for resampling in the appropriate folder and run it
                string sourceFilePath = @"\\raoariaapps\raoariaapps$\Utilities\Resample_CT_SolaAdaption\resample_CT.exe";
                string destinationFilePath = @"\\ariaimg.usz.ch\dicom$\Import\CT\" + patient.Id + @"\resample_CT.exe";
                string directoryPath = @"\\ariaimg.usz.ch\dicom$\Import\CT\" + patient.Id;
                string searchPattern = "CT*.dcm";

                // Check if any file exists that matches the pattern
                bool fileExists = false;
                if (Directory.Exists(directoryPath)) { fileExists = Directory.GetFiles(directoryPath, searchPattern).Any(); }
                
                if (!fileExists)
                {
                    MessageBox.Show("No CT data found in the folder: " + @"\\ariaimg.usz.ch\dicom$\Import\CT\" + patient.Id + "\nMake sure you have exported the CT and MR from the scanner to Aria!");
                }

                bool exe_copied = false;

                try
                {
                    // Copy the file to the destination
                    File.Copy(sourceFilePath, destinationFilePath, true); // 'true' to overwrite if the file exists
                    exe_copied = true;
                    //Console.WriteLine("File copied successfully.");
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("An I/O error occurred: " + ioEx.Message);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    Console.WriteLine("Access denied: " + uaEx.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }

                // run the script for sCT resampling
                if (exe_copied && fileExists)
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = destinationFilePath;
                    proc.StartInfo.WorkingDirectory = @"\\ariaimg.usz.ch\dicom$\Import\CT\" + patient.Id;
                    proc.Start();
                } else
                {
                    MessageBox.Show("Folder: " + @"\\ariaimg.usz.ch\dicom$\Import\CT\" + patient.Id + " not found.\nMake sure you have exported the CT and MR from the scanner to Aria!");
                }

                // delete the temporary exe
                try
                {
                    // Check if the file exists before attempting to delete
                    if (File.Exists(destinationFilePath))
                    {
                        // Delete the file
                        File.Delete(destinationFilePath);
                        //Console.WriteLine("File deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("File does not exist.");
                    }
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine("An I/O error occurred: " + ioEx.Message);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    Console.WriteLine("Access denied: " + uaEx.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }



        }


    }
}
