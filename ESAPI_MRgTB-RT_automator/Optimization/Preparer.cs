using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using USZ_RtPlanAutomator.DataQualification;
using USZ_RtPlanAutomator.DataExtraction;
using USZ_RtPlanAutomator.StructureCreation;

namespace USZ_RtPlanAutomator.Optimization
{
    enum OptimizationType
    {
        normal,
        SBRT,
        SIB
    }
    class Preparer
    {
        public static List<string> PreparePatient(PlanSetup plan, List<Structure> ptvs, List<Structure> itvs = null, OptimizationType optimizationType = OptimizationType.normal, bool createCouch = true, string couchModel=null, bool enableOverride = true, bool overrideAir = true, Nullable<double> ptvRingWidth = null, Nullable<double> ptvRingGap = null, Nullable<double> sibGapWidth = null, bool populateObjectives = true)
        {
            StructureSet structureSet = plan.StructureSet;
            List<List<Structure>> sbrtStructures = null;
            List<Structure> sbiStructures = null;
            List<string> allWarnings = new List<string>();
            List<string> warnings;
            Structure ptvRing = null;
            Structure ptv = null;

            // Check if body is approved
            IEnumerable<Structure> allStructures = structureSet.Structures;
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
                throw new Exception($"Error: the BODY is approved. Please un-approve body before launching the script.");

            }


            // Do preparation steps specific for the selected mode (normal, SBRT, SIB)
            switch (optimizationType)
            {
                case OptimizationType.SBRT:
                    DataChecker.CheckSbrtData(structureSet, ptvs, itvs);
                    try
                    {
                        sbrtStructures = StructureCreator.CreateSbrtRings(structureSet, ptvs, itvs, enableOverride, out warnings);
                        allWarnings.AddRange(warnings);
                    }
                    catch (Exception excp)
                    {
                        throw new Exception($"Error with SBRT structure creation: {excp.Message}");
                    }
                    break;
                case OptimizationType.SIB:
                    try
                    {
                        sbiStructures = StructureCreator.CreateSbiStructures(structureSet, ptvs, enableOverride, out warnings, sibGapWidth); 
                        allWarnings.AddRange(warnings);
                        ptv = ptvs.Last(); // Last one is the biggest when ordered alphabetically
                    }
                    catch (Exception excp)
                    {
                        throw new Exception($"Error with SIB structure creation: {excp.Message}");
                    }
                   
                    break;
            }

            // Do preparation steps that are same for all modes
            DataChecker.CheckNormalData(structureSet, ptvs);
            if (ptv == null)
            {

                if (ptvs.Count == 1)
                {
                    ptv = ptvs.FirstOrDefault();
                }
                else
                {
                    ptv = StructureHelpers.CreateCombinedPtv(structureSet, ptvs, enableOverride, out warnings);
                    allWarnings.AddRange(warnings);
                }

            }

            try
            {
                ptvRing = StructureCreator.CreatePTVRing(structureSet, ptv, enableOverride, out warnings, ptvRingWidth, ptvRingGap);
                allWarnings.AddRange(warnings);
            }
            catch (Exception excp)
            {
                throw new Exception($"Error within PTV Ring structure creation: {excp.Message}");
            }

            if (createCouch)
            {
                if (couchModel==null || couchModel.Length==0)
                {
                    couchModel = CouchDataExtractor.GetDefaultCouchModel();
                }
                try
                {
                    StructureCreator.CreateCouchStructures(structureSet, couchModel, enableOverride: true, out warnings);
                    allWarnings.AddRange(warnings);
                }
                catch (Exception excp)
                {
                    throw new Exception($"Error with couch structures creation: {excp.Message}");
                }
            }

            if(overrideAir)
            {
                try
                {
                    StructureCreator.CreateAirStructures(structureSet, enableOverride, out warnings);
                    allWarnings.AddRange(warnings);
                }
                catch (Exception excp)
                {
                    throw new Exception($"Error with Air structures creation: {excp.Message}");
                }
            }
            
            try
            {
                StructureCreator.CreateHighDensityStructures(structureSet, enableOverride, out warnings, ptvs);
                allWarnings.AddRange(warnings);
            }
            catch (Exception excp)
            {
                throw new Exception($"Error with High density structure creation: {excp.Message}");
            }

            if (populateObjectives)
            {
                try
                {
                    ObjectiveCreator.CreateAllObjectives(plan, ptv, ptvRing, sbrtStructures, out warnings);
                    allWarnings.AddRange(warnings);
                }
                catch (Exception excp)
                {
                    throw new Exception($"Error with Objective creation: {excp.Message}");
                }
            }
            

            return allWarnings;
        }
    }
}
