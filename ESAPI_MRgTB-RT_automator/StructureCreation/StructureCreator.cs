using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Media;
using USZ_RtPlanAutomator.DataQualification;
using USZ_RtPlanAutomator.DataExtraction;

namespace USZ_RtPlanAutomator.StructureCreation
{
    class StructureCreator
    {
       
        public static Structure CreatePTVRing(StructureSet structureSet, List<Structure> ptvs, bool enableOverride, out List<string> warnings, Nullable<double> ringWidth = null, Nullable<double> ringGap = null)
        {
            warnings = new List<string>();
            List<string> newWarnings;
            Structure ptv = null;

            DataChecker.CheckNormalData(structureSet, ptvs);
            if (ptvs.Count == 1)
            {
                ptv = ptvs.FirstOrDefault();
            }
            else
            {
                ptv = StructureHelpers.CreateCombinedPtv(structureSet, ptvs, enableOverride, out newWarnings);
                warnings.AddRange(newWarnings);
            }
            Structure ring = CreatePTVRing(structureSet, ptv, enableOverride, out newWarnings, ringWidth, ringGap);
            warnings.AddRange(newWarnings);

            return ring;
        }

        public static Structure CreatePTVRing(StructureSet structureSet, Structure ptv, bool enableOverride, out List<string> warnings, Nullable<double> ringWidth = null, Nullable<double> ringGap = null)
        {            
            string ringDicomType = "CONTROL";
            string ringId = StructureSettings.GetPtvRingId(ptv);
            Color ringColor = StructureSettings.white;
            warnings = new List<string>();

             // Set PTV ring thickness correctly
            double ringThicknessMm = StructureSettings.GetPtvRingDefaultWidth();
            if (ringWidth!=null)
            {
                ringThicknessMm = (double) ringWidth;
            }
            if (ringThicknessMm<=0)
            {
                throw new Exception("PTV ring width has to be a positive number or null!");
            }
            if (ringThicknessMm > 50)
            {
                throw new Exception("PTV ring width maximum is 50 mm!"); // This is maximum for structure margins
            }

            // Set PTV ring gap thickness correctly
            double ringGapMm = StructureSettings.GetPtvRingDefaultGap();
            if (ringGap != null)
            {
                ringGapMm = (double)ringGap;
            }
            if (ringGapMm < 0)
            {
                throw new Exception("PTV ring gap has to be a positive number or null!");
            }
            if (ringGapMm > 50)
            {
                throw new Exception("PTV ring gap maximum is 50 mm!"); // This is maximum for structure margins
            }

            var innerRingSegmentVolume = ptv.Margin(ringGapMm);
            var outerRingSegmentVolume = innerRingSegmentVolume.Margin(ringThicknessMm);

            // If the structure exists and override is not enabled, just return the existing one
            if (!StructureHelpers.StructureIsWritable(structureSet, ringId, enableOverride))
            {
                warnings.Add($"PTV ring structure {ringId} can not be created as it already exists!");
                return structureSet.Structures.FirstOrDefault(x => x.Id == ringId);
            }

            // Create PTV Ring structure
            var bodyStructure = StructureHelpers.GetBodyStructure(structureSet);
            var ring = structureSet.AddStructure(ringDicomType, ringId);
            //ring.SegmentVolume = outerRingSegmentVolume.Sub(innerRingSegmentVolume);
            ring.SegmentVolume = outerRingSegmentVolume;
            //ring.SegmentVolume = ring.And(bodyStructure); // Check that whole ring structure is inside body --> do not use this because PTV may be in high res and body not
            ring.Color = ringColor;

            if (ring.IsEmpty)
            {
                structureSet.RemoveStructure(ring);
                throw new Exception ($"PTV ring structure {ringId} is empty! Ring not created.");
            }

            return ring;
        }

