using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using USZ_RtPlanAutomator.Optimization;

namespace USZ_RtPlanAutomator.Optimization
{
    class ObjectiveCreator
    {
        public static void CreateAllObjectives(PlanSetup ps, Structure ptv, Structure ptvRing, List<List<Structure>> SbrtStructures, out List<string> allWarnings)
        {
            allWarnings = new List<string>();
            List<string> warnings = new List<string>();

            try
            {
                RemoveAllObjectives(ps);
            }
            catch (Exception excp)
            {
                throw new Exception($"ERROR at objective removal: {excp}");
            }

            try
            {
                ObjectivesFromGoals(ps, out warnings);
                allWarnings.AddRange(warnings);
            }
            catch (Exception excp)
            {
                throw new Exception($"ERROR at converting goals to objectives: {excp}");
            }

            try
            {
                CreatePtvRingObjectives(ps, ptv, ptvRing);
            }
            catch (Exception excp)
            {
                throw new Exception($"ERROR at creating PTV ring objectives: {excp}");
            }

            if (SbrtStructures != null)
            {
                try
                {
                    foreach(var sbrts in SbrtStructures)
                    {
                        CreateSbrtObjectives(ps, sbrts);
                    }
                }
                catch (Exception excp)
                {
                    throw new Exception($"ERROR at creating SBRT objectives: {excp}");
                }
            }
        }
        private static void CreatePtvRingObjectives(PlanSetup ps, Structure ptv ,Structure ptvRing)
        {
            String unit = "Gy";

            // PTV objective
            ps.OptimizationSetup.AddPointObjective(ptv, OptimizationObjectiveOperator.Lower, new DoseValue(ps.TotalDose.Dose, unit), 100, 100); 

            // Ring objectives
            ps.OptimizationSetup.AddPointObjective(ptvRing, OptimizationObjectiveOperator.Upper, new DoseValue(ps.TotalDose.Dose * 0.95, unit), 0, 100);
            ps.OptimizationSetup.AddPointObjective(ptvRing, OptimizationObjectiveOperator.Upper, new DoseValue(ps.TotalDose.Dose * 0.90, unit), 5, 75);
            ps.OptimizationSetup.AddPointObjective(ptvRing, OptimizationObjectiveOperator.Upper, new DoseValue(ps.TotalDose.Dose * 0.85, unit), 10, 75);
            ps.OptimizationSetup.AddPointObjective(ptvRing, OptimizationObjectiveOperator.Upper, new DoseValue(ps.TotalDose.Dose * 0.80, unit), 20, 75);
        }

