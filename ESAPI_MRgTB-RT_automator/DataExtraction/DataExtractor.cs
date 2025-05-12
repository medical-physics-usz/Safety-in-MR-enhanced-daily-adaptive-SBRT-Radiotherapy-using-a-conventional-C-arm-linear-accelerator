using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.DataExtraction
{
    class DataExtractor
    {
        public static List<ClinicalGoal> SelectGoalsByStructure(PlanSetup ps, Structure s)
        {
            if (ps == null)
            {
                return null;
            }
            List<ClinicalGoal> CGList = ps.GetClinicalGoals();
            if (CGList == null || CGList.Count == 0 || s == null)
            {
                return CGList;
            }
            List<ClinicalGoal> myClinicalGoals = new List<ClinicalGoal>();
            foreach (ClinicalGoal cg in CGList)
            {
                if (cg.StructureId == s.Id)
                {
                    myClinicalGoals.Add(cg);
                }
            }
            return myClinicalGoals;
        }

        public static List<MyObjective> SelectObjectivesByStructure(PlanSetup ps, Structure s)
        {
            if (ps == null)
            {
                return null;
            }
            List<OptimizationObjective> objectives = ps.OptimizationSetup.Objectives.ToList<OptimizationObjective>();
            if (objectives == null)
            {
                return null;
            }
            List<MyObjective> selectedObjectives = new List<MyObjective>();
            foreach (OptimizationObjective o in objectives)
            {
                if (s != null && o.StructureId != s.Id)
                {
                    continue;
                }
                double myVolume = -1;
                DoseValue myDose = new DoseValue();
                if (o is OptimizationPointObjective)
                {
                    OptimizationPointObjective po = (OptimizationPointObjective)o;
                    myVolume = po.Volume;
                    myDose = po.Dose;
                }
                if (o is OptimizationMeanDoseObjective)
                {
                    OptimizationMeanDoseObjective mdo = (OptimizationMeanDoseObjective)o;
                    myDose = mdo.Dose;
                }
                if (o is OptimizationEUDObjective)
                {
                    OptimizationEUDObjective eudo = (OptimizationEUDObjective)o;
                    myDose = eudo.Dose;
                }
                selectedObjectives.Add(
                    new MyObjective(
                        o.StructureId,
                        myVolume,
                        o.Operator,
                        myDose,
                        o.Priority
                        ));
            }
            return selectedObjectives;
        }

        public static string FindTargetItvId(PlanSetup ps)
        {
            string targetPtvId = ps.TargetVolumeID;
            string targetItvId = targetPtvId.Replace("PTV", "ITV");
            if (ps.StructureSet.Structures.Any(s => s.Id == targetItvId))
            {
                return targetItvId;
            }
            targetItvId = targetPtvId.Replace("PTV", "CTV");
            return ps.StructureSet.Structures.Any(s => s.Id == targetItvId) ? targetItvId : null;
        }
    }
}