        // Find corresponding ITV for each PTV and create SBRT structures for each pair
        public static List<List<Structure>> CreateSbrtRings(StructureSet structureSet, List<Structure> ptvs, List<Structure> itvs, bool enableOverride, out List<string> warnings)
        {
            List<List<Structure>> allNewStructures = new List<List<Structure>>();
            warnings = new List<string>();
            DataChecker.CheckSbrtData(structureSet, ptvs, itvs);

            if (ptvs.Count==1)
            {
                allNewStructures.Add(CreateSbrtRings(structureSet, ptvs.FirstOrDefault(), itvs.FirstOrDefault(), enableOverride, out warnings));
                return allNewStructures;
            }

            foreach(Structure ptv in ptvs)
            {
                // Find the correct ITV for current PTV, first try to find similar name
                string itvId = ptv.Id.Replace("PTV", "ITV");
                if (!itvs.Any(x => x.Id.Equals(itvId)))
                {
                    itvId = ptv.Id.Replace("PTV", "CTV");
                    if (!itvs.Any(x => x.Id.Equals(itvId)))
                    {
                        // If ITV with similar name not found, take the first ITV structure fully inside of the PTV
                        foreach (Structure testItv in itvs)
                        {
                            try
                            {
                                itvId = testItv.Id;
                                DataChecker.CheckSbrtData(structureSet, ptv, testItv);
                                break;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
                Structure itv = itvs.FirstOrDefault(x => x.Id.Equals(itvId));
                try
                {
                    DataChecker.CheckSbrtData(structureSet, ptv, itv);
                }
                catch
                {
                    throw new Exception($"Could not find the correct ITV for PTV structure {ptv.Id}. Try preparing each PTV-ITV pair separately.");
                }
                List<Structure> newSbrtStructures = CreateSbrtRings(structureSet, ptv, itv, enableOverride, out warnings);
                allNewStructures.Add(newSbrtStructures);
                itvs.Remove(itv); // Do not use same ITV for more than one PTV
            }

            return allNewStructures;
        }

        public static List<Structure> CreateSbrtRings(StructureSet structureSet, Structure ptv, Structure itv, bool enableOverride, out List<string> warnings)
        {
            // Set parameters
            string structureDicomType = "CONTROL";
            string idEnding = "_Ph";
            string warningList = "";
            warnings = new List<string>();

            // Check that data ok
            DataChecker.CheckSbrtData(structureSet, ptv, itv);

            // Create the names for new structures
            string gtvPhId = ptv.Id.Replace("PTV", "GTV") + idEnding;
            string itvPhId = ptv.Id.Replace("PTV", "ITV") + idEnding;
            string ptvPhId = ptv.Id + idEnding;

            List<Structure> sbrtStructures = new List<Structure>();
            var segmentVolumes = GetSbrtSegmentVolumes(structureSet, ptv, itv);

            //Create SBRT GTV structure
            if (StructureHelpers.StructureIsWritable(structureSet, gtvPhId, enableOverride))
            {
                var gtvPh = structureSet.AddStructure(structureDicomType, gtvPhId);
                gtvPh.SegmentVolume = segmentVolumes[0];
                gtvPh.Color = StructureSettings.blue;

                if (gtvPh.Volume < 0.02)
                {
                    segmentVolumes = GetSbrtSegmentVolumes(structureSet, ptv, itv, gtvThicknessPct: 75, gtvItvGapMm: 0.5);
                    gtvPh.SegmentVolume = segmentVolumes[0];
                    if (gtvPh.IsEmpty)
                    {
                        structureSet.RemoveStructure(gtvPh);
                        throw new Exception($"SBRT GTV volume is empty!");
                    }
                    //warnings.Add($"GTV structure {gtvPhId} is very small. Check that SBRT structures are ok.");
                }
                sbrtStructures.Add(gtvPh);
                structureSet.RemoveStructure(gtvPh);
            }
            else
            {
                warningList += warningList.Length > 0 ? $", {gtvPhId}" : gtvPhId;
                sbrtStructures.Add(structureSet.Structures.FirstOrDefault(x => x.Id == gtvPhId));
            }

            // Create SBRT ITV structure
            if (StructureHelpers.StructureIsWritable(structureSet, itvPhId, enableOverride))
            {
                var itvPh = structureSet.AddStructure(structureDicomType, itvPhId);
                itvPh.SegmentVolume = segmentVolumes[1];
                itvPh.Color = itv.Color;
                sbrtStructures.Add(itvPh);
                structureSet.RemoveStructure(itvPh);
            }
            else
            {
                warningList += warningList.Length > 0 ? $", {itvPhId}" : itvPhId;
                sbrtStructures.Add(structureSet.Structures.FirstOrDefault(x => x.Id == itvPhId));
            }

            // Create SBRT PTV structure
            if (StructureHelpers.StructureIsWritable(structureSet, ptvPhId, enableOverride))
            {
                var ptvPh = structureSet.AddStructure(structureDicomType, ptvPhId);
                ptvPh.SegmentVolume = segmentVolumes[2];
                ptvPh.Color = ptv.Color;
                sbrtStructures.Add(ptvPh);
            }
            else
            {
                warningList += warningList.Length > 0 ? $", {ptvPhId}" : ptvPhId;
                sbrtStructures.Add(structureSet.Structures.FirstOrDefault(x => x.Id == ptvPhId));
            }

            if (warningList.Length > 0)
            {
                warnings.Add($"Following SBRT structures can not be created, as they already exist: {warningList}");
            }
        
            return sbrtStructures;
        }


        /*public static List<Structure> CreateAirStructures(StructureSet structureSet, List<Structure> ptvs, bool enableOverride, out List<string> warnings)
        {
            warnings = new List<string>();
            List<string> newWarnings;
            Structure ptv = null;

            DataChecker.CheckNormalData(structureSet, ptvs);
            if (ptvs.Count == 1)
            {
                ptv = ptvs.FirstOrDefault();
            }
            else
            {
                ptv = StructureHelpers.CreateCombinedPtv(structureSet, ptvs, enableOverride, out newWarnings);
                warnings.AddRange(newWarnings);
            }
            List<Structure> airStructures = CreateAirStructures(structureSet, ptv, enableOverride, out newWarnings);
            warnings.AddRange(newWarnings);

            return airStructures;
        }*/

        // Find Air in CT and creates a structure for that
        // Only returns new structures, not existing ones
        public static List<Structure> CreateAirStructures(StructureSet structureSet, bool enableOverride, out List<string> warnings)
        {
            string structureDicomType = "CONTROL";
            string airPTVId = "OR_Air_Ph";
            string strictAirId = "Air_Thr_Ph";
            int airThreshold = -100; // HU threshold of all air voxels, including ones with partial volume effects (old threshold = -150)
            int strictAirThreshold = -850; // HU threshold under which all voxels are air for sure and not for example lung tissue (old threshold = -150)
            double airMarginMm = 5; // Margin in which all air should be from strictly thresholded air voxels
            double HUToAssingToPTV = -1000; // Air voxels inside PTV should be assigned to water.
            Color airStructColor = StructureSettings.white;
            string warningList = "";
            warnings = new List<string>();
            List<string> newWarnings = new List<string>();
            List<Structure> airStructures = new List<Structure>();

            // Threshold everything that is air for sure
            // Thresholding possible only with lower threshold value, so actually we get anti-air and not air
            Structure antiAir = StructureHelpers.ThresholdImage(structureSet, strictAirThreshold, out newWarnings, strictAirId, structureDicomType, enableOverride: enableOverride);
            warnings.AddRange(newWarnings);

            // Create structure for air inside the PTV
            if (StructureHelpers.StructureIsWritable(structureSet, airPTVId, enableOverride))
            {
                var airPTV = structureSet.AddStructure(structureDicomType, airPTVId);
                Structure body = StructureHelpers.GetBodyStructure(structureSet);
                airPTV.SegmentVolume = body.Sub(antiAir).Margin(airMarginMm);

                // Do thresholding again to find all possible air voxels
                structureSet.RemoveStructure(antiAir);
                antiAir = StructureHelpers.ThresholdImage(structureSet, airThreshold, out newWarnings, strictAirId, structureDicomType, enableOverride: enableOverride);
                warnings.AddRange(newWarnings);

                /*if (ptv.IsHighResolution)
                {
                    if (airPTV.CanConvertToHighResolution()) { airPTV.ConvertToHighResolution(); }
                    if (antiAir.CanConvertToHighResolution()) { antiAir.ConvertToHighResolution(); }
                }*/

                // Set the correct segment volume to the air inside ptv structure
                //airPTV.SegmentVolume = airPTV.Sub(antiAir).And(ptv);
                airPTV.SegmentVolume = airPTV.Sub(antiAir);

                if (airPTV.IsEmpty || airPTV.MeshGeometry.Bounds.IsEmpty)
                {
                    structureSet.RemoveStructure(airPTV);
                }

                if (airPTV.Volume < 0.2) // Small air volumes do not matter
                {
                    structureSet.RemoveStructure(airPTV);
                }
                airPTV.Color = airStructColor;
                airPTV.SetAssignedHU(HUToAssingToPTV);
                airStructures.Add(airPTV);
            }
            else
            {
                warningList += warningList.Length > 0 ? $", {airPTVId}" : airPTVId;
            }

            if (warningList.Length > 0)
            {
                warnings.Add($"Following air structures could not be created, as they already exist: {warningList}");
            }

            structureSet.RemoveStructure(antiAir);

            return airStructures;
        }

        // Creates high density structure for metal in body (HighDensity_Ph) and markers just outside of body (HighCheck_Ph)
        // Returns only the new high density structures, not existing ones
        public static List<Structure> CreateHighDensityStructures(StructureSet structureSet, bool enableOverride, out List<string> warnings, List<Structure> ptvs = null)
        {
            string structureDicomType = "CONTROL";
            string highDensInBodyId = "HighDensity_Ph";
            string markersId = "HighCheck_Ph";
            string highDensInBodyMarginId = "OR_Hip_Water_Ph";
            string highDensInPTVMarginId = "OR_Markers_Water_Ph";
            int HUThreshold = 1800; // Lowest that works at the moment // 2290; // HU value 2298 corresponds to density 3 g/cc
            double HUToAssing = 3567; // HU of titanium
            double bodyMarginMm = 5; // Margin (in mm) inside which markers are suppoused to be
            double markerMarginMm = 3; // Margin (in mm) with which marker structures are expanded
            Color highDensInColor = StructureSettings.magenta;
            Color highDensOutColor = StructureSettings.orange;

            string warningList = "";
            warnings = new List<string>();
            List<Structure> highDensStructures = new List<Structure>();
            List<string> newWarnings = new List<string>();

            // Threshold high densities
            Structure thresholdResult = StructureHelpers.ThresholdImage(structureSet, HUThreshold, out newWarnings, enableOverride: enableOverride);
            warnings.AddRange(newWarnings);

            // Separate markers from high density objects inside the body
            Structure body = StructureHelpers.GetBodyStructure(structureSet);
            SegmentVolume innerBody = body.Margin(-bodyMarginMm);
            SegmentVolume highDensInBody = thresholdResult.And(innerBody);
            SegmentVolume markers = thresholdResult.Sub(innerBody).And(body);
            markers = markers.Margin(markerMarginMm); // Expand the structures, otherwise often just one voxel

            // Remove markers from body and remove threshold structure
            body.SegmentVolume = body.Sub(markers);
            structureSet.RemoveStructure(thresholdResult);

            // Create structure for high density areas inside the body
            if (StructureHelpers.StructureIsWritable(structureSet, highDensInBodyId, enableOverride))
            {
                var highDensInStruct = structureSet.AddStructure(structureDicomType, highDensInBodyId);
                var highDensInMarginStruct = structureSet.AddStructure(structureDicomType, highDensInBodyMarginId);
                var highDensInPtvStruct = structureSet.AddStructure(structureDicomType, highDensInBodyId + "_inPTV");
                var highDensInPTVMarginStruct = structureSet.AddStructure(structureDicomType, highDensInPTVMarginId);
                AxisAlignedMargins margins = new AxisAlignedMargins ( 0, 10, 3, 3, 10, 3, 3 );
                foreach (Structure ptv in ptvs)
                {
                    // Create structure for high density areas inside the body but outside ptv
                    highDensInStruct.SegmentVolume = highDensInBody.Sub(ptv);
                    highDensInStruct.Color = highDensInColor;
                    highDensInStruct.SetAssignedHU(HUToAssing);

                    //create 1cm margin for high density areas inside the body but outside ptv
                    highDensInMarginStruct.SegmentVolume = highDensInBody.Sub(ptv).Margin(10);
                    highDensInMarginStruct.Color = highDensInColor;
                    highDensInMarginStruct.SetAssignedHU(0);

                    highDensInPtvStruct.SegmentVolume = highDensInBody.And(ptv);
                    highDensInPtvStruct.SetAssignedHU(HUToAssing);

                    //create 1cm margin for high density areas inside the ptv
                    //highDensInPTVMarginStruct.SegmentVolume = highDensInBody.And(ptv).Margin(8);
                    highDensInPTVMarginStruct.SegmentVolume = highDensInBody.And(ptv).AsymmetricMargin(margins);
                    highDensInPTVMarginStruct.Color = highDensInColor;
                    highDensInPTVMarginStruct.SetAssignedHU(0);

                }
                //structureSet.RemoveStructure(highDensInPtvStruct);

                // Ask user to assign high material
                warnings.Add($"High density areas inside body");
                highDensStructures.Add(highDensInStruct);

                if (highDensInStruct.IsEmpty){structureSet.RemoveStructure(highDensInStruct);}

                if (highDensInMarginStruct.IsEmpty){structureSet.RemoveStructure(highDensInMarginStruct);}

                if (highDensInPTVMarginStruct.IsEmpty){structureSet.RemoveStructure(highDensInPTVMarginStruct);}

            }
            else
            {
                warningList += warningList.Length > 0 ? $", {highDensInBodyId}" : highDensInBodyId;
            }

            // Create structure for high density areas outside the body
            if (StructureHelpers.StructureIsWritable(structureSet, markersId, enableOverride))
            {
                var markersStruct = structureSet.AddStructure(structureDicomType, markersId);
                markersStruct.SegmentVolume = markers;
                markersStruct.Color = highDensOutColor;
                if (markersStruct.IsEmpty)
                {
                    structureSet.RemoveStructure(markersStruct);
                }
                else
                {
                    warnings.Add($"Some high density areas removed from body, check structure {markersId}");
                    highDensStructures.Add(structureSet.Structures.FirstOrDefault(x => x.Id.Equals(markersId, StringComparison.OrdinalIgnoreCase)));
                }
            }
            else
            {
                warningList += warningList.Length > 0 ? $", {markersId}" : markersId;
            }

            if (warningList.Length > 0)
            {
                warnings.Add($"Following high density structures could not be created, as they already exist: {warningList}");
            }

            return highDensStructures;
        }

        // Positioning does not work perfectly, manual check needed
        public static void CreateCouchStructures(StructureSet structureSet, string couchModel, bool enableOverride, out List<string> warnings)
        {
            warnings = new List<string>();
            if (couchModel==null || couchModel.Length==0)
            {
                throw new Exception("Please select a couch model before creating couch structures!");
            }

            // Create couch structures
            AddCouchWithBodyMargin(structureSet, couchModel, 0, enableOverride);

            // Move created couch structures to correct height
            List<string> newWarnings = new List<string>();
            var couchShiftY = ImageFeatureExtractor.FindCouchShiftY(structureSet, out newWarnings);
            warnings.AddRange(newWarnings);
            if (couchShiftY > 0 && couchShiftY <= 50) // 50 is maximum for margin method, too big shift probably a bug anyhow, do to visible undelaying support structures in CT
            {
                AddCouchWithBodyMargin(structureSet, couchModel, couchShiftY, enableOverride);
            }
            if (DataChecker.CouchInsideBody(structureSet))
            {
                warnings.Add($"WARNING: Couch inside BODY! Check that created couch structures are correctly positioned!");
            }
        }

        public static List<Structure> CreatePtvsWithoutBuildupLayer(StructureSet structureSet, List<Structure> ptvs, bool enableOverride, out List<string> warnings, Nullable<double> buildupLayerWidth = null)
        {
            string structureDicomType = "CONTROL";
            string ptvIdEnding = "_Ph";
            Color structureColor = StructureSettings.orange;

            warnings = new List<string>();
            List<Structure> cuttedPtvs = new List<Structure>();

            var body = StructureHelpers.GetBodyStructure(structureSet); 

            // Set buildup layer thickness correctly
            double buildupLayerMm = StructureSettings.GetPtvDefaultBuildupLayer();
            if (buildupLayerWidth != null)
            {
                buildupLayerMm = (double)buildupLayerWidth;
            }
            if (buildupLayerMm <= 0)
            {
                throw new Exception("PTV buildup layer width has to be a positive number!");
            }
            if (buildupLayerMm > 50)
            {
                throw new Exception("PTV buildup layer width maximum is 50 mm!"); // This is maximum for structure margins
            }

            foreach (var ptv in ptvs)
            {
                // If the structure exists and override is not enabled, just return the existing one
                string ptvPhId = ptv.Id + ptvIdEnding;
                if (!StructureHelpers.StructureIsWritable(structureSet, ptvPhId, enableOverride))
                {
                    warnings.Add($"Physics structure {ptvPhId} can not be created as it already exists!");
                }

                Structure ptvPh = structureSet.AddStructure(structureDicomType, ptvPhId);
                ptvPh.Color = structureColor;

                // Test if PTV close to body edges
                ptvPh.SegmentVolume = ptv.Sub(body.Margin(-buildupLayerMm));
                if (!ptvPh.IsEmpty)  // PTV was actually close to body edges
                {
                    ptvPh.SegmentVolume = ptv.And(body.Margin(-buildupLayerMm));
                    cuttedPtvs.Add(ptvPh);
                }
                else // No cut needed
                {
                    structureSet.RemoveStructure(ptvPh);
                }
            }
            return cuttedPtvs;
        }

        public static List<Structure> CreateSbiStructures(StructureSet structureSet, List<Structure> ptvs, bool enableOverride, out List<string> warnings, Nullable<double> gapWidth = null)
        {
            // Set parameters
            string structureDicomType = "CONTROL";
            string idEnding = "_Ph";
            string warningList = "";

            warnings = new List<string>();
            List<Structure> sbiStructures = new List<Structure>();

            // Check data
            DataChecker.CheckSibData(structureSet, ptvs);

            // Set SBI gap thickness correctly
            double sbiGapMm = StructureSettings.GetSbiDefaultGap();
            if (gapWidth != null)
            {
                sbiGapMm = (double) gapWidth;
            }
            if (sbiGapMm <= 0)
            {
                throw new Exception("SIB gap width has to be a positive number!");
            }
            if (sbiGapMm > 50)
            {
                throw new Exception("SIB gap width maximum is 50 mm!"); // This is maximum margin size
            }

            //Create SIB physics structure(s)
            for (int i=1; i<ptvs.Count; i++) // skips the first PTV
            {
                Structure ptv = ptvs.ElementAt(i);
                Structure smallerPtv = ptvs.ElementAt(i - 1);
                string structureId = ptv.Id + idEnding;
                if (StructureHelpers.StructureIsWritable(structureSet, structureId, enableOverride))
                {
                    var ptvPh = structureSet.AddStructure(structureDicomType, structureId);
                    ptvPh.SegmentVolume = ptv.Sub(smallerPtv.Margin(sbiGapMm));
                    ptvPh.Color = ptv.Color;
                    sbiStructures.Add(ptvPh);
                }
                else
                {
                    warningList += warningList.Length > 0 ? $", {structureId}" : structureId;
                    sbiStructures.Add(structureSet.Structures.FirstOrDefault(x => x.Id == structureId));
                }
            }

            if (warningList.Length > 0)
            {
                warnings.Add($"Following SIB structures could not be created, as they already exist: {warningList}");
            }

            return sbiStructures;
        }
        public static Structure CreateBolusStructure(StructureSet structureSet, List<Structure> ptvs, bool enableOverride, out List<string> warnings)
        {
            warnings = new List<string>();
            List<string> newWarnings;
            Structure ptv = null;

            DataChecker.CheckNormalData(structureSet, ptvs);
            if (ptvs.Count == 1)
            {
                ptv = ptvs.FirstOrDefault();
            }
            else
            {
                ptv = StructureHelpers.CreateCombinedPtv(structureSet, ptvs, enableOverride, out newWarnings);
                warnings.AddRange(newWarnings);
            }

            var bolus = CreateBolusStructure(structureSet, ptv, enableOverride, out newWarnings);
            warnings.AddRange(newWarnings);

            return bolus;
        }

        public static Structure CreateBolusStructure(StructureSet structureSet, Structure ptv, bool enableOverride, out List<string> warnings)
        {
            string structureDicomType = "CONTROL";
            string structureId = "OR_Bolus_Ph";
            double HUToAssing = 0; // Bolus should be assigned as water.
            Color structColor = StructureSettings.magenta;

            warnings = new List<string>();
            Structure bolus = null;

            // Create bolus structure (PTV that is outside of body)
            if (StructureHelpers.StructureIsWritable(structureSet, structureId, enableOverride))
            {
                var body = StructureHelpers.GetBodyStructure(structureSet);

                bolus = structureSet.AddStructure(structureDicomType, structureId);
                bolus.SegmentVolume = ptv.Sub(body);
                bolus.Color = structColor;
                bolus.SetAssignedHU(HUToAssing);

                if (bolus.IsEmpty)
                {
                    structureSet.RemoveStructure(bolus);
                    bolus = null;
                    warnings.Add($"No bolus found! PTV is fully inside body.");
                }
                else
                {
                    warnings.Add($"Bolus structure {structureId} created. Adjust borders to match actual bolus.");
                }
            }
            else
            {
                warnings.Add($"Bolus structure {structureId} could not be created, as it already exists!");
            }

            return bolus;
        }

        public static List<Structure> SplitOrganPartlyInPtv(StructureSet structureSet, List<Structure> ptvs, Structure organ, bool enableOverride, out List<string> warnings, Nullable<double> gapWidth = null)
        {
            // Set parameters
            string structureDicomType = "CONTROL";
            string inPtvEnding = "_inPtv_Ph";
            string outPtvEnding = "_outPtv_Ph";
            Color structureColor = StructureSettings.magenta;

            warnings = new List<string>();
            List<string> newWarnings;
            Structure ptv = null;
            List<Structure> splittedOrgan = new List<Structure>();

            // Check data
            DataChecker.CheckNormalData(structureSet, ptvs);
            if (organ == null)
            {
                throw new Exception("Select an organ structure in Structure dropdown menu!");
            }
            if(organ.IsEmpty)
            {
                warnings.Add($"Selected organ structure is empty!");
                return splittedOrgan;
            }

            // Set PTV to organ gap thickness correctly
            double ptvOrganGapMm = StructureSettings.GetPtvOrganDefaultMargin();
            if (gapWidth != null)
            {
                ptvOrganGapMm = (double)gapWidth;
            }
            if (ptvOrganGapMm <= 0)
            {
                throw new Exception("PTV to organ gap width has to be a positive number!");
            }
            if (ptvOrganGapMm > 50)
            {
                throw new Exception("PTV to organ gap width maximum is 50 mm!"); // This is maximum margin size in ESAPI
            }

            // Combine PTVs if there are more than one
            if (ptvs.Count == 1)
            {
                ptv = ptvs.FirstOrDefault();
            }
            else
            {
                ptv = StructureHelpers.CreateCombinedPtv(structureSet, ptvs, enableOverride, out newWarnings);
                warnings.AddRange(newWarnings);
            }

            //Create new physics structures for given organ
            string structureInId = organ.Id + inPtvEnding;
            string structureOutId = organ.Id + outPtvEnding;
            if (StructureHelpers.StructureIsWritable(structureSet, structureInId, enableOverride) && StructureHelpers.StructureIsWritable(structureSet, structureOutId, enableOverride))
            {
                var organIn = structureSet.AddStructure(structureDicomType, structureInId);
                organIn.SegmentVolume = organ.And(ptv);
                organIn.Color = structureColor;
                splittedOrgan.Add(organIn);

                var organOut = structureSet.AddStructure(structureDicomType, structureOutId);
                organOut.SegmentVolume = organ.Sub(ptv.Margin(ptvOrganGapMm));
                organOut.Color = structureColor;
                splittedOrgan.Add(organOut);

                if (organIn.IsEmpty)
                {
                    warnings.Add($"Organ {organ.Id} fully outside PTV, splitting not done!");
                    structureSet.RemoveStructure(organIn);
                    structureSet.RemoveStructure(organOut);
                }else if(organOut.IsEmpty)
                {
                    warnings.Add($"Organ {organ.Id} fully inside PTV, splitting not done!");
                    structureSet.RemoveStructure(organIn);
                    structureSet.RemoveStructure(organOut);
                }
            }
            else
            {
                warnings.Add($"Splitting of organ {organ.Id} to parts inside and outside PTV could not be done, as corresponding structures already exist!");
            }

            return splittedOrgan;
        }

        // Calculate segment volumes, gtv thickness in percent is optional
        public static List<SegmentVolume> GetSbrtSegmentVolumes(StructureSet structureSet, Structure ptv, Structure itv, double gtvThicknessPct = 50, double gtvItvGapMm = 1, double itvPtvGapMm = 1)
        {
            // Set parameters
            double geomCoef = Math.Pow((3.0 / 4.0 / Math.PI), (1.0 / 3.0)); // structures approximated as spheres
            string structureDicomType = "CONTROL";
            string idEnding = "_HR";

            // Create ptv and itv in high resolution
            var hrPtv = structureSet.AddStructure(structureDicomType, ptv.Id + idEnding);
            var hrItv = structureSet.AddStructure(structureDicomType, itv.Id + idEnding);
            hrPtv.SegmentVolume = ptv.SegmentVolume;
            hrItv.SegmentVolume = itv.SegmentVolume;
            if (hrPtv.CanConvertToHighResolution()) { hrPtv.ConvertToHighResolution(); }
            if (hrItv.CanConvertToHighResolution()) { hrItv.ConvertToHighResolution(); }


            // Calculate correct volumes
            double itvToGtvMarginMm = geomCoef * (100 - gtvThicknessPct) / 100 * Math.Pow(hrItv.Volume, 1.0 / 3.0) * 10; // Last number is conversion from cm to mm
            var gtvPhSegVol = hrItv.Margin(-itvToGtvMarginMm);
            var gtvWithMargin = gtvPhSegVol.Margin(gtvItvGapMm);
            var itvPhSegVol = hrItv.Sub(gtvWithMargin);
            var itvWithMargin = hrItv.Margin(itvPtvGapMm);
            var ptvPhSegVol = hrPtv.Sub(itvWithMargin);

            // Remove high resolution PTV and ITV
            structureSet.RemoveStructure(hrPtv);
            structureSet.RemoveStructure(hrItv);

            List<SegmentVolume> segmentVolumes = new List<SegmentVolume>();
            segmentVolumes.Add(gtvPhSegVol);
            segmentVolumes.Add(itvPhSegVol);
            segmentVolumes.Add(ptvPhSegVol);
            return segmentVolumes;
        }


        public static void AddCouchWithBodyMargin(StructureSet structureSet, string couchModel, double bodyMargin, bool enableOverride)
        {
            string structureDicomType = "CONTROL";
            string tempBodyId = "tempBODY";

            PatientOrientation orientation = PatientOrientation.NoOrientation;
            RailPosition railPosition = RailPosition.In;
            var HUvalues = CouchDataExtractor.GetCouchHUValues(couchModel);

            IReadOnlyList<Structure> addedStructures;
            bool resized;
            string errorMsg;

            // Save current body to a temporal structure
            Structure realBody = StructureHelpers.GetBodyStructure(structureSet);
            while (structureSet.Structures.Any(x => x.Id == tempBodyId))
            {
                tempBodyId += "Z";
            }
            Structure tempBody = structureSet.AddStructure(structureDicomType, tempBodyId);
            tempBody.SegmentVolume = realBody;

            try
            {
                // Set margin to body, so that couch structures are set lower
                realBody.SegmentVolume = realBody.Margin(bodyMargin);

                // Remove existing couch and create new
                if (!structureSet.CanAddCouchStructures(out errorMsg))
                {
                    if (enableOverride)
                    {
                        structureSet.RemoveCouchStructures(out IReadOnlyList<string> removedStructureIds, out string error);
                    }
                    else
                    {
                        if (structureSet.Structures.Any(x => x.DicomType == "SUPPORT"))
                        {
                            throw new Exception("ERROR: Couch structures already exist!");
                        }
                        throw new Exception("ERROR: No couch structures created!");
                    }
                }
                structureSet.AddCouchStructures(couchModel, orientation, railPosition, railPosition, HUvalues[0], HUvalues[1], HUvalues[2], out addedStructures, out resized, out errorMsg);
                if (errorMsg != null && errorMsg.Length > 0)
                {
                    throw new Exception(errorMsg);
                }
                if (addedStructures == null)
                {
                    throw new Exception("ERROR: No couch structures created!");
                }

                // Put body volume back to normal and remove temporal body
                realBody.SegmentVolume = tempBody;
                structureSet.RemoveStructure(tempBody);
            }
            catch (Exception excp)
            {
                // Put body volume back to normal and remove temporal body
                realBody.SegmentVolume = tempBody;
                structureSet.RemoveStructure(tempBody);
                throw new Exception(excp.Message);
            }
        }

    }
}
