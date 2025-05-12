using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.DataExtraction
{
    class CouchDataExtractor
    {
        public static string[] GetCouchModels(Image image)
        {
            // simplified implementation by Riccardo to avoid generation of a image that cannot be deleted
            string[] modelList = { "Exact_IGRT_Couch_Top_medium" };

            return modelList;
        }

        public static string[] GetCouchModels_Riikka(Image image)
        {
            string modelListPrefix = "Supported couch models are";
            char modelListSep = ',';
            string modelListPostfix = ".";

            string couchModelDummy = "null_dummy_not_a_couch"; // any model name that does not exist
            PatientOrientation orientation = PatientOrientation.NoOrientation;
            RailPosition railPosition = RailPosition.In;

            IReadOnlyList<Structure> addedStructures;
            bool resized;
            string errorMsg;

            StructureSet structureSet = image.CreateNewStructureSet();
            structureSet.AddCouchStructures(couchModelDummy, orientation, railPosition, railPosition, null, null, null, out addedStructures, out resized, out errorMsg);

            string modelListStr = errorMsg.Substring(errorMsg.IndexOf(modelListPrefix) + modelListPrefix.Length);
            modelListStr = modelListStr.Remove(modelListStr.IndexOf(modelListPostfix), modelListPostfix.Length);
            string[] modelList = modelListStr.Split(modelListSep);

            for (int k = 0; k < modelList.Length; k++)
            {
                modelList[k] = modelList[k].Trim();
            }

            structureSet.Delete(); // it deletes the structure set but not the image

            return modelList;
        }

        public static List<Nullable<double>> GetCouchHUValues(string couchModel)
        {
            // Return HU values to surface, interior and rails
            // null means automatic value, wich are -300, -1000 and 200 respectively
            switch (couchModel)
            {
                case "Exact_IGRT_Couch_Top_medium":
                    return new List<Nullable<double>> { -432, null, null };
                case "Exact_Couch_Top_with_Flat_panel":
                    return new List<Nullable<double>> { -900, -700, null };
                default:
                    return new List<Nullable<double>> { null, null, null };
            }
        }

        public static string GetDefaultCouchModel()
        {
            return "Exact_IGRT_Couch_Top_medium";
        }
    }
}
