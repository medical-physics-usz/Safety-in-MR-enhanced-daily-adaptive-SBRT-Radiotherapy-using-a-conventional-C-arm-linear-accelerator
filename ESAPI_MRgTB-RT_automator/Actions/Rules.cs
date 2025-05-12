using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Microsoft.VisualBasic;
using System.IO;
using System.Diagnostics;


namespace USZ_RtPlanAutomator.Actions
{
    class Rules
    {

        public static string RetrieveRulesFile(PlanSetup SelectedPlan)
        {
            // Returns the path at which the rules for a given plan are saved
            string PatientID = SelectedPlan.Course.Patient.Id;
            string PlanID = SelectedPlan.Id;
            string CourseID = SelectedPlan.Course.Id;

            // if the script is ran form the adapted plan, take only the beginning of the string
            if (PlanID.Length >= 9)
            {
                PlanID = PlanID.Substring(0, 9);
            }

            string PathRules = @"\\raoariaapps\raoariaapps$\MRgTB-RT_automator\Rules\";

            PathRules = PathRules + PatientID + "_" + CourseID + "_" + PlanID + "_Rules.txt";

            return PathRules;

        }


        public static string SelectRuleType()
        {
            string selectedOption = null;

            // Create a new form for the pop-up
            Form popupForm = new Form();
            popupForm.Text = "Select an the rule type";
            popupForm.Width = 300;
            popupForm.Height = 150;

            // Create a dropdown (ComboBox)
            ComboBox comboBox = new ComboBox();
            comboBox.Items.AddRange(new string[] { "Expand", "Subtract", "Add" });
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList; // User can only select from the options
            comboBox.Location = new System.Drawing.Point(30, 20);
            comboBox.Width = 200;
            comboBox.SelectedIndex = 0; // Default to the first option

            // Create a button to continue
            Button continueButton = new Button();
            continueButton.Text = "Continue";
            continueButton.Location = new System.Drawing.Point(100, 60);
            continueButton.Width = 100;

            // Button click event
            continueButton.Click += (sender, e) =>
            {
                // Store the selected option and close the pop-up
                selectedOption = comboBox.SelectedItem.ToString();
                popupForm.Close();
            };

            // Add controls to the pop-up form
            popupForm.Controls.Add(comboBox);
            popupForm.Controls.Add(continueButton);

            // Show the pop-up form as a dialog (blocking call)
            popupForm.ShowDialog();

            // Return the selected option after the dialog closes
            return selectedOption;
        }

        public static string SelectStructure(PlanSetup SelectedPlan, string labelInstruction)
        {
            string selectedOption = null;

            // Create a new form for the pop-up
            Form popupForm = new Form();
            popupForm.Text = labelInstruction;
            popupForm.Width = 300;
            popupForm.Height = 150;

            // Create a dropdown (ComboBox)
            ComboBox comboBox = new ComboBox();

            foreach (Structure tempStructure in SelectedPlan.StructureSet.Structures)
            {
                comboBox.Items.Add(tempStructure.Id);
            }

            comboBox.DropDownStyle = ComboBoxStyle.DropDownList; // User can only select from the options
            comboBox.Location = new System.Drawing.Point(30, 20);
            comboBox.Width = 200;
            comboBox.SelectedIndex = 0; // Default to the first option

            // Create a button to continue
            Button continueButton = new Button();
            continueButton.Text = "Continue";
            continueButton.Location = new System.Drawing.Point(100, 60);
            continueButton.Width = 100;

            // Button click event
            continueButton.Click += (sender, e) =>
            {
                // Store the selected option and close the pop-up
                selectedOption = comboBox.SelectedItem.ToString();
                popupForm.Close();
            };

            // Add controls to the pop-up form
            popupForm.Controls.Add(comboBox);
            popupForm.Controls.Add(continueButton);

            // Show the pop-up form as a dialog (blocking call)
            popupForm.ShowDialog();

            // Return the selected option after the dialog closes
            return selectedOption;
        }

