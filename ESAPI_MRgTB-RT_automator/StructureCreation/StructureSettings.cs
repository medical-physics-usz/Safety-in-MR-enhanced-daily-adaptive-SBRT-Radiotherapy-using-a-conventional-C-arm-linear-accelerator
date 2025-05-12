using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.StructureCreation
{
    class StructureSettings
    {
        public static readonly Color magenta = Color.FromRgb(255, 51, 204);
        public static readonly Color orange = Color.FromRgb(237, 125, 49);
        public static readonly Color white = Color.FromRgb(255, 255, 255);
        public static readonly Color blue = Color.FromRgb(0, 51, 204);

        private static string ptvRingIdPostfix = "+2cm_Ph";

        public static string GetPtvRingId(Structure ptv)
        {
            return ptv.Id + ptvRingIdPostfix;
        }

        private static double ptvRingThicknessMm = 20; // default value, used if a value not given
        public static double GetPtvRingDefaultWidth()
        {
            return ptvRingThicknessMm;
        }

        private static double ptvRingGapMm = 2; // default value, used if a value not given
        public static double GetPtvRingDefaultGap()
        {
            return ptvRingGapMm;
        }

        private static double sbiGapMm = 2; // default value, used if a value not given
        public static double GetSbiDefaultGap()
        {
            return sbiGapMm;
        }

        private static double ptvBuildupLayerMm = 3; // default value, used if a value not given
        public static double GetPtvDefaultBuildupLayer()
        {
            return ptvBuildupLayerMm;
        }


        private static double ptvOrganMarginMm = 3; // default value, used if a value not given
        public static double GetPtvOrganDefaultMargin()
        {
            return ptvOrganMarginMm;
        }
    }
}