        private static void CreateSbrtObjectives(PlanSetup ps, List<Structure> SbrtStructures)
        {
            String unit = "Gy";
            Structure gtvPh = SbrtStructures[0];
            Structure itvPh = SbrtStructures[1];
            Structure ptvPh = SbrtStructures[2];

            ps.OptimizationSetup.AddNormalTissueObjective(150, 1, 100, 30, 0.3);

            ps.OptimizationSetup.AddPointObjective(ptvPh, OptimizationObjectiveOperator.Lower, new DoseValue(ps.TotalDose.Dose, unit), 100, 150);
            ps.OptimizationSetup.AddPointObjective(ptvPh, OptimizationObjectiveOperator.Upper, new DoseValue(ps.TotalDose.Dose * 1.4, unit), 0, 100);

            ps.OptimizationSetup.AddPointObjective(itvPh, OptimizationObjectiveOperator.Lower, new DoseValue(ps.TotalDose.Dose * 1.39, unit), 100, 150);
            ps.OptimizationSetup.AddPointObjective(itvPh, OptimizationObjectiveOperator.Upper, new DoseValue(ps.TotalDose.Dose * 1.48, unit), 0, 100);

            ps.OptimizationSetup.AddPointObjective(gtvPh, OptimizationObjectiveOperator.Lower, new DoseValue(ps.TotalDose.Dose * 1.47, unit), 100, 150);
            ps.OptimizationSetup.AddPointObjective(gtvPh, OptimizationObjectiveOperator.Upper, new DoseValue(ps.TotalDose.Dose * 1.54, unit), 0, 100);
        }
        private static void ObjectivesFromGoals(PlanSetup ps, out List<string> warnings)
        {
            warnings = new List<string>();
            List<ClinicalGoal> CGList = ps.GetClinicalGoals();

            if (CGList == null || CGList.Count==0)
            {
                warnings.Add($"No clinical goals found for plan { ps.Id }! Populating optimization objectives only for PTV and PTV_Ring");
                return;
            }

            foreach (ClinicalGoal cg in CGList)
            {
                Structure structure = ps.StructureSet.Structures.FirstOrDefault(x => x.Id == cg.StructureId);

                if (structure == null)
                {
                    throw new Exception($"Clinical goals found for non existend structure {cg.StructureId}!");
                }

                OptimizationObjectiveOperator oper = SelectObjectiveOperator(cg);
                double priority = SelectObjectivePriority(cg);
                String unit = "Gy";

                double dose = -1;
                double volume = -1;

                double roundLimit = 5; // Volume in cc, volumes smaller than this rounded to 0 % in optimization objectives 

                // Read in dose and volume and create the right type of objective depending on clinical goal type
                // Note: some values have to be converted, as dose needs to be in absolute units and volume as a percent (number in the range 0-100)
                // Volumes less than or equal to roundLimit are set to 0 %
                // Note: dividing absolute volume in conversion with number 10 is not a typo, clinical goal volume saved in mm^3
                switch (cg.MeasureType)
                {
                    case MeasureType.MeasureTypeDQP_DXXXcc:
                        dose = cg.Objective.LimitUnit == ObjectiveUnit.Relative ? cg.Objective.Limit * ps.TotalDose.Dose / 100 : cg.Objective.Limit;
                        volume = cg.Objective.Value / 1000 <= roundLimit ? 0 : cg.Objective.Value / structure.Volume / 10;
                        ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                        break;

                    case MeasureType.MeasureTypeDQP_DXXX:
                        dose = cg.Objective.LimitUnit == ObjectiveUnit.Relative ? cg.Objective.Limit * ps.TotalDose.Dose / 100 : cg.Objective.Limit;
                        volume = cg.Objective.Value * structure.Volume / 100 <= roundLimit ? 0 : cg.Objective.Value; 
                        ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                        break;

                    case MeasureType.MeasureTypeDQP_VXXX:
                        dose = ps.TotalDose.Dose * cg.Objective.Value / 100;
                        if (cg.Objective.LimitUnit == ObjectiveUnit.Absolute)
                        {
                            volume = cg.Objective.Limit / 1000 <= roundLimit ? 0 : cg.Objective.Limit / structure.Volume / 10;
                        }
                        else
                        {
                            volume = cg.Objective.Limit * structure.Volume / 100 <= roundLimit ? 0 : cg.Objective.Limit;
                        }
                        ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                        break;

                    case MeasureType.MeasureTypeDQP_VXXXGy:
                        dose = cg.Objective.Value;
                        if (cg.Objective.LimitUnit == ObjectiveUnit.Absolute)
                        {
                            volume = cg.Objective.Limit / 1000 <= roundLimit ? 0 : cg.Objective.Limit / structure.Volume / 10;
                        }
                        else
                        {
                            volume = cg.Objective.Limit * structure.Volume / 100 <= roundLimit ? 0 : cg.Objective.Limit;
                        }
                        ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                        break;

                    case MeasureType.MeasureTypeDoseMean:
                        dose = cg.Objective.LimitUnit == ObjectiveUnit.Relative ? cg.Objective.Limit * ps.TotalDose.Dose / 100 : cg.Objective.Limit;
                        if (structure.DicomType.Equals("PTV")) // Exact mean dose wanted
                        {
                            volume = 50; // Volume 50 % of structure volume
                            ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                            // // Check for the possibility to have exact operator in clinical goal. Not possible right now.
                            //if (oper== OptimizationObjectiveOperator.Exact) 
                            //{
                            //    ps.OptimizationSetup.AddPointObjective(structure, OptimizationObjectiveOperator.Lower, new DoseValue(dose, unit), volume, priority);
                            //    ps.OptimizationSetup.AddPointObjective(structure, OptimizationObjectiveOperator.Upper, new DoseValue(dose, unit), volume, priority);
                            //}
                            //else
                            //{
                            //    ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                            //}
                        }
                        else // Normal maximum mean objective
                        {                            
                            ps.OptimizationSetup.AddMeanDoseObjective(structure, new DoseValue(dose, unit), priority);
                        }
                        break;

                    case MeasureType.MeasureTypeDoseMax:
                        dose = cg.Objective.LimitUnit == ObjectiveUnit.Relative ? cg.Objective.Limit * ps.TotalDose.Dose / 100 : cg.Objective.Limit;
                        volume = 0;
                        ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                        break;

                    case MeasureType.MeasureTypeDoseMin:
                        dose = cg.Objective.LimitUnit == ObjectiveUnit.Relative ? cg.Objective.Limit * ps.TotalDose.Dose / 100 : cg.Objective.Limit;
                        volume = 100;
                        ps.OptimizationSetup.AddPointObjective(structure, oper, new DoseValue(dose, unit), volume, priority);
                        break;

                    case MeasureType.MeasureTypeDoseConformity:
                        // Used more as a quality assurance, objectives for ring structures created automatically
                        break;

                    case MeasureType.MeasureTypeGradient:
                        // Not currently used at USZ
                        break; 
                }
            }
        }

        private static void RemoveAllObjectives(PlanSetup ps)
        {
            foreach (OptimizationObjective o in ps.OptimizationSetup.Objectives)
            {
                ps.OptimizationSetup.RemoveObjective(o);
            }
        }

        private static OptimizationObjectiveOperator SelectObjectiveOperator(ClinicalGoal cg)
        {
            OptimizationObjectiveOperator oper = OptimizationObjectiveOperator.None;
            switch (cg.Objective.Operator)
            {
                case ObjectiveOperator.LessThan:
                    oper = OptimizationObjectiveOperator.Upper;
                    break;
                case ObjectiveOperator.LessThanOrEqual:
                    oper = OptimizationObjectiveOperator.Upper;
                    break;
                case ObjectiveOperator.GreaterThan:
                    oper = OptimizationObjectiveOperator.Lower;
                    break;
                case ObjectiveOperator.GreaterThanOrEqual:
                    oper = OptimizationObjectiveOperator.Lower;
                    break;
                case ObjectiveOperator.Equals:
                    oper = OptimizationObjectiveOperator.Exact;
                    break;
            }
            return oper;
        }

        private static double SelectObjectivePriority(ClinicalGoal cg)
        {
            double priority = 0;
            switch (cg.Priority)
            {
                case GoalPriority.MostImportant:
                    priority = 150;
                    break;
                case GoalPriority.VeryImportant:
                    priority = 150;
                    break;
                case GoalPriority.Important:
                    priority = 75;
                    break;
                case GoalPriority.LessImportant:
                    priority = 50;
                    break;
            }
            return priority;
        }
    }
}