        public static void CreateExpansion(PlanSetup SelectedPlan)
        {

            // Create a rule for an expansion

            // Start by definitng the output string for the database
            string separator = "#";
            string stringRule = "Expansion" + separator;

            // ask the user which structure to expand
            string structureIn1 = SelectStructure(SelectedPlan, "Structure to expand");
            stringRule = stringRule + structureIn1 + separator;
            string structureIn2 = "null";
            stringRule = stringRule + structureIn2 + separator;

            // ask the user by how many mm
            string options = Interaction.InputBox("Expansion in mm:", "Expansion margin", "3");
            stringRule = stringRule + options + separator;

            // ask the user which structure should be the output
            string structureOut1 = SelectStructure(SelectedPlan, "Output structure");
            stringRule = stringRule + structureOut1 + "\n";

            // write the rule in the txt
            File.AppendAllText(RetrieveRulesFile(SelectedPlan), stringRule);

        }

        public static void CreateSubtraction(PlanSetup SelectedPlan)
        {

            // Create a rule for a subtraction

            // Start by definitng the output string for the database
            string separator = "#";
            string stringRule = "Subtraction" + separator;

            // ask the user which structure to subtract
            string structureIn1 = SelectStructure(SelectedPlan, "Structure to crop");
            stringRule = stringRule + structureIn1 + separator;
            string structureIn2 = SelectStructure(SelectedPlan, "Cropping structure");
            stringRule = stringRule + structureIn2 + separator;

            // no options here
            string options = "null";
            stringRule = stringRule + options + separator;

            // ask the user which structure should be the output
            string structureOut1 = SelectStructure(SelectedPlan, "Output structure");
            stringRule = stringRule + structureOut1 + "\n";

            // write the rule in the txt
            File.AppendAllText(RetrieveRulesFile(SelectedPlan), stringRule);

        }

        public static void CreateAddition(PlanSetup SelectedPlan)
        {

            // Create a rule for an addition

            // Start by definitng the output string for the database
            string separator = "#";
            string stringRule = "Addition" + separator;

            // ask the user which structure to add
            string structureIn1 = SelectStructure(SelectedPlan, "Structure to add");
            stringRule = stringRule + structureIn1 + separator;
            string structureIn2 = SelectStructure(SelectedPlan, "Structure to add");
            stringRule = stringRule + structureIn2 + separator;

            // no options here
            string options = "null";
            stringRule = stringRule + options + separator;

            // ask the user which structure should be the output
            string structureOut1 = SelectStructure(SelectedPlan, "Output structure");
            stringRule = stringRule + structureOut1 + "\n";

            // write the rule in the txt
            File.AppendAllText(RetrieveRulesFile(SelectedPlan), stringRule);

        }


