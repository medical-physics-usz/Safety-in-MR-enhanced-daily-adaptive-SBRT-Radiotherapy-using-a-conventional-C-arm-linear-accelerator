using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using USZ_RtPlanAutomator.DataQualification;

namespace USZ_RtPlanAutomator.Optimization
{
    class Optimizer
    {
        public static void Run(PlanSetup plan, out List<string> warnings)
        {
            warnings = new List<string>();
            
            if (!(plan is ExternalPlanSetup))
            {
                throw new Exception("Plan can not be optimized, not an external beam plan!");
            }
            ExternalPlanSetup eps = (ExternalPlanSetup)plan;

            if (DataChecker.CouchInsideBody(eps.StructureSet))
            {
                throw new Exception("Plan can not be optimized, couch is inside patient!");
                //warnings.Add("Couch was inside patient, check that this was ok."); // Later maybe allow optimization even if couch and body overlap
            }

            // Add automatically some default beams if no beams found for selected plan
            if (eps.Beams==null || eps.Beams.Count()==0)
            {
                throw new Exception("Plan can not be optimized, no beams found!");
                //// TODO: change to this code when automatic beam creation ok
                //try
                //{
                //    AddDefaultBeams(eps);
                //}
                //catch (Exception excp)
                //{
                //    throw new Exception(excp.Message);
                //}
                //warnings.Add("No beams found, default beams created automatically.");
            }

            // TODO: calculation model as well as other optimization parameters can be set here
            eps.OptimizeVMAT();
            eps.CalculateDose();

            //// TODO: check which objectives are met and push a little more
            //foreach(Structure s in eps.StructureSet.Structures)
            //{
            //    DVHData dvh = eps.GetDVHCumulativeData(s, VMS.TPS.Common.Model.Types.DoseValuePresentation.Absolute, 
            //                                              VMS.TPS.Common.Model.Types.VolumePresentation.Relative, 1);            
            //}
        }

        // TODO: Create default beams, does not work yet
        private static void AddDefaultBeams(ExternalPlanSetup eps)
        {
            string ptvId = eps.TargetVolumeID;
            Structure ptv = eps.StructureSet.Structures.FirstOrDefault(x => x.Id.Equals(ptvId));
            if (ptv==null)
            {
                throw new Exception("No target structure defined, can not automatically create beams!");
            }
            VVector isocenter = new VVector(Math.Round(ptv.CenterPoint.x / 10.0f) * 10.0f, Math.Round(ptv.CenterPoint.y / 10.0f) * 10.0f, Math.Round(ptv.CenterPoint.z / 10.0f) * 10.0f);
            var ebmp = new ExternalBeamMachineParameters("Truebeam4068", "6X", 600, "ARC", null);
            eps.AddArcBeam(ebmp, new VRect<double>(-100, -100, 100, 100), 30, 181, 179, GantryDirection.Clockwise, 0, isocenter);
            eps.AddArcBeam(ebmp, new VRect<double>(-100, -100, 100, 100), 330, 179, 181, GantryDirection.CounterClockwise, 0, isocenter);
        }
    }
}
