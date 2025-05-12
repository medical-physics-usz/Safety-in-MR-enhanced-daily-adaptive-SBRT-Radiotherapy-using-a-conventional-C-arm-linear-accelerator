using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.DataExtraction
{
    class MyObjective
    {

        private string structureId;
        public string StructureId
        {
            get
            {
                return structureId;
            }
        }

        private double volume;
        public double Volume
        {
            get
            {
                return volume;
            }
        }

        private OptimizationObjectiveOperator oper;
        public OptimizationObjectiveOperator Oper
        {
            get
            {
                return oper;
            }
        }

        private DoseValue dose;
        public DoseValue Dose
        {
            get
            {
                return dose;
            }
        }

        private double priority;
        public double Priority
        {
            get
            {
                return priority;
            }
        }

        public string OperToMyString()
        {
            if (oper.ToString() == "Upper")
            {
                return "<";
            }
            if (oper.ToString() == "Lower")
            {
                return ">=";
            }
            return oper.ToString();
        }

        public string VolumeToMyString()
        {
            if (volume<0)
            {
                return "";
            }
            return $"{volume.ToString()} %";
        }

        public MyObjective(string structureId, double volume, OptimizationObjectiveOperator oper, DoseValue dose, double priority)
        {
            this.structureId = structureId;
            this.volume = volume;
            this.oper = oper;
            this.dose = dose;
            this.priority = priority;
        }
    }
}