        public static void ViewRules(PlanSetup SelectedPlan)
        {
            // load the file where the rules are stored
            string PathRules = "";
            if (File.Exists(RetrieveRulesFile(SelectedPlan)))
            {
                PathRules = RetrieveRulesFile(SelectedPlan);
            }
            else
            {
                MessageBox.Show("No database entry with rules was found for this plan. Please check at : \n \\\\raoariaapps\\raoariaapps$\\MRgTB-RT_automator\\Rules");
            }

            // Output message
            string outMessage = "List of rules: \n\n";

            // Read the file line by line
            try
            {
                using (StreamReader sr = new StreamReader(PathRules))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Split the line into variables using tab as the delimiter
                        string[] variables = line.Split('#');

                        // Ensure the line contains exactly 5 variables
                        if (variables.Length == 5)
                        {
                            // Call the Rules function with the variables
                            if (variables[0] == "Expansion")
                            {
                                outMessage = outMessage + variables[4] + " = " + variables[1] + " + " + variables[3] + "mm\n";
                            }

                            if (variables[0] == "Subtraction")
                            {
                                outMessage = outMessage + variables[4] + " = " + variables[1] + " - " + variables[2] + "\n";
                            }

                            if (variables[0] == "Addition")
                            {
                                outMessage = outMessage + variables[4] + " = " + variables[1] + " + " + variables[2] + "\n";
                            }



                        }
                        else
                        {
                            MessageBox.Show("The file with rules is corrupted. Please delete all rules, re-define the rules and try again.");
                        }
                    }

                    MessageBox.Show(outMessage);


                }
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred: " + e.Message);

            }

        }

        public static void DeleteRules(PlanSetup SelectedPlan)
        {
            // ask if you are sure
            string delete = Interaction.InputBox("Type 'yes' to delete all rules:", "Delete all rules?", "no");

            if (delete == "yes" && File.Exists(RetrieveRulesFile(SelectedPlan)))
            {
                File.Delete(RetrieveRulesFile(SelectedPlan));
                MessageBox.Show("All rules deleted. Please create new rules.");
            }

        }

        public static void ApplyRules(PlanSetup SelectedPlan)
        {
            // load the file where the rules are stored
            string PathRules = "";
            if (File.Exists(RetrieveRulesFile(SelectedPlan)))
            {
                PathRules = RetrieveRulesFile(SelectedPlan);
            }
            else
            {
                MessageBox.Show("No database entry with rules was found for this plan. Please check at : \n \\\\raoariaapps\\raoariaapps$\\MRgTB-RT_automator\\Rules");
            }

            // Read the file line by line
            try
            {
                using (StreamReader sr = new StreamReader(PathRules))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Split the line into variables using tab as the delimiter
                        string[] variables = line.Split('#');

                        // Ensure the line contains exactly 5 variables
                        if (variables.Length == 5)
                        {
                            // Call the Rules function with the variables
                            if (variables[0] == "Expansion")
                            {
                                ApplyExpansion(SelectedPlan, variables[1], variables[3], variables[4]);
                            }

                            if (variables[0] == "Subtraction")
                            {
                                ApplySubtraction(SelectedPlan, variables[1], variables[2], variables[4]);
                            }

                            if (variables[0] == "Addition")
                            {
                                ApplyAddition(SelectedPlan, variables[1], variables[2], variables[4]);
                            }



                        }
                        else
                        {
                            MessageBox.Show("The file with rules is corrupted. Please delete all rules, re-define the rules and try again.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred: " + e.Message);

            }
        }


        public static void ApplyExpansion(PlanSetup SelectedPlan, string structureIn1, string margin_mm, string structureOut)
        {
            // perform an expansion
            FindStructureFromId(SelectedPlan, structureOut).SegmentVolume = FindStructureFromId(SelectedPlan, structureIn1).SegmentVolume.Margin(Convert.ToDouble(margin_mm));
        }

        public static void ApplySubtraction(PlanSetup SelectedPlan, string structureIn1, string structureIn2, string structureOut)
        {
            // perform a subtraction
            FindStructureFromId(SelectedPlan, structureOut).SegmentVolume = FindStructureFromId(SelectedPlan, structureIn1).SegmentVolume.Sub(FindStructureFromId(SelectedPlan, structureIn2));
        }

        public static void ApplyAddition(PlanSetup SelectedPlan, string structureIn1, string structureIn2, string structureOut)
        {
            // perform an addition
            FindStructureFromId(SelectedPlan, structureOut).SegmentVolume = FindStructureFromId(SelectedPlan, structureIn1).SegmentVolume.Or(FindStructureFromId(SelectedPlan, structureIn2));
        }



        public static Structure FindStructureFromId(PlanSetup SelectedPlan, string structureID)
        {
            Structure outStructure = null;

            bool structureFound = false;

            foreach (Structure tempStructure in SelectedPlan.StructureSet.Structures)
            {
                if (tempStructure.Id.Equals(structureID)){
                    structureFound = true;
                    outStructure = tempStructure;
                    break;
                }
            }

            if (!structureFound) { MessageBox.Show("Error! Structure '" + structureID + "' not found in the structure set '" + SelectedPlan.StructureSet.Id + "'. \nPlease verify the rules"); }

            return outStructure;
        }


    }
}

