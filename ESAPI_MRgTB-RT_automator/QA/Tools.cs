using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.QA
{
    class Tools
    {

        public static string CompareAllTargetVolumes(PlanSetup adaptedPlan, PlanSetup originalPlan)
        {
            // compare all targets 
            string outString = "";

            // loop over all adapted structures 
            foreach (Structure tempStrAdapted in adaptedPlan.StructureSet.Structures)
            {
                double tempVolumeAdaptedStructure = 0;
                double tempVolumeOriginaStructure = 0;

                // select only the targets
                if (tempStrAdapted.DicomType.EndsWith("TV"))
                {
                    // exclude physics structures
                    if (!tempStrAdapted.Id.Contains("PH"))
                    {
                        if (!tempStrAdapted.Id.Contains("Ph"))
                        {
                            // target found, extract volume
                            tempVolumeAdaptedStructure = tempStrAdapted.Volume;



                            // search for volume with same ID in the original plan
                            foreach (Structure tempStrOrigina in originalPlan.StructureSet.Structures)
                            {
                                if (tempStrOrigina.Id.Equals(tempStrAdapted.Id))
                                {
                                    // found, extract volume
                                    tempVolumeOriginaStructure = tempStrOrigina.Volume;

                                }

                            }

                            // calculate change and write it to string
                            if (tempVolumeAdaptedStructure == 0 || tempVolumeOriginaStructure == 0)
                            {
                                outString = outString + tempStrAdapted.Id + " not matched or zero volume\n";
                            }
                            else
                            {
                                double volumeChangePerc = 100 * (tempVolumeAdaptedStructure - tempVolumeOriginaStructure) / tempVolumeOriginaStructure;
                                double volumeChangeAbs = tempVolumeAdaptedStructure - tempVolumeOriginaStructure;
                                outString = outString + tempStrAdapted.Id + " change = " + volumeChangePerc.ToString("F2") + " % (" + volumeChangeAbs.ToString("F2") + " cc)\n";
                            }


                        }
                    }


                }



            }

            return outString;

        }

        public static PlanSetup GetOriginalPlan(PlanSetup adaptedPlan)
        {
            // Retreive original plan assuming that only the last letter (A,B,C,..) was added

            string adaptedID = adaptedPlan.Id;
            string originaID = adaptedID.Substring(0, adaptedID.Length - 1);

            PlanSetup outPlan = null;
            foreach (PlanSetup tempPlan in adaptedPlan.Course.PlanSetups)
            {
                if (tempPlan.Id.Equals(originaID))
                {
                    outPlan = tempPlan;
                }

            }

            if (outPlan == null)
            {
                System.Windows.Forms.MessageBox.Show("Plan with ID  = " + originaID + " not found!\nIs the PlanID of the adapted plan = " + adaptedID + " correct?");
            }

            return outPlan;

        }


        public static bool DecisionPatientID(Patient Patient)
        {
            // Decide based on the patient ID if the plan requires QA or not (random yes once every 10 patients).
            int intID = 0;
            bool retQA = true;
            string PatientID = Patient.Id;

            int[] intArray = new int[PatientID.Length];
            for (int i = 0; i < PatientID.Length; i++)
            {
                intArray[i] = int.Parse(PatientID.Substring(i, 1));
                intID = intID + intArray[i];
            }

            if (intID % 10 == 4) // every 10 IDs one will return true (digits are not randomly distributed, see Random_selection_QA.xlsx in \\VM-90318\raoariaapps$\QaCalculator)
            {
                retQA = true;
                //System.Windows.Forms.MessageBox.Show("ID = " + PatientID + " sums to = " + intID.ToString() + " and requires QA");

            }
            else
            {
                retQA = false;
                //System.Windows.Forms.MessageBox.Show("ID = " + PatientID + " sums to = " + intID.ToString() + " and does not require QA");
            }

            return retQA;
        }

        public static bool DecisionPatientYear(Patient Patient)
        {
            // Decide based on the patient birth year if the plan requires QA or not (random yes once every 10 patients).
            int intYear = 1901;
            bool retQA = true;

            DateTime PatientBirth = Patient.DateOfBirth.GetValueOrDefault();
            if (Patient.DateOfBirth.HasValue)
            {
                intYear = PatientBirth.Year;
            }

            if (intYear % 10 == 0) // every 10 IDs one will return true
            {
                retQA = true;
                //System.Windows.Forms.MessageBox.Show("Year = " + intYear.ToString() + " and requires QA");

            }
            else
            {
                retQA = false;
                //System.Windows.Forms.MessageBox.Show("Year = " + intYear.ToString() + " and does not require QA");
            }

            return retQA;
        }


        public static string DecisionMMO(List<Double> MMO, double thMMO)
        {
            // check if MMO is above threshold and if yes, require QA

            string temp_return_QA = "error";
            string temp_return_Reason = "error";
            bool temp_QA = true;

            int temp_belowTh = 0;

            for (int i_MMO = 1; i_MMO < (int)MMO[0] + 1; i_MMO++)
            {
                double temp_MMO = MMO[i_MMO];
                if (temp_MMO < thMMO) { temp_belowTh = temp_belowTh + 1; }
            }

            if (temp_belowTh == 0)
            {
                temp_return_QA = "No further PSQA required";
                temp_return_Reason = "MMO for all fields is above the threshold of = " + thMMO.ToString() + " mm";
                temp_QA = false;
            }
            else
            {
                temp_return_QA = "Perform PSQA measurement (PD)";
                temp_return_Reason = "MMO is below threshold of " + thMMO.ToString() + " mm for N=" + temp_belowTh.ToString() + " fields";
                temp_QA = true;
            }


            string temp_return = temp_return_QA + "_" + temp_return_Reason + "_" + temp_QA.ToString();
            return temp_return;

        }


        public static double TimeBetweenControlPoints(ControlPoint ControlPointOld, ControlPoint ControlPointNew)
        {
            double temp_DeltaAngle = Math.Abs(ControlPointOld.GantryAngle - ControlPointNew.GantryAngle);
            double temp_MU = Math.Abs(ControlPointOld.MetersetWeight - ControlPointNew.MetersetWeight);
            temp_MU = temp_MU * ControlPointNew.Beam.Meterset.Value; // MUs delivered in the conrol point
            double MUperDeg = temp_DeltaAngle / temp_MU;

            // implementation according to Spyridonidis Aristotelis from KSGR
            double TimeBetweenControlPoints = 100;
            if (MUperDeg <= 1.6666666)
            {
                TimeBetweenControlPoints = temp_DeltaAngle / 6.0f;
            }
            else if (MUperDeg > 1.6666666)
            {
                TimeBetweenControlPoints = (temp_DeltaAngle / 6.0f) * (MUperDeg / 1.6666666f);
            }

            if (TimeBetweenControlPoints <= 0)
            {
                TimeBetweenControlPoints = -99; //error output
            }

            return TimeBetweenControlPoints;
        }

        // Returns the leaf width (different for Edge / TB2)
        public static double GetLeafWidth(ControlPoint controlPoint, int iLeaf)
        {

            int iLeafEclipse = iLeaf + 1;
            double width = 0;

            if (controlPoint.Beam.MLC.Model.Equals("Varian High Definition 120"))
            {
                if (iLeafEclipse <= 46 && iLeafEclipse >= 14)
                {
                    width = 2.5;
                }
                else
                {
                    width = 5;
                }

            }
            else if (controlPoint.Beam.MLC.Model.Equals("Millennium 120"))
            {
                if (iLeafEclipse <= 50 && iLeafEclipse >= 11)
                {
                    width = 5;
                }
                else
                {
                    width = 10;
                }
            }
            else
            {
                if (iLeaf == 0) { System.Windows.MessageBox.Show("!! Error in Tools/GetLeafWidth !! \nMLC type = " + controlPoint.Beam.MLC.Model + "\nNot identified!"); }
                width = 5; // assuming largest possible to avoid crashes of the program
            }


            return width;
        }


        public static string GetTargetVolume(PlanSetup plan)
        {
            // returns the volume of the target used for normalizing the plan
            double temp_V = 0;

            IEnumerable<Structure> Structures = plan.StructureSet.Structures;

            foreach (Structure structure in Structures)
            {
                if (structure.Id.Equals(plan.TargetVolumeID))
                {
                    temp_V = structure.Volume;
                }
            }

            return temp_V.ToString();

        }
    }
}
