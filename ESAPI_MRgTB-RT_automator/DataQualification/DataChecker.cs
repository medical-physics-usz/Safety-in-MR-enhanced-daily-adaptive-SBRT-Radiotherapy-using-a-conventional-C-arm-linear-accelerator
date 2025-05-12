﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using USZ_RtPlanAutomator.StructureCreation;

namespace USZ_RtPlanAutomator.DataQualification
{
    class DataChecker
    {
        // Check that input data for SBRT structure creation is ok
        public static void CheckSbrtData(StructureSet structureSet, List<Structure> ptvs, List<Structure> itvs)
        {
            if (ptvs == null || ptvs.Count==0)
            {
                throw new Exception("Cannot create SBRT structures: PTV not selected!");
            }
            if (itvs == null || itvs.Count==0)
            {
                throw new Exception("Cannot create SBRT structures: ITV is not selected!");
            }
            if(ptvs.Count != itvs.Count)
            {
                throw new Exception("Cannot create SBRT structures: Select same number of PTVs and ITVs!");
            }
        }

        public static void CheckSbrtData(StructureSet structureSet, Structure ptv, Structure itv)
        {
            string structureDicomType = "CONTROL";
            string structureId = "testItvPtvOverlay";
            if (ptv == null )
            {
                throw new Exception("Cannot create SBRT structures: PTV not selected!");
            }
            if (itv == null)
            {
                throw new Exception("Cannot create SBRT structures: ITV is not selected!");
            }

            // Test that ITV fully inside PTV
            while (structureSet.Structures.Any(x => x.Id == structureId))
            {
                structureId += "Z";
            }
            Structure testStruct = structureSet.AddStructure(structureDicomType, structureId);
            testStruct.SegmentVolume = ptv.Or(itv).Sub(ptv);
            if (!testStruct.IsEmpty)
            {
                structureSet.RemoveStructure(testStruct);
                throw new Exception("Cannot create SBRT structures: ITV not fully inside PTV!");
            }
            structureSet.RemoveStructure(testStruct);
        }

        public static void CheckNormalData(StructureSet structureSet, List<Structure> ptvs)
        {
            if (ptvs == null || ptvs.Count==0)
            {
                throw new Exception("Can not continue: PTV not selected!");
            }
        }

        public static bool CouchInsideBody(StructureSet structureSet)
        {
            string structureDicomType = "CONTROL";
            var couch1 = structureSet.Structures.FirstOrDefault(x => x.Id == "CouchSurface");
            var couch2 = structureSet.Structures.FirstOrDefault(x => x.Id == "CouchInterior");
            var body = StructureHelpers.GetBodyStructure(structureSet);

            if (couch1 == null || couch2 == null || body == null)
            {
                throw new Exception($"Can not check if body is inside couch, atleast on of the structures is missing!");
            }

            string tempId = "temp";
            while (structureSet.Structures.Any(x => x.Id == tempId))
            {
                tempId += "Z";
            }
            Structure tempStruct = structureSet.AddStructure(structureDicomType, tempId);
            tempStruct.SegmentVolume = body.And(couch1);
            bool isIn1 = !tempStruct.IsEmpty;
            tempStruct.SegmentVolume = body.And(couch2);
            bool isIn2 = !tempStruct.IsEmpty;
            structureSet.RemoveStructure(tempStruct);
            return isIn1 || isIn2;
        }

        // Checks that SIB data has 2 or 3 structures, and that they are ordered from smallest to biggest and that structures are fully inside one another
        // Expected naming style PTV1_1a, PTV2_1a, PTV3_1a works with alphabetical ordering
        // Alphabetical ordering is automatically achieved with GUI, because PTVs are alphabetically ordered in the check box list
        public static void CheckSibData(StructureSet structureSet, List<Structure> ptvs)
        {
            string structureDicomType = "CONTROL";
            string structureId = "testSbiPtvOverlay";

            if (ptvs == null || ptvs.Count == 0)
            {
                throw new Exception("Cannot create SIB structures: PTVs not selected!");
            }
            else if (ptvs.Count == 1)
            {
                throw new Exception("Cannot create SIB structures: only one PTV selected!");
            }
            else if (ptvs.Count > 3)
            {
                throw new Exception("Cannot create SIB structures: too many PTVs selected!");
            }

            // Test that PTV is fully inside the next PTV
            for(int i=0; i<ptvs.Count-1; i++)
            {
                while (structureSet.Structures.Any(x => x.Id == structureId))
                {
                    structureId += "Z";
                }
                Structure testStruct = structureSet.AddStructure(structureDicomType, structureId);
                testStruct.SegmentVolume = ptvs.ElementAt(i+1).Or(ptvs.ElementAt(i)).Sub(ptvs.ElementAt(i+1));
                if (!testStruct.IsEmpty)
                {
                    structureSet.RemoveStructure(testStruct);
                    throw new Exception($"Cannot create SIB structures: {ptvs.ElementAt(i).Id} not fully inside {ptvs.ElementAt(i+1).Id}!");
                }
                structureSet.RemoveStructure(testStruct);
            }
        }

        public static void CheckContext(ScriptContext context)
        {
            Patient patient;
            List<Course> courses;
            try
            {
                patient = context.Patient;
                if (patient == null)
                {
                    throw new Exception("No patient selected! Open a patient and restart the script.");
                }
            }
            catch
            {
                throw new Exception("No patient selected! Open a patient and restart the script.");
            }
            try
            {
                courses = patient.Courses.ToList();
                if(courses==null || courses.Count==0)
                {
                    throw new Exception("Patient has no courses! Create a course and restart the script.");
                }
            }
            catch
            {
                throw new Exception("Patient has no courses! Create a course and restart the script.");
            }
            bool plansExist = false;
            foreach (Course course in courses)
            {
                if (course.PlanSetups != null && course.PlanSetups.Count() > 0)
                {
                    plansExist = true;
                    break;
                }
            }
            if (!plansExist)
            {
                throw new Exception("Patient has no plan! Create a plan and restart the script.");
            }
        }
    }
}
